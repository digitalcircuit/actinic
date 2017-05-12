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

// Rendering
using Actinic.Rendering;

namespace Actinic.Animations
{
	public class SimpleStrobeAnimation:AbstractAnimation
	{
		private Random RandomGenerator;

		public enum StrobeMode
		{
			White,
			Color,
			Single,
			Fireflies,
			Rain,
			Thunderstorm
		}

		private const double Mode_Fireflies_Glow_Chance = 0.0007;
		private const double Mode_Fireflies_FadeOut_Chance = 0.02;

		private const double Mode_Rain_PitterPatter_Chance = 0.02;

		private const double Mode_Thunderstorm_Strike_Chance = 0.0003;
		// Chance of a lightening strike
		//  Originally:  0.0004, too often
		private const double Mode_Thunderstorm_TriggeredStrike_Chance = 0.002;
		// Chance of a lightning strike following another one immediately, triggered
		//  Originally:  0.0009, not often enough

		private StrobeMode _selectedStrobeMode;

		public StrobeMode SelectedStrobeMode {
			get {
				return _selectedStrobeMode;
			}
			set {
				this._selectedStrobeMode = value;
				lock (CurrentFrame) {
					for (int i = 0; i < Light_Count; i++) {
						CurrentFrame [i].R = 0;
						CurrentFrame [i].G = 0;
						CurrentFrame [i].B = 0;
						CurrentFrame [i].Brightness = 0;
					}
				}
				switch (_selectedStrobeMode) {
				case StrobeMode.Single:
					EnableSmoothing = false;
					break;
				default:
					EnableSmoothing = true;
					break;
				}
				switch (_selectedStrobeMode) {
				case StrobeMode.Fireflies:
					SmoothingAmount = 0.89;
					break;
				case StrobeMode.Rain:
				case StrobeMode.Thunderstorm:
					SmoothingAmount = 0.86;
					break;
				default:
					SmoothingAmount = SmoothingAmount_Default;
					break;
				}
			}
		}

		public SimpleStrobeAnimation (int Light_Count) : base (Light_Count)
		{
			InitBaseSystem (Light_Count, StrobeMode.White);
		}

		public SimpleStrobeAnimation (int Light_Count, StrobeMode DesiredStrobeMode) : base (Light_Count)
		{
			InitBaseSystem (Light_Count, DesiredStrobeMode);
		}

		public SimpleStrobeAnimation (List<Color> PreviouslyShownFrame) : base (PreviouslyShownFrame)
		{
			InitBaseSystem (PreviouslyShownFrame.Count, StrobeMode.White);
		}

		public SimpleStrobeAnimation (List<Color> PreviouslyShownFrame, StrobeMode DesiredStrobeMode) : base (PreviouslyShownFrame)
		{
			InitBaseSystem (PreviouslyShownFrame.Count, DesiredStrobeMode);
		}



		private void InitBaseSystem (int Light_Count, StrobeMode DesiredStrobeMode)
		{
			for (int i = 0; i < Light_Count; i++) {
				CurrentFrame [i].R = 0;
				CurrentFrame [i].G = 0;
				CurrentFrame [i].B = 0;
				CurrentFrame [i].Brightness = 0;
			}
			RandomGenerator = new Random ();
			SelectedStrobeMode = DesiredStrobeMode;
		}

		public override List<Color> GetNextFrame ()
		{
			switch (SelectedStrobeMode) {
			case StrobeMode.White:
				lock (CurrentFrame) {
					for (int i = 0; i < Light_Count; i++) {
						if (RandomGenerator.NextDouble () < 0.04) {
							CurrentFrame [i].R = 255;
							CurrentFrame [i].G = 255;
							CurrentFrame [i].B = 255;
							CurrentFrame [i].Brightness = 255;
						} else {
							CurrentFrame [i].Brightness = 0;
						}
					}
				}
				break;
			case StrobeMode.Color:
				lock (CurrentFrame) {
					for (int i = 0; i < Light_Count; i++) {
						if (RandomGenerator.NextDouble () < 0.06) {
							CurrentFrame [i].SetColor (RandomColorGenerator.GetRandomColor ());
							CurrentFrame [i].Brightness = 255;
						} else {
							CurrentFrame [i].Brightness = 0;
						}
					}
				}
				break;
			case StrobeMode.Single:
				lock (CurrentFrame) {
					bool wasOff = (CurrentFrame [0].Brightness == 0);
					for (int i = 0; i < Light_Count; i++) {
						if (wasOff) {
							CurrentFrame [i].R = 255;
							CurrentFrame [i].G = 255;
							CurrentFrame [i].B = 255;
							CurrentFrame [i].Brightness = 255;
						} else {
							CurrentFrame [i].Brightness = 0;
						}
					}
				}
				break;
			case StrobeMode.Fireflies:
				lock (CurrentFrame) {
					for (int i = 0; i < Light_Count; i++) {
						if (RandomGenerator.NextDouble () < Mode_Fireflies_Glow_Chance) {
							CurrentFrame [i].R = 128;
							CurrentFrame [i].G = 255;
							CurrentFrame [i].B = 0;
							CurrentFrame [i].Brightness = Styled_ModerateBrightness;
						} else if (RandomGenerator.NextDouble () < Mode_Fireflies_FadeOut_Chance) {
							CurrentFrame [i].Brightness = 0;
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
					for (int i = 0; i < Light_Count; i++) {
						if (RandomGenerator.NextDouble () < Mode_Thunderstorm_Strike_Chance) {
							// Chance of lightening strike
							CurrentFrame [i].R = 255;
							CurrentFrame [i].G = 255;
							CurrentFrame [i].B = 255;
							CurrentFrame [i].Brightness = 255;
						} else if (CurrentFrame [i].Brightness > 254) {
							CurrentFrame [i].R = 255;
							CurrentFrame [i].G = 255;
							CurrentFrame [i].B = 255;
							CurrentFrame [i].Brightness -= 1;
						} else if (CurrentFrame [i].Brightness == 254) {
							CurrentFrame [i].R = 255;
							CurrentFrame [i].G = 255;
							CurrentFrame [i].B = 255;
							CurrentFrame [i].Brightness = 128;
						} else if (CurrentFrame [i].Brightness == 128) {
							if (RandomGenerator.NextDouble () < Mode_Thunderstorm_TriggeredStrike_Chance) {
								// Chance of a lightning strike following another one immediately, triggered
								int random_chain_reaction = RandomGenerator.Next (Math.Max (0, i - 1), Math.Min (Light_Count - 1, i + 1));
								CurrentFrame [random_chain_reaction].R = 255;
								CurrentFrame [random_chain_reaction].G = 255;
								CurrentFrame [random_chain_reaction].B = 255;
								CurrentFrame [random_chain_reaction].Brightness = 255;
							}
							CurrentFrame [i].R = Styled_SoftColor;
							CurrentFrame [i].G = Styled_SoftColor;
							CurrentFrame [i].B = Styled_BrightColor;
							CurrentFrame [i].Brightness = Styled_SoftBrightness;
						} else if (RandomGenerator.NextDouble () < Mode_Rain_PitterPatter_Chance) {
							CurrentFrame [i] = GetRainParticle ();
						}
					}
				}
				break;
			default:
				break;
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
			return new Color (Styled_SoftColor, Styled_SoftColor, Styled_BrightColor, (byte)RandomGenerator.Next (Styled_SoftBrightness, Styled_ModerateBrightness));
		}

	}
}

