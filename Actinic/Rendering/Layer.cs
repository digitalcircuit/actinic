//
//  Layer.cs
//
//  Author:
//       Shane Synan <digitalcircuit36939@gmail.com>
//
//  Copyright (c) 2016-2017
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

namespace Actinic.Rendering
{
	/// <summary>
	/// Single render layer representing a collection of pixels.
	/// </summary>
	public class Layer : IEquatable<Layer>
	{
		#region Constructors

		/// <summary>
		/// Initializes a new <see cref="Layer"/> with
		/// all pixels cleared.
		/// </summary>
		/// <param name="PixelCount">Number of pixels.</param>
		/// <param name="Blending">Default mode for blending upper layers into this layer.</param>
		/// <param name="FillColor">Initial color for all pixels.</param>
		public Layer (
			int PixelCount, Color.BlendMode Blending = Color.BlendMode.Combine,
			Color FillColor = null)
		{
			if (PixelCount <= 0) {
				throw new ArgumentOutOfRangeException (
					"PixelCount",
					"Pixel count must be greater than zero."
				);
			}
			setPixelCount (PixelCount);
			this.Blending = Blending;
			if (FillColor != null) {
				// Fill color specified, set it now.
				Fill (FillColor);
			}
		}

		/// <summary>
		/// Initializes a new <see cref="Layer"/> by
		/// cloning an existing layer.
		/// </summary>
		/// <param name="ClonedLayer">Layer to clone.</param>
		public Layer (Layer ClonedLayer)
		{
			if (ClonedLayer == null) {
				// Need a valid layer
				throw new ArgumentNullException ("ClonedLayer");
			}

			Blending = ClonedLayer.Blending;
			clonePixels (ClonedLayer.PixelCount, ClonedLayer.layerPixels);
		}

		#endregion

		#region Properties

		/// <summary>
		/// Gets or sets the pixels in the layer at the specified index.
		/// </summary>
		/// <param name="index">Index.</param>
		public Color this [int index] {
			get {
				if (index < 0 || index >= PixelCount) {
					throw new IndexOutOfRangeException (String.Format (
						"Pixel index of {0} out of bounds.  Pixel index must " +
						"be within 0 to PixelCount-1 ({1}).",
						index, PixelCount - 1
					));
				} else {
					// Get pixel
					return layerPixels [index];
				}
			}
			set {
				if (index < 0 || index >= PixelCount) {
					throw new IndexOutOfRangeException (String.Format (
						"Pixel index of {0} out of bounds.  Pixel index must " +
						"be within 0 to PixelCount-1 ({1}).",
						index, PixelCount - 1
					));
				} else {
					// Update pixel
					layerPixels [index] = value;
				}
			}
		}

		/// <summary>
		/// Gets the pixel count.
		/// </summary>
		/// <value>The number of pixels.</value>
		public int PixelCount {
			get {
				return layerPixels.Length;
			}
		}

		/// <summary>
		/// Gets how the layer should be blended.
		/// </summary>
		/// <value>The blending mode.</value>
		public Color.BlendMode Blending {
			get {
				return blending_mode;
			}
			set {
				// Update mode
				blending_mode = value;
			}
		}

		/// <summary>
		/// Gets a value indicating whether this layer has any effect on output,
		/// i.e. not all black with no brightness.
		/// </summary>
		/// <value><c>true</c> if this layer has an effect; otherwise, <c>false</c>.</value>
		public bool HasEffect {
			get {
				// Check if any pixel has effect
				return Array.Exists (
					layerPixels, pixel => pixel.HasEffect == true
				);
			}
		}

		#endregion

		#region Shared Functions

		/// <summary>
		/// Blends color and brightness of the given (upper) layer into this
		/// (lower) layer using this layer's current blending mode.
		/// </summary>
		/// <param name="UpperLayer">Upper layer, blended into this one.</param>
		public void Blend (Layer UpperLayer)
		{
			Blend (UpperLayer, Blending);
		}

		/// <summary>
		/// Blends color and brightness of the given (upper) layer into this
		/// (lower) layer.
		/// </summary>
		/// <param name="UpperLayer">Upper layer, blended into this one.</param>
		/// <param name="Opacity">Strength of the upper layer's influence as a decimal from 0 to 1.</param>
		/// <param name="Fade">If set to <c>true</c> fades between current layer and new layer with <see cref="Opacity"/> specifying the fade amount.</param>
		public void Blend (
			Layer UpperLayer, double Opacity, bool Fade = false)
		{
			if (UpperLayer == null) {
				// Need a valid layer
				throw new ArgumentNullException ("UpperLayer");
			}
			if (UpperLayer.PixelCount != PixelCount)
				throw new ArgumentException (
					"UpperLayer must have the same number of pixels as this " +
					"layer to blend together."
				);
			if (Opacity < 0 || Opacity > 1)
				throw new ArgumentOutOfRangeException (
					"Opacity",
					"Opacity must be a value between 0 and 1."
				);

			for (int i = 0; i < UpperLayer.PixelCount; i++) {
				// Blend this layer with the upper layer
				this [i].Blend (UpperLayer [i], Opacity, Fade);
			}
		}

