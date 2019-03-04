//
//  DummyOutput.cs
//
//  Author:
//       Shane Synan <digitalcircuit36939@gmail.com>
//
//  Copyright (c) 2015 - 2016
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

// Output systems (transitioning legacy to modern)
using Actinic.Output;

// Rendering
using Actinic.Rendering;

namespace Actinic.Outputs
{
	public class DummyOutput:AbstractOutput, IOutputDummy
	{
		public DummyOutput ()
		{
			// Set update rate to something sensible
			deviceConfig.SetUpdateRate (5);
		}

		public override bool Initialized {
			get {
				return true;
			}
		}

		public override string Identifier {
			get {
				return "/dev/null";
			}
		}

		public override string VersionIdentifier {
			get {
				return "1.0";
			}
		}

		public override int Priority {
			get {
				return int.MaxValue;
				// This should be the last output system to try; it provides the least functionality.
			}
		}

		// Set up a test strand with 50 lights over 12.4 meters
		private readonly DeviceConfiguration deviceConfig =
			new DeviceConfiguration (50, 12.4);

		private ReadOnlyDeviceConfiguration deviceConfigRO;

		public override ReadOnlyDeviceConfiguration Configuration {
			get {
				if (deviceConfigRO == null) {
					deviceConfigRO =
						new ReadOnlyDeviceConfiguration (deviceConfig);
				}
				return deviceConfigRO;
			}
		}

		public override bool InitializeSystem ()
		{
			return true;
		}

		public override bool ShutdownSystem ()
		{
			return true;
		}

		public override bool UpdateLightsBrightness (Layer Actinic_Light_Set)
		{
			ValidateLightSet (Actinic_Light_Set);
			return true;
		}

		public override bool UpdateLightsColor (Layer Actinic_Light_Set)
		{
			ValidateLightSet (Actinic_Light_Set);
			return true;
		}

		public override bool UpdateLightsAll (Layer Actinic_Light_Set)
		{
			ValidateLightSet (Actinic_Light_Set);
			return true;
		}

	}
}

