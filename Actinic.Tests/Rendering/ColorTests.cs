//
//  ColorTests.cs
//
//  Author:
//       Shane Synan <digitalcircuit36939@gmail.com>
//
//  Copyright (c) 2017
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
using NUnit.Framework;
using System;
using System.Collections;

using Actinic.Rendering;

namespace Actinic.Tests.Rendering
{
	[TestFixture]
	public class ColorTests
	{
		#region Static Content - Named

		/// <summary>
		/// Test cases for named colors
		/// </summary>
		private class NamedColorCases
		{
			public static IEnumerable TestCases {
				get {
					yield return new TestCaseData ("white")
						.Returns (new Color (255, 255, 255));
					yield return new TestCaseData ("black")
						.Returns (new Color (0, 0, 0));
					yield return new TestCaseData ("red")
						.Returns (new Color (255, 0, 0));
					yield return new TestCaseData ("green")
						.Returns (new Color (0, 255, 0));
					yield return new TestCaseData ("blue")
						.Returns (new Color (0, 0, 255));
					yield return new TestCaseData ("yellow")
						.Returns (new Color (255, 255, 0));
					yield return new TestCaseData ("cyan")
						.Returns (new Color (0, 255, 255));
					yield return new TestCaseData ("purple")
						.Returns (new Color (255, 0, 255));
					yield return new TestCaseData ("azure")
						.Returns (new Color (41, 146, 255));
					yield return new TestCaseData ("orange")
						.Returns (new Color (255, 100, 0));
					yield return new TestCaseData ("pink")
						.Returns (new Color (255, 0, 128));
					yield return new TestCaseData ("ambient")
						.Returns (new Color (255, 130, 20));
				}
			}
		}

		[Test, TestCaseSource (typeof(NamedColorCases), "TestCases")]
		public Color Static_Named_Test (string name)
		{
			// [Arrange]
			Color namedColor;

			// [Act/Assert]
			// Make sure this item exists
			Assert.That (Color.Named.Keys,
				Contains.Item (name)
			);
			// Get the item
			namedColor = Color.Named [name];

			// Check the color's correct by returning it
			return namedColor;
		}

		#endregion

		#region Static Properties - Blank

		[Test]
		public void Static_Transparent_Test ()
		{
			// [Arrange]
			Color blankColor = new Color (0, 0, 0, 0);

			// [Act]
			Color resultColor = Color.Transparent;

			// [Assert]
			// Make sure the colors match
			Assert.That (resultColor,
				Is.EqualTo (blankColor)
			);
		}

		[Test]
		public void Static_Transparent_NoReference_Test ()
		{
			// [Arrange]
			// Test colors
			Color blankColor = Color.Transparent;
			Color secondBlankColor = Color.Transparent;
			Color expectedColor = new Color (0, 0, 0, 0);

			// [Act]
			// Change the second color
			secondBlankColor.SetColor (1, 1, 1, 1);

			// [Assert]
			// Make sure the first transparent color is not changed after
			// modification
			Assert.That (blankColor,
				Is.EqualTo (expectedColor)
			);
			// Make sure the Color Transparent color is not changed after
			// modification
			Assert.That (Color.Transparent,
				Is.EqualTo (expectedColor)
			);
		}

		#endregion

		#region Static Functions - FromArgb

		[Test]
		public void Static_FromArgb_Direct ()
		{
			// [Arrange]
			const byte bitR = 255;
			const byte bitG = 128;
			const byte bitB = 64;
			// Default brightness should be MAX
			const byte bitBrightness = Color.MAX;

			// [Act]
			Color resultColor = Color.FromArgb (bitR, bitG, bitB);

			// [Assert]
			// Make sure the colors match
			AssertColorMatches (resultColor, bitR, bitG, bitB, bitBrightness);
		}

		[Test]
		public void Static_FromArgb_Direct_Brightness ()
		{
			// [Arrange]
			const byte bitR = 255;
			const byte bitG = 128;
			const byte bitB = 64;
			const byte bitBrightness = 32;

			// [Act]
			Color resultColor =
				Color.FromArgb (bitR, bitG, bitB, bitBrightness);

			// [Assert]
			// Make sure the colors match
			AssertColorMatches (resultColor, bitR, bitG, bitB, bitBrightness);
		}

