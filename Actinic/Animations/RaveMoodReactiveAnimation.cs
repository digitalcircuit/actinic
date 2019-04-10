//
//  RaveMoodReactiveAnimation.cs
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

namespace Actinic.Animations
{
	public class RaveMoodReactiveAnimation:BeatPulseReactiveAnimation
	{

		/// <summary>
		/// Amount to reduce high-frequency intensity so as to not strobe below this average audio intensity
		/// </summary>
		private const double Strobe_Flicker_Minimum_Intensity = 0.67;

		/// <summary>
		/// Maximum number of a single array of LEDs to strobe at once
		/// </summary>
		protected int Strobe_Size_Maximum {
			get {
				// Take up at most 0.24 of the strand
				return (int)(0.24 * deviceConfig.FactorScaledSize);
			}
		}

		/// <summary>
		/// Minimum number of a single array of LEDs to strobe at once
		/// </summary>
		protected int Strobe_Size_Minimum {
			get {
				// Take up at least 0.06 of the strand
				return (int)(0.06 * deviceConfig.FactorScaledSize);
			}
		}

		/// <summary>
		/// Gets the maximum number of qualifying frames before a strobe will
		/// happen
		/// </summary>
		/// <value>The maximum number of qualifying frames before strobe.</value>
		protected double Single_Strobe_Max_Delay {
			get {
				// 0.5 seconds
				// This represents number of frames, not amount to change
				return (500 / deviceConfig.FactorTime);
			}
		}

		/// <summary>
		/// Gets the minimum number of qualifying frames before a strobe will
		/// happen
		/// </summary>
		/// <value>The minimum number of qualifying frames before strobe.</value>
		protected double Single_Strobe_Min_Delay {
			get {
				// 0 seconds
				return 0;
			}
		}

		/// <summary>
		/// When this number is greater than the above, call for LEDs to strobe,
		/// then reset to zero.
		/// </summary>
		private double Single_Strobe_Off_Count = 0;

		/// <summary>
		/// The duration before a strobe effect will fade quickly.
		/// </summary>
		private const int Strobe_Lifetime_Linger = 50;

		/// <summary>
		/// The minimum duration of a strobe effect after lingering.  See
		/// <see cref="Strobe_Lifetime_Linger"/>.
		/// </summary>
		private const int Strobe_Lifetime_Fade_Min = 10;

		/// <summary>
		/// The maximum duration of a strobe effect after lingering.  See
		/// <see cref="Strobe_Lifetime_Linger"/>.
		/// </summary>
		private const int Strobe_Lifetime_Fade_Max = 50;

		/// <summary>
		/// Gets the minimum brightness at which a strobe will shift from fading
		/// out slowly (1 unit per frame) to quickly
		/// </summary>
		/// <value>The minimum brightness before a strobe fades out quickly.</value>
		protected byte Single_Strobe_Linger_Min_Brightness {
			get {
				// This represents number of frames, not amount to change
				return (byte)Math.Min (LightSystem.Brightness_MAX,
					LightSystem.Brightness_MAX - (Strobe_Lifetime_Linger / deviceConfig.FactorTime)
				);
			}
		}

		/// <summary>
		/// Gets the minimum amount to fade out a strobe per frame
		/// </summary>
		/// <value>The minimum amount to fade out a strobe per frame.</value>
		protected byte Single_Strobe_Fade_MinRate {
			get {
				// Fade out to minimum brightness by the end of the strobe
				// (Max - end goal) / (number of frames for strobe)
				return (byte)Math.Min (
					LightSystem.Brightness_MAX,
					Math.Max (1,
						(Single_Strobe_Linger_Min_Brightness)
						* (deviceConfig.FactorTime / Strobe_Lifetime_Fade_Min)
					)
				);
			}
		}

		/// <summary>
		/// Gets the maximum amount to fade out a strobe per frame
		/// </summary>
		/// <value>The maximum amount to fade out a strobe per frame.</value>
		protected byte Single_Strobe_Fade_MaxRate {
			get {
				// Fade out to minimum brightness by the end of the strobe
				// (Max - end goal) / (number of frames for strobe)
				return (byte)Math.Min (
					LightSystem.Brightness_MAX,
					Math.Max (1,
						(Single_Strobe_Linger_Min_Brightness)
						* (deviceConfig.FactorTime / Strobe_Lifetime_Fade_Max)
					)
				);
			}
		}

		/// <summary>
		/// The minimum brightness for a strobe to be considered visible.
		/// </summary>
		private const byte Strobe_Min_Visible =
			LightSystem.Brightness_MIN_VISIBLE;

		/// <summary>
		/// Maximum number of times to try to place a strobe so it will not overlap an existing strobe
		/// </summary>
		private const int Single_Strobe_Max_Tries_For_Darkness = 10;

