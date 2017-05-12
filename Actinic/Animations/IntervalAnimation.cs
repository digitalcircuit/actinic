//
//  IntervalAnimation.cs
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
	public class IntervalAnimation:AbstractAnimation
	{

		private const int Animation_Update_Delay = 60 * 1000;
		//1 * 1000; // Testing
		//60 * 1000; // Typical usage, update once a minute

		// Time is represented by minutes in a day, starting from 0 at 12 am to 24*60 at midnight
		private LinearColorInterpolator TimeColorGradient =
			new LinearColorInterpolator (
				new KeyValuePair<int, Color>[] {
					new KeyValuePair<int, Color> (
						0, new Color (255, 30, 0, 150)),
					new KeyValuePair<int, Color> (
						4 * 60, new Color (255, 30, 0, 30)),
					new KeyValuePair<int, Color> (
						6 * 60, new Color (255, 30, 30, 30)),
					new KeyValuePair<int, Color> (
						7 * 60, new Color (255, 100, 30, 80)),
					new KeyValuePair<int, Color> (
						8 * 60, new Color (255, 150, 100)),
					new KeyValuePair<int, Color> (
						9 * 60, new Color (255, 255, 255)),
					new KeyValuePair<int, Color> (
						11 * 60, new Color (255, 255, 255)),
					new KeyValuePair<int, Color> (
						12 * 60, new Color (255, 100, 50)),
					new KeyValuePair<int, Color> (
						(5 + 12) * 60, new Color (255, 100, 30)),
					new KeyValuePair<int, Color> (
						(8 + 12) * 60, new Color (255, 50, 30)),
					new KeyValuePair<int, Color> (
						(9 + 12) * 60, new Color (255, 50, 0)),
					new KeyValuePair<int, Color> (
						(10 + 12) * 60, new Color (255, 30, 0)),
					new KeyValuePair<int, Color> (
						(11 + 12) * 60, new Color (255, 30, 0, 200)),
					new KeyValuePair<int, Color> (
						(12 + 12) * 60, new Color (255, 30, 0, 150))
				}
			);

		// Original full-brightness TimeColorGradient, red at night
		//		private LinearColorInterpolator TimeColorGradient = new LinearColorInterpolator (
		//			new KeyValuePair<int, Color>[] {
		//			new KeyValuePair<int, Color> (0, new Color (255, 0, 0)),
		//			new KeyValuePair<int, Color> (4 * 60, new Color (255, 0, 0)),
		//			new KeyValuePair<int, Color> (6 * 60, new Color (100, 0, 255)),
		//			new KeyValuePair<int, Color> (7 * 60, new Color (100, 100, 255)),
		//			new KeyValuePair<int, Color> (8 * 60, new Color (255, 150, 100)),
		//			new KeyValuePair<int, Color> (9 * 60, new Color (255, 255, 255)),
		//			new KeyValuePair<int, Color> (11 * 60, new Color (255, 255, 255)),
		//			new KeyValuePair<int, Color> (12 * 60, new Color (255, 100, 50)),
		//			new KeyValuePair<int, Color> ((5 + 12) * 60, new Color (255, 100, 30)),
		//			new KeyValuePair<int, Color> ((8 + 12) * 60, new Color (255, 50, 30)),
		//			new KeyValuePair<int, Color> ((9 + 12) * 60, new Color (255, 50, 0)),
		//			new KeyValuePair<int, Color> ((10 + 12) * 60, new Color (255, 30, 0)),
		//			new KeyValuePair<int, Color> ((11 + 12) * 60, new Color (255, 0, 0)),
		//			new KeyValuePair<int, Color> ((12 + 12) * 60, new Color (255, 0, 0))}
		//		);

		private Color TimeColor = new Color (0, 0, 0);
		private byte TimeColor_Brightness;

		public enum IntervalMode
		{
			Time,
			Weather
		}

		private IntervalMode _selectedIntervalMode;

		public IntervalMode SelectedIntervalMode {
			get {
				return _selectedIntervalMode;
			}
			set {
				this._selectedIntervalMode = value;
				UpdateColorsFromTime ();
			}
		}

		public IntervalAnimation (int Light_Count) : base (Light_Count)
		{
			RequestedAnimationDelay = Animation_Update_Delay;
			EnableSmoothing = false;
			UpdateColorsFromTime ();
		}

		public IntervalAnimation (List<LED> PreviouslyShownFrame) : base (PreviouslyShownFrame)
		{
			RequestedAnimationDelay = Animation_Update_Delay;
			EnableSmoothing = false;
			RequestSmoothCrossfade = true;
			// By default, this will immediately override the existing colors.  Set to true to smoothly transition.
			UpdateColorsFromTime ();
		}

		public override List<LED> GetNextFrame ()
		{
			UpdateColorsFromTime ();
			for (int i = 0; i < Light_Count; i++) {
				CurrentFrame [i].SetColor (TimeColor);
				CurrentFrame [i].Brightness = TimeColor_Brightness;
			}
			return CurrentFrame;
		}

		private void UpdateColorsFromTime ()
		{
			int currentTime = (DateTime.Now.Hour * 60) + (DateTime.Now.Minute);
			TimeColor = TimeColorGradient.GetColor (currentTime);

			switch (AnimationStyle) {
			case Style.Moderate:
				// Set brightness for all based on the current time of day
				TimeColor_Brightness = TimeColor.Brightness;
				break;
			case Style.Soft:
				// Dim the themed-brightness (themed-brightness isn't always COLOR_MAX) using a ratio of the style
				TimeColor_Brightness = (byte)(Math.Max (Math.Min ((int)(TimeColor.Brightness * ((double)Styled_BrightBrightness / LightSystem.Brightness_MAX)), LightSystem.Brightness_MAX), LightSystem.Brightness_MIN_VISIBLE));
				// ...and don't allow something brighter than Color_MAX, or dimmer than minimum-visible
				break;
			default:
				// Override the themed-brightness with the style, i.e. bright
				TimeColor_Brightness = Styled_BrightBrightness;
				break;
			}

			//Console.WriteLine ("Current time: {0}:{1}  Current color (R, G, B, Brightness):  {2}, {3}, {4}, {5}", currentTime / 60, currentTime % 60, TimeColor.R, TimeColor.G, TimeColor.B, TimeColor.Brightness)
		}
	}
}

