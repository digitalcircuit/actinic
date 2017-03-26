//
//  Color.cs
//
//  Author:
//       Shane Synan <digitalcircuit36939@gmail.com>
//
//  Copyright (c) 2013 - 2016
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
	public class Color
	{
		public static readonly Dictionary<string, Color> Named = new Dictionary<string, Color> {
			// Primary and secondary
			{ "white", new Color (255, 255, 255) },
			{ "black", new Color (0, 0, 0) },
			{ "red", new Color (255, 0, 0) },
			{ "green", new Color (0, 255, 0) },
			{ "blue", new Color (0, 0, 255) },
			{ "yellow", new Color (255, 255, 0) },
			{ "cyan", new Color (0, 255, 255) },
			{ "purple", new Color (255, 0, 255) },
			// Mixtures
			{ "azure", new Color (41, 146, 255) },
			{ "orange", new Color (255, 100, 0) },
			{ "pink", new Color (255, 0, 128) },
			// Accent
			{ "ambient", new Color (255, 130, 20) }
		};

		//LED_Colors.Add (new Color (255, 0, 30));
		//LED_Colors.Add (new Color (0, 30, 255));

		public byte R {
			get;
			private set;
		}

		public byte G {
			get;
			private set;
		}

		public byte B {
			get;
			private set;
		}

		public byte Brightness {
			get;
			private set;
		}

		/// <summary>
		/// Gets a value indicating whether this <see cref="Actinic.Color"/> has
		/// any effect on output, i.e. color is not all black with no
		/// brightness.
		/// </summary>
		/// <value><c>true</c> if color has no effect; otherwise, <c>false</c>.</value>
		public bool HasNoEffect {
			get {
				if ((R != 0) || (G != 0) || (B != 0) || (Brightness != 0))
					return false;
				return true;
			}
		}

		public Color (byte Red, byte Green, byte Blue)
		{
			R = Red;
			G = Green;
			B = Blue;
			Brightness = LightSystem.Brightness_MAX;
		}

		public Color (byte Red, byte Green, byte Blue, byte Intensity)
		{
			R = Red;
			G = Green;
			B = Blue;
			Brightness = Intensity;
		}

		public static Color FromArgb (byte Red, byte Green, byte Blue)
		{
			return new Color (Red, Green, Blue);
		}

		public static Color FromArgb (byte Red, byte Green, byte Blue, byte Intensity)
		{
			return new Color (Red, Green, Blue, Intensity);
		}

		public override string ToString ()
		{
			return string.Format ("[Color: R={0,-3}, G={1,-3}, B={2,-3}, Brightness={3,-3}]", R, G, B, Brightness);
		}
	}
}

