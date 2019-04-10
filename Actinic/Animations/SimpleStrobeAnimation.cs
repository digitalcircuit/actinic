//
//  SimpleStrobeAnimation.cs
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

using FoxSoft.Utilities;

namespace Actinic.Animations
{
	public class SimpleStrobeAnimation:AbstractAnimation
	{
		private Random RandomGenerator;

		public enum StrobeMode
		{
			/// <summary>
			/// Individual pixels strobing white.
			/// </summary>
			White,
			/// <summary>
			/// Individual pixels strobing in random colors.
			/// </summary>
			Color,
			/// <summary>
			/// All pixels strobing white in unison (traditional strobe).
			/// </summary>
			SingleWhite,
			/// <summary>
			/// All pixels strobing a random color in unison.
			/// </summary>
			SingleColor,
			/// <summary>
			/// Slow fading individual pixels strobing in firefly-like patterns.
			/// </summary>
			Fireflies,
			/// <summary>
			/// Individual pixels alternating between rain-like colors.
			/// </summary>
			Rain,
			/// <summary>
			/// Rain, plus occasional random "lightning" strikes with chains.
			/// </summary>
			Thunderstorm,
		}

		#region Properties - Individual Strobe

		/// <summary>
		/// The duration in milliseconds for an individual strobe to fade.
		/// </summary>
		/// <value>Duration in milliseconds for an individual strobe to fade.</value>
		private int Mode_IndividualStrobe_Linger_Time {
			get {
				switch (AnimationStyle) {
				case Style.Bright:
					return 25;
				case Style.Moderate:
					return 50;
				case Style.Soft:
					return 400;
				default:
					throw new NotSupportedException (
						"Unsupported Style type for AnimationStyle");
				}
			}
		}

		/// <summary>
		/// Gets the probability an individual white pixel starts strobing for
		/// each frame
		/// </summary>
		/// <value>Probability an individual white pixel starts strobing.</value>
		private double Mode_White_Strobe_Chance {
			get {
				// This is called per pixel, per frame, so scale by size and
				// time.  Every millisecond, there's a 0.04 (4%) probability
				// that a new strobe will be created.
				return (
				    0.04
				    * deviceConfig.FactorTime
				    * (1 / deviceConfig.FactorScaledSize)
				);
			}
		}

		/// <summary>
		/// Gets the probability an individual color pixel starts strobing for
		/// each frame
		/// </summary>
		/// <value>Probability an individual color pixel starts strobing.</value>
		private double Mode_Color_Strobe_Chance {
			get {
				// This is called per pixel, per frame, so scale by size and
				// time.  Every millisecond, there's a 0.06 (6%) probability
				// that a new strobe will be created.
				return (
				    0.06
				    * deviceConfig.FactorTime
				    * (1 / deviceConfig.FactorScaledSize)
				);
			}
		}

		#endregion

		#region Properties - Single Strobe

		/// <summary>
		/// The duration in milliseconds a single (full) strobe will stay on.
		/// </summary>
		private const int Mode_SingleStrobe_Linger_Time_On = 1;

		/// <summary>
		/// The duration in milliseconds a single (full) strobe will stay off.
		/// </summary>
		/// <value>Duration in milliseconds a single strobe stays off.</value>
		private int Mode_SingleStrobe_Linger_Time_Off {
			get {
				switch (AnimationStyle) {
				case Style.Bright:
					// Very fast
					return 10;
				case Style.Moderate:
					// Average
					return 50;
				case Style.Soft:
					// Slower
					return 150;
				default:
					throw new NotSupportedException (
						"Unsupported Style type for AnimationStyle");
				}
			}
		}

		#endregion

		#region Properties - Fireflies

		/// <summary>
		/// Gets the probability an individual firefly pixel starts glowing for
		/// each frame
		/// </summary>
		/// <value>Probability an individual firefly pixel starts glowing.</value>
		private double Mode_Fireflies_Glow_Chance {
			get {
				// This is called per pixel, per frame, so scale by size and
				// time.  Every millisecond, there's a 0.02 (2%) probability
				// that a new firefly will be created.
				return (
				    0.02
				    * deviceConfig.FactorTime
				    * (1 / deviceConfig.FactorScaledSize)
				);
			}
		}

		/// <summary>
		/// The minimum duration in milliseconds before a firefly will fade.
		/// </summary>
		private const int Mode_Fireflies_Linger_Time_Min = 350;

