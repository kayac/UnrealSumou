using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Battle : ManualUpdatableMonoBehaviour
{
	public enum ResultType
	{
		NotYet,
		Win1P,
		Win2P,
		Draw,
	}
	[SerializeField] Wrestler[] wrestlers;
	[SerializeField] BattleUi ui;

	public bool Settled { get; private set; }
	public ResultType Result { get; private set; }

	public void ManualStart(
		Camera camera, 
		Wrestler.Settings wrestlerSettings,
		Lighting lighting)
	{
		this.Settled = false;
		this.lighting = lighting;
		this.Result = ResultType.NotYet;
		
		base.ManualStart();
		wrestlers[0].ManualStart(wrestlerSettings, this, is2p: false);
		wrestlers[1].ManualStart(wrestlerSettings, this, is2p: true);

		ui.ManualStart(camera);
		EndControl();
	}

	public void StartControl()
	{
		controlStarted = true;
		foreach (var wrestler in wrestlers)
		{
			wrestler.StartControl();
		}
	}

	public void EndControl()
	{
		controlStarted = false;
	}

	public void ManualUpdate(float deltaTime, InputHandler input, bool aiEnabled)
	{
		base.ManualUpdate(deltaTime);

		wrestlers[0].ManualUpdate(deltaTime, controlStarted ? input.GetGamePad(0) : null, wrestlers[1]);
		wrestlers[1].ManualUpdate(deltaTime, controlStarted ? input.GetGamePad(1) : null, wrestlers[0]);
		wrestlers[1].SetAiEnabled(aiEnabled);
		ui.ManualUpdate(deltaTime);
	}

	public void ManualFixedUpdate(float deltaTime)
	{
		var p0 = wrestlers[0].MainPosition;
		var p1 = wrestlers[1].MainPosition;
		var forward = p1 - p0;
		forward.Normalize();

		wrestlers[0].ManualFixedUpdate(deltaTime, wrestlers[1]);
		wrestlers[1].ManualFixedUpdate(deltaTime, wrestlers[0]);

		// 敗北条件
		if (wrestlers[0].Lost || wrestlers[1].Lost)
		{
			Settled = true;
			if (Result == ResultType.NotYet)
			{
				if (wrestlers[0].Lost && wrestlers[1].Lost)
				{
					Result = ResultType.Draw;
				}
				else if (wrestlers[0].Lost)
				{
					Result = ResultType.Win2P;
				}
				else if (wrestlers[1].Lost)
				{
					Result = ResultType.Win1P;
				}
			}
		}
	}

	public void OnHit(Vector3 position, float strength, bool is2p)
	{
		lighting.FlashPointLight(position, strength, is2p);
	}

	public Wrestler GetWrestler(int index)
	{
		return wrestlers[index];
	}

	// non public ---------
	Lighting lighting;
	bool controlStarted;
}
