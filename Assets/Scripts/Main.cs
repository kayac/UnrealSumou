using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class Main : MonoBehaviour
{
	[SerializeField] Wrestler.Settings wrestlerSettings; 
	[SerializeField] PidSettings dynamicResolutionPidSettings;
	[SerializeField] Battle battlePrefab;
	[SerializeField] GameStart gameStartPrefab;
	[SerializeField] Lighting lighting;
	[SerializeField] MainUi ui;
	[SerializeField] Camera mainCamera;
	[SerializeField] GameObject nokotta;

	[SerializeField] Game game;
	[SerializeField] GameEnd gameEnd;

	// MonoBehaviour Events
	void Start()
	{
		Application.targetFrameRate = 60;
		dynamicResolutionLerpFactor = 1f;

		inputHandler = new InputHandler();
		ui.ManualStart(mainCamera);
		lighting.ManualStart();

		// 一旦消しとく
		game.gameObject.SetActive(false);
		gameEnd.gameObject.SetActive(false);

		dynamicResolutionController = new PidController1(dynamicResolutionPidSettings);
		DynamicResolutionHandler.SetDynamicResScaler(UpdateDynamicResolution, DynamicResScalePolicyType.ReturnsMinMaxLerpFactor);
		
		StartCoroutine(CoGameLoop());
	}

	void Update()
	{
		var dt = Time.deltaTime;
		inputHandler.ManualUpdate();
		ui.ManualUpdate(dt);
		if (battle != null)
		{
			battle.ManualUpdate(dt, inputHandler, ui.AiEnabled);
		}
		lighting.ManualUpdate(dt);

		if (game.gameObject.activeInHierarchy)
		{
			game.ManualUpdate(dt);
		}
	}

	void FixedUpdate()
	{
		var dt = Time.fixedDeltaTime;
		if (battle != null)
		{
			battle.ManualFixedUpdate(dt);
		}
	}

	// non public -----------
	Battle battle;
	InputHandler inputHandler;
	GameStart gameStart;
	float dynamicResolutionLerpFactor;
	PidController1 dynamicResolutionController;

	float UpdateDynamicResolution()
	{
		float f = dynamicResolutionController.Update(Time.deltaTime, 1f / 40f, Time.deltaTime);
		dynamicResolutionLerpFactor += f * Time.deltaTime;
		var hwVmin = Mathf.Min(Screen.width, Screen.height);
		var minScale = 540f / hwVmin; // 最低Vmin

		dynamicResolutionLerpFactor = Mathf.Clamp(dynamicResolutionLerpFactor, minScale, 1f);

//Debug.LogError("\t" + f.ToString("F2") + "\t " + dynamicResolutionLerpFactor.ToString("F2") + " " + minScale.ToString("F2"));
		return dynamicResolutionLerpFactor;
	}

	IEnumerator CoGameLoop()
	{
		while (true)
		{
			yield return CoBattle();
		}
	}

	IEnumerator CoBattleStart()
	{
		mainCamera.enabled = false; // Startの中に入ってるから切る
		Debug.Assert(gameStart == null);
		// gameStartが置いてあれば、それを使う
		gameStart = gameObject.GetComponentInChildren<GameStart>(includeInactive: false);
		if (gameStart == null) // なければ作る
		{
			gameStart = Instantiate(gameStartPrefab, transform, false);
		}

		gameStart.gameObject.SetActive(true);
		yield return gameStart.CoWait(inputHandler);
		gameStart.gameObject.SetActive(false);

		Destroy(gameStart.gameObject);
		gameStart = null;
		mainCamera.enabled = true;
	}

	IEnumerator CoBattle()
	{
		yield return CoBattleStart();

		Debug.Assert(battle == null);

		// battleを生成
		battle = Instantiate(battlePrefab, transform, false);
		battle.ManualStart(mainCamera, wrestlerSettings, lighting);

		game.gameObject.SetActive(true);
		game.ManualStart(mainCamera, battle);
		nokotta.SetActive(true);
		battle.StartControl();
		yield return game.CoWait(inputHandler);
		game.gameObject.SetActive(false);

		if (!game.Aborted)
		{
			gameEnd.gameObject.SetActive(true);
			yield return gameEnd.CoWait(battle.Result, inputHandler);
			gameEnd.gameObject.SetActive(false);
		}

		Destroy(battle.gameObject);
		battle = null;
	}
}