		/// <summary>
		/// The maximum duration in milliseconds before a firefly will fade.
		/// </summary>
		private const int Mode_Fireflies_Linger_Time_Max = 750;

		#endregion

		#region Properties - Rain

		/// <summary>
		/// Gets the probability an individual rain pixel updates for each frame
		/// </summary>
		/// <value>Probability an individual rain pixel updates.</value>
		private double Mode_Rain_PitterPatter_Chance {
			get {
				// This is called per pixel, per frame, so scale by size and
				// time.  Every millisecond, there's a 0.02 (2%) probability
				// that every rain particle within a meter of pixels will be
				// updated.
				//
				// The more dense the pixels (greater pixels per meter), the
				// more rain activity that will occur.  This accounts for
				// wider-spaced pixels having a more perceptible impact on
				// average illumination of a room.  When closer together, the
				// pixels tend to average each other out.
				return (
				    0.02
				    * deviceConfig.FactorTime
				    * (1 / deviceConfig.FactorScaledSize)
				    * deviceConfig.FactorFixedSize
				);
			}
		}

		#endregion

		#region Properties - Thunderstorm

		private readonly Color Mode_Thunderstorm_Strike_Color =
			Color.Named ["white"];

		/// <summary>
		/// The minimum size in pixels for a lightning strike.
		/// </summary>
		private double Mode_Thunderstorm_Strike_Size_Min {
			get {
				// At minimum, a lightning strike can take up either 1 pixel,
				// or 1/75th of the strand
				return Math.Max (1,
					(1 / 75.0) * deviceConfig.FactorScaledSize
				);
			}
		}

		/// <summary>
		/// The maximum size in pixels for a lightning strike.
		/// </summary>
		private double Mode_Thunderstorm_Strike_Size_Max {
			get {
				// At maximum, a lightning strike can take up either 1 pixel,
				// or 1/30th of the strand
				return Math.Max (1,
					(1 / 25.0) * deviceConfig.FactorScaledSize
				);
			}
		}

		/// <summary>
		/// The minimum duration in milliseconds before a lightning strike will
		/// end.
		/// </summary>
		private const int Mode_Thunderstorm_Strike_Linger_Time_Min = 85;

		/// <summary>
		/// The maximum duration in milliseconds before a lightning strike will
		/// end.
		/// </summary>
		private const int Mode_Thunderstorm_Strike_Linger_Time_Max = 130;

		/// <summary>
		/// Gets the probability a lightning strike occurs for each frame
		/// </summary>
		/// <value>Probability a lightning strike occurs.</value>
		private double Mode_Thunderstorm_Strike_Chance {
			get {
				// This is called per pixel, per frame, so scale by size and
				// time.  Every millisecond, there's a 0.003 (0.3%) probability
				// that a new lightning strike will occur.
				return (
				    0.0003
				    * deviceConfig.FactorTime
				    * (1 / deviceConfig.FactorScaledSize)
				);
			}
		}

		/// <summary>
		/// Gets the probability a lightning strike follows another one
		/// immediately, per pixel of a strike
		/// </summary>
		/// <value>Probability a lightning strike follows another one.</value>
		private double Mode_Thunderstorm_ChainStrike_Chance {
			get {
				// Find the average strike size
				double averageStrikeSize =
					0.5 * (
					    Mode_Thunderstorm_Strike_Size_Min
					    + Mode_Thunderstorm_Strike_Size_Max
					);
				// Maintain the probability for the average strike; smaller
				// lightning strikes will have less chance of triggering
				// another strike, larger strikes will have more chance.
				// Chance is calculated per pixel, not per strike!
				return 0.93 / averageStrikeSize;
			}
		}

		/// <summary>
		/// The maximum distance in pixels for a triggered thunderstorm strike.
		/// </summary>
		private double Mode_Thunderstorm_ChainStrike_Delta_Max {
			get {
				// At maximum, a triggered lightning strike can occur 1 pixel
				// away, or up to 1.5x the normal maximum strike size away
				return Math.Max (1,
					Mode_Thunderstorm_Strike_Size_Max * 1.5
				);
			}
		}

		#endregion

		#region Properties - General

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
		/// Gets the amount to decrease remaining linger lifetime per frame
		/// </summary>
		/// <value>The amount to decrease remaining linger lifetime per frame.</value>
		private double Linger_DecayRate {
			get {
				// Decrease by FactorTime each frame
				return deviceConfig.FactorTime;
			}
		}

