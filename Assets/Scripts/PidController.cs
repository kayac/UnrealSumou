using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PidSettings
{
	public float kp;
	public float ki;
	public float kd;
}

class PidController1
{
	public PidController1(PidSettings settings)
	{
		this.settings = settings;
	}

	public float Update(float x, float r, float deltaTime)
	{
		var f = 0f;
		// p
		var e = r - x;
		f += settings.kp * e;
		// i
		f += settings.ki * eSum;
		eSum += (e + prevE) * 0.5f * deltaTime;
		// d
		if (deltaTime > 0f)
		{
			f += settings.kd * (e - prevE) / deltaTime;
		}

		prevE = e;
		return f;
	}

	public void ResetError()
	{
		prevE = eSum = 0f;
	}

	float prevE; // 前回誤差
	float eSum; // 誤差積分値
	PidSettings settings;
}

class PidController3
{
	public PidController3(PidSettings settings)
	{
		this.settings = settings;
	}

	public Vector3 Update(Vector3 x, Vector3 r, float deltaTime)
	{
		var f = Vector3.zero;
		// p
		var e = r - x;
		f += settings.kp * e;
		// i
		f += settings.ki * eSum;
		eSum += (e + prevE) * 0.5f * deltaTime;
		// d
		if (deltaTime > 0f)
		{
			f += settings.kd * (e - prevE) / deltaTime;
		}
		prevE = e;
		return f;
	}

	public void ResetError()
	{
		prevE = eSum = Vector3.zero;
	}

	public Vector3 ErrorSum()
	{
		return eSum;
	}
	
	Vector3 prevE; // 前回誤差
	Vector3 eSum; // 誤差積分値
	PidSettings settings;
}

public class RotationPidController
{
	public RotationPidController(
		Rigidbody rigidbody,
		PidSettings settings,
		Vector3 localMainAxis)
	{
		this.rigidbody = rigidbody;
		this.settings = settings;
		this.localMainAxis = localMainAxis;
	}

	public void Update(Quaternion goal, float deltaTime)
	{
		// 現在の端点を算出する
		var x = rigidbody.position + (rigidbody.rotation * localMainAxis);
		var xc = rigidbody.position - (rigidbody.rotation * localMainAxis); // 逆側
		// 目標点を算出する
		var r = rigidbody.position + (goal * localMainAxis);

		var f = Vector3.zero;
		// p
		var e = r - x;
		f += settings.kp * e;
		// i
		f += settings.ki * eSum;
		eSum += (e + prevE) * 0.5f * deltaTime;
		// d
		if (deltaTime > 0f)
		{
			f += settings.kd * (e - prevE) / deltaTime;
		}
		prevE = e;

		rigidbody.AddForceAtPosition(f, x, ForceMode.Force);
		rigidbody.AddForceAtPosition(-f, xc, ForceMode.Force);
//Debug.Log(eSum + " " + f + " " + x + " -> " + r);
	}

	// non public -------
	PidSettings settings;
	Rigidbody rigidbody;
	Vector3 localMainAxis;
	Vector3 prevE;
	Vector3 eSum;
}
