using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class LandDetector : ManualUpdatableMonoBehaviour
{
	public bool Landing{ get; private set; }
[SerializeField] bool landingDebug;

	void OnCollisionEnter(Collision collision)
	{
		// 土俵?
		var ringCollider = collision.collider.gameObject.GetComponent<RingCollider>();
		if (ringCollider != null)
		{
			if (collision.contacts[0].point.y > -0.05f)
			{
				Landing = true;
			}
landingDebug = Landing;
		}
	}

	void OnCollisionExit(Collision collision)
	{
		// 土俵?
		var ringCollider = collision.collider.gameObject.GetComponent<RingCollider>();
		if (ringCollider != null)
		{
			Landing = false;
landingDebug = Landing;
		}
	}
#if UNITY_EDITOR
	[CustomEditor(typeof(LandDetector))]
	class Inspector : Editor
	{
 		public override void OnInspectorGUI()
	    {
    	    base.OnInspectorGUI();
			var self = target as LandDetector;
			EditorGUILayout.LabelField("landing", self.Landing.ToString());
	    }
	}
#endif
}