		/// <summary>
		/// The selected strobe mode.
		/// </summary>
		private StrobeMode _selectedStrobeMode;

		/// <summary>
		/// Gets or sets the selected strobe mode.
		/// </summary>
		/// <value>The selected strobe mode.</value>
		public StrobeMode SelectedStrobeMode {
			get {
				return _selectedStrobeMode;
			}
			set {
				this._selectedStrobeMode = value;
				// Reset the current frame
				lock (CurrentFrame) {
					CurrentFrame.Fill (Color.Transparent);
				}
				// Conditionally enable smoothing
				switch (_selectedStrobeMode) {
				case StrobeMode.SingleWhite:
				case StrobeMode.SingleColor:
					EnableSmoothing = false;
					break;
				default:
					EnableSmoothing = true;
					break;
				}
				// Conditionally adjust smoothing amount
				switch (_selectedStrobeMode) {
				case StrobeMode.Fireflies:
					SmoothingConstant = 859;
					break;
				default:
					SmoothingConstant = SmoothingConstant_Default;
					break;
				}
			}
		}

		#endregion

		public SimpleStrobeAnimation (
			ReadOnlyDeviceConfiguration Configuration) : base (Configuration)
		{
			InitBaseSystem (Light_Count, StrobeMode.White);
		}

		public SimpleStrobeAnimation (
			ReadOnlyDeviceConfiguration Configuration,
			StrobeMode DesiredStrobeMode) : base (Configuration)
		{
			InitBaseSystem (Light_Count, DesiredStrobeMode);
		}

		public SimpleStrobeAnimation (
			ReadOnlyDeviceConfiguration Configuration,
			Layer PreviouslyShownFrame)
			: base (Configuration, PreviouslyShownFrame)
		{
			InitBaseSystem (PreviouslyShownFrame.PixelCount, StrobeMode.White);
		}

		public SimpleStrobeAnimation (
			ReadOnlyDeviceConfiguration Configuration,
			Layer PreviouslyShownFrame, StrobeMode DesiredStrobeMode)
			: base (Configuration, PreviouslyShownFrame)
		{
			InitBaseSystem (PreviouslyShownFrame.PixelCount, DesiredStrobeMode);
		}

		/// <summary>
		/// Initialize the base system.
		/// </summary>
		/// <param name="Light_Count">Light count.</param>
		/// <param name="DesiredStrobeMode">Desired strobe mode.</param>
		private void InitBaseSystem (int Light_Count, StrobeMode DesiredStrobeMode)
		{
			// Prepare tracking for as many lights as are available
			Linger_Tracker = new double[Light_Count];
			Linger_Modified = new bool[Light_Count];

			CurrentFrame.Fill (Color.Transparent);
			RandomGenerator = new Random ();
			SelectedStrobeMode = DesiredStrobeMode;
		}

