//
//  DeviceConfiguration.cs
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
using System;

namespace Actinic.Output
{
	/// <summary>
	/// Output device configuration.
	/// </summary>
	public class DeviceConfiguration
	{
		#region Constructors

		/// <summary>
		/// Initializes a new output device configuration.
		/// </summary>
		/// <param name="LightCount">Number of lights connected to output device.</param>
		/// <param name="StrandLength">Length of light strand connected to output device.</param>
		public DeviceConfiguration (int LightCount, double StrandLength)
		{
			if (LightCount <= 0) {
				// Need at least one light
				throw new ArgumentOutOfRangeException (
					"LightCount", "LightCount must be greater than zero."
				);
			}
			if (StrandLength <= 0) {
				// Need more than an infinitely-small length of lights
				throw new ArgumentOutOfRangeException (
					"StrandLength", "StrandLength must be greater than zero."
				);
			}

			// Store values, calculate dependent values
			this.LightCount = LightCount;
			this.StrandLength = StrandLength;
			CalculateDependentConfiguration ();
		}

		#endregion

		#region Properties

		/// <summary>
		/// Gets the number of LEDs on the strand of lights, e.g. 100 LEDs.
		/// </summary>
		/// <value>Number of LEDs on the strand of lights.</value>
		public int LightCount {
			get;
			private set;
		}

		/// <summary>
		/// Gets the lighted length of the strand in meters, e.g. 10 meters.
		/// </summary>
		/// <value>The lighted length of the strand in meters.</value>
		public double StrandLength {
			get;
			private set;
		}

		/// <summary>
		/// Number of lights per meter of strand, e.g. 10 lights-per-meter.
		/// </summary>
		/// <value>Number of lights within each meter of strand as a positive decimal.</value>
		public double LightsPerMeter {
			get;
			private set;
		}

		/// <summary>
		/// Gets the average time taken to process and send an update to the
		/// device.
		/// <remarks>
		/// This is meant for performance tuning and smoothness.  To scale
		/// animations according to frames-per-second, <see cref="FactorTime"/>.
		/// </remarks>
		/// </summary>
		/// <value>The average total system latency.</value>
		public double AverageLatency {
			get {
				return AverageDeviceLatency + AverageRenderLatency;
			}
		}

		/// <summary>
		/// Gets the average time taken to send an update to the device, not
		/// including processing time.  For total average time, combine with
		/// <see cref="AverageRenderLatency"/>.
		/// <remarks>
		/// This is meant for performance tuning and smoothness.  To scale
		/// animations according to frames-per-second, <see cref="FactorTime"/>.
		/// </remarks>
		/// </summary>
		/// <value>The average device latency.</value>
		public double AverageDeviceLatency {
			get;
			private set;
		}

		/// <summary>
		/// Gets the average time taken for all processing, excluding time taken
		/// to send an update to the device.  For total average time, combine with
		/// <see cref="AverageDeviceLatency"/>.
		/// <remarks>
		/// This is meant for performance tuning and smoothness.  To scale
		/// animations according to frames-per-second, <see cref="FactorTime"/>.
		/// </remarks>
		/// </summary>
		/// <value>The average processing latency.</value>
		public double AverageRenderLatency {
			get;
			private set;
		}

		/// <summary>
		/// Gets the multiplier for maintaining a fixed amount of movement
		/// regardless of the current animation speed.
		/// <remarks>
		/// This should roughly equal 1 when running at 1 millisecond per frame.
		/// </remarks>
		/// <example>
		/// For example, "MoveAmount = 2 * (FactorTime / 50)" would result in a
		/// movement of 2 units every 50 milliseconds.  If updating twice as
		/// fast at 25 ms per frame, FactorTime would then result in 1 unit
		/// every 25 ms, keeping a consistent animation speed.
		/// </example>
		/// </summary>
		/// <value>The time factor as a positive decimal.</value>
		public double FactorTime {
			get {
				if (cachedFactorTime == -1) {
					// Cleared or never set, need to calculate
					// This is calculated on demand to avoid wasting time if it's never needed.
					CalculateTiming ();
				}
				return cachedFactorTime;
			}
		}

