//
//  LayerTests.cs
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

// For testing GetEnumerator()
using System.Linq;

using Actinic.Rendering;

namespace Actinic.Tests.Rendering
{
	[TestFixture]
	public class LayerTests
	{
		#region Construction

		[Test]
		public void Construct_Direct ()
		{
			// [Arrange]
			const int lightCount = 23;
			const Color.BlendMode expectBlend = Color.BlendMode.Combine;
			const bool expectHasEffect = true;

			// [Act]
			Layer resultLayer = new Layer (lightCount);

			// [Assert]
			// Make sure the count is accurate
			Assert.That (resultLayer.PixelCount,
				Is.EqualTo (lightCount)
			);
			// Verify the default blend mode
			Assert.That (resultLayer.Blending,
				Is.EqualTo (expectBlend)
			);
			// Check that the colors are created with effect
			Assert.That (resultLayer.HasEffect,
				Is.EqualTo (expectHasEffect)
			);
		}

		[Test]
		public void Construct_Direct_BlendMode ()
		{
			// [Arrange]
			const int lightCount = 23;
			const Color.BlendMode expectBlend = Color.BlendMode.Replace;
			const bool expectHasEffect = true;

			// [Act]
			Layer resultLayer = new Layer (lightCount, expectBlend);

			// [Assert]
			// Make sure the count is accurate
			Assert.That (resultLayer.PixelCount,
				Is.EqualTo (lightCount)
			);
			// Verify the specified blend mode
			Assert.That (resultLayer.Blending,
				Is.EqualTo (expectBlend)
			);
			// Check that the colors are created with effect
			Assert.That (resultLayer.HasEffect,
				Is.EqualTo (expectHasEffect)
			);
		}

		[Test]
		public void Construct_Direct_BlendMode_FillColor ()
		{
			// [Arrange]
			const int lightCount = 23;
			const Color.BlendMode expectBlend = Color.BlendMode.Replace;
			const bool expectHasEffect = true;
			Color expectColor = new Color (255, 128, 64, 32);

			// [Act]
			Layer resultLayer =
				new Layer (lightCount, expectBlend, expectColor);

			// [Assert]
			// Make sure the count is accurate
			Assert.That (resultLayer.PixelCount,
				Is.EqualTo (lightCount)
			);
			// Verify the specified blend mode
			Assert.That (resultLayer.Blending,
				Is.EqualTo (expectBlend)
			);
			// Check that the colors are created with effect
			Assert.That (resultLayer.HasEffect,
				Is.EqualTo (expectHasEffect)
			);
			// Check that it's the right color
			Assert.That (resultLayer.GetPixels (),
				Has.All.EqualTo (expectColor)
			);
		}

		[Test]
		public void Construct_Clone ()
		{
			// [Arrange]
			const int lightCount = 23;
			const Color.BlendMode expectBlend = Color.BlendMode.Replace;
			const bool expectHasEffect = true;
			Color sampleColorOriginal = new Color (255, 128, 64, 32);
			Color sampleColorModified = new Color (255, 128, 64, 32);
			// Set up the sample layer
			Layer sampleLayer = new Layer (lightCount, expectBlend);
			sampleLayer [0].SetColor (sampleColorOriginal);
			sampleLayer [lightCount - 1].SetColor (sampleColorOriginal);

			// [Act]
			Layer resultLayer = new Layer (sampleLayer);
			// Modify the sample layer after cloning, which should NOT be
			// reflected in the cloned layer
			sampleLayer [0].SetColor (sampleColorModified);

			// [Assert]
			// Make sure the count is accurate
			Assert.That (resultLayer.PixelCount,
				Is.EqualTo (lightCount)
			);
			// Verify the default blend mode
			Assert.That (resultLayer.Blending,
				Is.EqualTo (expectBlend)
			);
			// Check that the colors are created with effect
			Assert.That (resultLayer.HasEffect,
				Is.EqualTo (expectHasEffect)
			);
			// Check that the colors are cloned
			Assert.That (resultLayer [0],
				Is.EqualTo (sampleColorOriginal)
			);
			Assert.That (resultLayer [lightCount - 1],
				Is.EqualTo (sampleColorOriginal)
			);
			// And check that the sample layer is correct
			Assert.That (sampleLayer [0],
				Is.EqualTo (sampleColorModified)
			);
		}

