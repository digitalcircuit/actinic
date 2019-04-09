//
//  ReadOnlyDeviceConfigurationTests.cs
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
	public class ReadOnlyDeviceConfigurationTests
	{

		#region Construction

		[Test]
		public void Construct_Passthrough ()
		{
			// [Arrange]
			DeviceConfiguration sampleDevice =
				new DeviceConfiguration (25, 5.5);
			sampleDevice.SetUpdateRate (50, 25);

			// [Act]
			ReadOnlyDeviceConfiguration sampleReadOnlyDevice =
				new ReadOnlyDeviceConfiguration (sampleDevice);

			// [Assert]
			Assert.That (sampleReadOnlyDevice.LightCount,
				Is.EqualTo (sampleDevice.LightCount)
			);
			Assert.That (sampleReadOnlyDevice.StrandLength,
				Is.EqualTo (sampleDevice.StrandLength)
			);
			Assert.That (sampleReadOnlyDevice.AverageLatency,
				Is.EqualTo (sampleDevice.AverageLatency)
			);
			Assert.That (sampleReadOnlyDevice.AverageDeviceLatency,
				Is.EqualTo (sampleDevice.AverageDeviceLatency)
			);
			Assert.That (sampleReadOnlyDevice.AverageRenderLatency,
				Is.EqualTo (sampleDevice.AverageRenderLatency)
			);
			Assert.That (sampleReadOnlyDevice.FactorFixedSize,
				Is.EqualTo (sampleDevice.FactorFixedSize)
			);
			Assert.That (sampleReadOnlyDevice.FactorScaledSize,
				Is.EqualTo (sampleDevice.FactorScaledSize)
			);
			Assert.That (sampleReadOnlyDevice.FactorTime,
				Is.EqualTo (sampleDevice.FactorTime)
			);
		}

		[Test]
		public void Construct_Null_Throws ()
		{
			// [Arrange]
			const DeviceConfiguration invalidDevice = null;

			// [Act/Assert]
			Assert.That (
				delegate {
					new ReadOnlyDeviceConfiguration (invalidDevice);
				},
				Throws.TypeOf<ArgumentNullException> ()
				.With.Property ("ParamName").EqualTo ("DeviceConfig")
			);
		}

		#endregion

		#region Functions - ClipIndex

		[Test]
		public void ClipIndex_Passthrough_Test (
			[Values (-1, 0, 24, 25, 26)] int customIndex)
		{
			// [Arrange]
			const int customLightCount = 25;
			DeviceConfiguration sampleDevice =
				new DeviceConfiguration (customLightCount, 1);

			// [Act]
			ReadOnlyDeviceConfiguration sampleReadOnlyDevice =
				new ReadOnlyDeviceConfiguration (sampleDevice);

			// [Act/Assert]
			Assert.That (sampleReadOnlyDevice.ClipIndex (customIndex),
				Is.EqualTo (sampleDevice.ClipIndex (customIndex))
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

			ReadOnlyDeviceConfiguration sampleReadOnlyDevice =
				new ReadOnlyDeviceConfiguration (sampleDevice);

			// Prepare the expected result of converting to string
			string expectedResult =
				string.Format (
					"[ReadOnlyDeviceConfiguration: LightCount={0}, " +
					"StrandLength={1}, LightsPerMeter={2}, AverageLatency={3}" +
					"AverageDeviceLatency={4}, AverageRenderLatency={5}, " +
					"FactorTime={6}, FactorFixedSize={7}, " +
					"FactorScaledSize={8}]",
					sampleReadOnlyDevice.LightCount,
					sampleReadOnlyDevice.StrandLength,
					sampleReadOnlyDevice.LightsPerMeter,
					sampleReadOnlyDevice.AverageLatency,
					sampleReadOnlyDevice.AverageDeviceLatency,
					sampleReadOnlyDevice.AverageRenderLatency,
					sampleReadOnlyDevice.FactorTime,
					sampleReadOnlyDevice.FactorFixedSize,
					sampleReadOnlyDevice.FactorScaledSize);

			// [Act/Assert]
			Assert.That (sampleReadOnlyDevice.ToString (),
				Is.EqualTo (expectedResult)
			);
		}

		#endregion
	}
}

