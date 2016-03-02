//
//  LightSystem.cs
//
//  Author:
//       Shane Synan <digitalcircuit36939@gmail.com>
//
//  Copyright (c) 2015 - 2016
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.
using System;

namespace Actinic
{

	public static class LightSystem
	{

#region Lights

		private static int lastLightCount = 0;
		/// <summary>
		/// Gets the number of lights in the system.
		/// </summary>
		/// <value>Number of lights.</value>
		public static int LIGHT_COUNT {
			get {
				System.Diagnostics.Debug.Assert ((lastLightCount != 0), "LIGHT_COUNT should not equal 0 when requested");
				return lastLightCount;
			}
			private set {
				if (lastLightCount != value) {
					// Store the values
					lastLightCount = value;
					LIGHT_INDEX_MAX = lastLightCount - 1;
					LIGHT_INDEX_MIDDLE = ((lastLightCount / 2) - 1);
					// New value means this will need recalculated
					ReactiveSystem.Processing_ClearFrequencyStepMultiplier ();
				}
			}
		}

		/// <summary>
		/// Sets the number of lights in the system.  DO NOT SET without recreating queues!
		/// </summary>
		/// <param name="NewLightCount">Desired number of lights in the system.</param>
		public static void SetLightCount (int NewLightCount)
		{
			// Do this in a method in order to avoid inadvertently setting LIGHT_COUNT
			LIGHT_COUNT = NewLightCount;
		}

		/// <summary>
		/// Gets the index of the last light in the system.
		/// </summary>
		/// <value>Highest index of light.</value>
		public static int LIGHT_INDEX_MAX {
			get;
			private set;
		}

		/// <summary>
		/// Gets the index of the middle light in the system.
		/// </summary>
		/// <value>Middle index of light.</value>
		public static int LIGHT_INDEX_MIDDLE {
			get;
			private set;
		}

		// Based on above, just a range of handy LED indexes so you don't have to keep recalculating them

#endregion

#region Color and Brightness

		public const byte Color_MAX = 255;
		public const byte Color_MIN = 0;
		// Color is expressed in a range of 0-255, for convenience.  Actual Actinic lights use a range from 0-15
		public const byte Color_MID = Color_MAX / 2;
		public const byte Color_DARK = Color_MAX / 24;
		public const byte Color_VERY_DARK = Color_MIN + 3;
		// Based on above, just a range of handy color levels so you don't have to keep recalculating them

		public const byte Brightness_MAX = 255;
		public const byte Brightness_MIN = 0;
		// Brightness is expressed in a range of 0-255, mirroring the actual Actinic lights (see below for exception)
		public const byte Brightness_MIN_VISIBLE = 2;
		// Due to the range conversion (maximum posible in hex is 0xFF, but max allowed is 0xCC - see ArduinoOutput.cs),
		//  anything below a brightness of '2' is not visible.

#endregion

	}
}

