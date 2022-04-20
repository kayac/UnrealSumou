using UnityEngine;
using System.Collections.Generic;

namespace Kayac
{
	public class CameraController
	{
		public Vector3 PositionGoal{ get; set; }
		public Vector3 TargetGoal{ get; set; }
		public float FieldOfViewGoal { get; set; }
		public float Stiffness { get; set; }
		public float RotationStiffness { get; set; }
		public Vector3 UpVectorGoal { get; set; }

		public CameraController(Camera camera, Transform controlTransform)
		{
			tmpPoints = new List<Vector3>();
			this.controlTransform = controlTransform;
			this.camera = camera;
			position = PositionGoal = camera.transform.position;
			target = TargetGoal = position + camera.transform.forward;
			upVector = UpVectorGoal = new Vector3(0f, 1f, 0f);
			FieldOfViewGoal = camera.fieldOfView;
			RotationStiffness = Stiffness = 1f;
		}

		public void FitByMove(
			Vector3 forward,
			float topMargin,
			float bottomMargin,
			float leftMargin,
			float rightMargin,
			IList<Vector3> points)
		{
			Debug.Assert(forward.sqrMagnitude > 0.001f);
			Debug.Assert((topMargin + bottomMargin) < 1f);

			// 全点をビュー空間に移動する準備
			var toWorld = Matrix4x4.LookAt(Vector3.zero, Vector3.zero + forward, Vector3.up);
			var toView = toWorld.inverse;
			// 各種傾き
			var ay1full = Mathf.Tan(camera.fieldOfView * 0.5f * Mathf.Deg2Rad);
			var ay1 = ay1full;
			var ay0 = -ay1;
			var ax1 = ay1 * camera.aspect;
			var ax0 = -ax1;
			// 最大最小にある点を求める
			var y0Min = float.MaxValue;
			var y1Max = -float.MaxValue;
			var x0Min = float.MaxValue;
			var x1Max = -float.MaxValue;
			var yMinP = Vector3.zero;
			var yMaxP = Vector3.zero;
			var xMinP = Vector3.zero;
			var xMaxP = Vector3.zero;
			for (int i = 0; i < points.Count; i++)
			{
				var p = toView.MultiplyPoint3x4(points[i]);
				var by0 = p.y - (ay0 * p.z);
				var by1 = p.y - (ay1 * p.z);
				var bx0 = p.x - (ax0 * p.z);
				var bx1 = p.x - (ax1 * p.z);
				if (by0 < y0Min)
				{
					yMinP = p;
					y0Min = by0;
				}
				if (by1 > y1Max)
				{
					yMaxP = p;
					y1Max = by1;
				}
				if (bx0 < x0Min)
				{
					xMinP = p;
					x0Min = bx0;
				}
				if (bx1 > x1Max)
				{
					xMaxP = p;
					x1Max = bx1;
				}
			}

			// 連立方程式を立てる
			/*
			C.y - (1-2Mt)αC.z = Pmax.y-(1-2Mt)α*Pmax.z
			C.y - (2Mb-1)αC.z = Pmin.y-(2Mb-1)α*Pmin.z

			(1-2Mt)α = c
			(2Mb-1)α = d
			と置いて、

			C.y - c*C.z = Pmax.y - c*Pmax.z
			C.y - d*C.z = Pmin.y - d*Pmin.z

			右辺をe,fとすると、

			|1 -c||C.y| = e
			|1 -d||C.z| = f

			両辺引いてCyを消去すると、(-c+d)*Cz = e - f → Cz = (e-f)/(d-c)
			両辺d,cを乗じて引くと、
			(d-c)C.y = de-cf → Cy = (de-cf)/(d-c)

			xに関しても同様。α=tan(θ/2)なのでay1とax1
			*/
			var cy = (1f - (2f * topMargin)) * ay1;
			var dy = ((2f * bottomMargin) - 1f) * ay1;
			var ey = yMaxP.y - (cy * yMaxP.z);
			var fy = yMinP.y - (dy * yMinP.z);
			var z_y = (ey - fy) / (dy - cy);
			var y = ((dy * ey) - (cy * fy)) / (dy - cy);

			// xについても同様に計算
			var cx = (1f - (2f * rightMargin)) * ax1;
			var dx = ((2f * leftMargin) - 1f) * ax1;
			var ex = xMaxP.x - (cx * xMaxP.z);
			var fx = xMinP.x - (dx * xMinP.z);
			var z_x = (ex - fx) / (dx - cx);
			var x = ((dx * ex) - (cx * fx)) / (dx - cx);
			// より手前の方を選択。x,yはそのまま使う。
			var posInView = new Vector3(x, y, Mathf.Min(z_y, z_x));
			// ワールド座標に戻す
			PositionGoal = toWorld.MultiplyPoint3x4(posInView);
			TargetGoal = PositionGoal + forward;
//Debug.LogError("FitByMove: " + forward  + " " + points.Count + " " + PositionGoal + " " + TargetGoal + " " + z_y +  " " + z_x);
		}

		public void Converge()
		{
			target = TargetGoal;
			position = PositionGoal;
			upVector = UpVectorGoal;
			camera.fieldOfView = FieldOfViewGoal;
			controlTransform.position = position;
			controlTransform.LookAt(target);
//Debug.LogError("Converge: " + position + " -> " + PositionGoal);
		}

		public void ManualUpdate(float deltaTime)
		{
			UpdateInterpolation(deltaTime);
		}

		// non public --------
		Transform controlTransform;
		Camera camera;
		Vector3 position;
		Vector3 target; // ロールしないのでzはいらない
		Vector3 upVector;
		List<Vector3> tmpPoints;

		void UpdateInterpolation(float deltaTime)
		{
			// 現forward
			var forward0 = target - position;
			// 更新
			position += (PositionGoal - position) * deltaTime * Stiffness;

			// forwardGoal
			var forwardGoal = TargetGoal - PositionGoal;
			var d0 = forward0.magnitude;
			var dGoal = forwardGoal.magnitude;
			forward0 /= d0;
			forwardGoal /= dGoal;
			// forward0をforwardGoalに向かって指数補間する
			var q = Quaternion.FromToRotation(forward0, forwardGoal);
			q = Quaternion.Slerp(Quaternion.identity, q, deltaTime * RotationStiffness);
			var forward = q * forward0;

//			var forward = forward0 + ((forwardGoal - forward0) * deltaTime * Stiffness);
			// 距離を指数補間する
			var d = d0 + ((dGoal - d0) * deltaTime * Stiffness);
			forward.Normalize();
			forward *= d;

			target = position + forward;

			// 上ベクタ更新
			upVector += (UpVectorGoal - upVector) * deltaTime * Stiffness;

			controlTransform.position = position;
			controlTransform.LookAt(target, upVector);

			// fov
			camera.fieldOfView += (FieldOfViewGoal - camera.fieldOfView) * deltaTime * Stiffness;
		}
	}
}
