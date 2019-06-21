//
//  Color.cs
//
//  Author:
//       Shane Synan <digitalcircuit36939@gmail.com>
//
//  Copyright (c) 2013 - 2017
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

namespace Actinic.Rendering
{
	/// <summary>
	/// A single color/brightness pair, functioning like a screen pixel.
	/// </summary>
	public class Color : IEquatable<Color>
	{
		#region Static Content

		/// <summary>
		/// A list of named colors for easy reference.
		/// </summary>
		public static readonly Dictionary<string, Color> Named =
			new Dictionary<string, Color> {
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
				{ "ambient", new Color (255, 100, 30) }
			};

		/// <summary>
		/// Gets the transparent color, i.e. all black with no brightness.
		/// </summary>
		/// <value>The transparent color.</value>
		public static Color Transparent {
			get {
				return new Color (0, 0, 0, 0);
			}
		}

		/// <summary>
		/// The value representing full intensity.
		/// </summary>
		public const byte MAX = byte.MaxValue;

		#endregion

		#region Static Functions

		/// <summary>
		/// Gets a new instance of <see cref="Color"/> with the specified
		/// values.  Brightness is assumed maximum.
		/// </summary>
		/// <param name="Red">Red intensity.</param>
		/// <param name="Green">Green intensity.</param>
		/// <param name="Blue">Blue intensity.</param>
		/// <param name="DeriveBrightness">If set to <c>true</c> derive brightness from color, otherwise set it to maximum.</param>
		public static Color FromArgb (
			byte Red, byte Green, byte Blue, bool DeriveBrightness = false)
		{
			return new Color (Red, Green, Blue, DeriveBrightness);
		}

		/// <summary>
		/// Gets a new instance of <see cref="Color"/> with the specified
		/// values.
		/// </summary>
		/// <param name="Red">Red intensity.</param>
		/// <param name="Green">Green intensity.</param>
		/// <param name="Blue">Blue intensity.</param>
		/// <param name="Brightness">Brightness.</param>
		public static Color FromArgb (
			byte Red, byte Green, byte Blue, byte Intensity)
		{
			return new Color (Red, Green, Blue, Intensity);
		}

		#endregion

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="Color"/> class, setting
		/// to absence of light, i.e. black.
		/// </summary>
		public Color () : this (0, 0, 0, MAX)
		{
		}

		/// <summary>
		/// Initializes a new instance of <see cref="Color"/>, setting to the
		/// specified values.  Brightness is assumed maximum.
		/// </summary>
		/// <param name="Red">Red intensity.</param>
		/// <param name="Green">Green intensity.</param>
		/// <param name="Blue">Blue intensity.</param>
		/// <param name="DeriveBrightness">If set to <c>true</c> derive brightness from color, otherwise set it to maximum.</param>
		public Color (
			byte Red, byte Green, byte Blue, bool DeriveBrightness = false)
		{
			SetColor (Red, Green, Blue, DeriveBrightness);
		}

		/// <summary>
		/// Initializes a new instance of <see cref="Color"/>, setting to the
		/// specified values.
		/// </summary>
		/// <param name="Red">Red intensity.</param>
		/// <param name="Green">Green intensity.</param>
		/// <param name="Blue">Blue intensity.</param>
		/// <param name="Brightness">Brightness.</param>
		public Color (byte Red, byte Green, byte Blue, byte Brightness)
		{
			SetColor (Red, Green, Blue, Brightness);
		}

		/// <summary>
		/// Initializes a new instance of <see cref="Color"/>, cloning an
		/// existing color.
		/// </summary>
		/// <param name="ClonedColor">Color to clone.</param>
		public Color (Color ClonedColor)
		{
			if (ClonedColor == null) {
				// Need a valid color
				throw new ArgumentNullException ("ClonedColor");
			}

			SetColor (ClonedColor);
		}

		#endregion

		#region Properties

		/// <summary>
		/// Modes for blending multiple layers together
		/// </summary>
		public enum BlendMode
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

