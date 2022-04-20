using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MainUi : ManualUpdatableMonoBehaviour
{
	[SerializeField] Canvas canvas;
	[SerializeField] Button helpButton;
	[SerializeField] GameObject helpBoardObject;
	[SerializeField] Button aiButton;
	[SerializeField] Image aiButtonBack;
	[SerializeField] Text aiButtonText;

	public bool AiEnabled { get; private set; }

	public void ManualStart(Camera camera)
	{
		AiEnabled = false;
		base.ManualStart();
		canvas.renderMode = RenderMode.ScreenSpaceCamera;
		canvas.worldCamera = camera;
		helpButton.onClick.AddListener(OnClickHelpButton);
		aiButton.onClick.AddListener(OnClickAiButton);
	}

	public override void ManualUpdate(float deltaTime)
	{
		
	}

	// non public ---
	void OnClickHelpButton()
	{
		helpBoardObject.SetActive(!helpBoardObject.activeSelf);
	}

	void OnClickAiButton()
	{
		AiEnabled = !AiEnabled;
		var color = AiEnabled ? new Color(1f, 0.75f, 0.75f, 1f) : new Color(1f, 1f, 1f, 1f);
		aiButtonBack.color = color;
		aiButtonText.color = color;
	}
}
