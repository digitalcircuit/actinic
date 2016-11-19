//
//  DummyAudioInput.cs
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

namespace Actinic.AudioInputs
{
	public class DummyAudioInput : AbstractAudioInput, IAudioInputDummy
	{

		/// <summary>
		/// Dummy volume count, should be greater than expected number of lights
		/// </summary>
		private const int Dummy_VOLUME_COUNT = 256;

		public DummyAudioInput ()
		{
		}

		public override bool Running {
			get {
				return true;
			}
		}

		public override string Identifier {
			get {
				return "/dev/null";
			}
		}

		public override int Priority {
			get {
				return int.MaxValue;
				// This should be the last output system to try; it provides the least functionality.
			}
		}

		public override bool InitializeSystem ()
		{
			return true;
		}

		public override bool ShutdownSystem ()
		{
			return StopAudioCapture ();
		}

		public override bool StartAudioCapture ()
		{
			return true;
		}

		public override bool StopAudioCapture ()
		{
			return true;
		}

		public override double[] GetSnapshot ()
		{
			double[] audioSnapshot = new double[Dummy_VOLUME_COUNT];
			return audioSnapshot;
		}

	}
}