		[Test]
		public void Static_FromArgb_Derived ()
		{
			// [Arrange]
			const byte bitR = 100;
			const byte bitG = 128;
			const byte bitB = 64;
			// Brightness is taken as maximum of input
			const byte bitBrightness = 128;

			// [Act]
			Color resultColor = Color.FromArgb (bitR, bitG, bitB, true);

			// [Assert]
			// Make sure the colors match
			AssertColorMatches (resultColor, bitR, bitG, bitB, bitBrightness);
		}

		#endregion

		#region Construction

		[Test]
		public void Construct_Default ()
		{
			// [Arrange]
			const byte bitR = 0;
			const byte bitG = 0;
			const byte bitB = 0;
			const byte bitBrightness = Color.MAX;

			// [Act]
			Color resultColor = new Color ();

			// [Assert]
			// Make sure the colors match
			AssertColorMatches (resultColor, bitR, bitG, bitB, bitBrightness);
		}

		[Test]
		public void Construct_Direct ()
		{
			// [Arrange]
			const byte bitR = 255;
			const byte bitG = 128;
			const byte bitB = 64;
			// Default brightness should be MAX
			const byte bitBrightness = Color.MAX;

			// [Act]
			Color resultColor = new Color (bitR, bitG, bitB);

			// [Assert]
			// Make sure the colors match
			AssertColorMatches (resultColor, bitR, bitG, bitB, bitBrightness);
		}

		[Test]
		public void Construct_Direct_Brightness ()
		{
			// [Arrange]
			const byte bitR = 255;
			const byte bitG = 128;
			const byte bitB = 64;
			const byte bitBrightness = 32;

			// [Act]
			Color resultColor = new Color (bitR, bitG, bitB, bitBrightness);

			// [Assert]
			// Make sure the colors match
			AssertColorMatches (resultColor, bitR, bitG, bitB, bitBrightness);
		}

		[Test]
		public void Construct_Derived ()
		{
			// [Arrange]
			const byte bitR = 100;
			const byte bitG = 128;
			const byte bitB = 64;
			// Brightness is taken as maximum of input
			const byte bitBrightness = 128;

			// [Act]
			Color resultColor =
				Color.FromArgb (bitR, bitG, bitB, true);

			// [Assert]
			// Make sure the colors match
			AssertColorMatches (resultColor, bitR, bitG, bitB, bitBrightness);
		}

		[Test]
		public void Construct_Clone ()
		{
			// [Arrange]
			Color sampleColor =
				new Color (255, 128, 64, 32);

			// [Act]
			Color clonedColor = new Color (sampleColor);

			// [Assert]
			Assert.That (clonedColor,
				Is.EqualTo (sampleColor)
			);
		}

		[Test]
		public void Construct_Null_Throws ()
		{
			// [Arrange]
			const Color invalidColor = null;

			// [Act/Assert]
			Assert.That (
				delegate {
					new Color (invalidColor);
				},
				Throws.TypeOf<ArgumentNullException> ()
				.With.Property ("ParamName").EqualTo ("ClonedColor")
			);
		}

		#endregion

		#region Properties - HasEffect

		[Test]
		public void HasEffect_Test (
			[ValueSource ("GetColorTestChoices")] ColorTestChoice chosenTest,
			[Values (0, 1, 255)] byte colorBit)
		{
			// [Arrange]
			// Test colors
			Color hasEffectColor = new Color ();
			switch (chosenTest) {
			case ColorTestChoice.R:
				hasEffectColor.SetColor (colorBit, 0, 0, 0);
				break;
			case ColorTestChoice.G:
				hasEffectColor.SetColor (0, colorBit, 0, 0);
				break;
			case ColorTestChoice.B:
				hasEffectColor.SetColor (0, 0, colorBit, 0);
				break;
			case ColorTestChoice.Brightness:
				hasEffectColor.SetColor (0, 0, 0, colorBit);
				break;
			default:
				Assert.Fail ("Unknown ColorTestChoice!");
				break;
			}
			bool expectEffect = (colorBit != 0);

			// [Act]
			bool hasEffect = hasEffectColor.HasEffect;

			// [Assert]
			Assert.That (hasEffect,
				Is.EqualTo (expectEffect)
			);
		}

