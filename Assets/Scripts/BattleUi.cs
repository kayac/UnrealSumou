using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BattleUi : ManualUpdatableMonoBehaviour
{
	[SerializeField] Canvas canvas;

	public void ManualStart(Camera camera)
	{
		base.ManualStart();
		canvas.renderMode = RenderMode.ScreenSpaceCamera;
		canvas.worldCamera = camera;
	}

	public override void ManualUpdate(float deltaTime)
	{
	}
}
