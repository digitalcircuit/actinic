//
//  ScaledAverageTests.cs
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
using NUnit.Framework;
using System;
using System.Collections;

using Actinic.Output;
using Actinic.Utilities;

namespace Actinic.Tests
{
	[TestFixture ()]
	public class ScaledAverageTests
	{
		#region Construction

		[Test]
		public void Construct_Default ()
		{
			// [Arrange]
			DeviceConfiguration sampleDevice =
				new DeviceConfiguration (1, 1);
			sampleDevice.SetUpdateRate (50);
			ReadOnlyDeviceConfiguration sampleDeviceRO =
				new ReadOnlyDeviceConfiguration (sampleDevice);

			// [Act]
			ScaledAverage sampleFilter = new ScaledAverage (sampleDeviceRO);

			// [Assert]
			Assert.That (sampleFilter.TimeConstant,
				Is.EqualTo (0),
				"Default time constant value does not match"
			);
		}

		[Test]
		public void Construct_Direct ()
		{
			// [Arrange]
			DeviceConfiguration sampleDevice =
				new DeviceConfiguration (1, 1);
			sampleDevice.SetUpdateRate (50);
			ReadOnlyDeviceConfiguration sampleDeviceRO =
				new ReadOnlyDeviceConfiguration (sampleDevice);

			const double customTimeConstant = 92.857142857;
			// Weight multiplier is exhaustively checked in another test; this
			// just verifies basic setup
			const double expectedWeightMultiplier = 0.7;
			const double tolerance = 0.001;

			// [Act]
			ScaledAverage sampleFilter =
				new ScaledAverage (sampleDeviceRO, customTimeConstant);

			// [Assert]
			Assert.That (sampleFilter.TimeConstant,
				Is.EqualTo (customTimeConstant),
				"Custom time constant value does not match"
			);

			Assert.That (sampleFilter.WeightMultiplier,
				Is.InRange (expectedWeightMultiplier - tolerance,
					expectedWeightMultiplier + tolerance),
				"Expected weight multiplier value does not match"
			);
		}

		[Test]
		public void Construct_Null_Throws ()
		{
			// [Arrange]
			const ReadOnlyDeviceConfiguration invalidDeviceRO = null;

			// [Act/Assert]
			Assert.That (
				delegate {
					new ScaledAverage (invalidDeviceRO);
				},
				Throws.TypeOf<ArgumentNullException> ()
				.With.Property ("ParamName").EqualTo ("DeviceConfig")
			);
		}

		[Test]
		public void Construct_NegativeConstant_Throws ()
		{
			// [Arrange]
			DeviceConfiguration sampleDevice =
				new DeviceConfiguration (1, 1);
			sampleDevice.SetUpdateRate (50);
			ReadOnlyDeviceConfiguration sampleDeviceRO =
				new ReadOnlyDeviceConfiguration (sampleDevice);

			// [Act/Assert]
			Assert.That (
				delegate {
					new ScaledAverage (sampleDeviceRO, -1);
				},
				Throws.TypeOf<ArgumentOutOfRangeException> ()
				.With.Property ("ParamName").EqualTo ("Constant")
			);
		}

		#endregion

		#region Properties - TimeConstant

		/// <summary>
		/// Test cases for time constants at 50 ms
		/// </summary>
		private class KnownTimeConstants
		{
			public static IEnumerable TestCases {
				get {
					yield return new TestCaseData (50.0, 1.0);
					yield return new TestCaseData (51.0, 0.99009901);
					yield return new TestCaseData (61.1, 0.900090009);
					yield return new TestCaseData (92.857142857, 0.7);
					yield return new TestCaseData (1000, 0.095238095);
				}
			}
		}

