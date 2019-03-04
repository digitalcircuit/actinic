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

// Device configuration
using Actinic.Output;

// Rendering
using Actinic.Rendering;

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


		public SimpleFadeAnimation (
			ReadOnlyDeviceConfiguration Configuration) : base (Configuration)
		{
			// No need to enable RequestSmoothCrossfade as it's assumed no previous frame is available
		}

		public SimpleFadeAnimation (
			ReadOnlyDeviceConfiguration Configuration,
			Layer PreviouslyShownFrame)
			: base (Configuration, PreviouslyShownFrame)
		{
			RequestSmoothCrossfade = true;
			// By default, this will immediately override the existing colors.  Set to true to smoothly transition.
		}


		public override Layer GetNextFrame ()
		{
			Anim_Update_ColorShift ();
			CurrentFrame.Fill (new Color (
				Anim_ColorShift_Red,
				Anim_ColorShift_Green,
				Anim_ColorShift_Blue,
				Color.MAX
			));

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