		/// <summary>
		/// Gets or sets the red intensity.
		/// </summary>
		/// <value>The red intensity.</value>
		public byte R {
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the green intensity.
		/// </summary>
		/// <value>The green intensity.</value>
		public byte G {
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the blue intensity.
		/// </summary>
		/// <value>The blue intensity.</value>
		public byte B {
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the brightness.
		/// </summary>
		/// <value>The brightness.</value>
		public byte Brightness {
			get;
			set;
		}

		/// <summary>
		/// Gets a value indicating whether this color has any effect on output,
		/// i.e. not all black with no brightness.
		/// </summary>
		/// <value><c>true</c> if this color has an effect; otherwise, <c>false</c>.</value>
		public bool HasEffect {
			get {
				// Has an effect if the red, green, blue, or brightness values
				// are set
				if ((R != 0) || (G != 0) || (B != 0) || (Brightness != 0)) {
					return true;
				} else {
					return false;
				}
			}
		}

		#endregion

		#region Shared Functions

		/// <summary>
		/// Blends the color and brightness of the given (upper) color into
		/// this (lower) color.
		/// </summary>
		/// <param name="UpperColor">Upper color, blended into this one.</param>
		/// <param name="Opacity">Strength of the upper color's influence as a decimal from 0 to 1.</param>
		/// <param name="Fade">If set to <c>true</c> fades between current color and new color with <see cref="Opacity"/> specifying the fade amount.</param>
		public void Blend (
			Color UpperColor, double Opacity, bool Fade = false)
		{
			if (Opacity < 0 || Opacity > 1) {
				throw new ArgumentOutOfRangeException (
					"Opacity", "Opacity must be a value between 0 and 1."
				);
			}

			if (UpperColor == null) {
				// Need a valid color
				throw new ArgumentNullException ("UpperColor");
			}

			if (Fade) {
				// Reduce the current color by the inverse opacity of the new
				// color.  This will have 0 as entirely current, 1 as entirely
				// new, and 0.5 as half current, half new.
				double inverseOpacity = 1 - Opacity;
				// Sum the faded old and new colors
				// NOTE: Do not convert to byte until the end, otherwise loss of
				// precision will affect the result.
				R = (byte)((R * inverseOpacity) + (UpperColor.R * Opacity));
				G = (byte)((G * inverseOpacity) + (UpperColor.G * Opacity));
				B = (byte)((B * inverseOpacity) + (UpperColor.B * Opacity));
				Brightness = (byte)(
				    (Brightness * inverseOpacity)
				    + (UpperColor.Brightness * Opacity)
				);
			} else {
				// Reduce the intensity of the new color
				Color opacifiedNewColor =
					new Color (
						(byte)(UpperColor.R * Opacity),
						(byte)(UpperColor.G * Opacity),
						(byte)(UpperColor.B * Opacity),
						(byte)(UpperColor.Brightness * Opacity)
					);
				
				// Take the brightest colors
				R = Math.Max (R, opacifiedNewColor.R);
				G = Math.Max (G, opacifiedNewColor.G);
				B = Math.Max (B, opacifiedNewColor.B);
				Brightness = Math.Max (
					Brightness, opacifiedNewColor.Brightness
				);
			}
		}

		/// <summary>
		/// Blends color and brightness of the given (upper) color into this
		/// (lower) color.
		/// </summary>
		/// <param name="UpperColor">Upper color, blended into this one.</param>
		/// <param name="BlendMode">Mode for blending upper color into this color.</param>
		public void Blend (Color UpperColor, BlendMode Blending)
		{
			if (UpperColor == null) {
				// Need a valid color
				throw new ArgumentNullException ("UpperColor");
			}

			switch (Blending) {
			case BlendMode.Combine:
				// Take the brightest colors
				R = Math.Max (R, UpperColor.R);
				G = Math.Max (G, UpperColor.G);
				B = Math.Max (B, UpperColor.B);
				Brightness = Math.Max (Brightness, UpperColor.Brightness);
				break;
			case BlendMode.Favor:
				if (UpperColor.HasEffect) {
					// Overwrite the original with the new layer
					// Brightness controls the amount overriden
					double favor_amt =
						MathUtilities.ConvertRange (
							UpperColor.Brightness, 0, MAX, 0.0, 1.0
						);
					R = ((byte)Math.Min (
						((UpperColor.R * favor_amt) + (R * (1 - favor_amt))),
						MAX
					));
					G = ((byte)Math.Min (
						((UpperColor.G * favor_amt) + (G * (1 - favor_amt))),
						MAX
					));
					B = ((byte)Math.Min (
						((UpperColor.B * favor_amt) + (B * (1 - favor_amt))),
						MAX
					));
					Brightness = ((byte)Math.Min (
						((UpperColor.Brightness * favor_amt)
						+ (Brightness * (1 - favor_amt))),
						MAX
					));
				}
				break;
			case BlendMode.Mask:
				if (UpperColor.HasEffect) {
					// Don't override empty colors
					// Don't directly set (use .Clone() or value-by-value), for
					// by-reference improperly overrides the color values when
					// multiple 'replace' mode layers exist
					R = UpperColor.R;
					G = UpperColor.G;
					B = UpperColor.B;
					Brightness = UpperColor.Brightness;
				}
				break;
			case BlendMode.Replace:
				// Override even with empty colors
				// Don't directly set (use .Clone() or value-by-value), for
				// by-reference improperly overrides the color values when
				// multiple 'replace' mode layers exist
				R = UpperColor.R;
				G = UpperColor.G;
				B = UpperColor.B;
				Brightness = UpperColor.Brightness;
				break;
			case BlendMode.Sum:
				// Sum the colors together without exceeding the maximum
				R = (byte)Math.Min (R + UpperColor.R, MAX);
				G = (byte)Math.Min (G + UpperColor.G, MAX);
				B = (byte)Math.Min (B + UpperColor.B, MAX);
				Brightness =
					(byte)Math.Min (Brightness + UpperColor.Brightness, MAX);
				break;
			default:
				throw new ArgumentException (
					"Unexpected blending mode {0}", Blending.ToString ()
				);
			}
		}

		/// <summary>
		/// Sets the color and brightness to the defined color.
		/// </summary>
		/// <param name="NewColor">New color and brightness pair.</param>
		public void SetColor (Color NewColor)
		{
			if (NewColor == null) {
				// Need a valid color
				throw new ArgumentNullException ("NewColor");
			}

			SetColor (NewColor.R, NewColor.G, NewColor.B, NewColor.Brightness);
		}

		/// <summary>
		/// Sets the color, optionally calculating brightness.
		/// </summary>
		/// <param name="Red">Red intensity.</param>
		/// <param name="Green">Green intensity.</param>
		/// <param name="Blue">Blue intensity.</param>
		/// <param name="DeriveBrightness">If set to <c>true</c> derive brightness from color, otherwise set it to maximum.</param>
		public void SetColor (
			byte Red, byte Green, byte Blue, bool DeriveBrightness = false)
		{
			// If deriving brightness, calculate it from the Red/Green/Blue
			// components.  Otherwise, set it to the maximum brightness.
			SetColor (
				Red, Green, Blue,
				DeriveBrightness ? FindBrightness (Red, Green, Blue) : MAX
			);
		}

		/// <summary>
		/// Sets the color and brightness.
		/// </summary>
		/// <param name="Red">Red intensity.</param>
		/// <param name="Green">Green intensity.</param>
		/// <param name="Blue">Blue intensity.</param>
		/// <param name="Brightness">Brightness.</param>
		public void SetColor (byte Red, byte Green, byte Blue, byte Brightness)
		{
			this.R = Red;
			this.G = Green;
			this.B = Blue;
			this.Brightness = Brightness;
		}

		/// <summary>
		/// Clone this instance.
		/// </summary>
		public Color Clone ()
		{
			return new Color (this);
		}

		/// <summary>
		/// Determines whether the specified <see cref="Color"/> is equal to the
		/// current <see cref="Color"/>.
		/// </summary>
		/// <param name="other">The <see cref="Color"/> to compare with the current <see cref="Color"/>.</param>
		/// <returns><c>true</c> if the specified <see cref="Color"/> is equal to the current <see cref="Color"/>; otherwise, <c>false</c>.</returns>
		public bool Equals (Color other)
		{
			// Only equal if all fields match
			return (
			    (R == other.R)
			    && (G == other.G)
			    && (B == other.B)
			    && (Brightness == other.Brightness)
			);
		}

		/// <summary>
		/// Returns a <see cref="System.String"/> that represents the current
		/// <see cref="Color"/>.
		/// </summary>
		/// <returns>A <see cref="System.String"/> that represents the current <see cref="Color"/>.</returns>
		public override string ToString ()
		{
			return string.Format (
				"[Color: R={0,-3}, G={1,-3}, B={2,-3}, Brightness={3,-3}]",
				R, G, B, Brightness
			);
		}

		#endregion

		#region Internal

		/// <summary>
		/// Finds the brightness from the given color.
		/// </summary>
		/// <returns>The calculated brightness.</returns>
		/// <param name="GivenColor">Color with Red, Green, Blue intensities.</param>
		private static byte FindBrightness (Color GivenColor)
		{
			if (GivenColor == null) {
				// Need a valid color
				throw new ArgumentNullException ("GivenColor");
			}

			return FindBrightness (GivenColor.R, GivenColor.G, GivenColor.B);
		}

		/// <summary>
		/// Finds the brightness from the given color.
		/// </summary>
		/// <returns>The calculated brightness.</returns>
		/// <param name="Red">Red intensity.</param>
		/// <param name="Green">Green intensity.</param>
		/// <param name="Blue">Blue intensity.</param>
		private static byte FindBrightness (byte Red, byte Green, byte Blue)
		{
			// Take the brightest color and use it for the color's brightness
			return Math.Max (Red, Math.Max (Green, Blue));
		}

		#endregion
	}
}