		[Test, TestCaseSource (typeof(KnownTimeConstants), "TestCases")]
		public void TimeConstant_SetKnown_Test (
			double customTimeConstant, double expectedWeightMultiplier)
		{
			// [Arrange]
			const double customUpdateRate = 50;
			DeviceConfiguration sampleDevice =
				new DeviceConfiguration (1, 1);
			sampleDevice.SetUpdateRate (customUpdateRate);
			ReadOnlyDeviceConfiguration sampleDeviceRO =
				new ReadOnlyDeviceConfiguration (sampleDevice);
			ScaledAverage sampleFilter =
				new ScaledAverage (sampleDeviceRO);

			const double tolerance = 0.000001;

			// Ensure the TimeConstant has been calculated at least once
			sampleFilter.Filter (0, 0);

			// [Act]
			sampleFilter.TimeConstant = customTimeConstant;

			// [Assert]
			Assert.That (sampleFilter.TimeConstant,
				Is.EqualTo (customTimeConstant),
				"Time constant value does not match"
			);

			Assert.That (sampleFilter.WeightMultiplier,
				Is.InRange (expectedWeightMultiplier - tolerance,
					expectedWeightMultiplier + tolerance),
				"Expected weight multiplier value does not match"
			);
		}

		[Test]
		public void TimeConstant_LessThanUpdateRate_Test ()
		{
			// [Arrange]
			double customUpdateRate = 50;
			double customTimeConstant = customUpdateRate - 1;
			DeviceConfiguration sampleDevice =
				new DeviceConfiguration (1, 1);
			// Set update rate
			sampleDevice.SetUpdateRate (customUpdateRate);
			ReadOnlyDeviceConfiguration sampleDeviceRO =
				new ReadOnlyDeviceConfiguration (sampleDevice);
			ScaledAverage sampleFilter = new ScaledAverage (sampleDeviceRO);

			// [Act]
			sampleFilter.TimeConstant = customTimeConstant;

			// [Assert]
			Assert.That (sampleFilter.WeightMultiplier,
				Is.EqualTo (1),
				"Weight multiplier for time constants less than update rate " +
				"does not match"
			);
		}

		[Test]
		public void TimeConstant_Negative_Throws ()
		{
			// [Arrange]
			DeviceConfiguration sampleDevice =
				new DeviceConfiguration (1, 1);
			sampleDevice.SetUpdateRate (50);
			ReadOnlyDeviceConfiguration sampleDeviceRO =
				new ReadOnlyDeviceConfiguration (sampleDevice);
			ScaledAverage sampleFilter =
				new ScaledAverage (sampleDeviceRO);

			// [Act/Assert]
			Assert.That (
				delegate {
					sampleFilter.TimeConstant = -1;
				},
				Throws.TypeOf<ArgumentOutOfRangeException> ()
				.With.Property ("ParamName").EqualTo ("TimeConstant")
			);
		}

		#endregion

		#region Properties - WeightMultiplier

		// Other aspects of WeightMultiplier are tested in TimeConstant section

		[Test]
		public void WeightMultiplier_ZeroUpdateRate_Test ()
		{
			// [Arrange]
			DeviceConfiguration sampleDevice =
				new DeviceConfiguration (1, 1);
			ReadOnlyDeviceConfiguration sampleDeviceRO =
				new ReadOnlyDeviceConfiguration (sampleDevice);
			ScaledAverage sampleFilter = new ScaledAverage (sampleDeviceRO);

			// [Act]
			// Ensure update rate is set to zero
			sampleDevice.SetUpdateRate (0);

			// [Assert]
			Assert.That (sampleFilter.WeightMultiplier,
				Is.EqualTo (1),
				"Weight multiplier for zero update rate does not match"
			);
		}

		#endregion

		#region Functions - Filter (double)

