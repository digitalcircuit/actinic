//
//  DummyOutput.cs
//
//  Author:
//       Shane Synan <digitalcircuit36939@gmail.com>
//
//  Copyright (c) 2015 
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

namespace G35_USB
{
	public class DummyOutput:AbstractOutput
	{
		public DummyOutput ()
		{
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

		public override int ProcessingLatency {
			get {
				return 0;
				// Not much latency in returning a boolean value...
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


		public override bool UpdateLightsBrightness (System.Collections.Generic.List<LED> G35_Light_Set)
		{
			return true;
		}

		public override bool UpdateLightsColor (System.Collections.Generic.List<LED> G35_Light_Set)
		{
			return true;
		}

		public override bool UpdateLightsAll (System.Collections.Generic.List<LED> G35_Light_Set)
		{
			return true;
		}

	}
}

