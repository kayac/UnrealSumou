using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ManualUpdatableMonoBehaviour : MonoBehaviour
{
	public Vector3 Position { get => transform.position; }
	public Quaternion Rotation { get => transform.rotation; } 

	public virtual void ManualStart()
	{
	}

	public virtual void ManualUpdate(float deltaTime)
	{		
	}
}