		[Test]
		public void Filter_ApproachesQuickEnough_Test (
			[Values (4, 51, 1000)] double customTimeConstant,
			[Values (4, 50)] double customUpdateRate,
			[Values (-100, -1, 0, 1)] double startingValue,
			[Values (-1, 0, 1, 100)] double endingValue)
		{
			// [Arrange]
			DeviceConfiguration sampleDevice =
				new DeviceConfiguration (1, 1);
			sampleDevice.SetUpdateRate (customUpdateRate);
			ReadOnlyDeviceConfiguration sampleDeviceRO =
				new ReadOnlyDeviceConfiguration (sampleDevice);
			ScaledAverage sampleFilter =
				new ScaledAverage (sampleDeviceRO, customTimeConstant);

			// NOTE: To fully test value approaching, at least some starting
			// and ending values must not be the same, and at least one time
			// constant should be 2.5x larger than the update rate.

			// Expected iterations for 63%: ratio of customTimeConstant and
			// customUpdateRate
			int iterationsPartial =
				(int)(customTimeConstant / customUpdateRate) + 1;

			// Expected iterations for goal: a "long time"
			int iterationsLongterm = (int)(1000 / sampleDevice.FactorTime);

			// Reaching 63% of the way there
			const double percentage = 0.63;
			double delta = endingValue - startingValue;
			double thresholdPartial =
				startingValue + (delta * percentage);

			double tolerance = delta * 0.1;
			// Ensure tolerance exists for same values
			double variance = (tolerance != 0 ? tolerance : 0.01);

			// Find the from/to values for 63%
			double thresholdPartialFrom = 0;
			double thresholdPartialTo = 0;
			if (startingValue == endingValue) {
				// Keeping the same value
				thresholdPartialFrom = endingValue - variance;
				thresholdPartialTo = endingValue + variance;
			} else if (startingValue < endingValue) {
				// Target: increasing past 63%
				thresholdPartialFrom = thresholdPartial;
				thresholdPartialTo = endingValue;
			} else {
				// Target: decreasing past 63%
				thresholdPartialFrom = endingValue;
				thresholdPartialTo = thresholdPartial;
			}

			// Reaching most of the way there
			double thresholdLongtermFrom = 0;
			double thresholdLongtermTo = 0;
			if (variance >= 0) {
				// Trending positive
				thresholdLongtermFrom = endingValue - variance;
				thresholdLongtermTo = endingValue + variance;
			} else {
				// Trending negative
				thresholdLongtermFrom = endingValue + variance;
				thresholdLongtermTo = endingValue - variance;
			}

			// [Act]
			// Find the 63% value
			double currentValuePartial = startingValue;
			for (int i = 0; i < iterationsPartial; i++) {
				currentValuePartial =
					sampleFilter.Filter (currentValuePartial, endingValue);
			}

			// Find the longterm value
			double currentValueLongterm = currentValuePartial;
			for (int i = 0; i < iterationsPartial; i++) {
				currentValueLongterm =
					sampleFilter.Filter (currentValueLongterm, endingValue);
			}

			// [Assert]
			Assert.That (currentValuePartial,
				Is.InRange (thresholdPartialFrom, thresholdPartialTo),
				String.Format (
					"Value does not reach 63% ({0}) after {1} iterations",
					thresholdPartial, iterationsPartial
				)
			);

			Assert.That (currentValueLongterm,
				Is.InRange (thresholdLongtermFrom,
					thresholdLongtermTo),
				String.Format (
					"Value does not match end ({0}) after {1} iterations",
					endingValue, iterationsLongterm
				)
			);
		}

