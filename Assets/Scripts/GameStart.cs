using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameStart : ManualUpdatableMonoBehaviour
{
	[SerializeField] UnityEngine.Playables.PlayableDirector director;
	public IEnumerator CoWait(InputHandler inputHandler)
	{
		Kayac.SoundManager.Instance.StopBgm();
		prevDirectorTime = 0.0;
		while (true) 
		{
			// Aボタンでスキップ
			if (inputHandler.GetGamePad(0).JustEast || inputHandler.GetGamePad(1).JustEast)
			{
				director.time = director.duration - 0.001f;
				director.Evaluate();
				break;
			}

			if (director.time < prevDirectorTime)
			{
				break;
			}
			prevDirectorTime = director.time;
			yield return null;
		}
	}

	// non public ------
	double prevDirectorTime;
}
