﻿using System;
using System.Collections.Generic;
using UnityEngine;

namespace BasicOrbit.Modules.OrbitModules
{
	public class LongAscending : BasicModule
	{
		public LongAscending(string t)
			: base(t)
		{

		}

		protected override void UpdateSettings()
		{
			BasicSettings.Instance.showLAN = IsVisible;
			BasicSettings.Instance.showLANAlways = AlwaysShow;
		}

		protected override string fieldUpdate()
		{
			if (FlightGlobals.ActiveVessel == null)
				return "---";

			if (FlightGlobals.ActiveVessel.orbit == null)
				return "---";

			return result(FlightGlobals.ActiveVessel.orbit.LAN);
		}

		private string result(double d)
		{
			return string.Format("{0:N2}°", d);
		}
	}
}