		[Test]
		public void Filter_ApproachesSlowEnough_Test (
			[Values (4, 50)] double customUpdateRate,
			[Values (-100, -1, 0, 1)] double startingValue,
			[Values (-1, 0, 1, 100)] double endingValue)
		{
			// [Arrange]
			// Pick a time constant that ensures values won't change too quickly
			double customTimeConstant = customUpdateRate * 2.5;

			DeviceConfiguration sampleDevice =
				new DeviceConfiguration (1, 1);
			sampleDevice.SetUpdateRate (customUpdateRate);
			ReadOnlyDeviceConfiguration sampleDeviceRO =
				new ReadOnlyDeviceConfiguration (sampleDevice);
			ScaledAverage sampleFilter =
				new ScaledAverage (sampleDeviceRO, customTimeConstant);

			// Reaching 63% of the way there
			const double percentage = 0.63;
			double delta = endingValue - startingValue;
			double thresholdPartial =
				startingValue + (delta * percentage);

			double tolerance = delta * 0.1;
			// Ensure tolerance exists for same values
			double variance = (tolerance != 0 ? tolerance : 0.01);

			// Find the from/to values for 63%
			double thresholdFirstFrom = 0;
			double thresholdFirstTo = 0;
			if (startingValue == endingValue) {
				// Keeping the same value
				thresholdFirstFrom = endingValue - variance;
				thresholdFirstTo = endingValue + variance;
			} else if (startingValue < endingValue) {
				// First: increasing but not yet past 63%
				thresholdFirstFrom = startingValue;
				thresholdFirstTo = thresholdPartial;
			} else {
				// First: decreasing but not yet past 63%
				thresholdFirstFrom = thresholdPartial;
				thresholdFirstTo = startingValue;
			}

			// [Act]
			// Find the first value
			double firstValue =
				sampleFilter.Filter (startingValue, endingValue);

			// [Assert]
			Assert.That (firstValue,
				Is.InRange (thresholdFirstFrom, thresholdFirstTo),
				String.Format (
					"Value goes past 63% ({0}) too soon, after 1 iteration",
					thresholdPartial
				)
			);
		}

		#endregion

		#region Functions - Filter (non-double)

		[Test]
		public void Filter_TypesMatch_Test (
			[Values (0, 1, 255)] byte startingValue,
			[Values (0, 1, 255)] byte endingValue)
		{
			// [Arrange]
			const double customUpdateRate = 50;
			const double customTimeConstant = 61;
			DeviceConfiguration sampleDevice =
				new DeviceConfiguration (1, 1);
			sampleDevice.SetUpdateRate (customUpdateRate);
			ReadOnlyDeviceConfiguration sampleDeviceRO =
				new ReadOnlyDeviceConfiguration (sampleDevice);
			ScaledAverage sampleFilter =
				new ScaledAverage (sampleDeviceRO, customTimeConstant);

			const int iterations = 100;

			// [Act]
			// Ensure that functions match regardless of type
			double currentValueDouble = startingValue;
			int currentValueInt = startingValue;
			byte currentValueByte = startingValue;

			for (int i = 0; i < iterations; i++) {
				currentValueDouble =
					(int)sampleFilter.Filter (currentValueDouble, endingValue);
				currentValueInt =
					(int)sampleFilter.Filter (currentValueInt, endingValue);
				currentValueByte =
					(byte)sampleFilter.Filter (currentValueByte, endingValue);
			}

			// [Assert]
			Assert.That (currentValueInt,
				Is.EqualTo (currentValueDouble),
				"Integer value does not match double value"
			);

			Assert.That (currentValueByte,
				Is.EqualTo (currentValueDouble),
				"Byte value does not match double value"
			);
		}

		#endregion


		#region Functions - ToString

		[Test]
		public void ToString_Test ()
		{
			// [Arrange]
			DeviceConfiguration sampleDevice =
				new DeviceConfiguration (1, 1);
			sampleDevice.SetUpdateRate (50);
			ReadOnlyDeviceConfiguration sampleDeviceRO =
				new ReadOnlyDeviceConfiguration (sampleDevice);
			const double customTimeConstant = 92.857142857;
			ScaledAverage sampleFilter =
				new ScaledAverage (sampleDeviceRO, customTimeConstant);

			// Prepare the expected result of converting to string
			string expectedResult =
				string.Format (
					"[ScaledAverage: TimeConstant={0}, WeightMultiplier={1}]",
					sampleFilter.TimeConstant,
					sampleFilter.WeightMultiplier
				);

			// [Act/Assert]
			Assert.That (sampleFilter.ToString (),
				Is.EqualTo (expectedResult)
			);
		}

		#endregion

	}
}

