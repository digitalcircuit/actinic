//
//  SimpleFadeAnimation.cs
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

namespace Actinic.Animations
{
	public class SimpleFadeAnimation:AbstractAnimation
	{

		protected byte Anim_ColorShift_Red = LightSystem.Color_MAX;
		protected byte Anim_ColorShift_Green = LightSystem.Color_MIN;
		protected byte Anim_ColorShift_Blue = LightSystem.Color_MIN;
		protected ColorShift_Mode Anim_LastColorShift_Mode = ColorShift_Mode.ShiftingRed;

		protected enum ColorShift_Mode
		{
			ShiftingRed,
			ShiftingGreen,
			ShiftingBlue
		}


		public SimpleFadeAnimation (int Light_Count):base(Light_Count)
		{
			// No need to enable RequestSmoothCrossfade as it's assumed no previous frame is available
		}

		public SimpleFadeAnimation (List<LED> PreviouslyShownFrame):base(PreviouslyShownFrame)
		{
			RequestSmoothCrossfade = true;
			// By default, this will immediately override the existing colors.  Set to true to smoothly transition.
		}


		public override List<LED> GetNextFrame ()
		{
			Anim_Update_ColorShift ();
			for (int led_index = 0; led_index < CurrentFrame.Count; led_index++) {
				CurrentFrame [led_index].R = Anim_ColorShift_Red;
				CurrentFrame [led_index].G = Anim_ColorShift_Green;
				CurrentFrame [led_index].B = Anim_ColorShift_Blue;
				CurrentFrame [led_index].Brightness = LightSystem.Brightness_MAX;
			}

			return CurrentFrame;
		}

		protected void Anim_Update_ColorShift ()
		{
			switch (Anim_LastColorShift_Mode) {
				case ColorShift_Mode.ShiftingRed:
				Anim_ColorShift_Red = (byte)Math.Max (Anim_ColorShift_Red - Styled_ColorShiftAmount, LightSystem.Color_MIN);
				Anim_ColorShift_Green = (byte)Math.Min (Anim_ColorShift_Green + Styled_ColorShiftAmount, LightSystem.Color_MAX);
				if (Anim_ColorShift_Red == LightSystem.Color_MIN & Anim_ColorShift_Green == LightSystem.Color_MAX) {
					Anim_LastColorShift_Mode = ColorShift_Mode.ShiftingGreen;
					Anim_ColorShift_Red = LightSystem.Color_MIN;
					Anim_ColorShift_Green = LightSystem.Color_MAX;
				}
				break;
				case ColorShift_Mode.ShiftingGreen:
				Anim_ColorShift_Green = (byte)Math.Max (Anim_ColorShift_Green - Styled_ColorShiftAmount, LightSystem.Color_MIN);
				Anim_ColorShift_Blue = (byte)Math.Min (Anim_ColorShift_Blue + Styled_ColorShiftAmount, LightSystem.Color_MAX);
				if (Anim_ColorShift_Green == LightSystem.Color_MIN & Anim_ColorShift_Blue == LightSystem.Color_MAX) {
					Anim_LastColorShift_Mode = ColorShift_Mode.ShiftingBlue;
					Anim_ColorShift_Green = LightSystem.Color_MIN;
					Anim_ColorShift_Blue = LightSystem.Color_MAX;
				}
				break;
				case ColorShift_Mode.ShiftingBlue:
				Anim_ColorShift_Blue = (byte)Math.Max (Anim_ColorShift_Blue - Styled_ColorShiftAmount, LightSystem.Color_MIN);
				Anim_ColorShift_Red = (byte)Math.Min (Anim_ColorShift_Red + Styled_ColorShiftAmount, LightSystem.Color_MAX);
				if (Anim_ColorShift_Blue == LightSystem.Color_MIN & Anim_ColorShift_Red == LightSystem.Color_MAX) {
					Anim_LastColorShift_Mode = ColorShift_Mode.ShiftingRed;
					Anim_ColorShift_Blue = LightSystem.Color_MIN;
					Anim_ColorShift_Red = LightSystem.Color_MAX;
				}
				break;
				default:
				break;
			}
		}

	}
}

