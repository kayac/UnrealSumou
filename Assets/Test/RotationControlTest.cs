using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotationControlTest : MonoBehaviour
{
	[SerializeField] float stiff;
	[SerializeField] float damp;
	[SerializeField] Transform target;
	[SerializeField] new Rigidbody rigidbody;

	static Vector3 CalcTorque(Vector3 target, Vector3 position, Quaternion rotation)
	{
		// q*q0 = q1
		// q = q1 * q0.Inverse
		var d = target - position;
		var q1 = Quaternion.LookRotation(d, new Vector3(0f, 1f, 0f));
		var q0 = rotation;
		var q = q1 * Quaternion.Inverse(q0); // これはワールドにおける回転

		var axis = new Vector3(q.x, q.y, q.z);
		var r = Quaternion.FromToRotation(new Vector3(0f, 0f, 1f), axis);
		var cross = Vector3.Cross(q0 * new Vector3(0f, 0f, 1f), d);
		var sign = (Vector3.Dot(cross, axis) > 0f) ? 1f : -1f;

		var t = r * new Vector3(0f, 0f, sign * Mathf.Acos(q.w));
		return t;
	}

	void Update()
	{
		var t = CalcTorque(target.position, transform.position, transform.rotation);
		t -= rigidbody.angularVelocity * damp;

		rigidbody.AddTorque(t, ForceMode.Force);
	}
}
