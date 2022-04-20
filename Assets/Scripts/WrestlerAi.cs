using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WrestlerAi
{
	public WrestlerAi()
	{
		time = 0f;
		type = Type.Balance;
	}

	public void ToNextType()
	{
		type = (Type)(((int)type + 1) % (int)Type.Count);
	}

	public void Think(Wrestler.Input input, Wrestler me, Wrestler he, float deltaTime)
	{
		// TODO: 真面目に作るならvirtualになる
		if (type == Type.Balance)
		{
			ThinkBalance(input, me, he, deltaTime);
		}
		else if (type == Type.PunchOnly)
		{
			ThinkPunchOnly(input, me, he, deltaTime);
		}
		else if (type == Type.KickOnly)
		{
			ThinkKickOnly(input, me, he, deltaTime);
		}
		else if (type == Type.CrouchingPunchOnly)
		{
			ThinkCrouchingPunchOnly(input, me, he, deltaTime);
		}
		else if (type == Type.CrouchingKickOnly)
		{
			ThinkCrouchingKickOnly(input, me, he, deltaTime);
		}
		time += deltaTime;
	}

	public enum Type
	{
		Balance,
		PunchOnly,
		KickOnly,
		CrouchingPunchOnly,
		CrouchingKickOnly,
		Count,
	}
	Type type;
	float time;
	float prevAttackTime;
	bool crouching;

	void ThinkBalance(Wrestler.Input input, Wrestler me, Wrestler he, float deltaTime)
	{
		var toEnemy = (he.MainPosition - me.MainPosition);
		var toEnemyXz = toEnemy;
		toEnemyXz.y = 0f;
		var xzDistance = toEnemyXz.magnitude;

		// 近接していなければ移動する

		// 近接していればパンチとキックをランダムに繰り出す。その間隔は0.5秒間隔
		if (xzDistance >= 2.5f) // 離れてればパンチで近づく。本当は移動させたい
		{
			input.nJust = true; // パンチ
		}
		else if (xzDistance <= 1.5f)
		{
			if (Random.value < (deltaTime / 0.85f))
//			time > (prevAttackTime + 0.75f))
			{
				prevAttackTime = time;
				if (Random.value < 0.5f)
				{
					input.nJust = true; // パンチ
				}
				else
				{
					input.sJust = true; // キック
				}
			}
		}

		// たまにしゃがむ
		if (crouching) // すでにしゃがんでいれば、0.5秒くらいのうちに立つ感じに立つ
		{
			if (Random.value < (deltaTime / 0.5f))
			{
				crouching = false;
			}
		}
		else // まだしゃがんでいなければ、3秒に一回しゃがむ感じにしゃがむ
		{
			if (Random.value < (deltaTime / 3f))
			{
				crouching = true;
			}
		}
		input.trigger = crouching;
	}

	void ThinkPunchOnly(Wrestler.Input input, Wrestler me, Wrestler he, float deltaTime)
	{
		input.nJust = !input.nJust;
	}

	void ThinkKickOnly(Wrestler.Input input, Wrestler me, Wrestler he, float deltaTime)
	{
		input.sJust = !input.sJust;
	}

	void ThinkCrouchingPunchOnly(Wrestler.Input input, Wrestler me, Wrestler he, float deltaTime)
	{
		input.trigger = true;
		input.nJust = !input.nJust;
	}

	void ThinkCrouchingKickOnly(Wrestler.Input input, Wrestler me, Wrestler he, float deltaTime)
	{
		input.trigger = true;
		input.sJust = !input.sJust;
	}
}
