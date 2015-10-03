//
//  LightSystem.cs
//
//  Author:
//       Shane Synan <digitalcircuit36939@gmail.com>
//
//  Copyright (c) 2015 
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

namespace G35_USB
{

	public static class LightSystem
	{

#region Lights

//		public const int PROTOCOL_OUTPUT_PROCESSING_LATENCY = 47; // ms
//		// Above is an average of sampled values (see #define DEBUG_USB_PERFORMANCE)
//		//  Adjusting this affects the minimum VU animation speed
//		//  Before Arduino adjustments, 49, now, 47

		public const int LIGHT_COUNT = 50;
		// Number of lights in the system
		//  Note: The G35 LED protocol only allows for up to 63 individually-addressable lights

		public const int LIGHT_INDEX_MAX = LIGHT_COUNT - 1;
		public const int LIGHT_INDEX_MIDDLE = ((LIGHT_COUNT / 2) - 1);
		// Based on above, just a range of handy LED indexes so you don't have to keep recalculating them

#endregion

#region Color and Brightness
	
		public const byte Color_MAX = 255;
		public const byte Color_MIN = 0;
		// Color is expressed in a range of 0-255, for convenience.  Actual G35 lights use a range from 0-15
		public const byte Color_MID = Color_MAX / 2;
		public const byte Color_DARK = Color_MAX / 24;
		public const byte Color_VERY_DARK = Color_MIN + 3;
		// Based on above, just a range of handy color levels so you don't have to keep recalculating them

		public const byte Brightness_MAX = 255;
		public const byte Brightness_MIN = 0;
		// Brightness is expressed in a range of 0-255, mirroring the actual G35 lights (see below for exception)
		public const byte Brightness_MIN_VISIBLE = 2;
		// Due to the range conversion (maximum posible in hex is 0xFF, but max allowed is 0xCC - see ArduinoOutput.cs),
		//  anything below a brightness of '2' is not visible.

#endregion

	}
}

