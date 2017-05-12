//
//  LinearColorInterpolator.cs
//
//  Author:
//       Mark Byers <https://stackoverflow.com/users/61974/mark-byers>
//       (From <https://stackoverflow.com/questions/2307726/how-to-calculate-color-based-on-a-range-of-values-in-c>)
//  Edited by:
//       Shane Synan <digitalcircuit36939@gmail.com>
//
//  Copyright (c) 2014 - 2016
//
//  (I'm not sure what license the original code is under, but in good faith,
//   I'll assume it is GPL-compatible)
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

namespace Actinic
{
	public class LinearColorInterpolator
	{
		public SortedDictionary<int, Color> InterpolatedColors = new SortedDictionary<int, Color> ();

		public LinearColorInterpolator ()
		{
		}

		public LinearColorInterpolator (KeyValuePair<int, Color>[] ColorGradient)
		{
			foreach (KeyValuePair<int, Color> kvp_entry in ColorGradient) {
				InterpolatedColors.Add (kvp_entry.Key, kvp_entry.Value);
			}
		}

		private byte interpolate (byte a, byte b, double p)
		{
			return (byte)(a * (1 - p) + b * p);
		}

		public Color GetColor (int v)
		{
			if (InterpolatedColors.Count == 0)
				return Color.FromArgb (0, 0, 0);
			if (InterpolatedColors.Count == 1)
				return InterpolatedColors [0];

			KeyValuePair<int, Color> kvp_previous = new KeyValuePair<int,Color> (-1, Color.FromArgb (0, 0, 0));
			foreach (KeyValuePair<int, Color> kvp in InterpolatedColors) {
				if (kvp.Key > v) {
					double p = (v - kvp_previous.Key) / (double)(kvp.Key - kvp_previous.Key);
					Color a = kvp_previous.Value;
					Color b = kvp.Value;
					Color c = Color.FromArgb (
						          interpolate (a.R, b.R, p),
						          interpolate (a.G, b.G, p),
						          interpolate (a.B, b.B, p),
						          interpolate (a.Brightness, b.Brightness, p)
					          );
					return c;
				} else if (kvp.Key == v) {
					return kvp.Value;
				}
				kvp_previous = kvp;
			}

			return Color.FromArgb (0, 0, 0);
		}

	}
}