		#endregion

		#region Functions - Blend

		/// <summary>
		/// Test cases for blending by simple Additive/Fade with Opacity
		/// </summary>
		private class BlendSimpleColorCases
		{
			public static IEnumerable TestCases {
				get {
					// [Additive]
					// Full opacity
					yield return new TestCaseData (
						new Color (65, 128, 64, 32),
						new Color (64, 128, 255, 100),
						1,
						false
					).Returns (new Color (65, 128, 255, 100));
					// 0.5 opacity
					yield return new TestCaseData (
						new Color (65, 128, 64, 32),
						new Color (64, 128, 255, 100),
						0.5,
						false
					).Returns (new Color (65, 128, 127, 50));
					// 0 opacity
					yield return new TestCaseData (
						new Color (65, 128, 64, 32),
						new Color (64, 128, 255, 100),
						0,
						false
					).Returns (new Color (65, 128, 64, 32));

					// [Fade]
					// Full opacity = all new color
					yield return new TestCaseData (
						new Color (65, 128, 64, 32),
						new Color (64, 128, 255, 100),
						1,
						true
					).Returns (new Color (64, 128, 255, 100));
					// 0.985 opacity = mostly new color
					// This catches rounding errors from loss of precision
					yield return new TestCaseData (
						new Color (0, 255, 200, 2),
						new Color (0, 240, 200, 2),
						0.93,
						true
					).Returns (new Color (0, 241, 200, 2));
					// 0.75 opacity = 75% new, 25% old
					yield return new TestCaseData (
						new Color (65, 128, 64, 32),
						new Color (64, 128, 255, 100),
						0.75,
						true
					).Returns (new Color (64, 128, 207, 83));
					// 0.5 opacity = half new, half old
					yield return new TestCaseData (
						new Color (65, 128, 64, 32),
						new Color (64, 128, 255, 100),
						0.5,
						true
					).Returns (new Color (64, 128, 159, 66));
					// 0.015 opacity = mostly current color
					// This catches rounding errors from loss of precision
					yield return new TestCaseData (
						new Color (0, 255, 200, 2),
						new Color (0, 240, 200, 2),
						0.015,
						true
					).Returns (new Color (0, 254, 200, 2));
					// 0 opacity = all current color
					yield return new TestCaseData (
						new Color (65, 128, 64, 32),
						new Color (64, 128, 255, 100),
						0,
						true
					).Returns (new Color (65, 128, 64, 32));
				}
			}
		}

		[Test, TestCaseSource (typeof(BlendSimpleColorCases), "TestCases")]
		public Color Blend_Simple_Test (
			Color SourceColor, Color OtherColor, double Opacity, bool Fade)
		{
			// [Arrange]
			Color resultColor = SourceColor;

			// [Act]
			resultColor.Blend (OtherColor, Opacity, Fade);

			// [Assert]
			// Check the color's correct by returning it
			return resultColor;
		}

