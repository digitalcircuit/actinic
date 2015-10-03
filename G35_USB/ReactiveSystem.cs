//
//  ReactiveSystem.cs
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
	public class ReactiveSystem
	{
		public ReactiveSystem ()
		{
		}

		#region Audio Volume Processing

		/// <summary>
		/// Rate at which frequencies are bunched together for higher frequences
		/// </summary>
		/// <value>Decimal below 1</value>
		public static double Audio_Frequency_Scale_Multiplier {
			get;
			private set;
		}

		private static double audio_Frequency_Scale_Start = 0.000000000001;
		// Originally 0.0001, then 0.00001 - hard to choose, just try what looks best with 'vu set_frequency_start'
		/// <summary>
		/// Starting value for the frequency scaling multiplier, larger shifts bars more towards higher frequencies
		/// </summary>
		/// <value>Decimal below 1</value>
		public static double Audio_Frequency_Scale_Start {
			get {
				return audio_Frequency_Scale_Start;
			}
			set {
				audio_Frequency_Scale_Start = value;
				Audio_Frequency_Scale_Multiplier = 0;
			}
		}

		/// <summary>
		/// [Experimental] Prefer a linear method for grouping frequencies rather than exponential
		/// </summary>
		/// <value>If <c>true</c>, using linear grouping of frequencies; otherwise, frequencies are handled in an exponential manner.</value>
		public static bool Audio_Frequency_Scale_Linear {
			get;
			set;
		}


		// Usually you won't need to trim the start of frequency percentage
		private static double audio_Volume_Low_Percentage = 0;
		/// <summary>
		/// Percentage of frequencies bundled together as 'low', larger (up to 1) ignores more of the lowest
		/// </summary>
		/// <value>Decimal from the range of 0 to 1</value>
		public static double Audio_Volume_Low_Percentage {
			get {
				return audio_Volume_Low_Percentage;
			}
			set {
				audio_Volume_Low_Percentage = value;
			}
		}

		private static double audio_Volume_Mid_Percentage = 0.005;
		/// <summary>
		/// Percentage of frequencies bundled together as 'mid', larger (up to 1) collects more
		/// </summary>
		/// <value>Decimal from the range of 0 to 1</value>
		public static double Audio_Volume_Mid_Percentage {
			get {
				return audio_Volume_Mid_Percentage;
			}
			set {
				audio_Volume_Mid_Percentage = value;
			}
		}

		private static double audio_Volume_High_Percentage = 0.92;
		/// <summary>
		/// Percentage of frequencies bundled together as 'high', larger (up to 1) collects more
		/// </summary>
		/// <value>Decimal from the range of 0 to 1</value>
		public static double Audio_Volume_High_Percentage {
			get {
				return audio_Volume_High_Percentage;
			}
			set {
				audio_Volume_High_Percentage = value;
			}
		}

		// The above SHOULD add up to 1, i.e. 100%
		// Defaults were chosen from experimentation with 'vu set_low', 'vu set_mid', and 'vu set_high'


		/// <summary>
		/// Brute-force the frequency-step multiplier, used to determine how to bundle higher-frequencies together
		/// </summary>
		/// <param name="Current_VU_Volumes">Unprocessed audio volume snapshot.</param>
		/// <param name="MirrorMode">If set to <c>true</c>, number of VU meters are cut in half to allow for mirroring of lights.</param>
		public static void Processing_CalculateFrequencyStepMultiplier (List<double> Current_Audio_Volumes, bool MirrorMode)
		{
			int EmergencyLoop_ExitCount = 0;

			int Current_VU_Volumes_Count = Current_Audio_Volumes.Count;
			double NextStep = 0;
			double StepMultiplier_Attempt = 0;
			int DesiredVolumesCount = (MirrorMode ? (LightSystem.LIGHT_COUNT / 2) : (LightSystem.LIGHT_COUNT));
			int VU_Volumes_WouldProcess = (DesiredVolumesCount) + 1337;
			while (VU_Volumes_WouldProcess > DesiredVolumesCount) {
				VU_Volumes_WouldProcess = 0;
				StepMultiplier_Attempt += 0.0001;
				NextStep = Audio_Frequency_Scale_Start;
				for (int i = 0; i < Current_VU_Volumes_Count; i++) {
					if (i >= NextStep) {
						VU_Volumes_WouldProcess += 1;
						NextStep = NextStep * StepMultiplier_Attempt;
					}
				}
				if (EmergencyLoop_ExitCount >= 1000000) {
					Console.WriteLine ("Unable to calculate frequency step multiplier, current guess = " + StepMultiplier_Attempt.ToString ());
					StepMultiplier_Attempt = 1.337;
					break;
				} else {
					EmergencyLoop_ExitCount++;
				}
			}
			if (VU_Volumes_WouldProcess < (LightSystem.LIGHT_COUNT / 2)) {
				StepMultiplier_Attempt -= 0.0001;
			}
			Audio_Frequency_Scale_Multiplier = StepMultiplier_Attempt;
		}

		#endregion

		#region Display

		/// <summary>
		/// If <c>true</c>, show a break-down of audio frequencies and the current animation variables
		/// </summary>
		public static bool Processing_Show_Analysis = false;
		/// <summary>
		/// If <c>true</c>, show bars representing audio intensities
		/// </summary>
		public static bool Processing_Show_Variables = false;
		/// <summary>
		/// If <c>true</c>, show bars representing audio frequencies
		/// </summary>
		public static bool Processing_Show_Frequencies = false;

		/// <summary>
		/// If <c>true</c>, any printed output is limited; otherwise, use the full width of the console
		/// </summary>
		public static bool Processing_Limit_Display = true;

		/// <summary>
		/// Width of display when limiting the size
		/// </summary>
		private const int Processing_Limited_Display_Width = 80;

		/// <summary>
		/// If requested, prints information about audio processing to the console
		/// </summary>
		/// <param name="CurrentAnimation">Current abstract animation to retrieve audio information from.</param>
		public static void PrintAudioInformationToConsole (AbstractReactiveAnimation CurrentAnimation)
		{
			string VU_Intensity_Delta_Sign = " ";
			if (CurrentAnimation.Audio_Delta_Intensity < 0) {
				VU_Intensity_Delta_Sign = "-";
			}
			if (Processing_Show_Analysis) {
				try {
					Console.SetCursorPosition (0, 0);
				} catch (ArgumentOutOfRangeException) {
					// Stupid Mono System.Console bug...
				}
				string animationType = CurrentAnimation.GetType ().Name.Replace ("ReactiveAnimation", "").Trim ();
				if (animationType == "") {
					animationType = "Unknown";
				}
				if (CurrentAnimation is LegacyReactiveAnimation) {
					animationType = String.Format ("Legacy: {0}", (CurrentAnimation as LegacyReactiveAnimation).VU_Selected_Mode.ToString ());
				}
				Console.WriteLine ("--------------------- [{0}] Channels: {1} ------------------------", animationType, CurrentAnimation.AudioProcessedSnapshot.Count);
				Console.WriteLine ("Intensity: " + Math.Round (CurrentAnimation.Audio_Realtime_Intensity, 3).ToString ().PadRight (5) + " (avg: " + Math.Round (CurrentAnimation.Audio_Average_Intensity, 3).ToString ().PadRight (5) + ", delta: " + VU_Intensity_Delta_Sign + Math.Abs (Math.Round (CurrentAnimation.Audio_Delta_Intensity, 3)).ToString ().PadRight (5) + ")  Smoothing: " + Math.Round (CurrentAnimation.SmoothingAmount, 3).ToString ().PadRight (5) + "  Color fade: " + CurrentAnimation.ColorShift_Amount.ToString ().PadRight (4));
				Console.WriteLine ("Freq. distribution: " + Math.Round ((double)CurrentAnimation.Audio_Frequency_Distribution_Percentage, 3).ToString ().PadRight (5) + "  (avg: " + Math.Round ((double)CurrentAnimation.Audio_Average_Frequency_Distribution_Percentage, 3).ToString ().PadRight (5) + ")  Color boost, high: " + CurrentAnimation.ColorShift_HighBoost.ToString ().PadRight (4) + " - low: " + CurrentAnimation.ColorShift_LowBoost.ToString ().PadRight (4));

				int availableWidth = (Processing_Limit_Display ? Processing_Limited_Display_Width : Console.WindowWidth);
				// Limit the maximum output according to whether or not limited display width is requested
				int calculatedBarWidth = (availableWidth) - (5 + 2);
				// Two for padding, a few for the numbers at the end.  Only one bar, no need to divide it
				if (calculatedBarWidth < 10)
					return;

				for (int i = 0; i < CurrentAnimation.AudioProcessedSnapshot.Count; i++) {
					Console.WriteLine (MathUtilities.GenerateMeterBar (CurrentAnimation.AudioProcessedSnapshot [i], 0, 1, calculatedBarWidth, true));
					//Console.WriteLine (new String ('=', (int)MathUtilities.ConvertRange (CurrentAnimation.AudioProcessedSnapshot[i], 0, 1, 0, 50)).PadRight (50, ' ') + "|  (" + Math.Round (CurrentAnimation.AudioProcessedSnapshot[i], 3).ToString ().PadRight (5, ' ')+ ")");
				}
			}
			if (Processing_Show_Variables) {
				PrintAudioVariables (CurrentAnimation);
			}
			if (Processing_Show_Frequencies) {
				PrintAudioFrequencies (CurrentAnimation);
			}
		}

		private static void PrintAudioVariables (AbstractReactiveAnimation CurrentAnimation)
		{
			int availableWidth = (Processing_Limit_Display ? Processing_Limited_Display_Width : Console.WindowWidth);
			// Limit the maximum output according to whether or not limited display width is requested
			int calculatedBarWidth = (int)(availableWidth / 4) - 2;
			if (calculatedBarWidth < 10)
				return;
			// Divided by the number of meter bars displayed
			Console.WriteLine (MathUtilities.GenerateMeterBar (CurrentAnimation.Audio_Realtime_Intensity, 0, 1, calculatedBarWidth, true) + " " +
			                   MathUtilities.GenerateMeterBar (CurrentAnimation.Audio_Delta_Intensity, -1, 1, calculatedBarWidth, true) + " " + 
			                   MathUtilities.GenerateMeterBar (MathUtilities.ConvertRange (CurrentAnimation.Audio_Frequency_Distribution_Percentage, 0, 1, -1, 1), -1, 1, calculatedBarWidth, true) + " " + 
			                   MathUtilities.GenerateMeterBar (CurrentAnimation.Audio_Delta_Frequency_Distribution_Percentage, -1, 1, calculatedBarWidth, true)
			                   );
		}

		private static void PrintAudioFrequencies (AbstractReactiveAnimation CurrentAnimation)
		{
			int availableWidth = (Processing_Limit_Display ? Processing_Limited_Display_Width : Console.WindowWidth);
			// Limit the maximum output according to whether or not limited display width is requested
			int calculatedBarWidth = (int)(availableWidth / 3) - 2;
			if (calculatedBarWidth < 10)
				return;
			// Divided by the number of meter bars displayed
			Console.WriteLine (MathUtilities.GenerateMeterBar (CurrentAnimation.Audio_Low_Intensity, 0, 1, calculatedBarWidth, true) + " " +
			                   MathUtilities.GenerateMeterBar (CurrentAnimation.Audio_Mid_Intensity, 0, 1, calculatedBarWidth, true) + " " + 
			                   MathUtilities.GenerateMeterBar (CurrentAnimation.Audio_High_Intensity, 0, 1, calculatedBarWidth, true)
			                   );
		}

		#endregion

	}
}

