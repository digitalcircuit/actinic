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
		private const int Strobe_Size_Maximum = 12;
		// For non-mirrored, 10
		// For mirrored, 12

		/// <summary>
		/// Minimum number of a single array of LEDs to strobe at once
		/// </summary>
		private const int Strobe_Size_Minimum = 3;

		/// <summary>
		/// Highest number of qualifying frames before a strobe will happen
		/// </summary>
		private const double Single_Strobe_Max_Delay = 10; // * 50 ms
		/// <summary>
		/// Lowest number of qualifying frames before a strobe will happen
		/// </summary>
		private const double Single_Strobe_Min_Delay = 0; // * 50 ms
		/// <summary>
		/// When this number is greater than the above, call for LEDs to strobe, then reset to zero
		/// </summary>
		private double Single_Strobe_Off_Count = 0;

		/// <summary>
		/// How many frames a strobe will last
		/// </summary>
		private const int Single_Strobe_Linger_Cycles = 2;

		/// <summary>
		/// Maximum number of times to try to place a strobe so it will not overlap an existing strobe
		/// </summary>
		private const int Single_Strobe_Max_Tries_For_Darkness = 10;

		/// <summary>
		/// List of LEDs representing the upper strobe layer of animation
		/// </summary>
		protected List<LED> CurrentFrame_Strobe = new List<LED> ();

		/// <summary>
		/// List of LEDs representing the combined animation layers, to avoid interferring with below
		/// </summary>
		protected List<LED> CurrentFrame_Combined = new List<LED> ();

		public RaveMoodReactiveAnimation (int Light_Count):base(Light_Count)
		{
			InitializeLayers ();
		}
		public RaveMoodReactiveAnimation (List<LED> PreviouslyShownFrame):base(PreviouslyShownFrame)
		{
			InitializeLayers ();
		}

		private void InitializeLayers ()
		{
			// Enable the low-freqeuncy desaturation boost modifier
			LowFrequencyDesaturatesColors = true;

			// Add the empty LEDs to the upper layer
			for (int index = 0; index < Light_Count; index++) {
				CurrentFrame_Strobe.Add (new LED (0, 0, 0, 0));
				CurrentFrame_Combined.Add (new LED (0, 0, 0, 0));
			}
		}


		public override List<LED> GetNextFrame ()
		{

			for (int i = 0; i < LightSystem.LIGHT_INDEX_MAX; i++) {
				if (CurrentFrame_Strobe [i].Brightness > (LightSystem.Brightness_MAX - Single_Strobe_Linger_Cycles)) {
					CurrentFrame_Strobe [i].Brightness -= 1;
				} else {
					CurrentFrame_Strobe [i].SetColor (LightSystem.Color_MIN,
					                                  LightSystem.Color_MIN,
					                                  LightSystem.Color_MIN,
					                                  LightSystem.Brightness_MIN);
				}
			}

			int strobe_width = 0;
			int start_index = 0;
			double converted_Audio_Average_Intensity = MathUtilities.ConvertRange (Math.Max (Audio_Average_Intensity - Strobe_Flicker_Minimum_Intensity, 0), 0, 1 - Strobe_Flicker_Minimum_Intensity, 0, 1);
			if (Single_Strobe_Off_Count > MathUtilities.ConvertRange (converted_Audio_Average_Intensity, 0, 1, Single_Strobe_Max_Delay, Single_Strobe_Min_Delay)) {
				for (int attemptNumber = 0; attemptNumber < Single_Strobe_Max_Tries_For_Darkness; attemptNumber++) {
					start_index = Randomizer.RandomProvider.Next (0, LightSystem.LIGHT_INDEX_MAX);
					if (LightProcessing.Is_LED_Dark_Brightness (CurrentFrame_Strobe, start_index, LightSystem.Color_DARK))
						break;
				}
				strobe_width = (int)(MathUtilities.ConvertRange (converted_Audio_Average_Intensity, 0, 1, Strobe_Size_Minimum, Strobe_Size_Maximum) / 2);
				for (int strobe_index = Math.Max (start_index - strobe_width, 0); strobe_index < Math.Min ((start_index + strobe_width), LightSystem.LIGHT_INDEX_MAX); strobe_index++) {
					CurrentFrame_Strobe [strobe_index].SetColor (LightSystem.Color_MAX,
					                                            LightSystem.Color_MAX,
					                                            LightSystem.Color_MAX,
					                                            LightSystem.Brightness_MAX);
				}
				//Console.WriteLine ("### off_count: {0}, conv_intensity: {1}, strobe_width: {2}", Single_Strobe_Off_Count, Math.Round (converted_Audio_Average_Intensity, 4), (strobe_width * 2));
				Single_Strobe_Off_Count = 0;
			} else if (converted_Audio_Average_Intensity > 0) {
				Single_Strobe_Off_Count ++;
			}

			// Update the lower layer as in the BeatPulse animation
			base.GetNextFrame ();

			for (int i = 0; i < CurrentFrame_Combined.Count; i++) {
				CurrentFrame_Combined [i].SetColor (0, 0, 0, 0);
			}
			LightProcessing.MergeLayerDown (CurrentFrame_Strobe, CurrentFrame_Combined);
			LightProcessing.MergeLayerDown (CurrentFrame, CurrentFrame_Combined);
			// Merge each layer into the final combined, keeping layers separate to avoid interference

			return CurrentFrame_Combined;
		}

	}
}