		/// <summary>
		/// List of LEDs representing the upper strobe layer of animation
		/// </summary>
		protected Layer CurrentFrame_Strobe;

		/// <summary>
		/// List of LEDs representing the combined animation layers, to avoid interferring with below
		/// </summary>
		protected Layer CurrentFrame_Combined;

		public RaveMoodReactiveAnimation (
			ReadOnlyDeviceConfiguration Configuration) : base (Configuration)
		{
			InitializeLayers ();
		}

		public RaveMoodReactiveAnimation (
			ReadOnlyDeviceConfiguration Configuration,
			Layer PreviouslyShownFrame)
			: base (Configuration, PreviouslyShownFrame)
		{
			InitializeLayers ();
		}

		private void InitializeLayers ()
		{
			// Enable the low-freqeuncy desaturation boost modifier
			LowFrequencyDesaturatesColors = true;

			// Add the empty LEDs to the upper layers
			CurrentFrame_Strobe = new Layer (
				Light_Count, Color.BlendMode.Combine, Color.Transparent
			);
			CurrentFrame_Combined = CurrentFrame_Strobe.Clone ();
		}


		public override Layer GetNextFrame ()
		{
			// Fade faster at higher intensities
			byte fadeBeginBrightness = Single_Strobe_Linger_Min_Brightness;
			byte fadeRateAdjusted =
				(byte)MathUtilities.ConvertRange (
					Math.Max (Audio_Average_Intensity - Strobe_Flicker_Minimum_Intensity, 0),
					0, 1 - Strobe_Flicker_Minimum_Intensity,
					Single_Strobe_Fade_MaxRate, Single_Strobe_Fade_MinRate
				);
			for (int i = 0; i < LightSystem.LIGHT_INDEX_MAX; i++) {
				if (CurrentFrame_Strobe [i].Brightness > fadeBeginBrightness) {
					CurrentFrame_Strobe [i].Brightness -= 1;
				} else if (CurrentFrame_Strobe [i].Brightness > Strobe_Min_Visible) {
					CurrentFrame_Strobe [i].Brightness = (byte)Math.Max (
						0, CurrentFrame_Strobe [i].Brightness - fadeRateAdjusted
					);
				} else {
					CurrentFrame_Strobe [i].SetColor (
						LightSystem.Color_MIN,
						LightSystem.Color_MIN,
						LightSystem.Color_MIN,
						LightSystem.Brightness_MIN
					);
				}
			}

			int strobe_width = 0;
			int start_index = 0;
			double converted_Audio_Average_Intensity = MathUtilities.ConvertRange (Math.Max (Audio_Average_Intensity - Strobe_Flicker_Minimum_Intensity, 0), 0, 1 - Strobe_Flicker_Minimum_Intensity, 0, 1);
			if (Single_Strobe_Off_Count > MathUtilities.ConvertRange (converted_Audio_Average_Intensity, 0, 1, Single_Strobe_Max_Delay, Single_Strobe_Min_Delay)) {
				for (int attemptNumber = 0; attemptNumber < Single_Strobe_Max_Tries_For_Darkness; attemptNumber++) {
					start_index = Randomizer.RandomProvider.Next (0, LightSystem.LIGHT_INDEX_MAX);
					if (LightProcessing.Is_LED_Dark_Brightness (CurrentFrame_Strobe, start_index, Strobe_Min_Visible))
						break;
				}
				strobe_width = (int)(MathUtilities.ConvertRange (converted_Audio_Average_Intensity, 0, 1, Strobe_Size_Minimum, Strobe_Size_Maximum) / 2);
				for (int strobe_index = Math.Max (start_index - strobe_width, 0); strobe_index < Math.Min ((start_index + strobe_width), LightSystem.LIGHT_INDEX_MAX); strobe_index++) {
					CurrentFrame_Strobe [strobe_index].SetColor (
						LightSystem.Color_MAX,
						LightSystem.Color_MAX,
						LightSystem.Color_MAX,
						LightSystem.Brightness_MAX
					);
				}
				//Console.WriteLine ("### off_count: {0}, conv_intensity: {1}, strobe_width: {2}", Single_Strobe_Off_Count, Math.Round (converted_Audio_Average_Intensity, 4), (strobe_width * 2));
				Single_Strobe_Off_Count = 0;
			} else if (converted_Audio_Average_Intensity > 0) {
				Single_Strobe_Off_Count++;
			}

			// Update the lower layer as in the BeatPulse animation
			base.GetNextFrame ();

			// Clear the combined layer
			CurrentFrame_Combined.Fill (Color.Transparent);
			// Merge each layer into the final combined, keeping layers separate
			// to avoid interference.  Force simple blending with Opacity of 1.
			CurrentFrame_Combined.Blend (CurrentFrame_Strobe, 1);
			CurrentFrame_Combined.Blend (CurrentFrame, 1);

			return CurrentFrame_Combined;
		}

	}
}