		[Test]
		public void Construct_InvalidLights_Throws ()
		{
			// [Arrange]
			const int invalidPixelCount = 0;

			// [Act/Assert]
			Assert.That (
				delegate {
					new Layer (invalidPixelCount);
				},
				Throws.TypeOf<ArgumentOutOfRangeException> ()
				.With.Property ("ParamName").EqualTo ("PixelCount")
			);
		}

		[Test]
		public void Construct_Null_Throws ()
		{
			// [Arrange]
			const Layer invalidLayer = null;

			// [Act/Assert]
			Assert.That (
				delegate {
					new Layer (invalidLayer);
				},
				Throws.TypeOf<ArgumentNullException> ()
				.With.Property ("ParamName").EqualTo ("ClonedLayer")
			);
		}

		#endregion

		#region Properties - Indexer

		[Test]
		public void Indexer_GetSet_Test ()
		{
			// [Arrange]
			// Test layer
			Layer sampleLayer = new Layer (3);
			// Test colors
			Color colorA = new Color (255, 128, 64);
			Color colorB = new Color (128, 255, 128);
			Color colorC = new Color (64, 128, 255);

			// [Act]
			sampleLayer [0] = colorA;
			sampleLayer [1] = colorB;
			sampleLayer [2] = colorC;

			// [Assert]
			Assert.That (sampleLayer [0],
				Is.EqualTo (colorA)
			);
			Assert.That (sampleLayer [1],
				Is.EqualTo (colorB)
			);
			Assert.That (sampleLayer [2],
				Is.EqualTo (colorC)
			);
		}

		[Test]
		public void Indexer_Reference_Test ()
		{
			// [Arrange]
			// Test layer
			Layer sampleLayer = new Layer (2);
			// Test colors
			Color sampleColor = new Color (255, 128, 0);
			Color expectedColor = new Color (128, 128, 0);
			// Set up layer colors
			sampleLayer [0] = sampleColor;
			sampleLayer [1] = sampleColor;

			// [Act]
			// Change the second color, should update the first color and the
			// sample color
			sampleLayer [1].R = 128;

			// [Assert]
			Assert.That (sampleColor,
				Is.EqualTo (expectedColor)
			);
			Assert.That (sampleLayer [0],
				Is.EqualTo (expectedColor)
			);
			Assert.That (sampleLayer [1],
				Is.EqualTo (expectedColor)
			);
		}

		[Test]
		public void Indexer_InvalidIndex_Throws (
			[Values (-1, 2)] int invalidIndex)
		{
			// [Arrange]
			// Test layer
			Layer sampleLayer = new Layer (2);

			// Prepare the expected result of converting to string
			string expectedResult =
				string.Format (
					"Pixel index of {0} out of bounds.  Pixel index must " +
					"be within 0 to PixelCount-1 ({1}).",
					invalidIndex, 1
				);

			// [Act/Assert]
			// Getting
			Assert.That (
				delegate {
					var dummy = sampleLayer [invalidIndex];
				},
				Throws.TypeOf<IndexOutOfRangeException> ()
				.With.Property ("Message").EqualTo (expectedResult)
			);

			// Setting
			Assert.That (
				delegate {
					sampleLayer [invalidIndex] = null;
				},
				Throws.TypeOf<IndexOutOfRangeException> ()
				.With.Property ("Message").EqualTo (expectedResult)
			);
		}

		#endregion

		#region Properties - PixelCount

		[Test]
		public void PixelCount_Test ()
		{
			// [Arrange]
			const int samplePixelCount = 3;
			// Test layer
			Layer sampleLayer = new Layer (samplePixelCount);

			// [Act]
			int resultCount = sampleLayer.PixelCount;

			// [Assert]
			Assert.That (resultCount,
				Is.EqualTo (samplePixelCount)
			);
		}

		#endregion

		#region Properties - Blending

