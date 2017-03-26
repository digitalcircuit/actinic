//
//  SpinnerReactiveAnimation.cs
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
using System.Collections.Generic;
using FoxSoft.Utilities;

namespace Actinic.Animations
{
	public class SpinnerReactiveAnimation:AbstractReactiveAnimation
	{

		/// <summary>
		/// List of LEDs representing the range of colors
		/// </summary>
		protected List<LED> CalculuatedHues = new List<LED> ();

		public SpinnerReactiveAnimation (int Light_Count) : base (Light_Count)
		{
			InitializeLayers ();
		}

		public SpinnerReactiveAnimation (List<LED> PreviouslyShownFrame) : base (PreviouslyShownFrame)
		{
			InitializeLayers ();
		}

		private void InitializeLayers ()
		{
			for (int index = 0; index < CurrentFrame.Count; index++) {
				CurrentFrame [index].Brightness = LightSystem.Brightness_MAX;
			}

			// Get it to run once, so the following loop continues
			CalculuatedHues.Add (new LED (ColorShift_Red, ColorShift_Green, ColorShift_Blue, LightSystem.Brightness_MAX));
			ColorShift_Amount = (byte)((LightSystem.Color_MAX * 3) / CurrentFrame.Count);
			// (Max color * three stages) / light count
			AnimationUpdateColorShift ();
			while (!(ColorShift_LastMode == ColorShift_Mode.ShiftingRed && ColorShift_Blue == LightSystem.Color_MIN & ColorShift_Red == LightSystem.Color_MAX)) {
				CalculuatedHues.Add (new LED (ColorShift_Red, ColorShift_Green, ColorShift_Blue, LightSystem.Brightness_MAX));
				AnimationUpdateColorShift ();
			}
		}

		public override List<LED> GetNextFrame ()
		{
			int copy_color_index = (int)MathUtilities.ConvertRange (Audio_Average_Intensity, 0, 1, 0, CalculuatedHues.Count);
			for (int index = 0; index < CurrentFrame.Count; index++) {
				CurrentFrame [index] = CalculuatedHues [copy_color_index];
				copy_color_index++;
				if (copy_color_index >= CalculuatedHues.Count) {
					copy_color_index = 0;
				}
			}
			return CurrentFrame;
		}
	}
}

