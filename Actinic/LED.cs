//
//  LED.cs
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
using FoxSoft.Utilities;

namespace Actinic
{
	public class LED
	{

		/// <summary>
		/// Style for blending multiple layers together
		/// </summary>
		public enum BlendingStyle
		{
			/// <summary>
			/// Take the brightest components from each layer
			/// </summary>
			Combine,
			/// <summary>
			/// Prefer this layer, dependent upon opacity
			/// </summary>
			Favor,
			/// <summary>
			/// Completely replace other layers whenever opacity is not 0
			/// </summary>
			Mask,
			/// <summary>
			/// Only use this layer, completely replacing others
			/// </summary>
			Replace,
			/// <summary>
			/// Add the components of each layer
			/// </summary>
			Sum
		}

		public byte R {
			get;
			set;
		}

		public byte G {
			get;
			set;
		}

		public byte B {
			get;
			set;
		}

		public byte Brightness {
			get;
			set;
		}

		/// <summary>
		/// Gets a value indicating whether this <see cref="Actinic.LED"/> has any effect on output, i.e. LED
		/// is not all black with no brightness.
		/// </summary>
		/// <value><c>true</c> if LED has no effect; otherwise, <c>false</c>.</value>
		public bool HasNoEffect {
			get {
				if ((R != 0) || (G != 0) || (B != 0) || (Brightness != 0))
					return false;
				return true;
			}
		}

		public LED ()
		{
			R = LightSystem.Color_MIN;
			G = LightSystem.Color_MIN;
			B = LightSystem.Color_MIN;
			Brightness = LightSystem.Brightness_MAX;
		}

		// Automatically calculate the brightness component based on given values
		public LED (byte R_Component, byte G_Component, byte B_Component)
		{
			SetColor (R_Component, G_Component, B_Component);
		}

		// Directly specify the colors and brightness components
		public LED (byte R_Component, byte G_Component, byte B_Component, byte Brightness_Component)
		{
			R = R_Component;
			G = G_Component;
			B = B_Component;
			Brightness = Brightness_Component;
		}

		public Color GetColor ()
		{
			return new Color (R, G, B, Brightness);
		}

		public void SetColor (Actinic.Color SelectedColor)
		{
			R = SelectedColor.R;
			G = SelectedColor.G;
			B = SelectedColor.B;
			Brightness = SelectedColor.Brightness;
		}

		public void SetColor (byte R_Component, byte G_Component, byte B_Component)
		{
			SetColor (R_Component, G_Component, B_Component, true);
		}

		public void SetColor (byte R_Component, byte G_Component, byte B_Component, bool DeriveBrightness)
		{
			if (DeriveBrightness) {
				byte brightest_color = Math.Max (R_Component, Math.Max (G_Component, B_Component));
				Brightness = brightest_color;
			}

			R = R_Component;
			G = G_Component;
			B = B_Component;
		}

		public void SetColor (byte R_Component, byte G_Component, byte B_Component, byte Brightness_Component)
		{
			R = R_Component;
			G = G_Component;
			B = B_Component;
			Brightness = Brightness_Component;
		}

		/// <summary>
		/// Blends the current color with the new one.
		/// </summary>
		/// <param name="SelectedColor">Selected color.</param>
		/// <param name="Subtractive">If set to <c>true</c> the new color reduces the brightness and hue of the current.</param>
		/// <param name="Opacity">Amount this color influences the current color.</param>
		public void BlendColor (Actinic.Color SelectedColor, bool Subtractive, double Opacity)
		{
			if (Opacity < 0 || Opacity > 1)
				throw new ArgumentOutOfRangeException (
					"Opacity",
					"Opacity must be a value between 0 and 1."
				);

			// Reduce the intensity of the new color
			Actinic.Color opacifiedColor = new Actinic.Color (
				                               (byte)(SelectedColor.R * Opacity),
				                               (byte)(SelectedColor.G * Opacity),
				                               (byte)(SelectedColor.B * Opacity),
				                               (byte)(SelectedColor.Brightness * Opacity)
			                               );
			if (Subtractive) {
				// Reduce these colors from the current
				R = (byte)Math.Max (R - opacifiedColor.R, 0);
				G = (byte)Math.Max (G - opacifiedColor.G, 0);
				B = (byte)Math.Max (B - opacifiedColor.B, 0);
				Brightness = (byte)Math.Max (Brightness - opacifiedColor.Brightness, 0);
			} else {
				// Take the brightest colors
				R = Math.Max (R, opacifiedColor.R);
				G = Math.Max (G, opacifiedColor.G);
				B = Math.Max (B, opacifiedColor.B);
				Brightness = Math.Max (Brightness, opacifiedColor.Brightness);
			}
		}

