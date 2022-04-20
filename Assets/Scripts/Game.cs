using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// ゲーム対戦中制御クラス。決着がついて操作受け付けを完了したらこれの管轄ではなくなる
public class Game : ManualUpdatableMonoBehaviour
{
	[SerializeField] float depressionAngle;
	[SerializeField] float cameraStiffness;

	public bool Aborted { get; private set; }

	public void ManualStart(Camera mainCamera, Battle battle)
	{
		Aborted = false;
		cameraController = new Kayac.CameraController(mainCamera, mainCamera.transform);
		cameraController.Stiffness = 4f;
		this.battle = battle;
		UpdateCamera(0f);
		cameraController.Converge();

//		Kayac.SoundManager.Instance.PlayBgm("audiostock_158820", volume: -12f);
//		Kayac.SoundManager.Instance.PlaySe("nokotta", volume: -12f);
	}

	public override void ManualUpdate(float deltaTime)
	{
		UpdateCamera(deltaTime);
	}

	public IEnumerator CoWait(InputHandler inputHandler)
	{
		while (!battle.Settled)
		{
			if (inputHandler.JustHome)
			{
				Aborted = true;
				break;
			}
			yield return null;
		}
	}

	// non public ------
	Kayac.CameraController cameraController;
	Battle battle;

	void UpdateCamera(float deltaTime)
	{
		var p0 = battle.GetWrestler(0).MainTransformPosition;
		var p1 = battle.GetWrestler(1).MainTransformPosition;
		var right = p1 - p0;
		right.Normalize();
		var p0back = p0 - (right * 1f);
		var p1back = p1 + (right * 1f);
		var forward = new Vector3(-right.z, 0f, right.x); // 90度回転
		var a = Mathf.Deg2Rad * -depressionAngle;
		forward.x *= Mathf.Cos(a);
		forward.z *= Mathf.Cos(a);
		forward.y = Mathf.Sin(a);
		var points = new List<Vector3>();
		points.Add(p0back);
		points.Add(p1back);
		points.Add(p0back + new Vector3(0f, 1.2f, 0f));
		points.Add(p1back + new Vector3(0f, 1.2f, 0f));
		points.Add(p0back - new Vector3(0f, 1.2f, 0f));
		points.Add(p1back - new Vector3(0f, 1.2f, 0f));

		cameraController.Stiffness = cameraStiffness;
		cameraController.RotationStiffness = cameraStiffness;
		cameraController.FitByMove(forward, 0f, 0f, 0f, 0f, points);
		cameraController.ManualUpdate(deltaTime);
	}
}
