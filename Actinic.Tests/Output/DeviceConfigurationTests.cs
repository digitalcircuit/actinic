//
//  DeviceConfigurationTests.cs
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

using Actinic.Output;

namespace Actinic.Tests.Output
{
	[TestFixture]
	public class DeviceConfigurationTests
	{
		#region Construction

		[Test]
		public void Construct_Direct ()
		{
			// [Arrange]
			const int customLightCount = 25;
			const double customStrandLength = 5.5;

			// [Act]
			DeviceConfiguration sampleDevice =
				new DeviceConfiguration (customLightCount, customStrandLength);

			// [Assert]
			Assert.That (sampleDevice.LightCount,
				Is.EqualTo (customLightCount)
			);
			Assert.That (sampleDevice.StrandLength,
				Is.EqualTo (customStrandLength)
			);
		}

		[Test]
		public void Construct_Dependent_Standard ()
		{
			// [Arrange]
			const int customLightCount = 1;
			const double customStrandLength = 1;
			// For standard, multiplying factor should be 1
			const double expectedFactorFixed = 1;
			const double expectedFactorScaled = 1;
			// Update rate isn't yet known
			const double defaultUpdateRate = 0;

			// [Act]
			DeviceConfiguration sampleDevice =
				new DeviceConfiguration (customLightCount, customStrandLength);

			// [Assert]
			Assert.That (sampleDevice.LightsPerMeter,
				Is.EqualTo (customLightCount / customStrandLength)
			);
			Assert.That (sampleDevice.FactorFixedSize,
				Is.EqualTo (expectedFactorFixed)
			);
			Assert.That (sampleDevice.FactorScaledSize,
				Is.EqualTo (expectedFactorScaled)
			);
			Assert.That (sampleDevice.FactorTime,
				Is.EqualTo (defaultUpdateRate)
			);
		}

		[Test]
		public void Construct_Dependent_Custom (
			[Values (25, 50, 75, 300)] int customLightCount,
			[Values (5.5, 12.4, 26.0, 10)] double customStrandLength)
		{
			// [Arrange]
			const int baseLightCount = 1;
			const double baseStrandLength = 1;
			// Ratio of current lights-per-meter to base
			double expectedFactorFixed =
				(customLightCount / customStrandLength) /
				(baseLightCount / baseStrandLength);
			// Ratio of spacing between lights
			double expectedFactorScaled =
				expectedFactorFixed * (customStrandLength / baseStrandLength);
			// Update rate isn't yet known
			const double defaultUpdateRate = 0;

			// [Act]
			DeviceConfiguration sampleDevice =
				new DeviceConfiguration (customLightCount, customStrandLength);

			// [Assert]
			Assert.That (sampleDevice.LightsPerMeter,
				Is.EqualTo (customLightCount / customStrandLength)
			);
			Assert.That (sampleDevice.FactorFixedSize,
				Is.EqualTo (expectedFactorFixed)
			);
			Assert.That (sampleDevice.FactorScaledSize,
				Is.EqualTo (expectedFactorScaled)
			);
			Assert.That (sampleDevice.FactorTime,
				Is.EqualTo (defaultUpdateRate)
			);
		}

		[Test]
		public void Construct_ZeroLights_Throws ()
		{
			// [Arrange]
			// Zero or less lights aren't valid
			const int invalidLightCount = 0;
			const double validStrandLength = 1;

			// [Act/Assert]
			Assert.That (
				delegate {
					new DeviceConfiguration (
						invalidLightCount, validStrandLength);
				},
				Throws.TypeOf<ArgumentOutOfRangeException> ()
				.With.Property ("ParamName").EqualTo ("LightCount")
			);
		}

		[Test]
		public void Construct_ZeroLength_Throws ()
		{
			// [Arrange]
			const int validLightCount = 1;
			// Zero or less length isn't valid
			const double invalidStrandLength = 0;

			// [Act/Assert]
			Assert.That (
				delegate {
					new DeviceConfiguration (
						validLightCount, invalidStrandLength);
				},
				Throws.TypeOf<ArgumentOutOfRangeException> ()
				.With.Property ("ParamName").EqualTo ("StrandLength")
			);
		}

		#endregion

		#region Functions - ClipIndex

