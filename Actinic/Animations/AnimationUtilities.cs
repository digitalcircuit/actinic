//
//  AnimationUtilities.cs
//
//  Author:
//       Shane Synan <digitalcircuit36939@gmail.com>
//
//  Copyright (c) 2014 - 2016
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
using System.Collections.Generic;

namespace Actinic
{
	public static class AnimationUtilities
	{

		public static List<LED_Set> ConvertImageToLEDArray (int Light_Count, System.Drawing.Bitmap BitmapImage)
		{
			List<LED_Set> loaded_animation = new List<LED_Set> ();
			loaded_animation.Clear ();
			System.Drawing.Color CurrentColor;
			for (int y = 0; y < BitmapImage.Height; y++) {
				loaded_animation.Add (new LED_Set ());
				for (int x = 0; x < Light_Count; x++) {
					CurrentColor = BitmapImage.GetPixel (x, y);
					loaded_animation [y].LED_Values.Add (new LED (CurrentColor.R, CurrentColor.G, CurrentColor.B));
				}
			}
			return loaded_animation;
		}
	}
}