		/// <summary>
		/// Test cases for blending by BlendMode
		/// </summary>
		private class BlendModeColorCases
		{
			public static IEnumerable TestCases {
				get {
					// [Combine]
					// Take brightest colors
					yield return new TestCaseData (
						new Color (65, 128, 64, 32),
						new Color (64, 128, 255, 100),
						Color.BlendMode.Combine
					).Returns (new Color (65, 128, 255, 100));

					// [Favor]
					// Bias towards second color according to brightness level
					// > Full
					yield return new TestCaseData (
						new Color (65, 128, 64, 32),
						new Color (64, 128, 255, 255),
						Color.BlendMode.Favor
					).Returns (new Color (64, 128, 255, 255));
					// > Mixed, simple
					yield return new TestCaseData (
						new Color (0, 255, 0, 0),
						new Color (255, 0, 255, 128),
						Color.BlendMode.Favor
					).Returns (new Color (128, 127, 128, 64));
					// > Mixed, complex
					yield return new TestCaseData (
						new Color (65, 128, 64, 32),
						new Color (64, 128, 255, 100),
						Color.BlendMode.Favor
					).Returns (new Color (64, 128, 138, 58));
					// > Without any effect
					yield return new TestCaseData (
						new Color (65, 128, 64, 32),
						new Color (64, 128, 255, 0),
						Color.BlendMode.Favor
					).Returns (new Color (65, 128, 64, 32));

					// [Mask]
					// Replace with second color if it has any effect
					// > Has effect, brightness
					yield return new TestCaseData (
						new Color (65, 128, 64, 32),
						new Color (0, 0, 0, 255),
						Color.BlendMode.Mask
					).Returns (new Color (0, 0, 0, 255));
					// > Has effect, color
					yield return new TestCaseData (
						new Color (65, 128, 64, 32),
						new Color (64, 128, 255, 0),
						Color.BlendMode.Mask
					).Returns (new Color (64, 128, 255, 0));
					// > Without any effect
					yield return new TestCaseData (
						new Color (65, 128, 64, 32),
						new Color (0, 0, 0, 0),
						Color.BlendMode.Mask
					).Returns (new Color (65, 128, 64, 32));

					// [Mask]
					// Replace with second color all the time
					// > Has effect, brightness
					yield return new TestCaseData (
						new Color (65, 128, 64, 32),
						new Color (0, 0, 0, 255),
						Color.BlendMode.Replace
					).Returns (new Color (0, 0, 0, 255));
					// > Has effect, color
					yield return new TestCaseData (
						new Color (65, 128, 64, 32),
						new Color (64, 128, 255, 0),
						Color.BlendMode.Replace
					).Returns (new Color (64, 128, 255, 0));
					// > Without any effect
					yield return new TestCaseData (
						new Color (65, 128, 64, 32),
						new Color (0, 0, 0, 0),
						Color.BlendMode.Replace
					).Returns (new Color (0, 0, 0, 0));

					// [Sum]
					// Combine colors together, not exceeding maximum
					// > Simple
					yield return new TestCaseData (
						new Color (1, 0, 5, 32),
						new Color (1, 3, 0, 32),
						Color.BlendMode.Sum
					).Returns (new Color (2, 3, 5, 64));
					// > Exceeding bounds
					yield return new TestCaseData (
						new Color (255, 255, 0, 100),
						new Color (255, 0, 255, 50),
						Color.BlendMode.Sum
					).Returns (new Color (255, 255, 255, 150));
					// > Without any effect
					yield return new TestCaseData (
						new Color (65, 128, 64, 32),
						new Color (0, 0, 0, 0),
						Color.BlendMode.Sum
					).Returns (new Color (65, 128, 64, 32));
				}
			}
		}

		[Test, TestCaseSource (typeof(BlendModeColorCases), "TestCases")]
		public Color Blend_Mode_Test (
			Color SourceColor, Color OtherColor, Color.BlendMode Blending)
		{
			// [Arrange]
			Color resultColor = SourceColor;

			// [Act]
			resultColor.Blend (OtherColor, Blending);

			// [Assert]
			// Check the color's correct by returning it
			return resultColor;
		}

		[Test]
		public void Blend_Mode_Optional_Fade_Test ()
		{
			// [Arrange]
			// Test colors
			Color sampleColor = new Color (255, 0, 0);
			Color newColor = new Color (0, 255, 0);
			// Desired result
			Color expectedColor = new Color (255, 127, 0);

			// [Act]
			// Blend the colors
			sampleColor.Blend (newColor, 0.5);

			// [Assert]
			// Make sure the sample color is the expected result, ensuring that
			// fade is not chosen by default.
			Assert.That (sampleColor,
				Is.EqualTo (expectedColor)
			);
		}

		[Test]
		public void Blend_NoReference_Test ()
		{
			// [Arrange]
			// Test colors
			Color maskedColor = new Color (255, 0, 0);
			Color replacedColor = new Color (255, 0, 0);
			// Other color
			Color expectedColor = new Color (0, 255, 0);
			Color secondColor = new Color (0, 255, 0);

			// [Act]
			// Blend the colors
			maskedColor.Blend (secondColor, Color.BlendMode.Mask);
			replacedColor.Blend (secondColor, Color.BlendMode.Replace);

			// Change the second color
			secondColor.B = 255;

			// [Assert]
			// Make sure the masked/replaced color is not changed after blend
			Assert.That (maskedColor,
				Is.EqualTo (expectedColor)
			);
			Assert.That (replacedColor,
				Is.EqualTo (expectedColor)
			);
		}