		/// <summary>
		/// Gets the multiplier for maintaining a fixed physical size regardless
		/// of the output light spacing or length.
		/// This functions as a rough equivalent to density-independent pixels.
		/// Use carefully, otherwise size may exceed the length of small light
		/// strands.
		/// <example>
		/// For example, "LineLEDCount = 2 * (FactorFixedSize)" would result
		/// in the number of LEDs needed to make a line that is 2 meters long
		/// (ignoring integer rounding).
		/// </example>
		/// </summary>
		/// <value>The fixed size factor as a positive decimal.</value>
		public double FactorFixedSize {
			get;
			private set;
		}

		/// <summary>
		/// Gets the multiplier for maintaining a physical size that scales
		/// according to the length of the output lights irrespective of how the
		/// lights are spaced.
		/// This functions as a rough equivalent to scale-independent pixels, or
		/// scaling a layout for different density screens.
		/// <example>
		/// For example, "LineLEDCount = 0.5 * (FactorScaledSize)" would result
		/// in the number of pixels needed to make a line that occupies half the
		/// total length of the light (ignoring integer rounding).
		/// </example>
		/// </summary>
		/// <value>The scaled size factor as a positive decimal.</value>
		public double FactorScaledSize {
			get;
			private set;
		}

		#endregion

		#region Shared Functions

		/// <summary>
		/// Clips the index to fit within the bounds of the LED strand, e.g.
		/// from 0 to <see cref="LightCount"/> - 1.
		/// </summary>
		/// <returns>The index, clipped to ensure it points to a valid LED.</returns>
		/// <param name="Index">Unverified index.</param>
		public int ClipIndex (int Index)
		{
			if (Index < 0) {
				// Minimum bound: 0
				return 0;
			} else if (Index > (LightCount - 1)) {
				// Maximum bound: number of LEDs, adjusted for 0-indexing
				return (LightCount - 1);
			} else {
				// Fits within bound
				return Index;
			}
		}

		/// <summary>
		/// Sets the delay between each update in milliseconds for the output
		/// device.
		/// <remarks>
		/// To avoid inconsistency, this should not be called while rendering
		/// frames.  Wait until the start or end of a frame.
		/// </remarks>
		/// </summary>
		/// <param name="DeviceDelay">Delay between updates in milliseconds.</param>
		/// <param name="RenderDelay">Delay </param>
		public void SetUpdateRate (
			double DeviceDelay, double RenderDelay = 0)
		{
			if (DeviceDelay < 0) {
				// Need at least zero or positive update delay
				throw new ArgumentOutOfRangeException (
					"DeviceDelay",
					"DeviceDelay must be greater than or equal to zero."
				);
			}

			if (RenderDelay < 0) {
				// Need at least zero or positive update delay
				throw new ArgumentOutOfRangeException (
					"RenderDelay",
					"RenderDelay must be greater than or equal to zero."
				);
			}

			if (!hasSetUpdateRate) {
				// Skip averaging when first setting the value
				AverageDeviceLatency = DeviceDelay;
				AverageRenderLatency = RenderDelay;
				hasSetUpdateRate = true;
			} else {
				// Update moving average, biasing towards older values to reduce
				// the impact of sudden spikes
				const double freshness = 0.3;
				// Average = (new * freshness) + (old * inverse-freshness)
				AverageDeviceLatency = (
				    (DeviceDelay * freshness)
				    + (AverageDeviceLatency * (1 - freshness))
				);
				AverageRenderLatency = (
				    (RenderDelay * freshness)
				    + (AverageRenderLatency * (1 - freshness))
				);
			}

			// Find the combined device delay and processing delay
			double UpdateDelay = DeviceDelay + RenderDelay;

			if (cachedUpdateDelay != UpdateDelay) {
				cachedUpdateDelay = UpdateDelay;
				// Reset the time factor
				cachedFactorTime = -1;
			}
		}

		/// <summary>
		/// Returns a string that represents the current
		/// <see cref="DeviceConfiguration"/>.
		/// </summary>
		/// <returns>A string that represents the current <see cref="DeviceConfiguration"/>.</returns>
		public override string ToString ()
		{
			return string.Format (
				"[DeviceConfiguration: LightCount={0}, StrandLength={1}, " +
				"LightsPerMeter={2}, AverageLatency={3}, " +
				"AverageDeviceLatency={4}, AverageRenderLatency={5}, " +
				"FactorTime={6}, FactorFixedSize={7}, FactorScaledSize={8}]",
				LightCount, StrandLength, LightsPerMeter, AverageLatency,
				AverageDeviceLatency, AverageRenderLatency, FactorTime,
				FactorFixedSize, FactorScaledSize
			);
		}

