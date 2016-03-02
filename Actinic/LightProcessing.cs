//
//  LightProcessing.cs
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

namespace Actinic
{
	public class LightProcessing
	{
		public LightProcessing ()
		{
		}


		/// <summary>
		/// Shifts the lights outward.
		/// </summary>
		/// <param name='LightSet'>
		/// Light set to manipulate (List<LED> of GE lights).
		/// </param>
		/// <param name='ShiftCount'>
		/// Number of times to shift lights.
		/// </param>
		public static void ShiftLightsOutward (List<LED> LightSet, int ShiftCount)
		{
			//Shifts outwards, from LED 24 to 1 and from LED 25 to 50
			for (int times_shifted = 0; times_shifted < ShiftCount; times_shifted++) {
				for (int i = LightSystem.LIGHT_COUNT - 1; i >= ((LightSystem.LIGHT_COUNT / 2)); i--) {
					LightSet [i].R = LightSet [i - 1].R;
					LightSet [i].G = LightSet [i - 1].G;
					LightSet [i].B = LightSet [i - 1].B;
				}
				for (int i = 1; i < ((LightSystem.LIGHT_COUNT / 2) - 1); i++) {
					LightSet [i - 1].R = LightSet [i].R;
					LightSet [i - 1].G = LightSet [i].G;
					LightSet [i - 1].B = LightSet [i].B;
				}
			}
		}

		/// <summary>
		/// Shifts the lights' brightness outward.
		/// </summary>
		/// <param name='LightSet'>
		/// Light set to manipulate (List<LED> of GE lights).
		/// </param>
		/// <param name='ShiftCount'>
		/// Number of times to shift lights.
		/// </param>
		public static void ShiftLightsBrightnessOutward (List<LED> LightSet, int ShiftCount)
		{
			//Shifts outwards, from LED 24 to 1 and from LED 25 to 50
			for (int times_shifted = 0; times_shifted < ShiftCount; times_shifted++) {
				for (int i = LightSystem.LIGHT_COUNT - 1; i >= ((LightSystem.LIGHT_COUNT / 2)); i--) {
					LightSet [i].Brightness = LightSet [i - 1].Brightness;
				}
				for (int i = 1; i < ((LightSystem.LIGHT_COUNT / 2) - 1); i++) {
					LightSet [i - 1].Brightness = LightSet [i].Brightness;
				}
			}
		}


		public static bool Is_LED_Dark_Color (List<LED> LightSet, int Index, int Threshold)
		{
			if (Index >= 0 & Index < LightSystem.LIGHT_COUNT) {
				return (LightSet [Index].R < Threshold && LightSet [Index].G < Threshold && LightSet [Index].B < Threshold);
			} else {
				return false;
			}
		}

		public static bool Is_LED_Dark_Brightness (List<LED> LightSet, int Index, int Threshold)
		{
			if (Index >= 0 & Index < LightSystem.LIGHT_COUNT) {
				return (LightSet [Index].Brightness < Threshold);
			} else {
				return false;
			}
		}

		/// <summary>
		/// Merges the colors and brightness of the upper layer into the lower layer.
		/// </summary>
		/// <param name="UpperLayer">Upper layer of LEDs.</param>
		/// <param name="LowerLayer">Lower layer of LEDs.</param>
		/// <param name="Subtractive">If set to <c>true</c> the new color reduces the brightness and hue of the current.</param>
		/// <param name="Opacity">Strength of the upper layer's influence.</param>
		public static void MergeLayerDown (List<LED> UpperLayer, List<LED> LowerLayer, bool Subtractive = false, double Opacity = 1.0)
		{
			if (UpperLayer.Count != LowerLayer.Count)
				throw new ArgumentException ("UpperLayer and LowerLayer must have the same number of lights to merge together.");
			if (Opacity < 0 || Opacity > 1)
				throw new ArgumentOutOfRangeException ("Opacity", "Opacity must be a value between 0 and 1.");

			for (int i = 0; i < UpperLayer.Count; i++) {
				LowerLayer [i].BlendColor (UpperLayer [i].GetColor (), Subtractive, Opacity);
			}
		}

		/// <summary>
		/// Merges the colors and brightness of the upper layer into the lower layer.
		/// </summary>
		/// <param name="UpperLayer">Upper layer of LEDs.</param>
		/// <param name="LowerLayer">Lower layer of LEDs.</param>
		/// <param name="BlendMode">Blending mode for merging upper layer into lower layer.</param>
		public static void MergeLayerDown (List<LED> UpperLayer, List<LED> LowerLayer, LED.BlendingStyle BlendMode)
		{
			if (UpperLayer.Count != LowerLayer.Count)
				throw new ArgumentException ("UpperLayer and LowerLayer must have the same number of lights to merge together.");
			for (int i = 0; i < UpperLayer.Count; i++) {
				LowerLayer [i].BlendColor (UpperLayer [i].GetColor (), BlendMode);
			}
		}
	}
}