		[Test]
		public void Blending_Test ()
		{
			// [Arrange]
			// Test layer
			Layer sampleLayer = new Layer (5, Color.BlendMode.Combine);
			Color.BlendMode sampleBlendMode = Color.BlendMode.Mask;

			// [Act]
			sampleLayer.Blending = sampleBlendMode;

			// [Assert]
			Assert.That (sampleLayer.Blending,
				Is.EqualTo (sampleBlendMode)
			);
		}

		#endregion

		#region Properties - HasEffect

		[Test]
		public void HasEffect_Test ()
		{
			// [Arrange]
			// Test layer
			Layer sampleLayer = new Layer (5);
			Color blankColor = Color.Transparent;
			Color activeColor = new Color (255, 128, 64);

			// [Act]
			// Set all colors to the blank color
			for (int pixel = 0; pixel < sampleLayer.PixelCount; pixel++) {
				sampleLayer [pixel] = blankColor;	
			}
			bool blankColorHasEffect = sampleLayer.HasEffect;
			// Set one color to the active color
			sampleLayer [1] = activeColor;
			bool activeColorHasEffect = sampleLayer.HasEffect;

			// [Assert]
			Assert.That (blankColorHasEffect,
				Is.EqualTo (false)
			);
			Assert.That (activeColorHasEffect,
				Is.EqualTo (true)
			);
		}

		#endregion

		#region Functions - Blend

		[Test]
		public void Blend_Simple_Test ()
		{
			// [Arrange]
			// Source layer
			Layer sourceLayer = new Layer (1);
			sourceLayer [0] = new Color (65, 128, 64, 32);
			// Other layer
			Layer otherLayer = new Layer (1);
			otherLayer [0] = new Color (64, 128, 255, 100);
			// Desired result
			Layer expectedLayer = new Layer (1);
			// > Manually blend the color to confirm
			expectedLayer [0] = sourceLayer [0].Clone ();
			expectedLayer [0].Blend (otherLayer [0], 0.75, true);

			// [Act]
			// Blend the layers using the specified simple blending setup
			sourceLayer.Blend (otherLayer, 0.75, true);

			// [Assert]
			Assert.That (sourceLayer,
				Is.EqualTo (expectedLayer)
			);
		}

		[Test]
		public void Blend_Mode_Test ()
		{
			// [Arrange]
			// Source layer
			Layer sourceLayer = new Layer (1);
			sourceLayer [0] = new Color (65, 128, 64, 32);
			// Other layer
			Layer otherLayer = new Layer (1);
			otherLayer [0] = new Color (64, 128, 255, 100);
			// Desired result
			Layer expectedLayer = new Layer (1);
			// > Manually blend the color to confirm
			expectedLayer [0] = sourceLayer [0].Clone ();
			expectedLayer [0].Blend (otherLayer [0], Color.BlendMode.Favor);

			// [Act]
			// Blend the layers using the specified blending mode
			sourceLayer.Blend (otherLayer, Color.BlendMode.Favor);

			// [Assert]
			Assert.That (sourceLayer,
				Is.EqualTo (expectedLayer)
			);
		}

		[Test]
		public void Blend_Mode_Optional_Mode_Test ()
		{
			// [Arrange]
			// Source layer
			Layer sourceLayer = new Layer (1, Color.BlendMode.Mask);
			sourceLayer [0] = new Color (65, 128, 64, 32);
			// Other layer
			Layer otherLayer = new Layer (1);
			otherLayer [0] = new Color (64, 128, 255, 0);
			// Desired result
			Layer expectedLayer = new Layer (1, Color.BlendMode.Mask);
			// > Manually blend the color to confirm
			expectedLayer [0] = sourceLayer [0].Clone ();
			expectedLayer [0].Blend (otherLayer [0], sourceLayer.Blending);

			// [Act]
			// Blend the layers using the layer's current blending mode
			sourceLayer.Blend (otherLayer);

			// [Assert]
			Assert.That (sourceLayer,
				Is.EqualTo (expectedLayer)
			);
		}

