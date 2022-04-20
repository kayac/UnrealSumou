using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InputHandler
{
	public bool Home { get; private set; }
	public bool JustHome { get; private set; }


	public InputHandler()
	{
		gamePads = new GamePad[2];
		for (var i = 0; i < gamePads.Length; i++)
		{
			gamePads[i] = new GamePad();
		}
	}

	public GamePad GetGamePad(int index)
	{
		Debug.Assert((index >= 0) && (index < gamePads.Length));
		return gamePads[index];
	}

	public void ManualUpdate()
	{
		var up0 = false;
		var down0 = false;
		var left0 = false;
		var right0 = false;
		var n0 = false;
		var s0 = false;
		var e0 = false;
		var w0 = false;
		var l0 = false;
		var r0 = false;
		var start0 = false;

		var up1 = false;
		var down1 = false;
		var left1 = false;
		var right1 = false;
		var n1 = false;
		var s1 = false;
		var e1 = false;
		var w1 = false;
		var l1 = false;
		var r1 = false;
		var start1 = false;

		var kb = UnityEngine.InputSystem.Keyboard.current;
		if (kb != null)
		{
			up0 = kb.wKey.isPressed;
			down0 = kb.zKey.isPressed;
			left0 = kb.aKey.isPressed;
			right0 = kb.sKey.isPressed;
			n0 = kb.digit4Key.isPressed;
			s0 = kb.dKey.isPressed;
			e0 = kb.rKey.isPressed;
			w0 = kb.eKey.isPressed;
			l0 = kb.qKey.isPressed;
			start0 = kb.digit1Key.isPressed;

			up1 = kb.iKey.isPressed;
			down1 = kb.mKey.isPressed;
			left1 = kb.jKey.isPressed;
			right1 = kb.kKey.isPressed;
			n1 = kb.digit0Key.isPressed;
			s1 = kb.lKey.isPressed;
			e1 = kb.pKey.isPressed;
			w1 = kb.oKey.isPressed;
			l1 = kb.uKey.isPressed;
			start1 = kb.digit7Key.isPressed;

			JustHome = !Home && kb.escapeKey.isPressed;
			Home = kb.escapeKey.isPressed;

		}

		var pads = UnityEngine.InputSystem.Gamepad.all;
		if (pads.Count > 0)
		{
			var pad = pads[0];
			up0 = up0 || pad.dpad.up.isPressed;
			down0 = down0 || pad.dpad.down.isPressed;
			left0 = left0 || pad.dpad.left.isPressed;
			right0 = right0 || pad.dpad.right.isPressed;

			up0 = up0 || pad.leftStick.up.isPressed;
			down0 = down0 || pad.leftStick.down.isPressed;
			left0 = left0 || pad.leftStick.left.isPressed;
			right0 = right0 || pad.leftStick.right.isPressed;

			n0 = n0 || pad.buttonNorth.isPressed;
			s0 = s0 || pad.buttonSouth.isPressed;
			e0 = e0 || pad.buttonEast.isPressed;
			w0 = w0 || pad.buttonWest.isPressed;
			l0 = l0 || pad.leftShoulder.isPressed || pad.leftTrigger.isPressed;
			r0 = r0 || pad.rightShoulder.isPressed || pad.rightTrigger.isPressed;
			start0 = start0 || pad.startButton.isPressed;
		}

		if (pads.Count > 1)
		{
			var pad = pads[1];
			up1 = up1 || pad.dpad.up.isPressed;
			down1 = down1 || pad.dpad.down.isPressed;
			left1 = left1 || pad.dpad.left.isPressed;
			right1 = right1 || pad.dpad.right.isPressed;

			up1 = up1 || pad.leftStick.up.isPressed;
			down1 = down1 || pad.leftStick.down.isPressed;
			left1 = left1 || pad.leftStick.left.isPressed;
			right1 = right1 || pad.leftStick.right.isPressed;

			n1 = n1 || pad.buttonNorth.isPressed;
			s1 = s1 || pad.buttonSouth.isPressed;
			e1 = e1 || pad.buttonEast.isPressed;
			w1 = w1 || pad.buttonWest.isPressed;
			l1 = l1 || pad.leftShoulder.isPressed || pad.leftTrigger.isPressed;
			r1 = r1 || pad.rightShoulder.isPressed || pad.rightTrigger.isPressed;
			start1 = start1 || pad.startButton.isPressed;
		}

		gamePads[0].Update(up0, down0, left0, right0, n0, s0, e0, w0, l0, r0, start0);
		gamePads[1].Update(up1, down1, left1, right1, n1, s1, e1, w1, l1, r1, start1);
	}

	// non public ----
	GamePad[] gamePads;
}
