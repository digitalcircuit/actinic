//
//  LegacyReactiveAnimation.cs
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
	public class LegacyReactiveAnimation:AbstractReactiveAnimation
	{
		#region Definitions

		private Layer Actinic_Lights_Unprocessed;

		public VU_Meter_Mode VU_Selected_Mode = VU_Meter_Mode.AutomaticFastBeat;

		public enum VU_Meter_Mode
		{
			AutomaticFastBeat,
			Rainbow,
			RainbowSolid,
			RainbowBeat,
			RainbowBeatBass,
			Hueshift,
			HueshiftBeat,
			MovingBars,
			MovingBarsEquallySpaced,
			StationaryBars,
			SolidRainbowStrobe,
			SolidWhiteStrobe,
			SolidHueshiftStrobe,
			SolidSingleRainbowStrobe,
			SolidSingleWhiteStrobe
		}

		private byte CustomColorShift_Red = LightSystem.Color_MIN;
		private byte CustomColorShift_Green = LightSystem.Color_MIN;
		private byte CustomColorShift_Blue = LightSystem.Color_MIN;

		private const double VU_ColorShift_Brightness_Multiplier = 1.2;

		/// <summary>
		/// Gets the amount of time elapsed per frame
		/// </summary>
		/// <value>The amount of time elapsed per frame.</value>
		private double FrameTimeElapsed {
			get {
				// FactorTime amount of time elapsed per frame
				return deviceConfig.FactorTime;
			}
		}

		/// <summary>
		/// Tracks lingering time of individual lights.
		/// </summary>
		private double[] Linger_Tracker;

		/// <summary>
		/// Tracks if the individual lights have been modified outside of
		/// normal iteration (e.g. generating a strobe).
		/// </summary>
		private bool[] Linger_Modified;

		/// <summary>
		/// Tracks whether or not the linger tracker was in use.
		/// </summary>
		private bool Linger_Tracker_Used = false;

		/// <summary>
		/// Gets how many values to shift light brightness out per frame.
		/// </summary>
		/// <value>The decimal value of brightness outward shift per frame.</value>
		protected double ShiftOutBrightnessPerFrame {
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
		/// Gets how many values to shift light color out per frame.
		/// </summary>
		/// <value>The decimal value of color outward shift per frame.</value>
		protected double ShiftOutColorPerFrame {
			get {
				// Shift brightness out the entire strand over 0.8 seconds
				// Divide by 2 to account for mirroring the frame
				return (
				    (deviceConfig.FactorTime / 800)
				    * deviceConfig.FactorScaledSize / 2
				);
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

		#region VU Meter: Auto Management

		private Intensities VU_LastIntensity_Rating = Intensities.Light;
		private const double VU_Intensity_Threshold_MAX = 0.95;
		private const double VU_Intensity_Threshold_HEAVY = 0.73;
		private const double VU_Intensity_Threshold_MEDIUM = 0.4;

		/// <summary>
		/// The number of frames a new intensity threshold must be maintained
		/// before the intensity will be considered changed.  This avoids brief
		/// flickering.
		/// </summary>
		/// <value>Number of frames before an intensity change is applied.</value>
		private int VU_Intensity_Hysterisis_THRESHOLD {
			get {
				// Require at least 250 ms worth of frames
				return (int)Math.Round (250 / deviceConfig.FactorTime);
			}
		}

		private int VU_Intensity_Hysterisis = 0;
		private bool VU_Intensity_Hysterisis_HasReachedZero = true;
		private int VU_Intensity_Last_Random_Choice = 0;


		/// <summary>
		/// Maximum delay in milliseconds before the automatic mode will force a
		/// change.
		/// </summary>
		private const double VU_Auto_Force_Change_Max_Delay = 25 * 1000;

		/// <summary>
		/// Minimum delay in milliseconds before the automatic mode will force a
		/// change.
		/// </summary>
		private const double VU_Auto_Force_Change_Min_Delay = 7.5 * 1000;

		/// <summary>
		/// Time in milliseconds since the automatic mode has forced a change.
		/// </summary>
		private double VU_Auto_Force_Change_Delay = 0;

		private enum Intensities
		{
			Light,
			Medium,
			Heavy,
			Max
		}

		#endregion


		#region VU Meter: Hueshift

		private const double VU_Hueshift_Color_Multiplier = 550;
		//Originally 400, then 600, now 550 with new frequency processing code

		#endregion

		#region VU Meter: Hueshift Beat

		private const double VU_Hueshift_Beat_Pause_Fading_Intensity_Floor = 0.73;
		// Intensity * this

		/// <summary>
		/// Maximum delay in milliseconds before fading will resume after
		/// pausing.
		/// </summary>
		private const double VU_Hueshift_Beat_Pause_Fading_Max_Delay = 800;

		/// <summary>
		/// Minimum delay in milliseconds before fading will resume after
		/// pausing.
		/// </summary>
		private const double VU_Hueshift_Beat_Pause_Fading_Min_Delay = 0;

		/// <summary>
		/// Time in milliseconds that fading has been paused.
		/// </summary>
		private double VU_Hueshift_Beat_Pause_Fading_Off_Delay = 0;

		#endregion

		#region VU Meter: Rainbow Solid

		// Rainbow_Solid_Color_Change

		/// <summary>
		/// Maximum delay in milliseconds before picking another solid color.
		/// </summary>
		private const double VU_Rainbow_Solid_Color_Change_Max_Delay = 500;

		/// <summary>
		/// Minimum delay in milliseconds before picking another solid color.
		/// </summary>
		private const double VU_Rainbow_Solid_Color_Change_Min_Delay = 100;

		/// <summary>
		/// Time in milliseconds since a solid color was picked.
		/// </summary>
		private double VU_Rainbow_Solid_Color_Change_Delay = 0;

		/// <summary>
		/// Gets multiplier for how many values to shift light color out per
		/// frame.
		/// </summary>
		/// <value>The decimal multiplier of color outward shift per frame.</value>
		protected double VU_ShiftSpeed_Multiplier {
			get {
				// Shift brightness out the entire strand over 0.25 seconds
				// at most (highest intensity)
				// Divide by 2 to account for mirroring the frame
				return (
				    (deviceConfig.FactorTime / 250)
				    * deviceConfig.FactorScaledSize / 2
				);
			}
		}

		private double VU_ShiftSpeed = 1;

		#endregion

		#region VU Meter: Solid Strobe

		//private const double VU_Solid_Color_Strobe_Flicker_Chance = 0.035;

		/// <summary>
		/// Gets the probability an individual color pixel starts strobing for
		/// each frame, multiplied by audio intensity
		/// </summary>
		/// <value>Probability an individual color pixel starts strobing.</value>
		private double VU_Solid_Color_Strobe_Flicker_Chance {
			get {
				// This is called per pixel, per frame, so scale by size and
				// time.  Every millisecond, there's a 0.035 (3.5%) probability
				// that a new strobe will be created.
				return (
				    0.035
				    * deviceConfig.FactorTime
				    * (1 / deviceConfig.FactorScaledSize)
				);
			}
		}

		/// <summary>
		/// Gets the probability an individual hueshift color pixel starts
		/// strobing for each frame
		/// </summary>
		/// <value>Probability an individual hueshift color pixel starts strobing.</value>
		private double VU_Solid_Color_Strobe_Hueshift_Flicker_Chance {
			get {
				// Make this 1.5x less likely than solid colors, to account for
				// three sets of colors strobing
				return VU_Solid_Color_Strobe_Flicker_Chance / 1.5;
			}
		}

		/// <summary>
		/// The minimum size in pixels for a single strobe.
		/// </summary>
		private double VU_Solid_Color_Strobe_Size_Min {
			get {
				// At minimum, a solid strobe can take up either 1 pixel,
				// or 1/75th of the strand
				return Math.Max (1,
					(1 / 75.0) * deviceConfig.FactorScaledSize
				);
			}
		}

		/// <summary>
		/// The maximum size in pixels for a single strobe.
		/// </summary>
		private double VU_Solid_Color_Strobe_Size_Max {
			get {
				// At maximum, a solid strobe can take up either 1 pixel,
				// or 1/30th of the strand
				return Math.Max (1,
					(1 / 30.0) * deviceConfig.FactorScaledSize
				);
			}
		}

		/// <summary>
		/// The minimum duration in milliseconds before a strobe will
		/// end.
		/// </summary>
		private const int VU_Solid_Color_Strobe_Linger_Time_Min = 25;

		/// <summary>
		/// The maximum duration in milliseconds before a strobe will
		/// end.
		/// </summary>
		private const int VU_Solid_Color_Strobe_Linger_Time_Max = 50;

		/// <summary>
		/// Amount to decrease smoothing for solid color strobing.
		/// </summary>
		private const double VU_Solid_Color_Single_Strobe_Smoothing_Decrease = 0.3;

		/// <summary>
		/// Maximum delay in milliseconds before starting another strobe.
		/// </summary>
		private const double VU_Solid_Color_Single_Strobe_Max_Delay = 500;
		// Original: 1000

		/// <summary>
		/// Minimum delay in milliseconds before starting another strobe.
		/// </summary>
		private const double VU_Solid_Color_Single_Strobe_Min_Delay = 10;
		// Original: 100

		/// <summary>
		/// Time in milliseconds since a solid color strobe happened.
		/// </summary>
		private double VU_Solid_Color_Single_Strobe_Off_Delay = 0;

		private enum ColorStrobe
		{
			White,
			Rainbow,
			Hueshift,
			SingleWhite,
			SingleRainbow
		}

		#endregion

		private Color VU_RandomColor = new Color (0, 0, 0);

		#region VU Meter: Moving Bars

		private double VU_Moving_Bar_Max_Length = (LightSystem.LIGHT_COUNT / 2.7);
		//Above was originally LightSystem.LIGHT_COUNT / 2.2, but wrap-around was added for lights, making it too long
		private int VU_Moving_Bar_Half_Max_Length = -1;
		// See InitializeLayersAndVariables ()
		// Since the bars grow outwards from the middle, length / 2 * mirrored = desired value

		/// <summary>
		/// Gets the maximum amount of position change in pixels for the moving
		/// VU bars per frame
		/// </summary>
		/// <value>Maximum position change in pixels for the moving VU bars.</value>
		private double VU_Moving_Bar_Max_Position_Change {
			get {
				return (
				    1.3 * (deviceConfig.FactorTime / 50)
				    * (deviceConfig.FactorScaledSize / 50)
				);
			}
		}

		//Above was originally 1.5, but it seemed too fast
		private const byte VU_Moving_Bar_Red_Other_Color_Boost = 55;
		private const byte VU_Moving_Bar_Green_Other_Color_Boost = 75;
		private const byte VU_Moving_Bar_Blue_Other_Color_Boost = 75;
		// How much other colors should be mixed in for red, green, and blue

		/// <summary>
		/// Gets the maximum amount of position change in pixels for the
		/// equally-spaced moving VU bars per frame
		/// </summary>
		/// <value>Maximum position change in pixels for the equally-spaced moving VU bars.</value>
		private double VU_Moving_Bar_Max_Position_Change_Equally_Spaced {
			get {
				// Halve the normal speed
				return VU_Moving_Bar_Max_Position_Change / 2;
			}
		}

		private const byte VU_Moving_Bar_Min_Brightness = 128;
		private const byte VU_Moving_Bar_Flicker_Color = LightSystem.Color_MAX;
		private const byte VU_Moving_Bar_Flicker_Brightness = 50;

		/// <summary>
		/// Gets the probability of triggering a flicker per LED, per frame in
		/// the MovingBar mode.
		/// </summary>
		/// <value>The probability of triggering a flicker per LED, per frame.</value>
		private double VU_Moving_Bar_Flicker_Chance {
			get {
				// Decrease flicker chance with faster updates and larger LED
				// strands
				return (
				    0.07 * (deviceConfig.FactorTime / 50)
				    * (50 / deviceConfig.FactorScaledSize)
				);
			}
		}

		private double VU_Moving_Bar_Low_Position = 0;
		private double VU_Moving_Bar_Mid_Position = 0;
		private double VU_Moving_Bar_High_Position = 0;
		private bool VU_Moving_Bar_Low_Position_Increasing = true;
		private bool VU_Moving_Bar_Mid_Position_Increasing = true;
		private bool VU_Moving_Bar_High_Position_Increasing = true;
		private int VU_Moving_Bar_Split_Distance = (LightSystem.LIGHT_COUNT / 3);

		#endregion

		#region VU Meter: Stationary Bars

		private double VU_Stationary_Bar_Max_Length = (LightSystem.LIGHT_COUNT / 3.5);
		// Was / 4 with separate lengths for each

		private const byte VU_Stationary_Bar_Min_Brightness = 128;
		private const byte VU_Stationary_Bar_Flicker_Primary_Color = LightSystem.Color_MAX;
		private const byte VU_Stationary_Bar_Flicker_Secondary_Color = 70;
		private const byte VU_Stationary_Bar_Flicker_Brightness = 75;

		/// <summary>
		/// Gets the probability of triggering a flicker per LED, per frame in
		/// the StationaryBar mode.
		/// </summary>
		/// <value>The probability of triggering a flicker per LED, per frame.</value>
		private double VU_Stationary_Bar_Flicker_Chance {
			get {
				// Decrease flicker chance with faster updates and larger LED
				// strands
				return (
				    0.06 * (deviceConfig.FactorTime / 50)
				    * (50 / deviceConfig.FactorScaledSize)
				);
			}
		}

		private const double VU_Stationary_Bar_Color_Multiplier = 550 * 1.5;
		// Make it twice as noticable as in VU_Hueshift

		#endregion

		#endregion


		public LegacyReactiveAnimation (
			ReadOnlyDeviceConfiguration Configuration) : base (Configuration)
		{
			InitializeLayersAndVariables ();
		}

		public LegacyReactiveAnimation (
			ReadOnlyDeviceConfiguration Configuration,
			Layer PreviouslyShownFrame)
			: base (Configuration, PreviouslyShownFrame)
		{
			InitializeLayersAndVariables ();
		}

		private void InitializeLayersAndVariables ()
		{
			// Prepare tracking for as many lights as are available
			Linger_Tracker = new double[Light_Count];
			Linger_Modified = new bool[Light_Count];

			Actinic_Lights_Unprocessed = new Layer (CurrentFrame.PixelCount);
			// When LIGHT_COUNT was made a property, these could no longer be calculated at compile-time.  Do it here.
			VU_Moving_Bar_Half_Max_Length = (int)(VU_Moving_Bar_Max_Length / 2);
		}

		public override Layer GetNextFrame ()
		{
			// Disable algorithmic control of smoothing for these two modes, as the mode sets it manually
			EnableAlgorithmicSmoothingControl = !(VU_Selected_Mode == VU_Meter_Mode.SolidSingleWhiteStrobe || VU_Selected_Mode == VU_Meter_Mode.SolidSingleRainbowStrobe);

			// Default to enabling smoothing (specific modes override as needed)
			EnableSmoothing = true;

			switch (VU_Selected_Mode) {
			case VU_Meter_Mode.AutomaticFastBeat:
				AudioMeter_Manager_FastBeat ();
				break;
			case VU_Meter_Mode.Rainbow:
				AudioMeter_Extended_Rainbow (false);
				break;
			case VU_Meter_Mode.RainbowSolid:
				AudioMeter_Extended_Rainbow (true);
				break;
			case VU_Meter_Mode.RainbowBeat:
				AudioMeter_Extended_RainbowBeat ();
				break;
			case VU_Meter_Mode.RainbowBeatBass:
				AudioMeter_Extended_RainbowBeat (true);
				break;
			case VU_Meter_Mode.Hueshift:
				AudioMeter_Extended_Hueshift ();
				break;
			case VU_Meter_Mode.HueshiftBeat:
				AudioMeter_Extended_Hueshift_Beat ();
				break;
			case VU_Meter_Mode.MovingBars:
				AudioMeter_Extended_Moving_Bars ();
				break;
			case VU_Meter_Mode.MovingBarsEquallySpaced:
				AudioMeter_Extended_Moving_Bars (true);
				break;
			case VU_Meter_Mode.StationaryBars:
				AudioMeter_Extended_Stationary_Bars ();
				break;
			case VU_Meter_Mode.SolidRainbowStrobe:
				AudioMeter_Extended_Solid_Color_Strobe (ColorStrobe.Rainbow);
				break;
			case VU_Meter_Mode.SolidWhiteStrobe:
				AudioMeter_Extended_Solid_Color_Strobe (ColorStrobe.White);
				break;
			case VU_Meter_Mode.SolidHueshiftStrobe:
				AudioMeter_Extended_Solid_Color_Strobe (ColorStrobe.Hueshift);
				break;
			case VU_Meter_Mode.SolidSingleRainbowStrobe:
				AudioMeter_Extended_Solid_Color_Strobe (ColorStrobe.SingleRainbow);
				break;
			case VU_Meter_Mode.SolidSingleWhiteStrobe:
				AudioMeter_Extended_Solid_Color_Strobe (ColorStrobe.SingleWhite);
				break;
			default:
				throw new NotSupportedException (string.Format (
					"Unknown VU_Meter_Mode '{0}'",
					VU_Selected_Mode
				));
			}

			return CurrentFrame;
		}




		#region Compatibility

		private void FillLights_Color (byte R, byte G, byte B)
		{
			for (int i = 0; i < CurrentFrame.PixelCount; i++) {
				CurrentFrame [i].R = R;
				CurrentFrame [i].G = G;
				CurrentFrame [i].B = B;
			}
		}

		private void FillLights_Brightness (byte Brightness)
		{
			for (int i = 0; i < CurrentFrame.PixelCount; i++) {
				CurrentFrame [i].Brightness = Brightness;
			}
		}

		private void SetLight_Color (int Index, byte R, byte G, byte B)
		{
			if (Index >= 0 & Index < LightSystem.LIGHT_COUNT) {
				CurrentFrame [Index].R = R;
				CurrentFrame [Index].G = G;
				CurrentFrame [Index].B = B;
			}
		}

		private void TrySetLight_Color_R (int Index, byte R)
		{
			if (Index >= 0 & Index < LightSystem.LIGHT_COUNT) {
				CurrentFrame [Index].R = R;
			}
		}

		private void TrySetLight_Color_G (int Index, byte G)
		{
			if (Index >= 0 & Index < LightSystem.LIGHT_COUNT) {
				CurrentFrame [Index].G = G;
			}
		}

		private void TrySetLight_Color_B (int Index, byte B)
		{
			if (Index >= 0 & Index < LightSystem.LIGHT_COUNT) {
				CurrentFrame [Index].B = B;
			}
		}

		#endregion

		#region Legacy Handling

		private void VU_Update_RandomColor ()
		{
			if (VU_Rainbow_Solid_Color_Change_Delay > MathUtilities.ConvertRange (Audio_Average_Intensity, 0, 1, VU_Rainbow_Solid_Color_Change_Max_Delay, VU_Rainbow_Solid_Color_Change_Min_Delay)) {
				VU_Rainbow_Solid_Color_Change_Delay = 0;
				VU_RandomColor = RandomColorGenerator.GetRandomColor ();
				VU_ShiftSpeed = Audio_Average_Intensity * VU_ShiftSpeed_Multiplier;
			} else {
				VU_Rainbow_Solid_Color_Change_Delay += FrameTimeElapsed;
			}
//			if (ReactiveSystem.Processing_Show_Analysis) {
//				Console.WriteLine ("Intensity delta ceiling: " + Math.Round (MathUtilities.ConvertRange (VU_Average_Intensity, 0, 1, VU_Rainbow_Solid_Color_Change_Max_Delay, VU_Rainbow_Solid_Color_Change_Min_Delay), 3).ToString ().PadRight (5) + "  Shift speed: " + Math.Round (VU_ShiftSpeed, 3).ToString ().PadRight (5).ToString () + "  Current color: " + VU_RandomColor.Name);
//			}
		}

		private Color VU_Get_ShiftedColor (double Ratio)
		{
			byte color_R, color_G, color_B;
			if (Ratio > 0 & Ratio <= 0.33) {
				color_B = (byte)(LightSystem.Color_MAX * MathUtilities.ConvertRange (Ratio, 0, 0.33, LightSystem.Color_MAX, 0));
				color_G = (byte)(LightSystem.Color_MAX * MathUtilities.ConvertRange (Ratio, 0, 0.33, 0, LightSystem.Color_MAX));
				color_R = 0;
			} else if (Ratio > 0.33 & Ratio < 0.66) {
				color_B = 0;
				color_G = (byte)(LightSystem.Color_MAX * MathUtilities.ConvertRange (Ratio, 0.33, 0.66, LightSystem.Color_MAX, 0));
				color_R = (byte)(LightSystem.Color_MAX * MathUtilities.ConvertRange (Ratio, 0.33, 0.66, 0, LightSystem.Color_MAX));
			} else if (Ratio > 0.66 & Ratio <= 1) {
				color_B = (byte)(LightSystem.Color_MAX * MathUtilities.ConvertRange (Ratio, 0.66, 1, 0, LightSystem.Color_MAX));
				color_G = 0;
				color_R = (byte)(LightSystem.Color_MAX * MathUtilities.ConvertRange (Ratio, 0.66, 1, LightSystem.Color_MAX, 0));
			} else {
				color_R = 0;
				color_G = 0;
				color_B = 0;
			}
			return new Color (color_R, color_G, color_B);
		}

		private void VU_Update_MovingBars ()
		{
			VU_Update_MovingBars (false);
		}

		private void VU_Update_MovingBars (bool EquallySpaced)
		{
			if (EquallySpaced) {
				if (VU_Moving_Bar_Low_Position_Increasing == true & (VU_Moving_Bar_Low_Position >= LightSystem.LIGHT_INDEX_MAX)) {
					VU_Moving_Bar_Low_Position = LightSystem.LIGHT_INDEX_MAX;
					VU_Moving_Bar_Low_Position_Increasing = false;
				} else if (VU_Moving_Bar_Low_Position_Increasing == false & (VU_Moving_Bar_Low_Position <= 0)) {
					VU_Moving_Bar_Low_Position = 0;
					VU_Moving_Bar_Low_Position_Increasing = true;
				} else {
					if (VU_Moving_Bar_Low_Position_Increasing) {
						VU_Moving_Bar_Low_Position = Math.Min (VU_Moving_Bar_Low_Position + (Audio_Low_Intensity * VU_Moving_Bar_Max_Position_Change_Equally_Spaced), LightSystem.LIGHT_INDEX_MAX);
					} else {
						VU_Moving_Bar_Low_Position = Math.Max (VU_Moving_Bar_Low_Position - (Audio_Low_Intensity * VU_Moving_Bar_Max_Position_Change_Equally_Spaced), 0);
					}
				}
				VU_Moving_Bar_Mid_Position = MathUtilities.WrapAround (VU_Moving_Bar_Low_Position + VU_Moving_Bar_Split_Distance, 0, LightSystem.LIGHT_INDEX_MAX);
				VU_Moving_Bar_High_Position = MathUtilities.WrapAround (VU_Moving_Bar_Mid_Position + VU_Moving_Bar_Split_Distance, 0, LightSystem.LIGHT_INDEX_MAX);
			} else {
				if (VU_Moving_Bar_Low_Position_Increasing == true & (VU_Moving_Bar_Low_Position >= LightSystem.LIGHT_INDEX_MAX)) {
					VU_Moving_Bar_Low_Position = LightSystem.LIGHT_INDEX_MAX;
					VU_Moving_Bar_Low_Position_Increasing = false;
				} else if (VU_Moving_Bar_Low_Position_Increasing == false & (VU_Moving_Bar_Low_Position <= 0)) {
					VU_Moving_Bar_Low_Position = 0;
					VU_Moving_Bar_Low_Position_Increasing = true;
				} else {
					if (VU_Moving_Bar_Low_Position_Increasing) {
						VU_Moving_Bar_Low_Position = Math.Min (VU_Moving_Bar_Low_Position + (Audio_Low_Intensity * VU_Moving_Bar_Max_Position_Change), LightSystem.LIGHT_INDEX_MAX);
					} else {
						VU_Moving_Bar_Low_Position = Math.Max (VU_Moving_Bar_Low_Position - (Audio_Low_Intensity * VU_Moving_Bar_Max_Position_Change), 0);
					}
				}
				if (VU_Moving_Bar_Mid_Position_Increasing == true & (VU_Moving_Bar_Mid_Position >= LightSystem.LIGHT_INDEX_MAX)) {
					VU_Moving_Bar_Mid_Position = LightSystem.LIGHT_INDEX_MAX;
					VU_Moving_Bar_Mid_Position_Increasing = false;
				} else if (VU_Moving_Bar_Mid_Position_Increasing == false & (VU_Moving_Bar_Mid_Position <= 0)) {
					VU_Moving_Bar_Mid_Position = 0;
					VU_Moving_Bar_Mid_Position_Increasing = true;
				} else {
					if (VU_Moving_Bar_Mid_Position_Increasing) {
						VU_Moving_Bar_Mid_Position = Math.Min (VU_Moving_Bar_Mid_Position + (Audio_Mid_Intensity * VU_Moving_Bar_Max_Position_Change), LightSystem.LIGHT_INDEX_MAX);
					} else {
						VU_Moving_Bar_Mid_Position = Math.Max (VU_Moving_Bar_Mid_Position - (Audio_Mid_Intensity * VU_Moving_Bar_Max_Position_Change), 0);
					}
				}
				if (VU_Moving_Bar_High_Position_Increasing == true & (VU_Moving_Bar_High_Position >= LightSystem.LIGHT_INDEX_MAX)) {
					VU_Moving_Bar_High_Position = LightSystem.LIGHT_INDEX_MAX;
					VU_Moving_Bar_High_Position_Increasing = false;
				} else if (VU_Moving_Bar_High_Position_Increasing == false & (VU_Moving_Bar_High_Position <= 0)) {
					VU_Moving_Bar_High_Position = 0;
					VU_Moving_Bar_High_Position_Increasing = true;
				} else {
					if (VU_Moving_Bar_High_Position_Increasing) {
						VU_Moving_Bar_High_Position = Math.Min (VU_Moving_Bar_High_Position + (Audio_High_Intensity * VU_Moving_Bar_Max_Position_Change), LightSystem.LIGHT_INDEX_MAX);
					} else {
						VU_Moving_Bar_High_Position = Math.Max (VU_Moving_Bar_High_Position - (Audio_High_Intensity * VU_Moving_Bar_Max_Position_Change), 0);
					}
				}
			}
		}

		private void VU_Reset_Moving_Bars ()
		{
			VU_Moving_Bar_Low_Position = 0;
			VU_Moving_Bar_Mid_Position = 0;
			VU_Moving_Bar_High_Position = 0;
			VU_Moving_Bar_Low_Position_Increasing = true;
			VU_Moving_Bar_Mid_Position_Increasing = true;
			VU_Moving_Bar_High_Position_Increasing = true;
		}


		private void AudioMeter_Manager_FastBeat ()
		{
			Intensities Intensity_Rating = VU_LastIntensity_Rating;

			if (Audio_Average_Intensity > VU_Intensity_Threshold_MAX) {
				Intensity_Rating = Intensities.Max;
			} else if (Audio_Average_Intensity > VU_Intensity_Threshold_HEAVY) {
				Intensity_Rating = Intensities.Heavy;
			} else if (Audio_Average_Intensity > VU_Intensity_Threshold_MEDIUM) {
				Intensity_Rating = Intensities.Medium;
			} else {
				Intensity_Rating = Intensities.Light;
			}

			bool Intensity_Rating_Changed = (Intensity_Rating != VU_LastIntensity_Rating);
			if (Intensity_Rating_Changed) {
				if (VU_Intensity_Hysterisis >= VU_Intensity_Hysterisis_THRESHOLD) {
					if (VU_Intensity_Hysterisis > 0) {
						VU_Intensity_Hysterisis_HasReachedZero = false;
						VU_Intensity_Hysterisis -= 1;
					} else {
						VU_Intensity_Hysterisis = 0;
						VU_Intensity_Hysterisis_HasReachedZero = true;
					}
				} else {
					Intensity_Rating = VU_LastIntensity_Rating;
					VU_Intensity_Hysterisis += 1;
				}
			} else {
				if (VU_Intensity_Hysterisis > 0) {
					if (VU_Intensity_Hysterisis_HasReachedZero) {
						VU_Intensity_Hysterisis = 0;
					} else {
						VU_Intensity_Hysterisis -= 1;
					}
				} else {
					VU_Intensity_Hysterisis = 0;
					VU_Intensity_Hysterisis_HasReachedZero = true;
				}
			}

			if (Intensity_Rating_Changed) {
				//				VU_Intensity_Last_Random_Choice = -1;
				//				VU_Auto_Force_Change_Count = 0;
				// Note: I don't -think- Smoothing needs to be reset after each
				//				ResetSmoothing ();
			} else if (VU_Auto_Force_Change_Delay > MathUtilities.ConvertRange (Audio_Average_Intensity, 0, 1, VU_Auto_Force_Change_Max_Delay, VU_Auto_Force_Change_Min_Delay)) {
				// Force a new random pattern to be chosen
				VU_Intensity_Last_Random_Choice = -1;
				VU_Auto_Force_Change_Delay = 0;
				// Note: I don't -think- Smoothing needs to be reset after each
				//ResetSmoothing ();
			} else {
				VU_Auto_Force_Change_Delay += FrameTimeElapsed;
			}

			switch (Intensity_Rating) {
			case Intensities.Max:
				if (VU_Intensity_Last_Random_Choice < 0 | VU_Intensity_Last_Random_Choice > 1)
					VU_Intensity_Last_Random_Choice = Randomizer.RandomProvider.Next (0, 1);
				switch (VU_Intensity_Last_Random_Choice) {
				case 0:
					AudioMeter_Extended_Solid_Color_Strobe (ColorStrobe.SingleRainbow);
					break;
				case 1:
					AudioMeter_Extended_Solid_Color_Strobe (ColorStrobe.SingleWhite);
					break;
				default:
					throw new NotSupportedException (string.Format (
						"Invalid VU_Intensity_Last_Random_Choice {0} for" +
						"Intensities.Max",
						VU_Intensity_Last_Random_Choice
					));
				}
				break;
			case Intensities.Heavy:
				if (VU_Intensity_Last_Random_Choice < 0 | VU_Intensity_Last_Random_Choice > 2)
					VU_Intensity_Last_Random_Choice = Randomizer.RandomProvider.Next (0, 2);
				switch (VU_Intensity_Last_Random_Choice) {
				case 0:
					AudioMeter_Extended_Solid_Color_Strobe (ColorStrobe.Rainbow);
					break;
				case 1:
					AudioMeter_Extended_Solid_Color_Strobe (ColorStrobe.White);
					break;
				case 2:
					AudioMeter_Extended_Solid_Color_Strobe (ColorStrobe.Hueshift);
					break;
				default:
					throw new NotSupportedException (string.Format (
						"Invalid VU_Intensity_Last_Random_Choice {0} for" +
						"Intensities.Heavy",
						VU_Intensity_Last_Random_Choice
					));
				}
				break;
			case Intensities.Medium:
				if (VU_Intensity_Last_Random_Choice < 0 | VU_Intensity_Last_Random_Choice > 3)
					VU_Intensity_Last_Random_Choice = Randomizer.RandomProvider.Next (0, 3);
				switch (VU_Intensity_Last_Random_Choice) {
				case 0:
					AudioMeter_Extended_Moving_Bars (true);
					break;
				case 1:
					AudioMeter_Extended_Hueshift_Beat ();
					break;
				case 2:
					AudioMeter_Extended_Rainbow (true);
					break;
				case 3:
					AudioMeter_Extended_Moving_Bars (false);
					break;
				default:
					throw new NotSupportedException (string.Format (
						"Invalid VU_Intensity_Last_Random_Choice {0} for" +
						"Intensities.Medium",
						VU_Intensity_Last_Random_Choice
					));
				}
				break;
			case Intensities.Light:
				if (VU_Intensity_Last_Random_Choice < 0 | VU_Intensity_Last_Random_Choice > 1)
					VU_Intensity_Last_Random_Choice = Randomizer.RandomProvider.Next (0, 1);
				switch (VU_Intensity_Last_Random_Choice) {
				case 0:
					AudioMeter_Extended_Rainbow (false);
					break;
				case 1:
					AudioMeter_Extended_Hueshift_Beat ();
					break;
				default:
					throw new NotSupportedException (string.Format (
						"Invalid VU_Intensity_Last_Random_Choice {0} for" +
						"Intensities.Light",
						VU_Intensity_Last_Random_Choice
					));
				}
				break;
			default:
				throw new NotSupportedException (string.Format (
					"Unsupported Intensity_Rating {0}",
					Intensity_Rating
				));
			}

			VU_LastIntensity_Rating = Intensity_Rating;
		}

		private void AudioMeter_Extended_Rainbow (bool UseSolidColors)
		{
			// Mark linger tracker as not used
			Linger_Tracker_Used = false;

			if (UseSolidColors) {
				VU_Update_RandomColor ();
			} else {
				// Average intensity controls how quickly the colors fade
				trackColorChange += (
				    Scale_ColorShiftMultiplier
				    * Math.Max (1, Math.Min ((Audio_Average_Intensity * 6), LightSystem.Color_MAX))
				);
				if (trackColorChange.IntValue > 0) {
					// Shift by the integer pixel value
					AnimationUpdateColorShift (
						(byte)trackColorChange.TakeInt ());
				}
			}

			if (UseSolidColors) {
				Actinic_Lights_Unprocessed [LightSystem.LIGHT_INDEX_MIDDLE].SetColor (VU_RandomColor);
				Actinic_Lights_Unprocessed [LightSystem.LIGHT_INDEX_MIDDLE - 1].SetColor (VU_RandomColor);
			} else {
				Actinic_Lights_Unprocessed [LightSystem.LIGHT_INDEX_MIDDLE].R = ColorShift_Red;
				Actinic_Lights_Unprocessed [LightSystem.LIGHT_INDEX_MIDDLE].G = ColorShift_Green;
				Actinic_Lights_Unprocessed [LightSystem.LIGHT_INDEX_MIDDLE].B = ColorShift_Blue;
				Actinic_Lights_Unprocessed [LightSystem.LIGHT_INDEX_MIDDLE - 1].R = ColorShift_Red;
				Actinic_Lights_Unprocessed [LightSystem.LIGHT_INDEX_MIDDLE - 1].G = ColorShift_Green;
				Actinic_Lights_Unprocessed [LightSystem.LIGHT_INDEX_MIDDLE - 1].B = ColorShift_Blue;
			}

			if (UseSolidColors) {
				// > Shift the pulse effect outwards
				trackShiftOutColor += VU_ShiftSpeed;
				if (trackShiftOutColor.IntValue > 0) {
					// Shift by the integer pixel value
					int shiftAmount = trackShiftOutColor.TakeInt ();
					LightProcessing.ShiftLightsOutward (
						Actinic_Lights_Unprocessed, shiftAmount);
				}
			} else {
				// > Shift the pulse effect outwards
				trackShiftOutColor += ShiftOutColorPerFrame;
				if (trackShiftOutColor.IntValue > 0) {
					// Shift by the integer pixel value
					int shiftAmount = trackShiftOutColor.TakeInt ();
					LightProcessing.ShiftLightsOutward (
						Actinic_Lights_Unprocessed, shiftAmount);
				}
			}

			// Amount by which the warm (red) color is increased
			byte ColorShift_HighBoost = 0;
			// Amount by which the cool (blue) color is increased
			byte ColorShift_LowBoost = 0;

			// Find low and high frequency boost
			if (Audio_Average_Frequency_Distribution_Percentage > 0.5) {
				ColorShift_HighBoost =
					Convert.ToByte (Math.Max (Math.Min ((((Audio_Average_Frequency_Distribution_Percentage - 0.5) * 2) * 128), 255), 0));
			} else if (Audio_Average_Frequency_Distribution_Percentage < 0.5) {
				ColorShift_LowBoost =
					Convert.ToByte (Math.Max (Math.Min ((((0.5 - Audio_Average_Frequency_Distribution_Percentage) * 2) * 128), 255), 0));
			}

			for (int i = 0; i < LightSystem.LIGHT_COUNT; i++) {
				CurrentFrame [i].R = (byte)Math.Min (Actinic_Lights_Unprocessed [i].R + ColorShift_HighBoost, 255);
				CurrentFrame [i].G = Actinic_Lights_Unprocessed [i].G;
				CurrentFrame [i].B = (byte)Math.Min (Actinic_Lights_Unprocessed [i].B + ColorShift_LowBoost, 255);
			}

			double CurrentLight_VU_Max = 0;

			for (int i = 0; i < (LightSystem.LIGHT_COUNT / 2); i++) {
				if (i < AudioProcessedSnapshot.Count) {
					CurrentLight_VU_Max = AudioProcessedSnapshot [Math.Min (((LightSystem.LIGHT_COUNT / 2 - 1) - i), AudioProcessedSnapshot.Count - 1)];
				}
				CurrentFrame [i].Brightness = (byte)(255 * CurrentLight_VU_Max * VU_ColorShift_Brightness_Multiplier);
			}

			int i_source = 0;
			for (int i = LightSystem.LIGHT_INDEX_MAX; i > (LightSystem.LIGHT_INDEX_MIDDLE); i--) {
				i_source = (LightSystem.LIGHT_INDEX_MAX - i);
				CurrentFrame [i].Brightness = CurrentFrame [i_source].Brightness;
			}
		}


		private void AudioMeter_Extended_RainbowBeat ()
		{
			AudioMeter_Extended_RainbowBeat (false);
		}

		private void AudioMeter_Extended_RainbowBeat (bool BeatBasedBrightness)
		{
			// Mark linger tracker as not used
			Linger_Tracker_Used = false;

			if (BeatBasedBrightness) {
				Actinic_Lights_Unprocessed [LightSystem.LIGHT_INDEX_MIDDLE].Brightness = (byte)MathUtilities.ConvertRange (Audio_Low_Intensity, 0, 1, LightSystem.Color_MID, 255);
				Actinic_Lights_Unprocessed [LightSystem.LIGHT_INDEX_MIDDLE - 1].Brightness = (byte)MathUtilities.ConvertRange (Audio_Low_Intensity, 0, 1, LightSystem.Color_MID, 255);

				LightProcessing.ShiftLightsBrightnessOutward (Actinic_Lights_Unprocessed, 1);
			} else {
				for (int i = 0; i < LightSystem.LIGHT_COUNT; i++) {
					CurrentFrame [i].Brightness = 255;
				}
			}

			double CurrentLight_VU_Max = 0;

			for (int i = 0; i < (LightSystem.LIGHT_COUNT / 2); i++) {
				if (i < AudioProcessedSnapshot.Count) {
					CurrentLight_VU_Max = AudioProcessedSnapshot [Math.Min (((LightSystem.LIGHT_COUNT / 2 - 1) - i), AudioProcessedSnapshot.Count - 1)];
				}
				CurrentFrame [i].SetColor (VU_Get_ShiftedColor (CurrentLight_VU_Max));
			}


			int i_source = 0;
			for (int i = LightSystem.LIGHT_INDEX_MAX; i > (LightSystem.LIGHT_INDEX_MIDDLE); i--) {
				i_source = (LightSystem.LIGHT_INDEX_MAX - i);
				CurrentFrame [i].R = CurrentFrame [i_source].R;
				CurrentFrame [i].G = CurrentFrame [i_source].G;
				CurrentFrame [i].B = CurrentFrame [i_source].B;
				CurrentFrame [i].Brightness = CurrentFrame [i_source].Brightness;
			}
		}

		private void AudioMeter_Extended_Hueshift ()
		{
			// Mark linger tracker as not used
			Linger_Tracker_Used = false;

			if (Audio_Average_Frequency_Distribution_Percentage > 0.5) {
				CustomColorShift_Red = Convert.ToByte (Math.Max (Math.Min ((((Audio_Average_Frequency_Distribution_Percentage - 0.5) * 2) * VU_Hueshift_Color_Multiplier), LightSystem.Color_MAX), LightSystem.Color_MIN));
				CustomColorShift_Blue = LightSystem.Color_MIN;
				CustomColorShift_Green = (byte)(LightSystem.Color_MAX - CustomColorShift_Red);
			} else if (Audio_Average_Frequency_Distribution_Percentage < 0.5) {
				CustomColorShift_Red = LightSystem.Color_MIN;
				CustomColorShift_Blue = Convert.ToByte (Math.Max (Math.Min ((((0.5 - Audio_Average_Frequency_Distribution_Percentage) * 2) * VU_Hueshift_Color_Multiplier), LightSystem.Color_MAX), LightSystem.Color_MIN));
				CustomColorShift_Green = (byte)(LightSystem.Color_MAX - CustomColorShift_Blue);
			} else {
				CustomColorShift_Red = LightSystem.Color_MIN;
				CustomColorShift_Green = LightSystem.Color_MAX;
				CustomColorShift_Blue = LightSystem.Color_MIN;
			}

			for (int i = 0; i < LightSystem.LIGHT_COUNT; i++) {
				CurrentFrame [i].R = CustomColorShift_Red;
				CurrentFrame [i].G = CustomColorShift_Green;
				CurrentFrame [i].B = CustomColorShift_Blue;
			}

			double CurrentLight_VU_Max = 0;

			for (int i = 0; i < (LightSystem.LIGHT_COUNT / 2); i++) {
				if (i < AudioProcessedSnapshot.Count) {
					CurrentLight_VU_Max = AudioProcessedSnapshot [Math.Min (((LightSystem.LIGHT_COUNT / 2 - 1) - i), AudioProcessedSnapshot.Count - 1)];
				}
				CurrentFrame [i].Brightness = (byte)(LightSystem.Brightness_MAX * CurrentLight_VU_Max * VU_ColorShift_Brightness_Multiplier);
			}

			int i_source = 0;
			for (int i = LightSystem.LIGHT_INDEX_MAX; i > (LightSystem.LIGHT_INDEX_MIDDLE); i--) {
				i_source = (LightSystem.LIGHT_INDEX_MAX - i);
				CurrentFrame [i].Brightness = CurrentFrame [i_source].Brightness;
			}
		}

		private void AudioMeter_Extended_Hueshift_Beat ()
		{
			// Mark linger tracker as not used
			Linger_Tracker_Used = false;

			// Low frequency controls the brightness for all of the lights
			Actinic_Lights_Unprocessed [LightSystem.LIGHT_INDEX_MIDDLE].Brightness = (byte)MathUtilities.ConvertRange (Audio_Low_Intensity, 0, 1, LightSystem.Color_DARK, 255);
			Actinic_Lights_Unprocessed [LightSystem.LIGHT_INDEX_MIDDLE - 1].Brightness = Actinic_Lights_Unprocessed [LightSystem.LIGHT_INDEX_MIDDLE].Brightness;

			// > Shift the pulse effect outwards
			trackShiftOutBrightness += ShiftOutBrightnessPerFrame;
			if (trackShiftOutBrightness.IntValue > 0) {
				// Shift by the integer pixel value
				int shiftAmount = trackShiftOutBrightness.TakeInt ();
				LightProcessing.ShiftLightsBrightnessOutward (
					Actinic_Lights_Unprocessed, shiftAmount);
			}

			// Mid frequency controls how quickly the colors fade
			//VU_ColorShift_Amount = Convert.ToByte (Math.Max (Math.Min ((Audio_Mid_Intensity * 16) + 5, 255), 0));
			// EDIT: Seems the average intensity works better, and it makes this easier to deal with

			// Average intensity controls how quickly the colors fade
			trackColorChange += (
			    Scale_ColorShiftMultiplier
			    * Math.Max (1, Math.Min ((Audio_Average_Intensity * 6), LightSystem.Color_MAX))
			);
			if (trackColorChange.IntValue > 0) {
				// Shift by the integer pixel value
				AnimationUpdateColorShift ((byte)trackColorChange.TakeInt ());
			}

			//Console.WriteLine ("DEBUG:  Unupdated count: {0}, requirement: {1}", VU_Hueshift_Beat_Pause_Fading_Off_Count, MathUtilities.ConvertRange (Math.Max(VU_Volume_Mid_Intensity - VU_Hueshift_Beat_Pause_Fading_Intensity_Floor, 0), 0, 1 - VU_Hueshift_Beat_Pause_Fading_Intensity_Floor, VU_Hueshift_Beat_Pause_Fading_Min_Delay, VU_Hueshift_Beat_Pause_Fading_Max_Delay));

			// Mid frequency also controls whether or not the color fade freezes
			if (VU_Hueshift_Beat_Pause_Fading_Off_Delay >= MathUtilities.ConvertRange (Math.Max (Audio_Mid_Intensity - VU_Hueshift_Beat_Pause_Fading_Intensity_Floor, 0), 0, 1 - VU_Hueshift_Beat_Pause_Fading_Intensity_Floor, VU_Hueshift_Beat_Pause_Fading_Min_Delay, VU_Hueshift_Beat_Pause_Fading_Max_Delay)) {
				VU_Hueshift_Beat_Pause_Fading_Off_Delay = 0;
				for (int i = 0; i < LightSystem.LIGHT_COUNT; i++) {
					Actinic_Lights_Unprocessed [i].R = ColorShift_Red;
					Actinic_Lights_Unprocessed [i].G = ColorShift_Green;
					Actinic_Lights_Unprocessed [i].B = ColorShift_Blue;
				}
			} else {
				VU_Hueshift_Beat_Pause_Fading_Off_Delay += FrameTimeElapsed;
			}

			for (int i = 0; i < (LightSystem.LIGHT_COUNT / 2); i++) {
				CurrentFrame [i].R = Actinic_Lights_Unprocessed [i].R;
				CurrentFrame [i].G = Actinic_Lights_Unprocessed [i].G;
				CurrentFrame [i].B = Actinic_Lights_Unprocessed [i].B;
				CurrentFrame [i].Brightness = Actinic_Lights_Unprocessed [i].Brightness;
			}

			int i_source = 0;
			for (int i = LightSystem.LIGHT_INDEX_MAX; i > (LightSystem.LIGHT_INDEX_MIDDLE); i--) {
				i_source = (LightSystem.LIGHT_INDEX_MAX - i);
				CurrentFrame [i].R = CurrentFrame [i_source].R;
				CurrentFrame [i].G = CurrentFrame [i_source].G;
				CurrentFrame [i].B = CurrentFrame [i_source].B;
				CurrentFrame [i].Brightness = CurrentFrame [i_source].Brightness;
			}
		}

		private void AudioMeter_Extended_Stationary_Bars ()
		{
			// Mark linger tracker as not used
			Linger_Tracker_Used = false;

			// Reduce the past set of colors
			byte reduceAmount = (byte)(4 * deviceConfig.FactorTime);
			for (int i = 0; i < LightSystem.LIGHT_COUNT; i++) {
				CurrentFrame [i].R =
					(byte)Math.Max (0, CurrentFrame [i].R - reduceAmount);
				CurrentFrame [i].G =
					(byte)Math.Max (0, CurrentFrame [i].G - reduceAmount);
				CurrentFrame [i].B =
					(byte)Math.Max (0, CurrentFrame [i].B - reduceAmount);
			}


			// Scratch to the right - - - - For the high bar, add green (ColorShift_Blue) for bass
			// For the low bar, add green (ColorShift_Red) for treble
			if (Audio_Average_Frequency_Distribution_Percentage > 0.5) {
				CustomColorShift_Red = Convert.ToByte (Math.Max (Math.Min ((((Audio_Average_Frequency_Distribution_Percentage - 0.5) * 2) * VU_Stationary_Bar_Color_Multiplier), LightSystem.Color_MAX), LightSystem.Color_MIN));
			} else {
				CustomColorShift_Red = 0;
			}


			CustomColorShift_Blue = (byte)(200 * Math.Max ((1 - (Audio_High_Intensity * 2.8)), LightSystem.Color_MIN));

			int Stationary_Bar_Low_Index = LightSystem.LIGHT_INDEX_MIDDLE; // Always starts from the center
			int Stationary_Bar_Low_Size = (int)Math.Round (Audio_Low_Intensity * VU_Stationary_Bar_Max_Length);

			int Stationary_Bar_Mid_Index = (int)MathUtilities.CapToRange ((1 - Audio_Average_Intensity) * LightSystem.LIGHT_INDEX_MIDDLE, 1, LightSystem.LIGHT_INDEX_MAX - 1);

			// High bar always starts from ends
			const int Stationary_Bar_High_Index = 0;
			// Include mid as part of the bar length
			int Stationary_Bar_High_Size = (int)Math.Round (((Audio_High_Intensity + Audio_Mid_Intensity) / 2) * VU_Stationary_Bar_Max_Length);


			//Draw the bars according to scale of VU volumes

			for (int i = Stationary_Bar_High_Index; i <= (Stationary_Bar_High_Index + Stationary_Bar_High_Size); i++) {
				if (i > 0 & i < LightSystem.LIGHT_INDEX_MAX) {
					CurrentFrame [i].R = LightSystem.Color_MAX;
					CurrentFrame [i].G = CustomColorShift_Blue;
				}
			}

			for (int i = (Stationary_Bar_Low_Index - Stationary_Bar_Low_Size); i <= Stationary_Bar_Low_Index; i++) {
				if (i > 0 & i < LightSystem.LIGHT_INDEX_MAX) {
					CurrentFrame [i].B = LightSystem.Color_MAX;
					CurrentFrame [i].G = CustomColorShift_Red;
				}
			}

			// Do the green bars last, so they override the others
			CurrentFrame [Stationary_Bar_Mid_Index].R = (byte)Math.Max (CurrentFrame [Stationary_Bar_Mid_Index].R - LightSystem.Color_MAX / 2, 0);
			CurrentFrame [Stationary_Bar_Mid_Index].G = LightSystem.Color_MAX;
			CurrentFrame [Stationary_Bar_Mid_Index].B = (byte)Math.Max (CurrentFrame [Stationary_Bar_Mid_Index].B - LightSystem.Color_MAX / 2, 0);

			double CurrentLight_VU_Max = 0;

			for (int i = 0; i < (LightSystem.LIGHT_COUNT / 2); i++) {
				if (i < AudioProcessedSnapshot.Count) {
					CurrentLight_VU_Max = AudioProcessedSnapshot [Math.Min (((LightSystem.LIGHT_COUNT / 2 - 1) - i), AudioProcessedSnapshot.Count - 1)];
				}
				// Colors set by above
				CurrentFrame [i].Brightness = (byte)Math.Max (((LightSystem.Color_MAX - VU_Stationary_Bar_Min_Brightness) * CurrentLight_VU_Max * VU_ColorShift_Brightness_Multiplier), VU_Stationary_Bar_Min_Brightness);
				// Minimum brightness to ensure bars are always visible
			}


			for (int i = 0; i < LightSystem.LIGHT_INDEX_MIDDLE; i++) {
				if (LightProcessing.Is_LED_Dark_Color (CurrentFrame, i, LightSystem.Color_VERY_DARK)) {
					if (Randomizer.RandomProvider.NextDouble () < Audio_Average_Intensity * VU_Stationary_Bar_Flicker_Chance) {
						CurrentFrame [i].R = VU_Stationary_Bar_Flicker_Primary_Color;
						CurrentFrame [i].G = VU_Stationary_Bar_Flicker_Primary_Color;
						CurrentFrame [i].B = VU_Stationary_Bar_Flicker_Primary_Color;
						CurrentFrame [i].Brightness = (byte)(VU_Stationary_Bar_Flicker_Brightness + ((LightSystem.Color_MAX - VU_Stationary_Bar_Flicker_Brightness) * Audio_Average_Intensity));
						//Strobe mode overrides the standard VU-based flickering
					}
				}
			}


			int i_source = 0;
			for (int i = LightSystem.LIGHT_INDEX_MAX; i > (LightSystem.LIGHT_INDEX_MIDDLE); i--) {
				i_source = (LightSystem.LIGHT_INDEX_MAX - i);
				CurrentFrame [i].R = CurrentFrame [i_source].R;
				CurrentFrame [i].G = CurrentFrame [i_source].G;
				CurrentFrame [i].B = CurrentFrame [i_source].B;
				CurrentFrame [i].Brightness = CurrentFrame [i_source].Brightness;
			}
		}

		private void AudioMeter_Extended_Moving_Bars ()
		{
			AudioMeter_Extended_Moving_Bars (false);
		}

		private void AudioMeter_Extended_Moving_Bars (bool EquallySpaced)
		{
			// Mark linger tracker as not used
			Linger_Tracker_Used = false;

			VU_Update_MovingBars (EquallySpaced);

			int Moving_Bar_Low_Index = (int)Math.Round (VU_Moving_Bar_Low_Position);
			int Moving_Bar_Mid_Index = (int)Math.Round (VU_Moving_Bar_Mid_Position);
			int Moving_Bar_High_Index = (int)Math.Round (VU_Moving_Bar_High_Position);

			int Moving_Bar_Low_Size = (int)Math.Round (Audio_Low_Intensity * VU_Moving_Bar_Half_Max_Length);
			int Moving_Bar_Mid_Size = (int)Math.Round (Audio_Mid_Intensity * VU_Moving_Bar_Half_Max_Length);
			int Moving_Bar_High_Size = (int)Math.Round (Audio_High_Intensity * VU_Moving_Bar_Half_Max_Length);

			for (int i = 0; i < LightSystem.LIGHT_COUNT; i++) {
				CurrentFrame [i].R = 0;
				CurrentFrame [i].G = 0;
				CurrentFrame [i].B = 0;
			}

			//Draw the bars according to scale of VU volumes

			// Add a bit of other color spreading out from center
			//  Moving_Bar_High_Index = maximum
			//  Moving_Bar_High_Index +/- Moving_Bar_High_Size = regular
			byte other_color_boost = 0;
			other_color_boost = (byte)MathUtilities.ConvertRange (Audio_High_Intensity * VU_Moving_Bar_Half_Max_Length, 0, VU_Moving_Bar_Half_Max_Length, 0, VU_Moving_Bar_Red_Other_Color_Boost);
			for (int i = (Moving_Bar_High_Index - Moving_Bar_High_Size); i <= (Moving_Bar_High_Index + Moving_Bar_High_Size); i++) {
				TrySetLight_Color_R (MathUtilities.WrapAround (i, 0, LightSystem.LIGHT_INDEX_MAX), 255);
				TrySetLight_Color_G (MathUtilities.WrapAround (i, 0, LightSystem.LIGHT_INDEX_MAX), other_color_boost);
				TrySetLight_Color_B (MathUtilities.WrapAround (i, 0, LightSystem.LIGHT_INDEX_MAX), other_color_boost);
			}
			other_color_boost = (byte)MathUtilities.ConvertRange (Audio_Mid_Intensity * VU_Moving_Bar_Half_Max_Length, 0, VU_Moving_Bar_Half_Max_Length, 0, VU_Moving_Bar_Green_Other_Color_Boost);
			for (int i = (Moving_Bar_Mid_Index - Moving_Bar_Mid_Size); i <= (Moving_Bar_Mid_Index + Moving_Bar_Mid_Size); i++) {
				TrySetLight_Color_G (MathUtilities.WrapAround (i, 0, LightSystem.LIGHT_INDEX_MAX), 255);
				TrySetLight_Color_R (MathUtilities.WrapAround (i, 0, LightSystem.LIGHT_INDEX_MAX), other_color_boost);
				TrySetLight_Color_B (MathUtilities.WrapAround (i, 0, LightSystem.LIGHT_INDEX_MAX), other_color_boost);
			}
			other_color_boost = (byte)MathUtilities.ConvertRange (Audio_Low_Intensity * VU_Moving_Bar_Half_Max_Length, 0, VU_Moving_Bar_Half_Max_Length, 0, VU_Moving_Bar_Blue_Other_Color_Boost);
			for (int i = (Moving_Bar_Low_Index - Moving_Bar_Low_Size); i <= (Moving_Bar_Low_Index + Moving_Bar_Low_Size); i++) {
				TrySetLight_Color_B (MathUtilities.WrapAround (i, 0, LightSystem.LIGHT_INDEX_MAX), 255);
				TrySetLight_Color_R (MathUtilities.WrapAround (i, 0, LightSystem.LIGHT_INDEX_MAX), other_color_boost);
				TrySetLight_Color_G (MathUtilities.WrapAround (i, 0, LightSystem.LIGHT_INDEX_MAX), other_color_boost);
			}

			double CurrentLight_VU_Max = 0;

			for (int i = 0; i < (LightSystem.LIGHT_COUNT / 2); i++) {
				if (i < AudioProcessedSnapshot.Count) {
					CurrentLight_VU_Max = AudioProcessedSnapshot [Math.Min (((LightSystem.LIGHT_COUNT / 2 - 1) - i), AudioProcessedSnapshot.Count - 1)];
				}
				CurrentFrame [i].Brightness = (byte)Math.Max (((255 - VU_Moving_Bar_Min_Brightness) * CurrentLight_VU_Max * VU_ColorShift_Brightness_Multiplier), VU_Moving_Bar_Min_Brightness);
				//Minimum brightness to ensure bars are always visible
			}

			int i_source = 0;
			for (int i = LightSystem.LIGHT_INDEX_MAX; i > (LightSystem.LIGHT_INDEX_MIDDLE); i--) {
				i_source = (LightSystem.LIGHT_INDEX_MAX - i);
				CurrentFrame [i].Brightness = CurrentFrame [i_source].Brightness;
			}


			for (int i = 0; i < LightSystem.LIGHT_COUNT; i++) {
				if (LightProcessing.Is_LED_Dark_Color (CurrentFrame, i, LightSystem.Color_VERY_DARK)) {
					if (Randomizer.RandomProvider.NextDouble () < Audio_Average_Intensity * VU_Moving_Bar_Flicker_Chance) {
						CurrentFrame [i].R = VU_Moving_Bar_Flicker_Color;
						CurrentFrame [i].G = VU_Moving_Bar_Flicker_Color;
						CurrentFrame [i].B = VU_Moving_Bar_Flicker_Color;
						CurrentFrame [i].Brightness = (byte)(VU_Moving_Bar_Flicker_Brightness + ((LightSystem.Color_MAX - VU_Moving_Bar_Flicker_Brightness) * Audio_Average_Intensity));
						//Strobe mode overrides the standard VU-based flickering
					}
				}
			}
		}

		private void AudioMeter_Extended_Solid_Color_Strobe ()
		{
			AudioMeter_Extended_Solid_Color_Strobe (ColorStrobe.Rainbow);
		}

		private void AudioMeter_Extended_Solid_Color_Strobe (ColorStrobe LightStrobeMode, bool PersistentAudioOverlay = false)
		{
			if ((LightStrobeMode == ColorStrobe.White
			    || LightStrobeMode == ColorStrobe.Rainbow
			    || LightStrobeMode == ColorStrobe.Hueshift
			    )) {
				if (Linger_Tracker_Used == false) {
					// Mark linger tracker as used
					Linger_Tracker_Used = true;
					// Clear linger tracker
					Array.Clear (Linger_Tracker, 0, Linger_Tracker.Length);
					// Clear brightness
					for (int i = 0; i < LightSystem.LIGHT_COUNT; i++) {
						CurrentFrame [i].Brightness = 0;
					}
				}
			} else {
				// Mark linger tracker as not used
				Linger_Tracker_Used = false;
			}

			switch (LightStrobeMode) {
			case ColorStrobe.White:
			case ColorStrobe.SingleWhite:
				for (int i = 0; i < LightSystem.LIGHT_COUNT; i++) {
					CurrentFrame [i].R = 255;
					CurrentFrame [i].G = 255;
					CurrentFrame [i].B = 255;
				}
				break;
			case ColorStrobe.Rainbow:
			case ColorStrobe.SingleRainbow:
				// Average intensity controls how quickly the colors fade
				trackColorChange += (
				    Scale_ColorShiftMultiplier
				    * Math.Max (1, Math.Min ((Audio_Average_Intensity * 6), LightSystem.Color_MAX))
				);
				if (trackColorChange.IntValue > 0) {
					// Shift by the integer pixel value
					AnimationUpdateColorShift (
						(byte)trackColorChange.TakeInt ());
				}
				for (int i = 0; i < LightSystem.LIGHT_COUNT; i++) {
					CurrentFrame [i].R = ColorShift_Red;
					CurrentFrame [i].G = ColorShift_Green;
					CurrentFrame [i].B = ColorShift_Blue;
				}
				break;
			case ColorStrobe.Hueshift:
				for (int i = 0; i < LightSystem.LIGHT_COUNT; i++) {
					if (LightProcessing.Is_LED_Dark_Brightness (CurrentFrame, i, LightSystem.Color_DARK)) {
						CurrentFrame [i].R = 0;
						CurrentFrame [i].G = 0;
						CurrentFrame [i].B = 0;
					}
				}
				//Assigning colors handled below
				break;
			default:
				break;
			}

			double CurrentLight_VU_Max = 0;

			int LightMaxIndex = LightSystem.LIGHT_COUNT - 1;
			if (PersistentAudioOverlay) {
				// Mirror for audio effect
				LightMaxIndex = LightSystem.LIGHT_INDEX_MIDDLE;
			}

			if (LightStrobeMode != ColorStrobe.SingleRainbow & LightStrobeMode != ColorStrobe.SingleWhite) {
				// Clear collection of modified pixels
				Array.Clear (Linger_Modified, 0, Linger_Modified.Length);

				for (int i = 0; i <= LightMaxIndex; i++) {
					if (PersistentAudioOverlay && (i < AudioProcessedSnapshot.Count)) {
						CurrentLight_VU_Max = AudioProcessedSnapshot [Math.Min (((LightSystem.LIGHT_COUNT / 2 - 1) - i), AudioProcessedSnapshot.Count - 1)];
					}
					if (Linger_Modified [i]) {
						// Ignore pixels that were already modified
						continue;
					}
					if (LightStrobeMode == ColorStrobe.Hueshift) {
						if (Linger_Tracker [i] > 0) {
							// Strobe exists, decrease strobe lifetime
							Linger_Tracker [i] -=
								FrameTimeElapsed;

							if (Linger_Tracker [i] <= 0) {
								// Strobe reached end, fade out brightness
								// (Slower fade than fading out colors, too)
								CurrentFrame [i].Brightness = 0;
							}
						} else {
							if ((Randomizer.RandomProvider.NextDouble () < Audio_Low_Intensity * VU_Solid_Color_Strobe_Hueshift_Flicker_Chance)) {
								// Blue
								GenerateStrobe (i, new Color (0, 0, 255));
							} else if ((Randomizer.RandomProvider.NextDouble () < Audio_Mid_Intensity * VU_Solid_Color_Strobe_Hueshift_Flicker_Chance)) {
								// Green
								GenerateStrobe (i, new Color (0, 255, 0));
							} else if ((Randomizer.RandomProvider.NextDouble () < Audio_High_Intensity * VU_Solid_Color_Strobe_Hueshift_Flicker_Chance)) {
								// Red
								GenerateStrobe (i, new Color (255, 0, 0));
							}
						}
					} else {
						if (Linger_Tracker [i] > 0) {
							// Strobe exists, decrease strobe lifetime
							Linger_Tracker [i] -=
								FrameTimeElapsed;

							if (Linger_Tracker [i] <= 0) {
								// Strobe reached end, fade out brightness
								// (Slower fade than fading out colors, too)
								CurrentFrame [i].Brightness = 0;
							}
						} else if ((Randomizer.RandomProvider.NextDouble () < Audio_Average_Intensity * VU_Solid_Color_Strobe_Flicker_Chance)) {
							GenerateStrobe (i, new Color (255, 255, 255));
							//Strobe mode overrides the standard VU-based flickering
						} else if (PersistentAudioOverlay && CurrentLight_VU_Max > 0.5) {
							// Set brightness to audio overlay
							CurrentFrame [i].Brightness = (byte)(255 * (CurrentLight_VU_Max - 0.5) * VU_ColorShift_Brightness_Multiplier);
						}
					}
				}

				if (PersistentAudioOverlay) {
					// Clone upper half to lower half
					int i_source = 0;
					for (int i = LightSystem.LIGHT_INDEX_MAX; i > (LightSystem.LIGHT_INDEX_MIDDLE); i--) {
						i_source = (LightSystem.LIGHT_INDEX_MAX - i);
						CurrentFrame [i].R = CurrentFrame [i_source].R;
						CurrentFrame [i].G = CurrentFrame [i_source].G;
						CurrentFrame [i].B = CurrentFrame [i_source].B;
						CurrentFrame [i].Brightness = CurrentFrame [i_source].Brightness;
					}
				}
			} else if (LightStrobeMode == ColorStrobe.SingleRainbow | LightStrobeMode == ColorStrobe.SingleWhite) {
				if (VU_Solid_Color_Single_Strobe_Off_Delay > MathUtilities.ConvertRange (Audio_Average_Intensity, 0, 1, VU_Solid_Color_Single_Strobe_Max_Delay, VU_Solid_Color_Single_Strobe_Min_Delay)) {
					VU_Solid_Color_Single_Strobe_Off_Delay = 0;
					for (int i = 0; i < LightSystem.LIGHT_COUNT; i++) {
						CurrentFrame [i].Brightness = 255;
					}
				} else {
					VU_Solid_Color_Single_Strobe_Off_Delay += FrameTimeElapsed;
					for (int i = 0; i < LightSystem.LIGHT_COUNT; i++) {
						CurrentFrame [i].Brightness = 0;
					}
				}

				// Adjust smoothing for single strobe
				if (Audio_Average_Intensity > VU_Intensity_Threshold_HEAVY) {
					// Above the threshold for heavy intensity, disable
					// smoothing
					EnableSmoothing = false;
					// To keep smoothing disabled for flashier effect, turn it
					// off all the time with 'anim fade disable'
				} else {
					// Enable smoothing, but decrease smoothing amount according
					// to intensity.  Convert from old FPS-dependent values to
					// independent values.
					EnableSmoothing = true;
					double SmoothingAmount = Math.Min (Math.Max ((1 - (Audio_Average_Intensity)), 0.1), 0.75);
					SmoothingAmount = Math.Max (SmoothingAmount - (VU_Solid_Color_Single_Strobe_Smoothing_Decrease * Audio_Average_Intensity), 0);
					SmoothingConstant = ((2 / (1 - SmoothingAmount)) - 1) * 50;
				}
			}
		}

		/// <summary>
		/// Generates and applies a strobe centered on the specified middle
		/// index.
		/// </summary>
		/// <param name="Middle">Middle of the strobe.</param>
		/// <param name="ChosenColor">Color of the strobe.</param>
		private void GenerateStrobe (int Middle, Color ChosenColor)
		{
			// Pick a duration according to inverse intensity
			double strobeDuration =
				MathUtilities.ConvertRange (
					Audio_Average_Intensity, 0, 1,
					VU_Solid_Color_Strobe_Linger_Time_Max,
					VU_Solid_Color_Strobe_Linger_Time_Min
				);

			// Pick a size according to intensity
			int strobeSize =
				(int)Math.Round (MathUtilities.ConvertRange (
					Audio_Average_Intensity, 0, 1,
					VU_Solid_Color_Strobe_Size_Min,
					VU_Solid_Color_Strobe_Size_Max
				));

			// Start halfway before current index
			int strobeStart =
				deviceConfig.ClipIndex (Middle - (strobeSize / 2));
			// End halfway past current index
			int strobeEnd =
				deviceConfig.ClipIndex (Middle + (strobeSize / 2));

			if (strobeStart == strobeEnd) {
				// Increment strobeEnd to ensure at least one pixel gets set
				strobeEnd++;
			}

			// Build a strobe along the entire array
			for (int strobeIndex = strobeStart; strobeIndex < strobeEnd; strobeIndex++) {
				// Set color (blending with existing)
				CurrentFrame [strobeIndex].Blend (
					ChosenColor, Color.BlendMode.Combine
				);
				// Set duration
				Linger_Tracker [strobeIndex] = strobeDuration;
				// Mark as modified
				Linger_Modified [strobeIndex] = true;
			}

			//Console.WriteLine (
			//	"[Generated strobe.  Middle: {0}, size: {1}, duration: {2}, " +
			//	"start: {3}, end: {4}]",
			//	Middle, strobeSize, strobeDuration, strobeStart, strobeEnd
			//);
		}

		#endregion

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