		[Test]
		public void Blend_Mode_Optional_Fade_Test ()
		{
			// [Arrange]
			// Sample layer
			Layer sampleLayer = new Layer (1);
			sampleLayer [0] = new Color (255, 0, 0);
			// New layer
			Layer newLayer = new Layer (1);
			newLayer [0] = new Color (0, 255, 0);
			// Desired result
			Layer expectedLayer = new Layer (1);
			expectedLayer [0] = new Color (255, 127, 0);

			// [Act]
			// Blend the layers
			sampleLayer.Blend (newLayer, 0.5);

			// [Assert]
			// Make sure the sample layer is the expected result, ensuring that
			// fade is not chosen by default.
			Assert.That (sampleLayer,
				Is.EqualTo (expectedLayer)
			);
		}

		[Test]
		public void Blend_MismatchCount_Throws ()
		{
			// [Arrange]
			Layer sampleLayer = new Layer (1);
			Layer differentCountLayer = new Layer (2);

			string expectedResult =
				"UpperLayer must have the same number of pixels as this " +
				"layer to blend together.";

			// [Act/Assert]
			// Check simple blending
			Assert.That (
				delegate {
					sampleLayer.Blend (
						differentCountLayer, 1
					);
				},
				Throws.TypeOf<ArgumentException> ()
				.With.Property ("Message").EqualTo (expectedResult)
			);
			// Check mode-based blending
			Assert.That (
				delegate {
					sampleLayer.Blend (
						differentCountLayer, Color.BlendMode.Combine
					);
				},
				Throws.TypeOf<ArgumentException> ()
				.With.Property ("Message").EqualTo (expectedResult)
			);
		}

		[Test]
		public void Blend_InvalidOpacity_Throws ()
		{
			// [Arrange]
			Layer sampleLayer = new Layer (1);
			double invalidOpacityPositive = 1.1;
			double invalidOpacityNegative = -0.1;

			// [Act/Assert]
			Assert.That (
				delegate {
					sampleLayer.Blend (
						new Layer (1), invalidOpacityPositive, false
					);
				},
				Throws.TypeOf<ArgumentOutOfRangeException> ()
				.With.Property ("ParamName").EqualTo ("Opacity")
			);

			Assert.That (
				delegate {
					sampleLayer.Blend (
						new Layer (1), invalidOpacityNegative, false
					);
				},
				Throws.TypeOf<ArgumentOutOfRangeException> ()
				.With.Property ("ParamName").EqualTo ("Opacity")
			);
		}

		[Test]
		public void Blend_NullLayer_Throws ()
		{
			// [Arrange]
			Layer sampleLayer = new Layer (1);
			const Layer invalidLayer = null;

			// [Act/Assert]
			Assert.That (
				delegate {
					sampleLayer.Blend (invalidLayer, 0.5, false);
				},
				Throws.TypeOf<ArgumentNullException> ()
				.With.Property ("ParamName").EqualTo ("UpperLayer")
			);

			Assert.That (
				delegate {
					sampleLayer.Blend (invalidLayer, Color.BlendMode.Combine);
				},
				Throws.TypeOf<ArgumentNullException> ()
				.With.Property ("ParamName").EqualTo ("UpperLayer")
			);
		}

		#endregion

		#region Functions - Fill

		[Test]
		public void Fill_Test ()
		{
			// [Arrange]
			const int lightCount = 23;
			Layer resultLayer = new Layer (lightCount);
			Color testColor = new Color (32, 64, 128, 255);
			// Ensure there's no reference
			Color expectColor = testColor.Clone ();

			// [Act]
			resultLayer.Fill (testColor);

			// [Assert]
			// Check that it's the right color
			Assert.That (resultLayer.GetPixels (),
				Has.All.EqualTo (expectColor)
			);
		}

		[Test]
		public void Fill_NullColor_Throws ()
		{
			// [Arrange]
			Layer sampleLayer = new Layer (1);
			const Color invalidColor = null;

			// [Act/Assert]
			Assert.That (
				delegate {
					sampleLayer.Fill (invalidColor);
				},
				Throws.TypeOf<ArgumentNullException> ()
				.With.Property ("ParamName").EqualTo ("FillColor")
			);
		}

		#endregion

		#region Functions - GetPixels