		[Test]
		public void ClipIndex_Valid_Test ()
		{
			// [Arrange]
			const int validIndex = 3;
			DeviceConfiguration sampleDevice = new DeviceConfiguration (10, 1);

			// [Act/Assert]
			Assert.That (sampleDevice.ClipIndex (validIndex),
				Is.EqualTo (validIndex)
			);
		}

		[Test]
		public void ClipIndex_OutOfBounds_Positive_Test (
			[Values (25, 50, 75)] int customLightCount)
		{
			// [Arrange]
			int outOfBoundsPositiveIndex = customLightCount + 1;
			int clippedIndexLast = customLightCount - 1;
			DeviceConfiguration sampleDevice =
				new DeviceConfiguration (customLightCount, 1);

			// [Act/Assert]
			Assert.That (sampleDevice.ClipIndex (outOfBoundsPositiveIndex),
				Is.EqualTo (clippedIndexLast)
			);
		}

		[Test]
		public void ClipIndex_OutOfBounds_Negative_Test ()
		{
			// [Arrange]
			const int invalidIndex = -1;
			const int clippedIndexFirst = 0;
			DeviceConfiguration sampleDevice = new DeviceConfiguration (10, 1);

			// [Act/Assert]
			Assert.That (sampleDevice.ClipIndex (invalidIndex),
				Is.EqualTo (clippedIndexFirst)
			);
		}

		#endregion

		#region Functions - SetUpdateRate

		[Test]
		public void SetUpdateRate_Default_Test ()
		{
			// [Arrange]
			const double defaultUpdateRate = 1;
			DeviceConfiguration sampleDevice = new DeviceConfiguration (1, 1);

			// [Act]
			sampleDevice.SetUpdateRate (defaultUpdateRate);

			// [Assert]
			Assert.That (sampleDevice.FactorTime,
				Is.EqualTo (1)
			);
		}

		[Test]
		public void SetUpdateRate_NotSet_Test ()
		{
			// [Arrange]
			const double minimumValidFactorTime = 0;
			// Infinitely-fast updates are theoretically possible...
			DeviceConfiguration sampleDevice = new DeviceConfiguration (1, 1);

			// [Act]
			// Do nothing

			// [Assert]
			Assert.That (sampleDevice.FactorTime,
				Is.GreaterThanOrEqualTo (minimumValidFactorTime)
			);
		}

		[Test]
		public void SetUpdateRate_Custom_Test ()
		{
			// [Arrange]
			const double customUpdateRate = 50;
			const double expectedFactorTime = customUpdateRate;
			// 1-to-1 ratio, 50 ms to update, 50 times more should happen to
			// make up for the difference.
			DeviceConfiguration sampleDevice = new DeviceConfiguration (1, 1);

			// [Act]
			sampleDevice.SetUpdateRate (customUpdateRate);

			// [Assert]
			Assert.That (sampleDevice.FactorTime,
				Is.EqualTo (expectedFactorTime)
			);
		}

		[Test]
		public void SetUpdateRate_CustomDeviceRender_Test ()
		{
			// [Arrange]
			const double customDeviceUpdateRate = 3;
			const double customRenderUpdateRate = 5;
			const double expectedFactorTime =
				(customDeviceUpdateRate + customRenderUpdateRate);
			// 1-to-1 ratio, 8 ms to update, 8 times more should happen to
			// make up for the difference.
			DeviceConfiguration sampleDevice = new DeviceConfiguration (1, 1);

			// [Act]
			sampleDevice.SetUpdateRate (
				customDeviceUpdateRate, customRenderUpdateRate);

			// [Assert]
			Assert.That (sampleDevice.FactorTime,
				Is.EqualTo (expectedFactorTime)
			);
		}

		[Test]
		public void SetUpdateRate_NegativeDeviceDelay_Throws ()
		{
			// [Arrange]
			DeviceConfiguration sampleDevice = new DeviceConfiguration (1, 1);
			double invalidUpdateRate = -1;

			// [Act/Assert]
			Assert.That (
				delegate {
					sampleDevice.SetUpdateRate (invalidUpdateRate);
				},
				Throws.TypeOf<ArgumentOutOfRangeException> ()
				.With.Property ("ParamName").EqualTo ("DeviceDelay")
			);
		}

