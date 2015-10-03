//
//  BeatPulseReactiveAnimation.cs
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
using System.Collections.Generic;
using FoxSoft.Math;

namespace G35_USB
{
	public class BeatPulseReactiveAnimation:G35_USB.AbstractReactiveAnimation
	{

		protected const double Pause_Fading_Intensity_Floor = 0.73; // Intensity * this

		protected const double Pause_Fading_Max_Delay = 16; // * 50 ms
		protected const double Pause_Fading_Min_Delay = 0; // * 50 ms
		protected double Pause_Fading_Off_Count = 0; // when this number greater than above, will update and reset to zero

		/// <summary>
		/// Red component of the unprocessed current shifting color
		/// </summary>
		private byte Held_ColorShift_Red;
		/// <summary>
		/// Green component of the unprocessed current shifting color
		/// </summary>
		private byte Held_ColorShift_Green;
		/// <summary>
		/// Blue component of the unprocessed current shifting color
		/// </summary>
		private byte Held_ColorShift_Blue;

		/// <summary>
		/// When true, enables the desaturation of colors during intense low-frequency
		/// </summary>
		protected bool LowFrequencyDesaturatesColors = false;

#region Desaturate Boost

		/// <summary>
		/// Cut-off above which low-frequency intensity will desaturate the colors
		/// </summary>
		private const double DesaturateBoost_Low_Intensity_Floor = 0.55;

		/// <summary>
		/// Maximum amount to increase non-primary colors when low-intensity calls for highest desaturation
		/// </summary>
		private const byte DesaturateBoost_Max_Desaturation = 80;

		/// <summary>
		/// Red component of the desaturation-processed current shifting color
		/// </summary>
		private byte DesaturateBoost_ColorShift_Red;
		/// <summary>
		/// Green component of the desaturation-processed current shifting color
		/// </summary>
		private byte DesaturateBoost_ColorShift_Green;
		/// <summary>
		/// Blue component of the desaturation-processed current shifting color
		/// </summary>
		private byte DesaturateBoost_ColorShift_Blue;

#endregion

		public BeatPulseReactiveAnimation (int Light_Count):base(Light_Count)
		{
			PrepareFirstFrame ();
		}
		public BeatPulseReactiveAnimation (List<LED> PreviouslyShownFrame):base(PreviouslyShownFrame)
		{
			PrepareFirstFrame ();
		}

		private void PrepareFirstFrame ()
		{
			for (int index = 0; index < CurrentFrame.Count; index++) {
				CurrentFrame [index].Brightness = LightSystem.Color_DARK;
			}
		}

		public override List<LED> GetNextFrame ()
		{
			// Low frequency controls the brightness for all of the lights
			CurrentFrame [LightSystem.LIGHT_INDEX_MIDDLE].Brightness = (byte)MathUtilities.ConvertRange (Audio_Low_Intensity, 0, 1, LightSystem.Color_DARK, LightSystem.Color_MAX);
			CurrentFrame [LightSystem.LIGHT_INDEX_MIDDLE - 1].Brightness = CurrentFrame [LightSystem.LIGHT_INDEX_MIDDLE].Brightness;

			LightProcessing.ShiftLightsBrightnessOutward (CurrentFrame, 2);

			// Mid frequency controls how quickly the colors fade
			ColorShift_Amount = Convert.ToByte (Math.Max (Math.Min ((Audio_Mid_Intensity * 16) + 5, LightSystem.Color_MAX), 0));
			AnimationUpdateColorShift ();

			// Mid frequency also controls whether or not the color fade freezes
			if (Pause_Fading_Off_Count >= MathUtilities.ConvertRange (Math.Max(Audio_Mid_Intensity - Pause_Fading_Intensity_Floor, 0), 0, 1 - Pause_Fading_Intensity_Floor, Pause_Fading_Min_Delay, Pause_Fading_Max_Delay)) {
				Pause_Fading_Off_Count = 0;

				Held_ColorShift_Red = ColorShift_Red;
				Held_ColorShift_Green = ColorShift_Green;
				Held_ColorShift_Blue = ColorShift_Blue;
			} else {
				Pause_Fading_Off_Count ++;
			}

			if (LowFrequencyDesaturatesColors) {
				// Add anywhere from 0 - DesaturateBoost_Max_Desaturation to all colors based on a scale of Audio_Low_Intensity
				byte DesaturationAdded = (byte)MathUtilities.ConvertRange (Math.Max (Audio_Low_Intensity - DesaturateBoost_Low_Intensity_Floor, 0), 0, 1 - DesaturateBoost_Low_Intensity_Floor, 0, DesaturateBoost_Max_Desaturation);
				DesaturateBoost_ColorShift_Red = (byte)Math.Min (Held_ColorShift_Red + DesaturationAdded, LightSystem.Color_MAX);
				DesaturateBoost_ColorShift_Green = (byte)Math.Min (Held_ColorShift_Green + DesaturationAdded, LightSystem.Color_MAX);
				DesaturateBoost_ColorShift_Blue = (byte)Math.Min (Held_ColorShift_Blue + DesaturationAdded, LightSystem.Color_MAX);
				// Console.WriteLine ("Desaturate: {0}", MathUtilities.GenerateMeterBar (DesaturationAdded, 0, DesaturateBoost_Max_Desaturation, 40, true));
			} else {
				// Disabled, so just directly copy the colors
				DesaturateBoost_ColorShift_Red = Held_ColorShift_Red;
				DesaturateBoost_ColorShift_Green = Held_ColorShift_Green;
				DesaturateBoost_ColorShift_Blue = Held_ColorShift_Blue;
			}

			for (int i = 0; i < LightSystem.LIGHT_COUNT; i++) {
				CurrentFrame [i].R = DesaturateBoost_ColorShift_Red;
				CurrentFrame [i].G = DesaturateBoost_ColorShift_Green;
				CurrentFrame [i].B = DesaturateBoost_ColorShift_Blue;
			}

			// Mirror the one half of the lights to the other side
			int i_source = 0;
			for (int i = LightSystem.LIGHT_INDEX_MAX; i > (LightSystem.LIGHT_INDEX_MIDDLE); i--) {
				i_source = (LightSystem.LIGHT_INDEX_MAX - i);
				CurrentFrame [i].R = CurrentFrame [i_source].R;
				CurrentFrame [i].G = CurrentFrame [i_source].G;
				CurrentFrame [i].B = CurrentFrame [i_source].B;
				CurrentFrame [i].Brightness = CurrentFrame [i_source].Brightness;
			}

			return CurrentFrame;
		}
	}
}

