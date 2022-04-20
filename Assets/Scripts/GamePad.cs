using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GamePad
{
	public bool Up { get; private set; }
	public bool Down { get; private set; }
	public bool Left { get; private set; }
	public bool Right { get; private set; }
	public bool North { get; private set; }
	public bool South { get; private set; }
	public bool East { get; private set; }
	public bool West { get; private set; }
	public bool LTrigger { get; private set; }
	public bool RTrigger { get; private set; }
	public bool Start { get; private set; }

	public bool JustUp { get; private set; }
	public bool JustDown { get; private set; }
	public bool JustLeft { get; private set; }
	public bool JustRight { get; private set; }
	public bool JustNorth { get; private set; }
	public bool JustSouth { get; private set; }
	public bool JustEast { get; private set; }
	public bool JustWest { get; private set; }
	public bool JustLTrigger { get; private set; }
	public bool JustRTrigger { get; private set; }
	public bool JustStart { get; private set; }

	public GamePad()
	{
	}

	public void Update(
		bool up,
		bool down,
		bool left,
		bool right,
		bool north,
		bool south,
		bool east,
		bool west,
		bool lTrigger,
		bool rTrigger,
		bool start)
	{
		this.JustUp = !Up && up;
		this.JustDown = !Down && down;
		this.JustLeft = !Left && left;
		this.JustRight = !Right && right;

		this.JustNorth = !North && north;
		this.JustSouth = !South && south;
		this.JustEast = !East && east;
		this.JustWest = !West && west;

		this.JustLTrigger = !LTrigger && lTrigger;
		this.JustRTrigger = !RTrigger && rTrigger;

		this.JustStart = !Start && start;

		this.Up = up;
		this.Down = down;
		this.Left = left;
		this.Right = right;

		this.North = north;
		this.South = south;
		this.East = east;
		this.West = west;

		this.LTrigger = lTrigger;
		this.RTrigger = rTrigger;

		this.Start = start;
	}
	// non public -------
}