		[Test]
		public void Blend_InvalidOpacity_Throws ()
		{
			// [Arrange]
			Color sampleColor = new Color ();
			double invalidOpacityPositive = 1.1;
			double invalidOpacityNegative = -0.1;

			// [Act/Assert]
			Assert.That (
				delegate {
					sampleColor.Blend (
						new Color (), invalidOpacityPositive, false
					);
				},
				Throws.TypeOf<ArgumentOutOfRangeException> ()
				.With.Property ("ParamName").EqualTo ("Opacity")
			);

			Assert.That (
				delegate {
					sampleColor.Blend (
						new Color (), invalidOpacityNegative, false
					);
				},
				Throws.TypeOf<ArgumentOutOfRangeException> ()
				.With.Property ("ParamName").EqualTo ("Opacity")
			);
		}

		[Test]
		public void Blend_NullColor_Throws ()
		{
			// [Arrange]
			Color sampleColor = new Color ();
			const Color invalidColor = null;

			// [Act/Assert]
			Assert.That (
				delegate {
					sampleColor.Blend (invalidColor, 0.5, false);
				},
				Throws.TypeOf<ArgumentNullException> ()
				.With.Property ("ParamName").EqualTo ("UpperColor")
			);

			Assert.That (
				delegate {
					sampleColor.Blend (invalidColor, Color.BlendMode.Combine);
				},
				Throws.TypeOf<ArgumentNullException> ()
				.With.Property ("ParamName").EqualTo ("UpperColor")
			);
		}

		#endregion

		#region Functions - SetColor

		[Test]
		public void SetColor_Direct ()
		{
			// [Arrange]
			const byte bitR = 255;
			const byte bitG = 128;
			const byte bitB = 64;
			const byte bitBrightness = Color.MAX;
			Color resultColor = new Color (1, 1, 1, 1);

			// [Act]
			resultColor.SetColor (bitR, bitG, bitB);

			// [Assert]
			// Make sure the colors match
			AssertColorMatches (resultColor, bitR, bitG, bitB, bitBrightness);
		}

		[Test]
		public void SetColor_Direct_Brightness ()
		{
			// [Arrange]
			const byte bitR = 255;
			const byte bitG = 128;
			const byte bitB = 64;
			const byte bitBrightness = 32;
			Color resultColor = new Color (1, 1, 1, 1);

			// [Act]
			resultColor.SetColor (bitR, bitG, bitB, bitBrightness);

			// [Assert]
			// Make sure the colors match
			AssertColorMatches (resultColor, bitR, bitG, bitB, bitBrightness);
		}

		[Test]
		public void SetColor_Derived (
			[ValueSource ("GetColorTestChoices")] ColorTestChoice chosenTest
		)
		{
			// [Arrange]
			// Test colors
			const byte colorHighBit = 128;
			const byte colorLowBit = 64;
			// Brightness is determined by taking the maximum of input colors
			byte expectedBrightness = Math.Max (colorHighBit, colorLowBit);
			Color resultColor = new Color ();

			// [Act]
			switch (chosenTest) {
			case ColorTestChoice.R:
				resultColor.SetColor (
					colorHighBit, colorLowBit, colorLowBit, true
				);
				break;
			case ColorTestChoice.G:
				resultColor.SetColor (
					colorLowBit, colorHighBit, colorLowBit, true
				);
				break;
			case ColorTestChoice.B:
				resultColor.SetColor (
					colorLowBit, colorLowBit, colorHighBit, true
				);
				break;
			case ColorTestChoice.Brightness:
				Assert.Pass ("Brightness does not apply to SetColor_Derived");
				break;
			default:
				Assert.Fail ("Unknown ColorTestChoice!");
				break;
			}

			// [Assert]
			// Make sure the brightness is as expected
			Assert.That (resultColor.Brightness,
				Is.EqualTo (expectedBrightness)
			);
		}

