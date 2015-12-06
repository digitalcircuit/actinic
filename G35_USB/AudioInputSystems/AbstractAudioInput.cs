//
//  AbstractAudioInput.cs
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
using System.Collections.Generic;

namespace G35_USB
{
	public abstract class AbstractAudioInput : IComparable <AbstractAudioInput>
	{
		/// <summary>
		/// Gets a value indicating whether the audio input system is capturing audio
		/// and set up for use.
		/// </summary>
		/// <value><c>true</c> if audio input system is capturing audio; otherwise, <c>false</c>.</value>
		public abstract bool Running {
			get;
		}

		/// <summary>
		/// Gets the identifier for which audio input system is selected.
		/// </summary>
		/// <value>Output system connection identifier.</value>
		public abstract string Identifier {
			get;
		}

		/// <summary>
		/// Gets the priority of this audio input system, with lower numbers being higher priority.
		/// </summary>
		/// <value>The priority.</value>
		public abstract int Priority {
			get;
		}

		/// <summary>
		/// When true, system is presently resetting
		/// </summary>
		protected bool ResettingSystem;

		public AbstractAudioInput ()
		{
		}

		/// <summary>
		/// Initializes the audio input system.
		/// </summary>
		/// <returns><c>true</c>, if system was successfully initialized, <c>false</c> otherwise.</returns>
		public abstract bool InitializeSystem ();

		/// <summary>
		/// Shutdowns the audio input system.
		/// </summary>
		/// <returns><c>true</c>, if system was successfully shutdown, <c>false</c> otherwise.</returns>
		public abstract bool ShutdownSystem ();

		/// <summary>
		/// Starts capturing audio.
		/// </summary>
		/// <returns><c>true</c>, if system was successfully started, <c>false</c> otherwise.</returns>
		public abstract bool StartAudioCapture ();

		/// <summary>
		/// Stops capturing audio.
		/// </summary>
		/// <returns><c>true</c>, if system was successfully stopped, <c>false</c> otherwise.</returns>
		public abstract bool StopAudioCapture ();

		/// <summary>
		/// Gets a snapshot of processed audio, clearing the buffer
		/// </summary>
		/// <returns>A list of doubles representing a snapshot of current audio volumes.</returns>
		public abstract double[] GetSnapshot ();

		/// <summary>
		/// Returns the sort order of this audio input system compared with another.
		/// </summary>
		/// <returns>The sort order, positive preceding, negative following, zero equals.</returns>
		/// <param name="obj">The AbstractAudioInput to compare to this instance.</param>
		public int CompareTo (AbstractAudioInput otherAudioInput) {
			if (otherAudioInput == null) return 1;

			return this.Priority.CompareTo(otherAudioInput.Priority);
		}
	}
}

