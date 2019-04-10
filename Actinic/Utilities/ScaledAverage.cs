//
//  ScaledAverage.cs
//
//  Author:
//       Shane Synan <digitalcircuit36939@gmail.com>
//
//  Copyright (c) 2019 FoxSoft
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

using Actinic.Output;

namespace Actinic.Utilities
{
	/// <summary>
	/// Exponential moving average filter that scales according to update rate.
	/// </summary>
	public class ScaledAverage
	{
		#region Constructors

		/// <summary>
		/// Initializes a new exponential moving average filter.
		/// </summary>
		/// <param name="DeviceConfig">Active output device configuration.</param>
		/// <param name="Constant">The time constant of the scaled average in milliseconds.</param>
		public ScaledAverage (
			ReadOnlyDeviceConfiguration DeviceConfig, double Constant = 0)
		{
			if (DeviceConfig == null) {
				// Need a valid device configuration
				throw new ArgumentNullException ("DeviceConfig");
			}
			if (Constant < 0) {
				throw new ArgumentOutOfRangeException (
					"Constant",
					"Constant must be greater than zero."
				);
			}
			deviceConfig = DeviceConfig;
			TimeConstant = Constant;
		}

		#endregion

		#region Properties

		/// <summary>
		/// Gets or sets the time constant of the scaled average in
		/// milliseconds, e.g. the time it takes to reach 63.2% of the new
		/// value.
		/// <remarks>
		/// This does not account for applying the first value.  When first
		/// applying the filter, the time constant instead roughly indicates how
		/// long it will take to reach 90% of the new value.
		/// </remarks>
		/// </summary>
		/// <value>The time constant of the scaled average in milliseconds.</value>
		public double TimeConstant {
			get {
				return timeConstant;
			}
			set {
				if (value < 0) {
					throw new ArgumentOutOfRangeException (
						"TimeConstant",
						"TimeConstant must be greater than zero."
					);
				}
				if (timeConstant != value) {
					timeConstant = value;
					// Clear the stored weighting multiplier
					cachedWeightMultiplier = -1;
				}
			}
		}

		/// <summary>
		/// Gets the multiplier for maintaining a fixed amount of smoothing
		/// regardless of the current animation speed.
		/// <remarks>
		/// In most cases, you should use <see cref="Filter()"/> instead.
		/// </remarks>
		/// </summary>
		/// <value>The weighting multiplier as a positive decimal.</value>
		public double WeightMultiplier {
			get {
				if (cachedWeightMultiplier == -1
				    || lastFactorTime != deviceConfig.FactorTime) {
					// Cleared or never set, or time factor has changed, need to
					// calculate
					CalculateWeightMultiplier ();
				}
				return cachedWeightMultiplier;
			}
		}

		#endregion

		#region Shared Functions

		/// <summary>
		/// Applies the new value onto the past values, filtering using the
		/// chosen <see cref="TimeConstant"/>.
		/// </summary>
		/// <returns>The filtered value, combining the past values and new value.</returns>
		/// <param name="Past">Past values.</param>
		/// <param name="New">New value.</param>
		public double Filter (double Past, double New)
		{
			// Apply the exponential moving average filter
			//
			// WeightMultiplier represents the "freshness" of the value, i.e.
			// how much to emphasize the new value over the old value.
			return (
			    ((1 - WeightMultiplier) * Past) + (WeightMultiplier * New)
			);
		}

		/// <summary>
		/// Applies the new value onto the past values, filtering using the
		/// chosen <see cref="TimeConstant"/>.
		/// </summary>
		/// <returns>The filtered value, combining the past values and new value.</returns>
		/// <param name="Past">Past values.</param>
		/// <param name="New">New value.</param>
		public int Filter (int Past, int New)
		{
			// Call the most general version, double
			return (int)Filter ((double)Past, New);
		}

