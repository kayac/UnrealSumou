using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Wrestler : ManualUpdatableMonoBehaviour
{
	[System.Serializable]
	public class Settings
	{
		public float moveSpeed;
		public float moveGoalUpdateSpeed;
		public PidSettings movePid;
		public PidSettings standPid;
		public PidSettings pivotFootPid;
		public PidSettings rotationPid;

		public float damageRecovery;
		public float damageAccumulation;
		public float damageAccumulationMax;
		public float damageFactor;
		public float punchCoolTime;
		public float kickCoolTime;
		public float punchVelocity;
		public float kickVelocity;
		public float squatLimit;
		public float headK;
		public float headD;
		public float turnK;
		public float turnD;
		public float loseY;
		public float defaultControlRotationX;
		public float defaultControlPositionY;
	}

	public class Input
	{
		public void Update(GamePad pad)
		{
			up = pad.Up;
			down = pad.Down;
			left = pad.Left;
			right = pad.Right;
			trigger = pad.LTrigger || pad.RTrigger;
			
			nJust = nJust || pad.JustNorth;
			sJust = sJust || pad.JustSouth;
			eJust = eJust || pad.JustEast;
			wJust = wJust || pad.JustWest;
		}

		public void Reset()
		{
			up = down = left = right = trigger = false;
			ResetJust();
		}

		public void ResetJust()
		{
			nJust = sJust = eJust = wJust = false;
		}

		public bool up;
		public bool down;
		public bool left;
		public bool right;
		public bool trigger;
		// just系
		public bool nJust;
		public bool sJust;
		public bool eJust;
		public bool wJust;
	}

	[SerializeField] Animator animator;
	[SerializeField] Transform rootBone;
	[SerializeField] Rigidbody controlRigidbody;
	[SerializeField] Rigidbody leftHandRigidbody;
	[SerializeField] Rigidbody rightHandRigidbody;
	[SerializeField] Rigidbody leftFootRigidbody;
	[SerializeField] Rigidbody rightFootRigidbody;
	[SerializeField] Rigidbody leftKneeRigidbody;
	[SerializeField] Rigidbody rightKneeRigidbody;
	[SerializeField] Rigidbody headRigidbody;
	[SerializeField] LandDetector leftFootLandDetector;
	[SerializeField] LandDetector rightFootLandDetector;
	

	[SerializeField] Vector3 controlForceDebug;
	[SerializeField] Vector3 controlErrorSum;
	[SerializeField] Vector3 moveGoalDebug;
	[SerializeField] bool landingDebug;
	[SerializeField] float damageDebug;

	public Vector3 MainTransformPosition { get => controlRigidbody.transform.position; }
	public Vector3 MainPosition { get => controlRigidbody.position; }
	public Vector3 HeadPosition { get => headRigidbody.position; }
	public Vector3 RightKneePosition { get => rightKneeRigidbody.position; }
	public Vector3 LeftKneePosition { get => leftKneeRigidbody.position; }
	public bool Lost { get; private set; }
	public bool Is2p { get => is2p; }
	public bool IsAuto { get => (ai != null); }

	public void ManualStart(Settings settings, Battle battle, bool is2p)
	{
		this.is2p = is2p;
		this.battle = battle;
		this.settings = settings;
		this.Lost = false;
		this.pivotFoot = rightFootRigidbody;
		this.input = new Input();

		moveController = new PidController3(settings.movePid);
		pivotFootController = new PidController3(settings.pivotFootPid);
		standController = new PidController1(settings.standPid);
		rotationController = new RotationPidController(controlRigidbody, settings.rotationPid, new Vector3(0f, 1f, 0f));

		rigidbodies = gameObject.GetComponentsInChildren<Rigidbody>();
		foreach (var rigidbody in rigidbodies)
		{
			rigidbody.maxAngularVelocity = Mathf.PI * 2f * 10f;
			rigidbody.isKinematic = true;
		}
		landDetectors = gameObject.GetComponentsInChildren<LandDetector>();
	}

	public void SetAiEnabled(bool enabled)
	{
		if (enabled && (ai == null))
		{
			ai = new WrestlerAi();
		}
		else if (!enabled && (ai != null))
		{
			ai = null;
		}
	}

	public void StartControl()
	{
		controlStarted = true;		
		animator.enabled = false;
		this.moveGoal = controlRigidbody.transform.position;

		foreach (var rigidbody in rigidbodies)
		{
			rigidbody.isKinematic = false;
		}
		var forward = new Vector3(Mathf.Cos(-settings.defaultControlRotationX * Mathf.Deg2Rad), Mathf.Sin(-settings.defaultControlRotationX * Mathf.Deg2Rad), 0f);
		if (is2p)
		{
			forward.x *= -1f;
		}
		var defaultQ = Quaternion.LookRotation(forward);
		controlRigidbody.MoveRotation(defaultQ); // これやると試合開始で暴れるのでやりたくない

		var queue = new Queue<Transform>();
		Flatten(queue, rootBone);		

		foreach (var item in queue)
		{
			item.SetParent(rootBone, worldPositionStays: true);
		}
	}

	public void ManualUpdate(float deltaTime, GamePad pad, Wrestler opponent)
	{
		if (!controlStarted)
		{
			return;
		}

		base.ManualUpdate(deltaTime);

		if (IsAuto)
		{
			ai.Think(input, this, opponent, deltaTime);
		}
		else if (pad != null)
		{
			input.Update(pad);
			if (pad.JustStart)
			{
				SetAiEnabled(!IsAuto);
				if (!IsAuto)
				{
					ai.ToNextType();
				}
			}
		}
		else
		{
			input.Reset();
		}
	}

	public void ManualFixedUpdate(float deltaTime, Wrestler opponent)
	{
		if (!controlStarted)
		{
			return;
		}
		base.ManualUpdate(deltaTime);
		attackCoolTime -= deltaTime;

		temporalDamage *= Mathf.Clamp01(1f - (deltaTime * settings.damageRecovery)); // 雑
		var damage = temporalDamage + accumulatedDamage;
damageDebug = temporalDamage + accumulatedDamage;

		var landing = false;
		foreach (var detector in landDetectors)
		{
			landing = landing | detector.Landing;
		}
//landing = true;
landingDebug = landing;
		// 敗北条件
		if (MainPosition.y < settings.loseY)
		{
			Lost = true;
			controlRigidbody.constraints = 0; // 回転制約なくす
		}

		CalcMoveGoal(deltaTime, opponent);
moveGoalDebug = moveGoal;
		var damageForceFactor = Mathf.Clamp01(1f - damage);
		var f = moveController.Update(controlRigidbody.position, moveGoal, deltaTime);
		f.y = standController.Update(controlRigidbody.position.y, moveGoal.y, deltaTime);

// 敵向いて少し下向いた回転作る
var p1 = opponent.MainPosition;
var p0 = MainPosition;
var forward = (p1 - p0);
forward.y = 0f;
var qx = Quaternion.AngleAxis(settings.defaultControlRotationX, new Vector3(1f, 0f, 0f));
var q = Quaternion.LookRotation(forward, new Vector3(0f, 1f, 0f));
rotationController.Update(q * qx, deltaTime);

		// 軸足を確定
		if (pivotFoot == leftFootRigidbody) // 左が軸足
		{
			if (leftFootLandDetector.Landing) // 左が着地してるなら現状維持
			{
			}
			else if (rightFootLandDetector.Landing) // 右は着地してる
			{
				pivotFoot = rightFootRigidbody;
				pivotFootController.ResetError();
			}
			else //  両方着地してない場合は継続
			{
			}
		}
		else if (pivotFoot == rightFootRigidbody)
		{
			if (rightFootLandDetector.Landing) // 右が着地してるなら現状維持
			{
			}
			else if (leftFootLandDetector.Landing) // 左は着地してる
			{
				pivotFoot = leftFootRigidbody;
				pivotFootController.ResetError();
			}
			else //  両方着地してない場合は継続
			{
			}
		}
		var pivotFootGoal = MainPosition;
		pivotFootGoal.y = 0f;
		var pivotFootF = pivotFootController.Update(pivotFoot.position, pivotFootGoal, deltaTime);


		if (landing && !Lost) // 着地中で負けてない時のみ計算した力を適用できる
		{
			f *= damageForceFactor;
			pivotFootF *= damageForceFactor;
			controlRigidbody.AddForce(f, ForceMode.Force);
			pivotFoot.AddForce(pivotFootF, ForceMode.Force);
		}
controlForceDebug = f;		
controlErrorSum = moveController.ErrorSum();

/* トルクで動かすのキツい。無理そう。
		var toOpponent = opponent.MainPosition - this.MainPosition;
		toOpponent.y = 0f;
		toOpponent = Quaternion.AngleAxis(-15f, new Vector3(1f, 0f, 0f)) * toOpponent;
		var mainTorque = CalcTorque(
			Vector3.zero + toOpponent, 
			Vector3.zero, 
			controlRigidbody.rotation,
			settings.headK); 
		mainTorque -= controlRigidbody.angularVelocity * settings.headD;
		mainTorque *= damageForceFactor;
		controlRigidbody.AddTorque(mainTorque, ForceMode.Force);
*/
		// 首回せ
		var headTorque = CalcTorque(opponent.HeadPosition, this.HeadPosition, headRigidbody.rotation, settings.headK);
		headTorque -= headRigidbody.angularVelocity * settings.headD;
		headTorque *= damageForceFactor;
		headRigidbody.AddTorque(headTorque, ForceMode.Force);

		// 前向け
		var controlTorque = CalcTorque(opponent.MainPosition, this.MainPosition, controlRigidbody.rotation, settings.turnK);
		controlTorque -= controlRigidbody.angularVelocity * settings.turnD;
		controlTorque *= damageForceFactor;
		controlRigidbody.AddTorque(controlTorque, ForceMode.Force);

		if (attackCoolTime <= 0f)
		{
			ProcessAttack(opponent, landing);
		}
		// Just系入力リセット
		input.ResetJust();
	}

	public void OnHit(Vector3 point, float strength)
	{
		// 330で最大とする。Log10(330) == 2.5程度 
		var logStrength = Mathf.Log10(strength);
		var volume = -30f + (logStrength * 15f);
		volume = Mathf.Min(0f, volume);
//Debug.Log("SE " + volume + " " + strength);
		if (volume >= -50f)
		{
			var pitch = 0.25f + (logStrength * 0.3f);
			pitch = Mathf.Max(0.25f, pitch);
//			Kayac.SoundManager.Instance.PlaySe("hit", volume, false, false, pitch);
		}
		battle.OnHit(point, strength, is2p);
	}

	public void OnDamage(Vector3 point, float strength)
	{
		var totalDamage = strength * settings.damageFactor;
		var accumulation = totalDamage * settings.damageAccumulation;
		var maxAccumulation = settings.damageAccumulationMax - accumulatedDamage;
		if (accumulation > maxAccumulation)
		{
			accumulation = maxAccumulation;
		}
		accumulatedDamage += accumulation;
		temporalDamage += totalDamage - accumulation;
	}

	// non public -------
	enum AiType
	{
		Balance,
		PunchOnly,
		KickOnly,
		CrouchingPunchOnly,
		CrouchingKickOnly,
		Count,
	}
	Rigidbody[] rigidbodies;
	Settings settings;
	float attackCoolTime;
	Battle battle;
	bool is2p;
	bool lastHandIsLeft;
	bool lastFootIsLeft;
	LandDetector[] landDetectors;
	PidController3 moveController;
	PidController1 standController;
	PidController3 pivotFootController;
	RotationPidController rotationController;
	Vector3 moveGoal;
	float temporalDamage;
	float accumulatedDamage;
	bool crouching;
	bool controlStarted;
	Rigidbody pivotFoot;

	Input input;
	WrestlerAi ai;

	void CalcMoveGoal(float deltaTime, Wrestler opponent)
	{
		var t = Mathf.Clamp01(deltaTime * settings.moveGoalUpdateSpeed);
		moveGoal += (controlRigidbody.position - moveGoal) * t;
		moveGoal.y = settings.defaultControlPositionY;
		if (crouching) // しゃがみの継続は
		{
			moveGoal.y -= settings.squatLimit;
		}
		else if (attackCoolTime <= 0f) // しゃがみ中、攻撃中の移動を禁ず
		{
			var xAxis = opponent.MainPosition - this.MainPosition;
			if (is2p)
			{
				xAxis = -xAxis;
			}
			xAxis.y = 0f;
			xAxis.Normalize();

			var dx = deltaTime * settings.moveSpeed;

			if (input.left)
			{
				moveGoal -= xAxis * dx;
			}

			if (input.right)
			{
				moveGoal += xAxis * dx;
			}

			var zAxis = Vector3.Cross(xAxis, new Vector3(0f, 1f, 0f));
			zAxis.Normalize();

			if (input.up)
			{
				moveGoal += zAxis * dx;
			}

			if (input.down)
			{
				moveGoal -= zAxis * dx;
			}
		}

		if (attackCoolTime <= 0f) // 攻撃硬直中以外はしゃがみ状態更新
		{
			crouching = input.trigger; // 姿勢変更
		}
	}

	void ProcessAttack(Wrestler opponent, bool onGround)
	{
		if (input.nJust || input.eJust) 
		{
			Rigidbody rb;
			if (lastHandIsLeft)
			{
				rb = rightHandRigidbody;
			}
			else
			{
				rb = leftHandRigidbody;
			}
			var forceDirection = (opponent.HeadPosition - rb.position).normalized;
			var f = forceDirection * settings.punchVelocity;
			rb.AddForce(f, ForceMode.VelocityChange);
/*
			if (!onGround)
			{
				controlRigidbody.AddForce(f * -0.5f, ForceMode.VelocityChange);
				leftFootRigidbody.AddForce(f * -0.25f, ForceMode.VelocityChange);
				rightFootRigidbody.AddForce(f * -0.25f, ForceMode.VelocityChange);
			}
*/
			lastHandIsLeft = !lastHandIsLeft;
			attackCoolTime = settings.punchCoolTime;
		}

		if (input.wJust || input.sJust) 
		{
			Rigidbody rb;
			Rigidbody anotherRb;
			if (lastFootIsLeft)
			{
				rb = rightFootRigidbody;
				anotherRb = leftFootRigidbody;
			}
			else
			{
				rb = leftFootRigidbody;
				anotherRb = rightFootRigidbody;
			}
			pivotFoot = anotherRb;
			pivotFootController.ResetError();
			var forceDirection = (opponent.MainPosition - rb.position).normalized;
			var f = forceDirection * settings.kickVelocity;
			rb.AddForce(f, ForceMode.VelocityChange);
/*
			if (!onGround)
			{
				controlRigidbody.AddForce(f * -0.667f, ForceMode.VelocityChange);
				anotherRb.AddForce(f * -0.333f, ForceMode.VelocityChange);
			}
*/
			lastFootIsLeft = !lastFootIsLeft;
			attackCoolTime = settings.kickCoolTime;
		}
	}

	void Flatten(Queue<Transform> queue, Transform node)
	{
		for (var i = 0; i < node.childCount; i++)
		{
			var child = node.GetChild(i);
			Flatten(queue, child);
			var rigidbody = child.gameObject.GetComponent<Rigidbody>();
			if (rigidbody != null)
			{
				var hitDetector = rigidbody.gameObject.AddComponent<HitDetector>();
				hitDetector.ManualStart(this);
				queue.Enqueue(child);
			}
		}
	}

	static Vector3 CalcTorque(Vector3 target, Vector3 position, Quaternion rotation, float k)
	{
		// q*q0 = q1
		// q = q1 * q0.Inverse
		Vector3 ret;
		var d = target - position;
		if ((d.x == 0f) && (d.y == 0f) && (d.z == 0f))
		{
			ret = Vector3.zero;
		}
		else
		{
			var q1 = Quaternion.LookRotation(d, new Vector3(0f, 1f, 0f));
			var q0 = rotation;
			var q = q1 * Quaternion.Inverse(q0); // これはワールドにおける回転

			var axis = new Vector3(q.x, q.y, q.z);
			var r = Quaternion.FromToRotation(new Vector3(0f, 0f, 1f), axis);
			var cross = Vector3.Cross(q0 * new Vector3(0f, 0f, 1f), d);
			var sign = (Vector3.Dot(cross, axis) > 0f) ? 1f : -1f;

			var theta = Mathf.Acos(Mathf.Clamp(q.w, -1f, 1f));
			ret = r * new Vector3(0f, 0f, k * sign * theta);
		}
		Debug.Assert(float.IsFinite(ret.x) && !float.IsNaN(ret.x));
		return ret;
	}
}