		/// <summary>
		/// Blends the current color with the new one.
		/// </summary>
		/// <param name="SelectedColor">Selected color.</param>
		/// <param name="BlendMode">Mode for blending colors together.</param>
		public void BlendColor (Actinic.Color SelectedColor, Actinic.LED.BlendingStyle BlendMode)
		{
			switch (BlendMode) {
			case LED.BlendingStyle.Combine:
				// Take the brightest colors
				R = Math.Max (R, SelectedColor.R);
				G = Math.Max (G, SelectedColor.G);
				B = Math.Max (B, SelectedColor.B);
				Brightness = Math.Max (Brightness, SelectedColor.Brightness);
				break;
			case LED.BlendingStyle.Favor:
				if (SelectedColor.HasNoEffect == false) {
					// Overwrite the original with the new layer
					// Brightness controls the amount overriden
					double override_amount = MathUtilities.ConvertRange ((double)SelectedColor.Brightness, (double)LightSystem.Brightness_MIN, (double)LightSystem.Brightness_MAX, 0, 1);
					R = ((byte)Math.Min (((SelectedColor.R * override_amount) + (R * (1 - override_amount))), LightSystem.Color_MAX));
					G = ((byte)Math.Min (((SelectedColor.G * override_amount) + (G * (1 - override_amount))), LightSystem.Color_MAX));
					B = ((byte)Math.Min (((SelectedColor.B * override_amount) + (B * (1 - override_amount))), LightSystem.Color_MAX));
					Brightness = ((byte)Math.Min (((SelectedColor.Brightness * override_amount) + (Brightness * (1 - override_amount))), LightSystem.Color_MAX));
				}
				break;
			case LED.BlendingStyle.Mask:
				if (SelectedColor.HasNoEffect == false) {
					// Don't override empty LEDs
					// Don't directly set (use .Clone() or value-by-value), for by-reference improperly overrides the LED values when multiple 'replace' mode layers exist
					R = SelectedColor.R;
					G = SelectedColor.G;
					B = SelectedColor.B;
					Brightness = SelectedColor.Brightness;
				}
				break;
			case LED.BlendingStyle.Replace:
				// Override even with empty LEDs
				// Don't directly set (use .Clone() or value-by-value), for by-reference improperly overrides the LED values when multiple 'replace' mode layers exist
				R = SelectedColor.R;
				G = SelectedColor.G;
				B = SelectedColor.B;
				Brightness = SelectedColor.Brightness;
				break;
			case LED.BlendingStyle.Sum:
				// Sum the colors together without exceeding the maximum
				R = (byte)Math.Min (R + SelectedColor.R, LightSystem.Color_MAX);
				G = (byte)Math.Min (G + SelectedColor.G, LightSystem.Color_MAX);
				B = (byte)Math.Min (B + SelectedColor.B, LightSystem.Color_MAX);
				Brightness = (byte)Math.Min (Brightness + SelectedColor.Brightness, LightSystem.Brightness_MAX);
				break;
			default:
				throw new ArgumentException ("Unexpected blending mode {0}", BlendMode.ToString ());
			}
		}

		public LED Clone ()
		{
			return new LED (R, G, B, Brightness);
		}

	}
}

