//
//  RandomColorGenerator.cs
//
//  Author:
//       Shane Synan <digitalcircuit36939@gmail.com>
//
//  Copyright (c) 2014 
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

namespace G35_USB
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
			//LED_Colors.Add (new Color ("White", 255, 255, 255));
			LED_Colors.Add (new Color ("Red", 255, 0, 0));
			LED_Colors.Add (new Color ("Green", 0, 255, 0));
			LED_Colors.Add (new Color ("Blue", 0, 0, 255));
			LED_Colors.Add (new Color ("Yellow", 255, 255, 0));
			LED_Colors.Add (new Color ("Cyan", 0, 255, 255));
			LED_Colors.Add (new Color ("Purple", 255, 0, 255));
			//LED_Colors.Add (new Color ("Orange", 255, 30, 0));
			LED_Colors.Add (new Color ("Orange", 255, 100, 0));
			//LED_Colors.Add (new Color ("LightBlue", 0, 128, 255));
			LED_Colors.Add (new Color ("Pink", 255, 0, 128));
			//LED_Colors.Add (new Color ("", 255, 0, 30));
			//LED_Colors.Add (new Color ("", 0, 30, 255));
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