		/// <summary>
		/// Blends color and brightness of the given (upper) layer into this
		/// (lower) layer.
		/// </summary>
		/// <param name="UpperLayer">Upper layer, blended into this one.</param>
		/// <param name="BlendMode">Mode for blending upper layer into this layer.</param>
		public void Blend (Layer UpperLayer, Color.BlendMode BlendMode)
		{
			if (UpperLayer == null) {
				// Need a valid layer
				throw new ArgumentNullException ("UpperLayer");
			}
			if (UpperLayer.PixelCount != PixelCount)
				throw new ArgumentException (
					"UpperLayer must have the same number of pixels as this " +
					"layer to blend together."
				);
			for (int i = 0; i < UpperLayer.PixelCount; i++) {
				// Blend this layer with the upper layer
				this [i].Blend (UpperLayer [i], BlendMode);
			}
		}

		/// <summary>
		/// Fills all pixels of the current layer with the specified color.
		/// </summary>
		/// <param name="FillColor">Fill color.</param>
		public void Fill (Color FillColor)
		{
			if (FillColor == null) {
				// Need a valid layer
				throw new ArgumentNullException ("FillColor");
			}
			for (int index = 0; index < layerPixels.Length; index++) {
				layerPixels [index] = FillColor.Clone ();
			}
		}

		/// <summary>
		/// Gets a clone of the pixels in this layer.
		/// </summary>
		/// <returns>The pixels.</returns>
		public Color[] GetPixels ()
		{
			// Return a clone to avoid potential modification and not deal with
			// synchronization locks.
			return clonePixelArray (layerPixels);
			// If performance becomes an issue, mark as read-only to avoid
			// modification and use ReadOnlyCollection.
			//return Array.AsReadOnly(layerPixels);
		}

		/// <summary>
		/// Clone this instance.
		/// </summary>
		public Layer Clone ()
		{
			return new Layer (this);
		}

		/// <summary>
		/// Determines whether the specified <see cref="Layer"/> is equal to the
		/// current <see cref="Layer"/>.
		/// </summary>
		/// <param name="other">The <see cref="Layer"/> to compare with the current <see cref="Layer"/>.</param>
		/// <returns><c>true</c> if the specified <see cref="Layer"/> is equal to the current <see cref="Layer"/>; otherwise, <c>false</c>.</returns>
		public bool Equals (Layer other)
		{
			// Only equal if all fields match...
			if (PixelCount != other.PixelCount
			    || Blending != other.Blending) {
				// HasEffect is tested by comparing pixel values below
				return false;
			}
			// ...and if all colors match
			for (int pixel = 0; pixel < PixelCount; pixel++) {
				if (!this [pixel].Equals (other [pixel])) {
					return false;
				}
			}
			// All match
			return true;
		}

		public override string ToString ()
		{
			return string.Format (
				"[Layer: PixelCount={0}, BlendMode={1}, HasEffect={2}]",
				PixelCount, Blending, HasEffect
			);
		}

		#endregion

		#region Internal

		/// <summary>
		/// Sets number of pixels on this layer.
		/// </summary>
		/// <param name="Count">Desired number of pixels.</param>
		/// <param name="PreserveExisting">If set to <c>true</c> copy existing pixel values, discarding and padding as needed to fit.</param>
		private void setPixelCount (int Count, bool PreserveExisting = false)
		{
			if (Count <= 0) {
				throw new ArgumentOutOfRangeException (
					"Count", "Pixel count must be greater than zero."
				);
			}
			if (PreserveExisting) {
				// Clone the existing pixels
				clonePixels (Count, layerPixels);
			} else {
				// Clear all by creating a new array
				layerPixels = new Color[Count];
				// This initializes all to default value.  Fill the color later
				// if needed.
				for (int index = 0; index < Count; index++) {
					layerPixels [index] = new Color ();
				}
			}
		}

		/// <summary>
		/// Clones the pixel array, avoiding any reference links (deep rather
		/// than shallow copy).
		/// </summary>
		/// <returns>A deep clone of the pixel array.</returns>
		/// <param name="ClonedPixels">Pixels to clone.</param>
		private Color[] clonePixelArray (Color[] ClonedPixels)
		{
			if (ClonedPixels == null) {
				// Need a valid set of pixels
				throw new ArgumentNullException ("ClonedPixels");
			}

			Color[] newPixels = new Color[ClonedPixels.Length];
			for (int index = 0; index < ClonedPixels.Length; index++) {
				// Perform a deep copy to avoid unintentional overwrites
				newPixels [index] = ClonedPixels [index].Clone ();
			}
			return newPixels;
		}

		/// <summary>
		/// Clones a given set of pixels into the current set of pixels.
		/// </summary>
		/// <param name="Count">Desired number of pixels.</param>
		/// <param name="ClonedPixels">Pixels to clone.</param>
		private void clonePixels (int Count, Color[] ClonedPixels)
		{
			if (ClonedPixels == null) {
				// Need a valid set of pixels
				throw new ArgumentNullException ("ClonedPixels");
			}

			if (Count <= 0) {
				throw new ArgumentOutOfRangeException (
					"Count", "Pixel count must be greater than zero."
				);
			}
			// Set up a new pixel array
			Color[] newPixels = new Color[Count];
			// Copy as many existing pixels as possible
			int maxIndex = Math.Min (ClonedPixels.Length, newPixels.Length);
			for (int index = 0; index < maxIndex; index++) {
				// Perform a deep copy to avoid unintentional overwrites
				newPixels [index] = ClonedPixels [index].Clone ();
			}
			// Overwrite the existing layer with this new layer
			layerPixels = newPixels;
		}

		/// <summary>
		/// Sum of pixels that make up this layer.
		/// </summary>
		private Color[] layerPixels;

		/// <summary>
		/// The layer blending mode.  See <see cref="Blending"/>.
		/// </summary>
		private Color.BlendMode blending_mode = Color.BlendMode.Combine;

		#endregion
	}
}

