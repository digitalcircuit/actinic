//
//  SimpleSpinnerAnimation.cs
//
//  Author:
//       Shane Synan <digitalcircuit36939@gmail.com>
//
//  Copyright (c) 2014 
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

namespace G35_USB
{
	public class SimpleSpinnerAnimation:G35_USB.SimpleFadeAnimation
	{

//		private byte Anim_ColorShift_Red = Color_MAX;
//		private byte Anim_ColorShift_Green = Color_MIN;
//		private byte Anim_ColorShift_Blue = Color_MIN;
//		private ColorShift_Mode Anim_LastColorShift_Mode = ColorShift_Mode.ShiftingRed;
//
//		private enum ColorShift_Mode
//		{
//			ShiftingRed,
//			ShiftingGreen,
//			ShiftingBlue
//		}


		public SimpleSpinnerAnimation (int Light_Count):base(Light_Count)
		{
			// No need to clear RequestSmoothCrossfade as inherited class SimpleFadeAnimation doesn't touch it here
		}

		public SimpleSpinnerAnimation (List<LED> PreviouslyShownFrame):base(PreviouslyShownFrame)
		{
			RequestSmoothCrossfade = false;
			// Inherited class SimpleFadeAnimation sets this to true; so reset it back to false
		}


		public override List<LED> GetNextFrame ()
		{
			Anim_Update_ColorShift ();
			CurrentFrame [0].R = Anim_ColorShift_Red;
			CurrentFrame [0].G = Anim_ColorShift_Green;
			CurrentFrame [0].B = Anim_ColorShift_Blue;
			CurrentFrame [0].Brightness = LightSystem.Brightness_MAX;

			ShiftLightsForwards (CurrentFrame, 1);

			return CurrentFrame;
		}

		/// <summary>
		/// Shifts the lights forward.
		/// </summary>
		/// <param name='LightSet'>
		/// Light set to manipulate (List<LED> of GE lights).
		/// </param>
		/// <param name='ShiftCount'>
		/// Number of times to shift lights.
		/// </param>
		private void ShiftLightsForwards (List<LED> LightSet, int ShiftCount)
		{
			//Shifts forwards from LED 1 to 50
			for (int times_shifted = 0; times_shifted < ShiftCount; times_shifted++) {
				for (int i = Light_Count - 1; i > 0; i--) {
					LightSet [i].R = LightSet [i - 1].R;
					LightSet [i].G = LightSet [i - 1].G;
					LightSet [i].B = LightSet [i - 1].B;
					LightSet [i].Brightness = LightSet [i - 1].Brightness;
				}
			}
		}

//		private void Anim_Update_ColorShift ()
//		{
//			switch (Anim_LastColorShift_Mode) {
//			case ColorShift_Mode.ShiftingRed:
//				Anim_ColorShift_Red = (byte)Math.Max (Anim_ColorShift_Red - Styled_ColorShiftAmount, Color_MIN);
//				Anim_ColorShift_Green = (byte)Math.Min (Anim_ColorShift_Green + Styled_ColorShiftAmount, Color_MAX);
//				if (Anim_ColorShift_Red == Color_MIN & Anim_ColorShift_Green == Color_MAX) {
//					Anim_LastColorShift_Mode = ColorShift_Mode.ShiftingGreen;
//					Anim_ColorShift_Red = Color_MIN;
//					Anim_ColorShift_Green = Color_MAX;
//				}
//				break;
//			case ColorShift_Mode.ShiftingGreen:
//				Anim_ColorShift_Green = (byte)Math.Max (Anim_ColorShift_Green - Styled_ColorShiftAmount, Color_MIN);
//				Anim_ColorShift_Blue = (byte)Math.Min (Anim_ColorShift_Blue + Styled_ColorShiftAmount, Color_MAX);
//				if (Anim_ColorShift_Green == Color_MIN & Anim_ColorShift_Blue == Color_MAX) {
//					Anim_LastColorShift_Mode = ColorShift_Mode.ShiftingBlue;
//					Anim_ColorShift_Green = Color_MIN;
//					Anim_ColorShift_Blue = Color_MAX;
//				}
//				break;
//			case ColorShift_Mode.ShiftingBlue:
//				Anim_ColorShift_Blue = (byte)Math.Max (Anim_ColorShift_Blue - Styled_ColorShiftAmount, Color_MIN);
//				Anim_ColorShift_Red = (byte)Math.Min (Anim_ColorShift_Red + Styled_ColorShiftAmount, Color_MAX);
//				if (Anim_ColorShift_Blue == Color_MIN & Anim_ColorShift_Red == Color_MAX) {
//					Anim_LastColorShift_Mode = ColorShift_Mode.ShiftingRed;
//					Anim_ColorShift_Blue = Color_MIN;
//					Anim_ColorShift_Red = Color_MAX;
//				}
//				break;
//			default:
//				break;
//			}
//		}
	}
}

