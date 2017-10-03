//
//  BitmapAnimation.cs
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
using System.IO;
using System.Collections.Generic;

// Rendering
using Actinic.Rendering;

namespace Actinic.Animations
{
	public class BitmapAnimation:AbstractAnimation
	{

		private List<Layer> AnimationFrames = new List<Layer> ();
		private int animation_frame = 0;

		public int AnimationFrame {
			get { return animation_frame; }
			set {
				if (value < 0) {
					animation_frame = 0;
				} else if (value >= AnimationFrames.Count) {
					animation_frame = AnimationFrames.Count - 1;
				} else {
					animation_frame = value;
				}
			}
		}

		public string ImageFilePath {
			get;
			private set;
		}

		public BitmapAnimation (int Light_Count, string BitmapImageFilePath) : base (Light_Count)
		{
			if (File.Exists (BitmapImageFilePath) == false)
				throw new System.IO.FileNotFoundException ("BitmapImageFilePath must point to an image.", BitmapImageFilePath);
			System.Drawing.Bitmap bitmapImage = new System.Drawing.Bitmap (BitmapImageFilePath);
			if (bitmapImage.Width < Light_Count)
				throw new System.IO.InvalidDataException (String.Format (
					"The provided image [BitmapImageFilePath = '{0}'] " +
					"is not wide enough for the current number of lights.  " +
					"Expected '{1}' width, but got '{2}' width.",
					BitmapImageFilePath, Light_Count, bitmapImage.Width)
				);
			AnimationFrames = AnimationUtilities.ConvertImageToLEDArray (Light_Count, bitmapImage);
		}

		public override Layer GetNextFrame ()
		{
			if (animation_frame > -1 && animation_frame < AnimationFrames.Count) {
				int actual_frame = animation_frame;
				// Increment the counter by one
				if (animation_frame >= AnimationFrames.Count - 1) {
					animation_frame = 0;
				} else {
					animation_frame++;
				}
				//Console.WriteLine ("Frame: {0} | {1}, {2}, {3}", actual_frame, AnimationFrames [actual_frame].LED_Values[24].R, AnimationFrames [actual_frame].LED_Values[24].G, AnimationFrames [actual_frame].LED_Values[24].B);
				return AnimationFrames [actual_frame];
			} else {
				throw new System.ArgumentOutOfRangeException (
					"animation_frame",
					animation_frame,
					"animation_frame must " +
					"be within range of AnimationFrames, e.g. [0, " + AnimationFrames.Count.ToString () + "]"
				);
			}
		}
	}
}

