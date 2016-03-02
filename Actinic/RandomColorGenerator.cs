//
//  RandomColorGenerator.cs
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

namespace Actinic
{
	public static class RandomColorGenerator
	{
		private static Random RandomGenerator;
		private static List<Color> LED_Colors = new List<Color> ();
		private static List<int> RandomPrevColors = new List<int> ();

		private static void InitalizeSystem ()
		{
			RandomGenerator = new Random ();
			InitializeColors ();
		}

		private static void InitializeColors ()
		{
			LED_Colors.Add (new Color (255, 0, 0)); // Red
			LED_Colors.Add (new Color (0, 255, 0)); // Green
			LED_Colors.Add (new Color (0, 0, 255)); // Blue
			LED_Colors.Add (new Color (255, 255, 0)); // Yellow
			LED_Colors.Add (new Color (0, 255, 255)); // Cyan
			LED_Colors.Add (new Color (255, 0, 255)); // Purple
			LED_Colors.Add (new Color (255, 100, 0)); // Orange
			LED_Colors.Add (new Color (255, 0, 128)); // Pink
		}

		public static Color GetRandomColor ()
		{
			if (LED_Colors.Count == 0)
				InitalizeSystem ();
			int selectedIndex = RandomGenerator.Next (LED_Colors.Count - 1);
			while (RandomPrevColors.Contains (selectedIndex)) {
				selectedIndex = RandomGenerator.Next (LED_Colors.Count - 1);
			}
			RandomPrevColors.Add (selectedIndex);
			while (RandomPrevColors.Count > 4) {
				RandomPrevColors.RemoveAt (0);
			}
			return LED_Colors [selectedIndex];
		}

	}
}

