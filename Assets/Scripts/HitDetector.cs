using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HitDetector : MonoBehaviour
{
	public void ManualStart(Wrestler owner)
	{
		this.owner = owner;
	}

	public void OnCollisionEnter(Collision collision)
	{
		var another = collision.gameObject.GetComponent<HitDetector>();
		if (another != null)
		{
			if (another.owner != owner) // 敵に衝突し
			{
				var rb0 = gameObject.GetComponent<Rigidbody>();
				var rb1 = collision.rigidbody;
				var p = collision.contacts[0].point;
				var v0 = rb0.GetPointVelocity(p);
				var v1 = rb1.GetPointVelocity(p);
				if ((rb0 != null) && (rb1 != null) && (v0.sqrMagnitude > v1.sqrMagnitude)) // 速度が高い方が攻撃者
				{
					var rv = (v1 - v0);
					//collision.relativeVelocity;
					var energy = rv.sqrMagnitude * rb0.mass;
					owner.OnHit(p, energy);
//Debug.Log("C " + rb0.gameObject.name + " -> " + rb1.gameObject.name + " " + Time.frameCount + " E:" + energy + " " + v0.magnitude + " " + v1.magnitude + " " + rv.magnitude);	
					another.owner.OnDamage(p, energy);
				}
			}
		}
	}

	// non public ----
	Wrestler owner;
}
