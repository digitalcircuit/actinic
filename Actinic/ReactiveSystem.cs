//
//  ReactiveSystem.cs
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

// Animation management
using Actinic.Animations;

namespace Actinic
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
		// Originally 0.0001, then 0.00001 - hard to choose, just try what
		// looks best with 'vu set_frequency_start'.

		/// <summary>
		/// Starting value for the frequency scaling multiplier, larger shifts
		/// bars more towards higher frequencies.
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
		/// [Experimental] Prefer a linear method for grouping frequencies
		/// rather than exponential.
		/// </summary>
		/// <value>If <c>true</c>, using linear grouping of frequencies; otherwise, frequencies are handled in an exponential manner.</value>
		public static bool Audio_Frequency_Scale_Linear {
			get;
			set;
		}


		// Usually you won't need to trim the start of frequency percentage
		private static double audio_Volume_Low_Percentage = 0;

		/// <summary>
		/// Percentage of frequencies bundled together as 'low', larger
		/// (up to 1) ignores more of the lowest.
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
		/// Percentage of frequencies bundled together as 'mid', larger
		/// (up to 1) collects more.
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
		/// Percentage of frequencies bundled together as 'high', larger
		/// (up to 1) collects more.
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
		/// Brute-force the frequency-step multiplier, used to determine how to
		/// bundle higher-frequencies together.
		/// </summary>
		/// <param name="Current_Audio_Volumes">Unprocessed audio volume snapshot.</param>
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

		/// <summary>
		/// Reset the frequency-step multiplier to account for the current
		/// lights, e.g. if the number of lights change.
		/// </summary>
		public static void Processing_ClearFrequencyStepMultiplier ()
		{
			Audio_Frequency_Scale_Multiplier = 1;
		}

		#endregion

		#region Display

		/// <summary>
		/// If <c>true</c>, show a break-down of audio frequencies and the
		/// current animation variables.
		/// </summary>
		public static bool Processing_Show_Analysis = false;
		/// <summary>
		/// If <c>true</c>, show bars representing audio intensities.
		/// </summary>
		public static bool Processing_Show_Variables = false;
		/// <summary>
		/// If <c>true</c>, show bars representing audio frequencies.
		/// </summary>
		public static bool Processing_Show_Frequencies = false;

		/// <summary>
		/// If <c>true</c>, any printed output is limited; otherwise, use the
		/// full width of the console.
		/// </summary>
		public static bool Processing_Limit_Display = true;

		/// <summary>
		/// The last known width of the display console.
		/// </summary>
		private static int Processing_Console_Width = -1;
		/// <summary>
		/// The last known height of the display console.
		/// </summary>
		private static int Processing_Console_Height = -1;
		/// <summary>
		/// The last state of whether or not the console width was limited.
		/// </summary>
		private static bool Processing_Console_WidthWasLimited = true;

		/// <summary>
		/// Width of display when limiting the size.
		/// </summary>
		private const int Processing_Limited_Display_Width = 80;

		/// <summary>
		/// If requested, prints information about audio processing to the
		/// console.
		/// </summary>
		/// <param name="CurrentAnimation">Current abstract animation to retrieve audio information from.</param>
		public static void PrintAudioInformationToConsole (AbstractReactiveAnimation CurrentAnimation)
		{
			string VU_Intensity_Delta_Sign = " ";
			if (CurrentAnimation.Audio_Delta_Intensity < 0) {
				VU_Intensity_Delta_Sign = "-";
			}
			// Clear console if dimensions change and output is shown
			if (Processing_Show_Analysis
			    || Processing_Show_Variables
			    || Processing_Show_Frequencies) {
				// Output is shown, check if dimensions changed
				if ((Processing_Console_Width != Console.WindowWidth)
				    || (Processing_Console_Height != Console.WindowHeight)
				    || (Processing_Console_WidthWasLimited != Processing_Limit_Display)) {
					// Track dimensions, clear console
					Processing_Console_Width = Console.WindowWidth;
					Processing_Console_Height = Console.WindowHeight;
					Processing_Console_WidthWasLimited = Processing_Limit_Display;
					Console.Clear ();
				}
			}
			if (Processing_Show_Analysis) {
				try {
					Console.SetCursorPosition (0, 0);
				} catch (ArgumentOutOfRangeException) {
					// Stupid Mono System.Console bug...
				}
				string animationType;
				if (CurrentAnimation is LegacyReactiveAnimation) {
					animationType = String.Format (
						"Legacy: {0}",
						(CurrentAnimation as LegacyReactiveAnimation).VU_Selected_Mode.ToString ()
					);
				} else {
					animationType = CurrentAnimation.GetType ().Name.Replace ("ReactiveAnimation", "").Trim ();
				}
				if (animationType == "") {
					animationType = "Unknown";
				}

				int availableWidth = (Processing_Limit_Display ? Processing_Limited_Display_Width : Console.WindowWidth);
				// Limit the maximum output according to whether or not limited display width is requested

				// CurrentAnimation.AudioProcessedSnapshot is being updated by
				// the audio input thread.  Acquire a lock to avoid having the
				// contents changed out from underneath the display, preventing
				// inconsistencies or (worse) crashes due to race conditions.
				//
				// As the meter display is not usually enabled, the additional
				// overhead should be fine to overlook.
				List<double> lastAudioSnapshot;
				lock (CurrentAnimation.AudioProcessedSnapshot) {
					// Duplicate the snapshot into a local, non-reference copy
					lastAudioSnapshot =
						new List<double> (CurrentAnimation.AudioProcessedSnapshot);
				}

				// Generate header
				string animationHeader =
					String.Format (
						" [{0}] Channels: {1} ",
						animationType,
						lastAudioSnapshot.Count
					);

				// [Line 1]
				// Determine padding, accounting for header length to try to
				// center the result
				int paddingLeft =
					(int)(availableWidth / 2) + (animationHeader.Length / 2);
				Console.WriteLine (
					animationHeader.PadLeft (paddingLeft, '-').PadRight (availableWidth, '-')
				);
				// [Line 2]
				Console.WriteLine (
					"Intensity: {0,5:F3} (avg: {1,5:F3}, delta: {2}{3,5:F3}) Smoothing: {4,7:F3}",
					CurrentAnimation.Audio_Realtime_Intensity,
					CurrentAnimation.Audio_Average_Intensity,
					VU_Intensity_Delta_Sign,
					Math.Abs (CurrentAnimation.Audio_Delta_Intensity),
					CurrentAnimation.SmoothingConstant
				);
				// [Line 3]
				Console.WriteLine (
					"Freq. distribution: {0,-5:F3} (avg: {1,-5:F3})",
					CurrentAnimation.Audio_Frequency_Distribution_Percentage,
					CurrentAnimation.Audio_Average_Frequency_Distribution_Percentage
				);

				// [Line 4+]
				int calculatedBarWidth = (availableWidth) - (5 + 2);
				// Two for padding, a few for the numbers at the end.  Only one bar, no need to divide it
				if (calculatedBarWidth < 10)
					return;

				// How much console height is left?  Try to avoid scrolling.
				int remainingHeight =
					Console.WindowHeight - Console.CursorTop - 1;
				// Reserve 1 line for newline at end

				// Require at least 1 bar
				if (remainingHeight < 1) {
					return;
				}

				// How many snapshots are represented by each meter bar
				// Round up to the nearest integer
				int snapshotsPerMeterBar =
					(int)Math.Round (
						(double)(lastAudioSnapshot.Count / remainingHeight)
					);

				// Can't show a fraction of a meter
				if (snapshotsPerMeterBar < 1) {
					snapshotsPerMeterBar = 1;
				}

				int snapshotIndex = 0;
				for (int line = 0; line < remainingHeight; line++) {
					int snapshotsGrouped = 0;
					double snapshotMax = 0;

					// Add together snapshots
					// Don't exceed maximum
					while ((snapshotsGrouped < snapshotsPerMeterBar)
					       && (snapshotIndex < lastAudioSnapshot.Count)) {
						// Add another snapshot
						//
						// One might guess that averaging makes the most sense.
						// However, showing the highest value results in what
						// appears closer to one's expectations.
						//snapshotAverage +=
						//	lastAudioSnapshot [snapshotIndex];
						//
						// Find the maximum
						snapshotMax =
							Math.Max (snapshotMax, lastAudioSnapshot [snapshotIndex]);
						++snapshotIndex;
						++snapshotsGrouped;

						if (snapshotIndex >= lastAudioSnapshot.Count) {
							// No more snapshots, exit
							break;
						}
					}

					if (snapshotsGrouped > 0) {
						// Find the average of all values
						//snapshotAverage /= snapshotsGrouped;

						// Show result
						Console.WriteLine (
							MathUtilities.GenerateMeterBar (
								snapshotMax,
								0, 1, calculatedBarWidth, true
							)
						);
					}
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
			Console.WriteLine (
				MathUtilities.GenerateMeterBar (CurrentAnimation.Audio_Realtime_Intensity, 0, 1, calculatedBarWidth, true) + " " +
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
			Console.WriteLine (
				MathUtilities.GenerateMeterBar (CurrentAnimation.Audio_Low_Intensity, 0, 1, calculatedBarWidth, true) + " " +
				MathUtilities.GenerateMeterBar (CurrentAnimation.Audio_Mid_Intensity, 0, 1, calculatedBarWidth, true) + " " +
				MathUtilities.GenerateMeterBar (CurrentAnimation.Audio_High_Intensity, 0, 1, calculatedBarWidth, true)
			);
		}

		#endregion

	}
}