		[Test]
		public void GetPixels_Test ()
		{
			// [Arrange]
			// Test layer
			Layer sampleLayer = new Layer (2);
			sampleLayer [0] = new Color (65, 128, 64);
			sampleLayer [1] = new Color (64, 128, 255);
			// Test colors
			Color expectedColor0 = new Color (65, 128, 64);
			Color expectedColor1 = new Color (64, 128, 255);

			// [Act]
			// Get the pixels
			Color[] pixelData = sampleLayer.GetPixels ();

			// [Assert]
			// Make sure the layer's colors are copied properly
			Assert.That (pixelData [0],
				Is.EqualTo (expectedColor0)
			);
			Assert.That (pixelData [1],
				Is.EqualTo (expectedColor1)
			);
		}

		[Test]
		public void GetPixels_NoReference_Test ()
		{
			// [Arrange]
			// Test layer
			Layer sampleLayer = new Layer (2);
			sampleLayer [0] = new Color (65, 128, 64);
			sampleLayer [1] = new Color (64, 128, 255);
			// Test colors
			Color replacedColor = new Color (255, 0, 0);
			Color expectedColor0 = new Color (65, 128, 64);
			Color expectedColor1 = new Color (64, 128, 255);

			// [Act]
			// Get the pixels
			Color[] pixelData = sampleLayer.GetPixels ();
			// Change the first color
			pixelData [0] = replacedColor;

			// Change the second color's component
			pixelData [1].B = 111;

			// [Assert]
			// Make sure the layer's colors are not changed
			Assert.That (sampleLayer [0],
				Is.EqualTo (expectedColor0)
			);
			Assert.That (sampleLayer [1],
				Is.EqualTo (expectedColor1)
			);
		}

		#endregion

		#region Functions - Clone

		[Test]
		public void Clone_NoReference_Test ()
		{
			// [Arrange]
			const int lightCount = 23;
			const Color.BlendMode expectBlend = Color.BlendMode.Replace;
			const bool expectHasEffect = true;
			Color sampleColorOriginal = new Color (255, 128, 64, 32);
			Color sampleColorModified = new Color (255, 128, 64, 32);
			// Set up the sample layer
			Layer sampleLayer = new Layer (lightCount, expectBlend);
			sampleLayer [0].SetColor (sampleColorOriginal);
			sampleLayer [lightCount - 1].SetColor (sampleColorOriginal);

			// [Act]
			Layer resultLayer = sampleLayer.Clone ();
			// Modify the sample layer after cloning, which should NOT be
			// reflected in the cloned layer
			sampleLayer [0].SetColor (sampleColorModified);

			// [Assert]
			// Make sure the count is accurate
			Assert.That (resultLayer.PixelCount,
				Is.EqualTo (lightCount)
			);
			// Verify the default blend mode
			Assert.That (resultLayer.Blending,
				Is.EqualTo (expectBlend)
			);
			// Check that the colors are created with effect
			Assert.That (resultLayer.HasEffect,
				Is.EqualTo (expectHasEffect)
			);
			// Check that the colors are cloned
			Assert.That (resultLayer [0],
				Is.EqualTo (sampleColorOriginal)
			);
			Assert.That (resultLayer [lightCount - 1],
				Is.EqualTo (sampleColorOriginal)
			);
			// And check that the sample layer is correct
			Assert.That (sampleLayer [0],
				Is.EqualTo (sampleColorModified)
			);
		}

		#endregion

		#region Functions - Equals

		[Test]
		public void Equals_Same_Test ()
		{
			// [Arrange]
			// Start layer
			Layer startLayer = new Layer (2, Color.BlendMode.Favor);
			startLayer [0] = new Color (65, 128, 64);
			startLayer [1] = new Color (64, 128, 255);
			// Other layer
			Layer otherLayer = new Layer (2, Color.BlendMode.Favor);
			otherLayer [0] = new Color (65, 128, 64);
			otherLayer [1] = new Color (64, 128, 255);

			// [Act/Assert]
			Assert.That (startLayer,
				Is.EqualTo (otherLayer)
			);
		}