		public override Layer GetNextFrame ()
		{
			// Generate a frame depending on the current mode
			switch (SelectedStrobeMode) {
			case StrobeMode.White:
				lock (CurrentFrame) {
					// Clear collection of modified pixels
					Array.Clear (Linger_Modified, 0, Linger_Modified.Length);

					for (int i = 0; i < Light_Count; i++) {
						if (Linger_Modified [i]) {
							// Ignore pixels that were already modified
							continue;
						}
						if (Linger_Tracker [i] > 0) {
							// Strobe exists, decrease strobe lifetime
							Linger_Tracker [i] -=
								Linger_DecayRate;

							if (Linger_Tracker [i] <= 0) {
								// Strobe reached end, fade out brightness
								// (Slower fade than fading out colors, too)
								CurrentFrame [i].Brightness = 0;
							}
						} else if (RandomGenerator.NextDouble () < Mode_White_Strobe_Chance) {
							// No active strobe here, and chance passed, time
							// to start a new one
							CurrentFrame [i].SetColor (Color.Named ["white"]);

							// Assign a time for the strobe to fade out
							Linger_Tracker [i] =
								Mode_IndividualStrobe_Linger_Time;
						}
					}
				}
				break;
			case StrobeMode.Color:
				lock (CurrentFrame) {
					// Clear collection of modified pixels
					Array.Clear (Linger_Modified, 0, Linger_Modified.Length);

					for (int i = 0; i < Light_Count; i++) {
						if (Linger_Modified [i]) {
							// Ignore pixels that were already modified
							continue;
						}
						// Difference from other strobing modes:
						// Deliberately allow starting a new strobe atop an
						// existing one, as it will likely change the color.
						if (RandomGenerator.NextDouble () < Mode_Color_Strobe_Chance) {
							// Ignoring if an active strobe is here, and chance
							// passed, time to start a new one
							CurrentFrame [i].SetColor (
								RandomColorGenerator.GetRandomColor ());

							// Assign a time for the strobe to fade out
							Linger_Tracker [i] =
								Mode_IndividualStrobe_Linger_Time;
						} else if (Linger_Tracker [i] > 0) {
							// Strobe exists, decrease strobe lifetime
							Linger_Tracker [i] -=
								Linger_DecayRate;

							if (Linger_Tracker [i] <= 0) {
								// Strobe reached end, fade out brightness
								// (Slower fade than fading out colors, too)
								CurrentFrame [i].Brightness = 0;
							}
						}
					}
				}
				break;
			case StrobeMode.SingleWhite:
			case StrobeMode.SingleColor:
				lock (CurrentFrame) {
					// Tracked pixel
					// Must always be valid regardless of LightCount, e.g. 0
					const int trackedPixel = 0;
					if (Linger_Tracker [trackedPixel] > 0) {
						// Decrease strobe lifetime if positive
						Linger_Tracker [trackedPixel] -=
							Linger_DecayRate;

						if (Linger_Tracker [trackedPixel] <= 0) {
							// Strobe reached end, fade out all
							CurrentFrame.Fill (Color.Transparent);
							// Reset linger tracker in order to track off time
							Linger_Tracker [trackedPixel] = 0;
						}
					} else if (Math.Abs (Linger_Tracker [trackedPixel]) < Mode_SingleStrobe_Linger_Time_Off) {
						// Linger tracker is negative, but absolute value has
						// not yet reached the linger time, so we're counting
						// downwards.  Keep decreasing strobe lifetime.
						Linger_Tracker [trackedPixel] -=
							Linger_DecayRate;
					} else {
						// Lingered in off state for set time, time to start a
						// new strobe
						switch (SelectedStrobeMode) {
						case StrobeMode.SingleWhite:
							CurrentFrame.Fill (Color.Named ["white"]);
							break;
						case StrobeMode.SingleColor:
							CurrentFrame.Fill (
								RandomColorGenerator.GetRandomColor()
							);
							break;
						default:
							throw new NotSupportedException (
								"Unsupported StrobeMode for SelectedStrobeMode" +
								"within SingleWhite/SingleColor"
							);
						}

						// Assign a time for the strobe to fade out
						Linger_Tracker [trackedPixel] =
							Mode_SingleStrobe_Linger_Time_On;
					}
				}
				break;
			case StrobeMode.Fireflies:
				lock (CurrentFrame) {
					// Clear collection of modified pixels
					Array.Clear (Linger_Modified, 0, Linger_Modified.Length);

					for (int i = 0; i < Light_Count; i++) {
						if (Linger_Modified [i]) {
							// Ignore pixels that were already modified
							continue;
						}
						if (Linger_Tracker [i] > 0) {
							// Firefly glow exists, decrease glow lifetime
							Linger_Tracker [i] -=
								Linger_DecayRate;

							if (Linger_Tracker [i] <= 0) {
								// Firefly glow reached end, fade out
								CurrentFrame [i] = Color.Transparent;
							}
						} else if (RandomGenerator.NextDouble () < Mode_Fireflies_Glow_Chance) {
							// No active firefly here, and chance passed, time
							// to start a new one
							CurrentFrame [i].R = 128;
							CurrentFrame [i].G = 255;
							CurrentFrame [i].B = 0;
							CurrentFrame [i].Brightness =
								Styled_ModerateBrightness;

							// Assign a time for the firefly to fade out
							Linger_Tracker [i] = MathUtilities.ConvertRange (
								RandomGenerator.NextDouble (), 0, 1,
								Mode_Fireflies_Linger_Time_Min,
								Mode_Fireflies_Linger_Time_Max
							);
						}
					}
				}
				break;
			case StrobeMode.Rain:
				lock (CurrentFrame) {
					for (int i = 0; i < Light_Count; i++) {
						if (RandomGenerator.NextDouble () < Mode_Rain_PitterPatter_Chance) {
							CurrentFrame [i] = GetRainParticle ();
						}
					}
				}
				break;
			case StrobeMode.Thunderstorm:
				lock (CurrentFrame) {
					// Clear collection of modified pixels
					Array.Clear (Linger_Modified, 0, Linger_Modified.Length);

					for (int i = 0; i < Light_Count; i++) {
						if (Linger_Modified [i]) {
							// Ignore pixels that were already modified
							continue;
						}
						if (Linger_Tracker [i] > 0) {
							// Lightning strike exists, decrease strike lifetime
							Linger_Tracker [i] -=
								Linger_DecayRate;

							if (Linger_Tracker [i] <= 0) {
								// Lightning strike reached end
								// Make into rain
								CurrentFrame [i] = GetRainParticle ();
								// Check if another strike should be triggered
								if (RandomGenerator.NextDouble () < Mode_Thunderstorm_ChainStrike_Chance) {
									// Chance passed for a lightning strike to
									// follow another one immediately, start a
									// nearby strike

									// Find a random location within striking
									// index

									// Furthest away
									int strikeChainStart =
										deviceConfig.ClipIndex (i - (int)Mode_Thunderstorm_ChainStrike_Delta_Max);
									// End halfway past current index
									int strikeChainEnd =
										deviceConfig.ClipIndex (i + (int)Mode_Thunderstorm_ChainStrike_Delta_Max);

									// Pick a random location within striking
									// distance
									int strikeChainIndex =
										RandomGenerator.Next (
											strikeChainStart,
											strikeChainEnd
										);
									//Console.WriteLine (
									//	"> Chain strike at {0} (from {1})!",
									//	strikeChainIndex, i
									//);
									GenerateLightningStrike (strikeChainIndex);
								}
							}
						} else if (RandomGenerator.NextDouble () < Mode_Thunderstorm_Strike_Chance) {
							// No active lightning here, and chance passed, time
							// to start a new one
							//Console.WriteLine ("Strike at {0}!", i);
							GenerateLightningStrike (i);
						} else if (RandomGenerator.NextDouble () < Mode_Rain_PitterPatter_Chance) {
							// Update rain
							CurrentFrame [i] = GetRainParticle ();
						}
					}
				}
				break;
			default:
				throw new NotSupportedException (
					"Unsupported StrobeMode for SelectedStrobeMode");
			}
			return CurrentFrame;
		}

