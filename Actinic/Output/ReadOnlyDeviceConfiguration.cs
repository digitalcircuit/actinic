//
//  ReadOnlyDeviceConfiguration.cs
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
	/// Read-only view of output device configuration.
	/// </summary>
	public class ReadOnlyDeviceConfiguration
	{
		#region Constructors

		/// <summary>
		/// Initializes a new read-only view of the given output device
		/// configuration.
		/// </summary>
		/// <param name="DeviceConfig">Read-write view of the device configuration.</param>
		public ReadOnlyDeviceConfiguration (DeviceConfiguration DeviceConfig)
		{
			if (DeviceConfig == null) {
				// Need a valid device configuration
				throw new ArgumentNullException ("DeviceConfig");
			}

			// Keep the real device configuration private; only provide access
			// via the wrapper.  This enforces the read-only nature, though it
			// may make synchronization more difficult.
			// See https://stackoverflow.com/questions/2724731/how-can-i-make-a-read-only-version-of-a-class
			realDeviceConfig = DeviceConfig;
		}

		#endregion

		#region Properties

		/// <summary>
		/// Gets the number of LEDs on the strand of lights, e.g. 100 LEDs.
		/// </summary>
		/// <value>Number of LEDs on the strand of lights.</value>
		public int LightCount {
			get {
				return realDeviceConfig.LightCount;
			}
		}

		/// <summary>
		/// Gets the lighted length of the strand in meters, e.g. 10 meters.
		/// </summary>
		/// <value>The lighted length of the strand in meters.</value>
		public double StrandLength {
			get {
				return realDeviceConfig.StrandLength;
			}
		}

		/// <summary>
		/// Number of lights per meter of strand, e.g. 10 lights-per-meter.
		/// </summary>
		/// <value>Number of lights within each meter of strand as a positive decimal.</value>
		public double LightsPerMeter {
			get {
				return realDeviceConfig.LightsPerMeter;
			}
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
				return realDeviceConfig.AverageLatency;
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
			get {
				return realDeviceConfig.AverageDeviceLatency;
			}
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
			get {
				return realDeviceConfig.AverageRenderLatency;
			}
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
				return realDeviceConfig.FactorTime;
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
			get {
				return realDeviceConfig.FactorFixedSize;
			}
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
			get {
				return realDeviceConfig.FactorScaledSize;
			}
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
			return realDeviceConfig.ClipIndex (Index);
		}

		/// <summary>
		/// Returns a string that represents the current
		/// <see cref="ReadOnlyDeviceConfiguration"/>.
		/// </summary>
		/// <returns>A string that represents the current <see cref="ReadOnlyDeviceConfiguration"/>.</returns>
		public override string ToString ()
		{
			return string.Format (
				"[ReadOnlyDeviceConfiguration: LightCount={0}, " +
				"StrandLength={1}, LightsPerMeter={2}, AverageLatency={3}" +
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
		/// The underlying read-write output device configuration.
		/// </summary>
		private DeviceConfiguration realDeviceConfig;

		#endregion
	}
}