		#endregion

		#region Functions - Clone

		[Test]
		public void Clone_NoReference_Test ()
		{
			// [Arrange]
			// Test colors
			Color originalColor = new Color (2, 2, 2, 2);
			Color expectedColor = new Color (2, 2, 2, 2);
			Color clonedColor = expectedColor.Clone ();

			// [Act]
			// Change the second color
			clonedColor.SetColor (1, 1, 1, 1);

			// [Assert]
			// Make sure the original color is not changed after modification
			Assert.That (originalColor,
				Is.EqualTo (expectedColor)
			);
		}

		#endregion

		#region Functions - Equals

		[Test]
		public void Equals_Same_Test ()
		{
			// [Arrange]
			// Test colors
			Color startColor = new Color (128, 128, 128, 128);
			Color otherColor = new Color (128, 128, 128, 128);

			// [Act/Assert]
			Assert.That (startColor,
				Is.EqualTo (otherColor)
			);
		}

		[Test]
		public void Equals_Different_Test (
			[ValueSource ("GetColorTestChoices")] ColorTestChoice chosenTest)
		{
			// [Arrange]
			// Test colors
			Color startColor = new Color (128, 128, 128, 128);
			Color modifiedColor = new Color (128, 128, 128, 128);

			switch (chosenTest) {
			case ColorTestChoice.R:
				modifiedColor.R = Color.MAX;
				break;
			case ColorTestChoice.G:
				modifiedColor.G = Color.MAX;
				break;
			case ColorTestChoice.B:
				modifiedColor.B = Color.MAX;
				break;
			case ColorTestChoice.Brightness:
				modifiedColor.Brightness = Color.MAX;
				break;
			default:
				Assert.Fail ("Unknown ColorTestChoice!");
				break;
			}

			// [Act/Assert]
			Assert.That (startColor,
				Is.Not.EqualTo (modifiedColor)
			);
		}

		#endregion

		#region Functions - ToString

		[Test]
		public void ToString_Test ()
		{
			// [Arrange]
			const byte bitR = 255;
			const byte bitG = 128;
			const byte bitB = 64;
			const byte bitBrightness = 32;
			Color sampleColor = new Color (bitR, bitG, bitB, bitBrightness);

			// Prepare the expected result of converting to string
			string expectedResult =
				string.Format (
					"[Color: R={0,-3}, G={1,-3}, B={2,-3}, Brightness={3,-3}]",
					bitR, bitG, bitB, bitBrightness
				);

			// [Act/Assert]
			Assert.That (sampleColor.ToString (),
				Is.EqualTo (expectedResult)
			);
		}

		#endregion

		#region Internal

		/// <summary>
		/// Possible color properties to test.
		/// </summary>
		public enum ColorTestChoice
		{
			R,
			G,
			B,
			Brightness
		}

		/// <summary>
		/// Gets the possible choices of color properties to test.
		/// </summary>
		/// <returns>The color test choices.</returns>
		private Array GetColorTestChoices ()
		{
			// See https://automaticchainsaw.blogspot.com/2010/04/unit-test-all-enum-values-with-nunit.html
			return Enum.GetValues (typeof(ColorTestChoice));
		}

		/// <summary>
		/// Asserts that the given color matches expectations.
		/// </summary>
		/// <param name="testColor">Color to test.</param>
		/// <param name="Red">Red intensity.</param>
		/// <param name="Green">Green intensity.</param>
		/// <param name="Blue">Blue intensity.</param>
		/// <param name="Brightness">Brightness.</param>
		private void AssertColorMatches (
			Color testColor, byte Red, byte Green, byte Blue, byte Brightness)
		{
			// Make sure the colors match
			Assert.That (testColor.R,
				Is.EqualTo (Red)
			);
			Assert.That (testColor.G,
				Is.EqualTo (Green)
			);
			Assert.That (testColor.B,
				Is.EqualTo (Blue)
			);
			Assert.That (testColor.Brightness,
				Is.EqualTo (Brightness)
			);
		}

		#endregion
	}
}