		#endregion

		#region Internal

		/// <summary>
		/// Calculates all configuration dependent on the number of lights and
		/// length of strand.
		/// </summary>
		private void CalculateDependentConfiguration ()
		{
			// Update the number of lights-per-meter
			CalculateLightsPerMeter ();
			// Update the density-independent scaling factors
			CalculateDensityScaling ();
		}

		/// <summary>
		/// Calculates the number of lights per meter.  See
		/// <see cref="LightsPerMeter"/>.
		/// </summary>
		private void CalculateLightsPerMeter ()
		{
			// If you have 50 lights on 12.4 meters, this would give you
			// 50 / 12.4 = 4.0-ish lights per meter.
			LightsPerMeter = (LightCount / StrandLength);
		}

		/// <summary>
		/// Calculates the scaling factors to maintain a density-independent
		/// display.
		/// </summary>
		private void CalculateDensityScaling ()
		{
			// Find the fixed scaling factor
			// > If LPM = 4, 10 lights / LPM = 2.5 meters
			// > If LPM = 2, 10 lights / LPM = 5   meters
			// For a consistent scale, multiply the number of lights from the
			// old value to the scaled, e.g.
			// > lights * (new LPM / base LPM) = new light count
			// LPM changes according to the number of lights or the length, thus
			// remaining fixed.
			FactorFixedSize = LightsPerMeter / baseLightsPerMeter;
			// This is dividing by 1.  Originally designed before settling on 1
			// light per meter, this allows changing the base scaling if needed.

			// Adjust the fixed scaling factor according to the differences in
			// length
			// For a length-dependent scale, multiply the number of lights from
			// old value to scaled, e.g.
			// > lights * (fixed scaling factor * (new length / base length))
			//   = new light count
			FactorScaledSize =
				FactorFixedSize * (StrandLength / baseStrandLength);
			// Similar to above, this is dividing by 1.  As before, allows
			// changing the base scaling if needed.
		}

		/// <summary>
		/// Calculates the scaling factor for time.  See
		/// <see cref="FactorTime"/>.
		/// </summary>
		private void CalculateTiming ()
		{
			// This provides a ratio centered on 1 millisecond, or 1000 frames
			// per second.
			cachedFactorTime = cachedUpdateDelay / baseUpdateDelay;
			// This is dividing by 1.  Originally designed before settling on
			// the 1 ms ratio, this allows changing the base update rate if
			// needed.
		}

		/// <summary>
		/// Whether or not the update rate has ever been set.  This is used to
		/// skip averaging update rate when it's first set.
		/// </summary>
		private bool hasSetUpdateRate = false;

		/// <summary>
		/// The time factor as a positive decimal, or -1 if not calculated.  See
		/// <see cref="FactorTime"/>.
		/// </summary>
		private double cachedFactorTime = -1;

		/// <summary>
		/// The delay between updates in milliseconds.
		/// </summary>
		private double cachedUpdateDelay = 0;

		/// <summary>
		/// The base delay between updates in milliseconds.
		/// <remarks>
		/// By setting this to 1, scaling works on a millisecond level, making
		/// it easier to reason about the exact amount of time used.  See
		/// <see cref="FactorTime"/> for an example.
		/// </remarks>
		/// </summary>
		private const double baseUpdateDelay = 1;
		// The first used LED strand could update at least once every 50 ms.
		// Thus, FactorTime = 50.

		/// <summary>
		/// The base lighted length of the strand in meters as a positive
		/// decimal.  See <see cref="StrandLength"/>.
		/// <remarks>
		/// By setting this to 1, scaling works on a meter level, making
		/// it easier to reason about the exact length used.
		/// </remarks>
		/// </summary>
		private const double baseStrandLength = 1;
		// The first used LED strand was 12.4 meters long.

		/// <summary>
		/// The base number of lights per meter of strand as a positive decimal.
		/// See <see cref="LightsPerMeter"/>.
		/// <remarks>
		/// By setting this to 1, scaling works on a meter level, making
		/// it easier to reason about the exact length used.
		/// </remarks>
		/// </summary>
		private const double baseLightsPerMeter = (1 / baseStrandLength);
		// The first used LED strand had 50 lights over 12.4 meters, roughly
		// equal to 4.03225806451613.
		// Thus, FactorFixedSize = 4.03225806451613 and FactorScaledSize = 50.

		#endregion
	}
}