		[Test]
		public void Equals_Different_Test (
			[ValueSource ("GetLayerTestChoices")] LayerTestChoice chosenTest)
		{
			// [Arrange]
			// Start layer
			Layer startLayer = new Layer (2, Color.BlendMode.Favor);
			startLayer [0] = new Color (128, 128, 128, 128);
			startLayer [1] = new Color (128, 128, 128, 128);
			// Other layer
			Layer modifiedLayer = new Layer (2, Color.BlendMode.Favor);
			modifiedLayer [0] = new Color (128, 128, 128, 128);
			modifiedLayer [1] = new Color (128, 128, 128, 128);

			switch (chosenTest) {
			case LayerTestChoice.Blending:
				modifiedLayer.Blending = Color.BlendMode.Mask;
				break;
			case LayerTestChoice.Color0:
				modifiedLayer [0].R = Color.MAX;
				break;
			case LayerTestChoice.Color1:
				modifiedLayer [1].R = Color.MAX;
				break;
			default:
				Assert.Fail ("Unknown LayerTestChoice!");
				break;
			}

			// [Act/Assert]
			Assert.That (startLayer,
				Is.Not.EqualTo (modifiedLayer)
			);
		}

		#endregion

		#region Functions - GetEnumerator

		[Test]
		public void GetEnumerator_Equals_Test ()
		{
			// [Arrange]
			Color[] sampleColors = new Color[] {
				new Color (255, 128, 64, 32),
				new Color (111, 151, 222, 1),
				new Color (255, 255, 255, 16)
			};
			// Dynamically set lightCount to make changing the test easier
			int lightCount = sampleColors.Length;
			// Set up the sample layer
			Layer sampleLayer = new Layer (lightCount);
			for (int i = 0; i < lightCount; i++) {
				sampleLayer [i].SetColor (sampleColors [i]);
			}

			// [Act]
			// Take all entries and store as an array
			Color[] resultColors = sampleLayer.GetEnumerator ().ToArray ();

			// [Assert]
			// Make sure each color is correct
			Assert.That (resultColors,
				Is.EqualTo (sampleColors)
			);

			// This might not be a solid way to unit test the GetEnumerator()
			// method.  However, we're at least ensuring the basics work.
			//
			// See https://stackoverflow.com/questions/1510031/c-how-do-you-test-the-ienumerable-getenumerator-method
		}

		[Test]
		public void GetEnumerator_InvalidCast_Test ()
		{
			// [Arrange]
			const int lightCount = 1;
			// Set up the sample layer
			Layer sampleLayer = new Layer (lightCount);

			// [Act/Assert]
			Assert.That (
				delegate {
					// Try to cast the enumerator back to a modifiable object
					// See https://stackoverflow.com/questions/7310454/simple-ienumerator-use-with-example#comment8811203_7310570
					Color[] test = (Color[])sampleLayer.GetEnumerator ();
					// This should fail, meaning the following won't work,
					// either...
					test [0] = new Color ();
					// Arguably I won't be trying to break my own code, but it's
					// probably still a good idea to defend against this.
				},
				Throws.TypeOf<InvalidCastException> ()
			);
		}

		#endregion

		#region Functions - ToString

		[Test]
		public void ToString_Test ()
		{
			// [Arrange]
			const int bitPixelCount = 2;
			const Color.BlendMode bitBlending = Color.BlendMode.Replace;
			const bool bitHasEffect = false;
			Layer sampleLayer = new Layer (bitPixelCount, bitBlending);
			// Set individual colors to nothingness
			sampleLayer [0] = Color.Transparent;
			sampleLayer [1] = Color.Transparent;

			// Prepare the expected result of converting to string
			string expectedResult =
				string.Format (
					"[Layer: PixelCount={0}, BlendMode={1}, HasEffect={2}]",
					bitPixelCount, bitBlending, bitHasEffect
				);

			// [Act/Assert]
			Assert.That (sampleLayer.ToString (),
				Is.EqualTo (expectedResult)
			);
		}

		#endregion

		#region Internal

		/// <summary>
		/// Possible layer properties to test.
		/// </summary>
		public enum LayerTestChoice
		{
			Blending,
			Color0,
			Color1
		}

		/// <summary>
		/// Gets the possible choices of layer properties to test.
		/// </summary>
		/// <returns>The layer test choices.</returns>
		private Array GetLayerTestChoices ()
		{
			// See https://automaticchainsaw.blogspot.com/2010/04/unit-test-all-enum-values-with-nunit.html
			return Enum.GetValues (typeof(LayerTestChoice));
		}

		#endregion
	}
}

