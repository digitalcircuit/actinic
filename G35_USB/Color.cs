//
//  Color.cs
//
//  Author:
//       Shane Synan <digitalcircuit36939@gmail.com>
//
//  Copyright (c) 2013 FoxSoft
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

namespace G35_USB
{
	public class Color
	{
		public string Name {
			get;
			private set;
		}

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
		/// Gets a value indicating whether this <see cref="G35_USB.Color"/> has any effect on output, i.e. color
		/// is not all black with no brightness.
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
			Name = "";
			R = Red;
			G = Green;
			B = Blue;
			Brightness = LightSystem.Brightness_MAX;
		}

		public Color (byte Red, byte Green, byte Blue, byte Intensity)
		{
			Name = "";
			R = Red;
			G = Green;
			B = Blue;
			Brightness = Intensity;
		}

		public Color (string ColorName, byte Red, byte Green, byte Blue)
		{
			Name = ColorName;
			R = Red;
			G = Green;
			B = Blue;
			Brightness = LightSystem.Brightness_MAX;
		}

		public Color (string ColorName, byte Red, byte Green, byte Blue, byte Intensity)
		{
			Name = ColorName;
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
			return string.Format ("[Color: Name={0}, R={1,-3}, G={2,-3}, B={3,-3}, Brightness={4,-3}]", Name, R, G, B, Brightness);
		}
	}
}