		/// <summary>
		/// Gets a new Color representing a random rain particle.
		/// </summary>
		/// <returns>
		/// An LED colored like a rain particle.
		/// </returns>
		private Color GetRainParticle ()
		{
			return new Color (
				Styled_SoftColor, Styled_SoftColor, Styled_BrightColor,
				(byte)RandomGenerator.Next (
					Styled_SoftBrightness,
					Styled_ModerateBrightness
				)
			);
		}

		/// <summary>
		/// Generates and applies a random lightning strike centered on the
		/// specified middle index.
		/// </summary>
		/// <param name="Middle">Middle of the lightning strike.</param>
		private void GenerateLightningStrike (int Middle)
		{
			// Pick a random duration
			double strikeDuration =
				MathUtilities.ConvertRange (
					RandomGenerator.NextDouble (), 0, 1,
					Mode_Thunderstorm_Strike_Linger_Time_Min,
					Mode_Thunderstorm_Strike_Linger_Time_Max
				);

			// Pick a random size
			int strikeSize =
				(int)Math.Round (MathUtilities.ConvertRange (
					RandomGenerator.NextDouble (), 0, 1,
					Mode_Thunderstorm_Strike_Size_Min,
					Mode_Thunderstorm_Strike_Size_Max
				));

			// Start halfway before current index
			int strikeStart =
				deviceConfig.ClipIndex (Middle - (strikeSize / 2));
			// End halfway past current index
			int strikeEnd =
				deviceConfig.ClipIndex (Middle + (strikeSize / 2));

			if (strikeStart == strikeEnd) {
				// Increment strikeEnd to ensure at least one pixel gets set
				strikeEnd++;
			}

			// Build a lightning strike along the entire array
			for (int strikeIndex = strikeStart; strikeIndex < strikeEnd; strikeIndex++) {
				// Set color
				CurrentFrame [strikeIndex].SetColor (
					Mode_Thunderstorm_Strike_Color
				);
				// Set duration
				Linger_Tracker [strikeIndex] = strikeDuration;
				// Mark as modified
				Linger_Modified [strikeIndex] = true;
			}

			//Console.WriteLine (
			//	"[Generated strike.  Middle: {0}, size: {1}, duration: {2}, " +
			//	"start: {3}, end: {4}]",
			//	Middle, strikeSize, strikeDuration, strikeStart, strikeEnd
			//);
		}

	}
}

