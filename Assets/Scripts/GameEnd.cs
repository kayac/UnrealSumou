using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameEnd : ManualUpdatableMonoBehaviour
{
	[SerializeField] TMPro.TextMeshProUGUI text;
	[SerializeField] Image win1p;
	[SerializeField] Image win2p;

	public IEnumerator CoWait(Battle.ResultType result, InputHandler inputHandler)
	{
		win1p.enabled = win2p.enabled = false;
		this.text.enabled = false;
		if (result == Battle.ResultType.Draw)
		{
			this.text.text = "引き分け\nDRAW";
			this.text.enabled = true;
		}
		else if (result == Battle.ResultType.Win1P)
		{
			win1p.enabled = true;
		}
		else if (result == Battle.ResultType.Win2P)
		{
			win2p.enabled = true;
		}
		else
		{
			Debug.LogError("馬鹿な");	
		}
	
		Kayac.SoundManager.Instance.StopBgm(0.3f);
//		Kayac.SoundManager.Instance.PlaySe("don", volume: 1);

		// TODO: Timelineでなんか作るならGameStart同様にDirectorから終了を取る
		var time = 0f;
		var endTime = 3f;
		var inputWait = 2f;
		while (true) 
		{
			// Aボタンでスキップ
			if ((time > inputWait) && (inputHandler.GetGamePad(0).JustEast || inputHandler.GetGamePad(1).JustEast))
			{
				break;
			}

			time += Time.deltaTime;
			if (time >= endTime)
			{
				break;
			}
			yield return null;
		}
	}
}
