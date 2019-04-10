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
using FoxSoft.Utilities;

// Device configuration
using Actinic.Output;

// Rendering
using Actinic.Rendering;
using Actinic.Utilities;

namespace Actinic.Animations
{
	public class BeatPulseReactiveAnimation:AbstractReactiveAnimation
	{

		protected const double Pause_Fading_Intensity_Floor = 0.73;
		// Intensity * this

		/// <summary>
		/// Gets the maximum number of frames to pause fading colors.
		/// </summary>
		/// <value>The maximum number of frames to pause fading colors.</value>
		protected double Pause_Fading_Max_Delay {
			get {
				// 0.8 seconds
				// This represents number of frames, not amount to change
				return (800 / deviceConfig.FactorTime);
			}
		}

		/// <summary>
		/// Gets the minimum number of frames to pause fading colors
		/// </summary>
		/// <value>The minimum number of frames to pause fading colors.</value>
		protected double Pause_Fading_Min_Delay {
			get {
				// 0 seconds
				return 0;
			}
		}

		/// <summary>
		/// How many frames fading color has been paused, or zero if none
		/// </summary>
		protected double Pause_Fading_Off_Count = 0;

		/// <summary>
		/// Gets how many pixels of brightness shift per frame.
		/// </summary>
		/// <value>The decimal value of brightness shift per frame.</value>
		protected double Scale_ShiftOutBrightness {
			get {
				// Shift brightness out the entire strand over 0.4 seconds
				// Divide by 2 to account for mirroring the frame
				return (
				    (deviceConfig.FactorTime / 400)
				    * deviceConfig.FactorScaledSize / 2
				);
			}
		}

		/// <summary>
		/// Gets how many pixels of color shift per frame.
		/// </summary>
		/// <value>The decimal value of color shift per frame.</value>
		protected double Scale_ShiftOutColor {
			get {
				// Shift color twice as fast as brightness
				return Scale_ShiftOutBrightness * 2;
			}
		}

		/// <summary>
		/// Gets how many values to shift color hue per frame.
		/// </summary>
		/// <value>The decimal value of color shift per frame.</value>
		protected double Scale_ColorShiftMultiplier {
			get {
				// Aim for the scale of 1 at 10 ms
				return (deviceConfig.FactorTime / 10);
			}
		}

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
		protected Layer CurrentFrame_Backdrop;

		/// <summary>
		/// List of LEDs representing the upper layer that pulses according to the beat
		/// </summary>
		protected Layer CurrentFrame_Pulse;

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

		public BeatPulseReactiveAnimation (
			ReadOnlyDeviceConfiguration Configuration) : base (Configuration)
		{
			InitializeLayers ();
		}

		public BeatPulseReactiveAnimation (
			ReadOnlyDeviceConfiguration Configuration,
			Layer PreviouslyShownFrame)
			: base (Configuration, PreviouslyShownFrame)
		{
			InitializeLayers ();
		}

		private void InitializeLayers ()
		{
			// Lower the brightness of all the LEDs in the base layer
			for (int index = 0; index < CurrentFrame.PixelCount; index++) {
				CurrentFrame [index].Brightness = LightSystem.Color_DARK;
			}

			// Add empty LEDs to the standalone layers
			CurrentFrame_Backdrop = new Layer (
				Light_Count, Color.BlendMode.Combine, Color.Transparent
			);
			CurrentFrame_Pulse = CurrentFrame_Backdrop.Clone ();
		}