		/// <summary>
		/// Applies the new value onto the past values, filtering using the
		/// chosen <see cref="TimeConstant"/>.
		/// </summary>
		/// <returns>The filtered value, combining the past values and new value.</returns>
		/// <param name="Past">Past values.</param>
		/// <param name="New">New value.</param>
		public byte Filter (byte Past, byte New)
		{
			// Call the most general version, double
			return (byte)Filter ((double)Past, New);
		}

		/// <summary>
		/// Returns a string that represents the current
		/// <see cref="ScaledAverage"/>.
		/// </summary>
		/// <returns>A string that represents the current <see cref="ScaledAverage"/>.</returns>
		public override string ToString ()
		{
			return string.Format (
				"[ScaledAverage: TimeConstant={0}, WeightMultiplier={1}]",
				TimeConstant, WeightMultiplier);
		}

		#endregion

		#region Internal

		/// <summary>
		/// Calculates the internal weight multiplier.
		/// </summary>
		private void CalculateWeightMultiplier ()
		{
			// Track the last-used FactorTime
			lastFactorTime = deviceConfig.FactorTime;

			if (lastFactorTime <= 0) {
				// Dividing by zero is a no-go, assume the percentage of 1 to
				// ensure filtering always has an effect
				cachedWeightMultiplier = 1;
			} else if (TimeConstant < lastFactorTime) {
				// If requested delay amount is less than FactorTime, the
				// percentage will always be greater than 1.  Cap to 1.
				cachedWeightMultiplier = 1;
			} else {
				// Find the weighting multiplier
				// This is based on references for exponential moving averages.
				// See http://stockcharts.com/school/doku.php?id=chart_school:technical_indicators:moving_averages

				cachedWeightMultiplier =
					1 / (((TimeConstant / lastFactorTime) + 1) / 2);

				// Derivation:
				// ((2÷percent)−1)×FactorTime = ms
				// (ms ÷ FactorTime) = ((2÷percent)-1)
				// (ms ÷ FactorTime) + 1 = (2÷percent)
				// ((ms ÷ FactorTime) + 1) ÷ 2 = (1÷percent)
				// percent = 1 ÷ (((ms ÷ FactorTime) + 1) ÷ 2)
				//
				// (If ms < FactorTime, set percent = 1)
				//
				// Finding the time period:
				// x=0
				// x=((x×(1−p))+(255×p))
				// p = 0.7 freshness (e.g. 0.3 of past value)
				// ((2÷0.7)−1) = 1.85 periods, which at 50 ms per period...
				// ((2÷0.7)−1)×50 = 92.8 ms to 63% mark
				//
				// With p = 0.9
				// ((2÷0.9)−1) = 1.22 periods
				// ((2÷0.9)−1)×50 = 61.1 ms to 63% mark
				//
				// Examples:
				// Using this equation: 1÷(((61ms ÷ FactorTime)+1)÷2)
				// For 50ms, 1÷(((61 ÷ 50)+1)÷2) = 0.900900901 (rounding errors)
				// For 4ms, 1÷(((61 ÷ 4)+1)÷2) = 0.123076923
				// To check...
				// ((2÷0.123076923)−1)×4 = 61.0 ms
				//
				// Converting old values to TimeConstant:
				// ms = ((2÷percent)−1)×50
				// (Assuming 50 ms update rate of the first LED strand)
				//
				// NOTE: SmoothingAmount from AbstractAnimation specifies the
				// "oldness" value, NOT the "freshness" value.  To convert it,
				// go with...
				// ms = ((2÷(1−smoothing_percent)−1)×50
			}
		}

		/// <summary>
		/// The weighting multiplier as a positive decimal.
		/// </summary>
		private double cachedWeightMultiplier = 0;

		/// <summary>
		/// The last-used time factor as a positive decimal, or -1 if not used.
		/// </summary>
		private double lastFactorTime = -1;

		/// <summary>
		/// The time constant of the scaled average in milliseconds.
		/// </summary>
		private double timeConstant = -1;

		/// <summary>
		/// The device configuration used for scaling the smoothing amount.
		/// </summary>
		private ReadOnlyDeviceConfiguration deviceConfig;

		#endregion
	}
}

