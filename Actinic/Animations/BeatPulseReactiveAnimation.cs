//
//  BeatPulseReactiveAnimation.cs
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
using FoxSoft.Math;

namespace Actinic.Animations
{
	public class BeatPulseReactiveAnimation:AbstractReactiveAnimation
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

		/// <summary>
		/// List of LEDs representing the hue-shifting backdrop layer
		/// </summary>
		protected List<LED> CurrentFrame_Backdrop = new List<LED> ();

		/// <summary>
		/// List of LEDs representing the upper layer that pulses according to the beat
		/// </summary>
		protected List<LED> CurrentFrame_Pulse = new List<LED> ();

#region Desaturate Boost

		/// <summary>
		/// Cut-off above which low-frequency intensity will desaturate the colors
		/// </summary>
		private const double DesaturateBoost_Low_Intensity_Floor = 0.55;

		/// <summary>
		/// Maximum amount to increase non-primary colors when low-intensity calls for highest desaturation
		/// </summary>
		private const byte DesaturateBoost_Max_Desaturation = 80;

#endregion

		public BeatPulseReactiveAnimation (int Light_Count):base(Light_Count)
		{
			InitializeLayers ();
		}
		public BeatPulseReactiveAnimation (List<LED> PreviouslyShownFrame):base(PreviouslyShownFrame)
		{
			InitializeLayers ();
		}

		private void InitializeLayers ()
		{
			// Lower the brightness of all the LEDs in the base layer
			for (int index = 0; index < CurrentFrame.Count; index++) {
				CurrentFrame [index].Brightness = LightSystem.Color_DARK;
			}

			// Add empty LEDs to the standalone layers
			for (int index = 0; index < Light_Count; index++) {
				CurrentFrame_Backdrop.Add (new LED (0, 0, 0, 0));
				CurrentFrame_Pulse.Add (new LED (0, 0, 0, 0));
			}
		}

		public override List<LED> GetNextFrame ()
		{
			// Low frequency controls the brightness and desaturation for all of the lights
			// > Brightness (scales from 0 to 1, dark to maximum)
			byte beatBrightness = (byte)MathUtilities.ConvertRange (Audio_Low_Intensity,
			                                                  0, 1,
			                                                  LightSystem.Color_DARK, LightSystem.Color_MAX);
			// > Desaturation (scales from floor to 1, least to most)
			byte desaturationAdded = 0;
			if (LowFrequencyDesaturatesColors)
				desaturationAdded = (byte)MathUtilities.ConvertRange (Math.Max (Audio_Low_Intensity - DesaturateBoost_Low_Intensity_Floor, 0),
				                                                      0, 1 - DesaturateBoost_Low_Intensity_Floor,
				                                                      0, DesaturateBoost_Max_Desaturation);
			// Brightness and hues to add to the backdrop
			Color pulseColorAdditive = new Color (desaturationAdded, desaturationAdded, desaturationAdded, beatBrightness);
			CurrentFrame_Pulse [LightSystem.LIGHT_INDEX_MIDDLE].SetColor (pulseColorAdditive);
			CurrentFrame_Pulse [LightSystem.LIGHT_INDEX_MIDDLE - 1].SetColor (pulseColorAdditive);
			// > Shift the pulse effect outwards
			LightProcessing.ShiftLightsBrightnessOutward (CurrentFrame_Pulse, 2);

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

			// Backdrop color, with a default brightness of none
			// The pulse layer provides the desired brightness
			Color backdropColor = new Color (Held_ColorShift_Red, Held_ColorShift_Green, Held_ColorShift_Blue, 0);
			// > Set the middle lights to the new colors
			CurrentFrame_Backdrop [LightSystem.LIGHT_INDEX_MIDDLE].SetColor (backdropColor);
			CurrentFrame_Backdrop [LightSystem.LIGHT_INDEX_MIDDLE - 1].SetColor (backdropColor);

			// Ripple the colors outwards more quickly than brightness
			LightProcessing.ShiftLightsOutward (CurrentFrame_Backdrop, 4);

			// Reset the current frame with the backdrop
			LightProcessing.MergeLayerDown (CurrentFrame_Backdrop, CurrentFrame, LED.BlendingStyle.Replace);
			// Add the backdrop to the beat-pulse layer
			LightProcessing.MergeLayerDown (CurrentFrame_Pulse, CurrentFrame, LED.BlendingStyle.Sum);

			// Mirror the one half of the lights to the other side
			// Note: change to handling each layer individually if any layers should not be mirrored
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