		public override Layer GetNextFrame ()
		{
			// Low frequency controls the brightness and desaturation for all of the lights
			// > Brightness (scales from 0 to 1, dark to maximum)
			byte beatBrightness = (byte)MathUtilities.ConvertRange (
				                      Audio_Low_Intensity,
				                      0, 1,
				                      LightSystem.Color_DARK, LightSystem.Color_MAX
			                      );
			// > Desaturation (scales from floor to 1, least to most)
			byte desaturationAdded = 0;
			if (LowFrequencyDesaturatesColors)
				desaturationAdded = (byte)MathUtilities.ConvertRange (
					Math.Max (Audio_Low_Intensity - DesaturateBoost_Low_Intensity_Floor, 0),
					0, 1 - DesaturateBoost_Low_Intensity_Floor,
					0, DesaturateBoost_Max_Desaturation
				);
			// Brightness and hues to add to the backdrop
			Color pulseColorAdditive = new Color (desaturationAdded, desaturationAdded, desaturationAdded, beatBrightness);
			CurrentFrame_Pulse [LightSystem.LIGHT_INDEX_MIDDLE].SetColor (pulseColorAdditive);
			CurrentFrame_Pulse [LightSystem.LIGHT_INDEX_MIDDLE - 1].SetColor (pulseColorAdditive);

			// > Shift the pulse effect outwards
			trackShiftOutBrightness += Scale_ShiftOutBrightness;
			if (trackShiftOutBrightness.IntValue > 0) {
				// Shift by the integer pixel value
				int shiftAmount = trackShiftOutBrightness.TakeInt ();
				// Shift color and brightness due to desaturation colors
				LightProcessing.ShiftLightsOutward (
					CurrentFrame_Pulse, shiftAmount, true);
			}

			// Mid frequency controls how quickly the colors fade
			trackColorChange += (
			    Scale_ColorShiftMultiplier *
			    Math.Max (1,
				    Math.Min ((Audio_Mid_Intensity * 6), LightSystem.Color_MAX)
			    )
			);
			if (trackColorChange.IntValue > 0) {
				// Shift by the integer pixel value
				ColorShift_Amount = (byte)trackColorChange.TakeInt ();
				AnimationUpdateColorShift ();
			}


			// Mid frequency also controls whether or not the color fade freezes
			if (Pause_Fading_Off_Count >= MathUtilities.ConvertRange (Math.Max (Audio_Mid_Intensity - Pause_Fading_Intensity_Floor, 0), 0, 1 - Pause_Fading_Intensity_Floor, Pause_Fading_Min_Delay, Pause_Fading_Max_Delay)) {
				Pause_Fading_Off_Count = 0;

				Held_ColorShift_Red = ColorShift_Red;
				Held_ColorShift_Green = ColorShift_Green;
				Held_ColorShift_Blue = ColorShift_Blue;
			} else {
				Pause_Fading_Off_Count++;
			}

			// Backdrop color, with a default brightness of none
			// The pulse layer provides the desired brightness
			Color backdropColor = new Color (Held_ColorShift_Red, Held_ColorShift_Green, Held_ColorShift_Blue, 0);
			// > Set the middle lights to the new colors
			CurrentFrame_Backdrop [LightSystem.LIGHT_INDEX_MIDDLE].SetColor (backdropColor);
			CurrentFrame_Backdrop [LightSystem.LIGHT_INDEX_MIDDLE - 1].SetColor (backdropColor);

			// Ripple the colors outwards more quickly than brightness
			trackShiftOutColor += Scale_ShiftOutColor;
			if (trackShiftOutColor.IntValue > 0) {
				// Shift by the integer pixel value
				LightProcessing.ShiftLightsOutward (
					CurrentFrame_Backdrop, trackShiftOutColor.TakeInt ());
			}

			// Reset the current frame with the backdrop
			CurrentFrame.Blend (CurrentFrame_Backdrop, Color.BlendMode.Replace);
			// Add the backdrop to the beat-pulse layer
			CurrentFrame.Blend (CurrentFrame_Pulse, Color.BlendMode.Sum);

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

		#region Internal

		/// <summary>
		/// Tracking brightness shift outwards per frame
		/// </summary>
		private IntFraction trackShiftOutBrightness = new IntFraction ();

		/// <summary>
		/// Tracking color shift outwards per frame
		/// </summary>
		private IntFraction trackShiftOutColor = new IntFraction ();

		/// <summary>
		/// Tracking amount of color change shift per frame
		/// </summary>
		private IntFraction trackColorChange = new IntFraction ();

		#endregion
	}
}