		[Test]
		public void SetUpdateRate_NegativeRenderDelay_Throws ()
		{
			// [Arrange]
			DeviceConfiguration sampleDevice = new DeviceConfiguration (1, 1);
			double validUpdateRate = 0;
			double invalidUpdateRate = -1;

			// [Act/Assert]
			Assert.That (
				delegate {
					sampleDevice.SetUpdateRate (
						validUpdateRate, invalidUpdateRate);
				},
				Throws.TypeOf<ArgumentOutOfRangeException> ()
				.With.Property ("ParamName").EqualTo ("RenderDelay")
			);
		}

		[Test]
		public void UpdateRate_AverageDeviceLatency_Initial_Test ()
		{
			// [Arrange]
			double updateRate = 50;
			double defaultRenderUpdateRate = 0;
			DeviceConfiguration sampleDevice = new DeviceConfiguration (1, 1);

			// [Act]
			// On the first call, this should directly set average latency
			sampleDevice.SetUpdateRate (updateRate);

			// [Assert]
			Assert.That (sampleDevice.AverageDeviceLatency,
				Is.EqualTo (updateRate),
				"AverageDeviceLatency does not match value."
			);
			Assert.That (sampleDevice.AverageRenderLatency,
				Is.EqualTo (defaultRenderUpdateRate),
				"AverageRenderLatency should not change from default."
			);
		}

		[Test]
		public void UpdateRate_AverageDeviceLatency_Averages_Test ()
		{
			// [Arrange]
			double updateRateInitial = 1;
			double updateRateNext = 100;
			const double tolerance = 0.1;
			DeviceConfiguration sampleDevice = new DeviceConfiguration (1, 1);

			// [Act]
			// Set the first
			sampleDevice.SetUpdateRate (updateRateInitial);
			// Set the next
			sampleDevice.SetUpdateRate (updateRateNext);

			// [Assert]
			// Check that average latency does not match final already
			Assert.That (sampleDevice.AverageDeviceLatency,
				Is.Not.InRange (updateRateNext - tolerance,
					updateRateNext + tolerance),
				"AverageDeviceLatency jumped from initial to second value."
			);
		}

		[Test]
		public void UpdateRate_AverageDeviceLatency_Tolerance_Test ()
		{
			// [Arrange]
			double[] updateRates = { 1, 50, 4.5, 1 };
			const double tolerance = 0.1;
			const int iterations = 20;
			double defaultRenderUpdateRate = 0;
			DeviceConfiguration sampleDevice = new DeviceConfiguration (1, 1);

			// [Act/Assert]
			foreach (var updateRate in updateRates) {
				// Set update rate many times
				for (int i = 0; i < iterations; i++) {
					sampleDevice.SetUpdateRate (updateRate);
				}
				// Check that average latency matches
				Assert.That (sampleDevice.AverageDeviceLatency,
					Is.InRange (updateRate - tolerance,
						updateRate + tolerance),
					String.Format (
						"AverageDeviceLatency does not match end ({0}) " +
						"after {1} iterations",
						updateRate, iterations
					)
				);
				// Check that processing latency remains unchanged
				Assert.That (sampleDevice.AverageRenderLatency,
					Is.EqualTo (defaultRenderUpdateRate),
					"AverageRenderLatency should not change from default."
				);
			}
		}

		[Test]
		public void UpdateRate_AverageRenderLatency_Initial_Test ()
		{
			// [Arrange]
			double deviceUpdateRate = 1;
			double updateRate = 50;
			DeviceConfiguration sampleDevice = new DeviceConfiguration (1, 1);

			// [Act]
			// On the first call, this should directly set average latency
			sampleDevice.SetUpdateRate (deviceUpdateRate, updateRate);

			// [Assert]
			Assert.That (sampleDevice.AverageRenderLatency,
				Is.EqualTo (updateRate)
			);
		}

