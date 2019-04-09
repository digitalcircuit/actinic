//
//  AbstractAnimation.cs
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
using System.Collections.Generic;

// Device configuration
using Actinic.Output;

// Rendering
using Actinic.Rendering;
using Actinic.Utilities;

namespace Actinic.Animations
{
	public abstract class AbstractAnimation
	{

		public enum Style
		{
			Soft,
			Moderate,
			Bright
		}

		/// <summary>
		/// Whether or not to apply smoothing to this <see cref="Actinic.GenericAnimation"/>.
		/// </summary>
		/// <value><c>true</c> if smoothing is enabled; otherwise, <c>false</c>.</value>
		public bool EnableSmoothing {
			get;
			protected set;
		}

		/// <summary>
		/// Default amount of smoothing to provide a nice, mostly-seamless
		/// transition
		/// </summary>
		protected const double SmoothingConstant_Default = 283;

		/// <summary>
		/// Amount of smoothing applied to the animation if enabled.
		/// <remarks>
		/// The smoothing constant is represented as the time in milliseconds
		/// after which the output will have 63% or more of the new frame
		/// applied.
		/// </remarks>
		/// </summary>
		/// <value>Value in milliseconds starting from 0, with lower values resulting in more gradual changes.</value>
		public double SmoothingConstant {
			get {
				return smoothingFilter.TimeConstant;
			}
			protected set {
				smoothingFilter.TimeConstant = value;
			}
		}


		private Style animation_style = Style.Moderate;

		/// <summary>
		/// Intensity or energy of of the animation, controlled via styles.
		/// </summary>
		/// <value>The animation style.</value>
		public Style AnimationStyle {
			get {
				return animation_style;
			}
			set {
				animation_style = value;
				switch (animation_style) {
				case Style.Bright:
					Styled_SoftBrightness = 30;
					Styled_ModerateBrightness = 128;
					Styled_BrightBrightness = 255;
					Styled_SoftColor = 100;
					Styled_ModerateColor = 150;
					Styled_BrightColor = 255;
					break;
				case Style.Moderate:
					Styled_SoftBrightness = 5;
					Styled_ModerateBrightness = 30;
					Styled_BrightBrightness = 255;
					Styled_SoftColor = 60;
					Styled_ModerateColor = 128;
					Styled_BrightColor = 255;
					break;
				case Style.Soft:
					Styled_SoftBrightness = 2;
					Styled_ModerateBrightness = 6;
					Styled_BrightBrightness = 60; // 30 -> 60 as interval soft brightness is too dark
					Styled_SoftColor = 20; // 60 -> 20
					Styled_ModerateColor = 43; // 85 / 2
					Styled_BrightColor = 85; // (255÷60)×20
					break;
				default:
					throw new NotSupportedException (
						"Unsupported Style type for AnimationStyle");
				}
			}
		}

		// Styled values that inherited classes can use to easily adapt
		protected byte Styled_SoftBrightness = 0;
		protected byte Styled_ModerateBrightness = 0;
		protected byte Styled_BrightBrightness = 0;
		protected byte Styled_SoftColor = 0;
		protected byte Styled_ModerateColor = 0;
		protected byte Styled_BrightColor = 0;

		/// <summary>
		/// Gets how many values to shift color hue per frame.
		/// </summary>
		/// <value>The decimal value of color shift per frame.</value>
		protected double Styled_ColorShiftAmount
		{
			get {
				double baseShiftAmount;
				switch (AnimationStyle) {
				case Style.Bright:
					baseShiftAmount = 30;
					break;
				case Style.Moderate:
					baseShiftAmount = 12;
					break;
				case Style.Soft:
					baseShiftAmount = 3;
					break;
				default:
					// This shouldn't ever happen
					throw new NotSupportedException (
						"Unsupported Style type for AnimationStyle");
				}
				// Aim for the scale of baseShiftAmount at 50 ms
				return baseShiftAmount * (deviceConfig.FactorTime / 50);
			}
		}

