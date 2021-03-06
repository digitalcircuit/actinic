//
//  SimpleSpinnerAnimation.cs
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
	public class SimpleSpinnerAnimation:SimpleFadeAnimation
	{
		public SimpleSpinnerAnimation (
			ReadOnlyDeviceConfiguration Configuration) : base (Configuration)
		{
			// No need to clear RequestSmoothCrossfade as inherited class SimpleFadeAnimation doesn't touch it here
		}

		public SimpleSpinnerAnimation (
			ReadOnlyDeviceConfiguration Configuration,
			Layer PreviouslyShownFrame)
			: base (Configuration, PreviouslyShownFrame)
		{
			RequestSmoothCrossfade = false;
			// Inherited class SimpleFadeAnimation sets this to true; so reset it back to false
		}

		/// <summary>
		/// Gets how many pixels to shift forwards per frame.
		/// </summary>
		/// <value>The decimal value to shift forwards per frame.</value>
		protected double Scale_ShiftForward {
			get {
				// Shift brightness out the entire strand in 2.5 seconds
				return (
				    (deviceConfig.FactorTime / 2500)
				    * deviceConfig.FactorScaledSize
				);
			}
		}

		public override Layer GetNextFrame ()
		{
			Anim_Update_ColorShift ();
			CurrentFrame [0].R = Anim_ColorShift_Red;
			CurrentFrame [0].G = Anim_ColorShift_Green;
			CurrentFrame [0].B = Anim_ColorShift_Blue;
			CurrentFrame [0].Brightness = LightSystem.Brightness_MAX;

			trackShiftForward += Scale_ShiftForward;
			if (trackShiftForward.IntValue > 0) {
				// Shift by the integer pixel value
				int shiftAmount = trackShiftForward.TakeInt ();
				// Shift color and brightness due to desaturation colors
				ShiftLightsForwards (CurrentFrame, shiftAmount);
			}

			return CurrentFrame;
		}

		/// <summary>
		/// Shifts the lights forward.
		/// </summary>
		/// <param name='LightSet'>
		/// Light set to manipulate (List<Color> of GE lights).
		/// </param>
		/// <param name='ShiftCount'>
		/// Number of times to shift lights.
		/// </param>
		private void ShiftLightsForwards (Layer LightSet, int ShiftCount)
		{
			//Shifts forwards from LED 1 to 50
			for (int times_shifted = 0; times_shifted < ShiftCount; times_shifted++) {
				for (int i = Light_Count - 1; i > 0; i--) {
					LightSet [i].R = LightSet [i - 1].R;
					LightSet [i].G = LightSet [i - 1].G;
					LightSet [i].B = LightSet [i - 1].B;
					LightSet [i].Brightness = LightSet [i - 1].Brightness;
				}
			}
		}

		#region Internal

		/// <summary>
		/// Tracking color shift outwards per frame
		/// </summary>
		private IntFraction trackShiftForward = new IntFraction ();

		#endregion
	}
}