		[Test]
		public void UpdateRate_AverageRenderLatency_Averages_Test ()
		{
			// [Arrange]
			double deviceUpdateRate = 1;
			double updateRateInitial = 1;
			double updateRateNext = 100;
			const double tolerance = 0.1;
			DeviceConfiguration sampleDevice = new DeviceConfiguration (1, 1);

			// [Act]
			// Set the first
			sampleDevice.SetUpdateRate (deviceUpdateRate, updateRateInitial);
			// Set the next
			sampleDevice.SetUpdateRate (deviceUpdateRate, updateRateNext);

			// [Assert]
			// Check that average latency does not match final already
			Assert.That (sampleDevice.AverageRenderLatency,
				Is.Not.InRange (updateRateNext - tolerance,
					updateRateNext + tolerance),
				"AverageRenderLatency jumped from initial to second value."
			);
			Assert.That (sampleDevice.AverageDeviceLatency,
				Is.EqualTo (deviceUpdateRate),
				"AverageDeviceLatency does not match fixed device update rate."
			);
		}

		[Test]
		public void UpdateRate_AverageRenderLatency_Tolerance_Test ()
		{
			// [Arrange]
			double deviceUpdateRate = 1;
			double[] updateRates = { 1, 50, 4.5, 1 };
			const double tolerance = 0.1;
			const int iterations = 20;
			DeviceConfiguration sampleDevice = new DeviceConfiguration (1, 1);

			// [Act/Assert]
			foreach (var updateRate in updateRates) {
				// Set update rate many times
				for (int i = 0; i < iterations; i++) {
					sampleDevice.SetUpdateRate (deviceUpdateRate, updateRate);
				}
				// Check that average latency matches
				Assert.That (sampleDevice.AverageRenderLatency,
					Is.InRange (updateRate - tolerance,
						updateRate + tolerance),
					String.Format (
						"AverageRenderLatency does not match end ({0}) " +
						"after {1} iterations",
						updateRate, iterations
					)
				);
				Assert.That (sampleDevice.AverageDeviceLatency,
					Is.EqualTo (deviceUpdateRate),
					"AverageDeviceLatency does not match fixed device update" +
					"rate."
				);
			}
		}

		[Test]
		public void UpdateRate_AverageLatency_Sum_Test ()
		{
			// [Arrange]
			const double customDeviceUpdateRate = 13.5;
			const double customRenderUpdateRate = 14.7;
			DeviceConfiguration sampleDevice = new DeviceConfiguration (1, 1);

			// [Act]
			sampleDevice.SetUpdateRate (
				customDeviceUpdateRate, customRenderUpdateRate);

			// [Assert]
			Assert.That (sampleDevice.AverageLatency,
				Is.EqualTo (
					sampleDevice.AverageDeviceLatency
					+ sampleDevice.AverageRenderLatency
				),
				String.Format (
					"AverageLatency does not match summation of " +
					"AverageDeviceLatency ({0}) and AverageRenderLatency " +
					"({1}).",
					sampleDevice.AverageDeviceLatency,
					sampleDevice.AverageRenderLatency
				)
			);
		}

		#endregion

		#region Functions - ToString

		[Test]
		public void ToString_Test ()
		{
			// [Arrange]
			const int customLightCount = 25;
			const double customStrandLength = 5.5;

			const double customDeviceUpdateRate = 13.5;
			const double customRenderUpdateRate = 14.7;

			DeviceConfiguration sampleDevice =
				new DeviceConfiguration (customLightCount, customStrandLength);
			sampleDevice.SetUpdateRate (
				customDeviceUpdateRate, customRenderUpdateRate);

			// Prepare the expected result of converting to string
			string expectedResult =
				string.Format (
					"[DeviceConfiguration: LightCount={0}, StrandLength={1}, " +
					"LightsPerMeter={2}, AverageLatency={3}, " +
					"AverageDeviceLatency={4}, AverageRenderLatency={5}, " +
					"FactorTime={6}, FactorFixedSize={7}, " +
					"FactorScaledSize={8}]",
					sampleDevice.LightCount,
					sampleDevice.StrandLength,
					sampleDevice.LightsPerMeter,
					sampleDevice.AverageLatency,
					sampleDevice.AverageDeviceLatency,
					sampleDevice.AverageRenderLatency,
					sampleDevice.FactorTime,
					sampleDevice.FactorFixedSize,
					sampleDevice.FactorScaledSize
				);

			// [Act/Assert]
			Assert.That (sampleDevice.ToString (),
				Is.EqualTo (expectedResult)
			);
		}

		#endregion
	}
}