		/// <summary>
		/// Gets the requested animation delay for this <see cref="Actinic.GenericAnimation"/> animation
		/// </summary>
		/// <value>
		/// The amount of time between frames as desired by this animation.  If zero, the default of 50ms will be used.
		/// </value>
		public int RequestedAnimationDelay {
			get;
			protected set;
		}

		/// <summary>
		/// Gets a boolean indicating this <see cref="Actinic.GenericAnimation"/> should be smoothly cross-faded to by the animation manager.
		/// </summary>
		/// <value><c>true</c> if requesting a smooth cross-fade; otherwise, <c>false</c>.</value>
		public bool RequestSmoothCrossfade {
			get;
			protected set;
		}

		/// <summary>
		/// Number of lights in use
		/// </summary>
		protected int Light_Count {
			get {
				return CurrentFrame.PixelCount;
			}
		}

		/// <summary>
		/// List of LEDs representing the current frame of animation
		/// </summary>
		protected Layer CurrentFrame;

		protected ReadOnlyDeviceConfiguration deviceConfig;

		public AbstractAnimation (ReadOnlyDeviceConfiguration Configuration)
		{
			deviceConfig = Configuration;
			smoothingFilter = new ScaledAverage (deviceConfig);
			CurrentFrame = new Layer (deviceConfig.LightCount);
			RequestedAnimationDelay = 0;
			AnimationStyle = Style.Moderate;
			if (SmoothingConstant == 0)
				SmoothingConstant = SmoothingConstant_Default;
		}

		public AbstractAnimation (
			ReadOnlyDeviceConfiguration Configuration,
			Layer PreviouslyShownFrame)
		{
			deviceConfig = Configuration;
			smoothingFilter = new ScaledAverage (deviceConfig);
			CurrentFrame = PreviouslyShownFrame.Clone ();
			RequestedAnimationDelay = 0;
			AnimationStyle = Style.Moderate;
			if (SmoothingConstant == 0)
				SmoothingConstant = SmoothingConstant_Default;
		}

		/// <summary>
		/// Gets the next frame of animation.
		/// </summary>
		/// <returns>A list of LEDs that represent the next frame.</returns>
		public abstract Layer GetNextFrame ();

		/// <summary>
		/// Gets the next frame of animation filtered atop the current frame.
		/// </summary>
		/// <returns>The next frame of animation, filtered.</returns>
		/// <param name="DisplayedFrame">The currently displayed frame.</param>
		/// <param name="SmoothBrightnessOnIncrease">If true, smooth brightness when it is increasing, otherwise brightness jumps are immediate.</param>
		public Layer GetNextFrameFiltered (
			Layer DisplayedFrame, bool SmoothBrightnessOnIncrease = false) {
			// Get the next frame
			Layer newFrame = GetNextFrame();

			// Filter it atop the current frame
			for (int i = 0; i < DisplayedFrame.PixelCount; i++) {
				DisplayedFrame [i].R = smoothingFilter.Filter (
					DisplayedFrame [i].R, newFrame [i].R
				);
				DisplayedFrame [i].G = smoothingFilter.Filter (
					DisplayedFrame [i].G, newFrame [i].G
				);
				DisplayedFrame [i].B = smoothingFilter.Filter (
					DisplayedFrame [i].B, newFrame [i].B
				);
				DisplayedFrame [i].Brightness = smoothingFilter.Filter (
					DisplayedFrame [i].Brightness, newFrame [i].Brightness
				);
				if (!SmoothBrightnessOnIncrease) {
					DisplayedFrame [i].Brightness = (byte)Math.Max (
						DisplayedFrame [i].Brightness, newFrame [i].Brightness
					);
				}
			}

			return DisplayedFrame;
		}

		#region Internal

		/// <summary>
		/// The device-adjusted smoothing filter
		/// </summary>
		private ScaledAverage smoothingFilter;

		#endregion
	}
}

