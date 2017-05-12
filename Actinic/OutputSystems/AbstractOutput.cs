//
//  AbstractOutput.cs
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
using System.Collections.Generic;

// Rendering
using Actinic.Rendering;

namespace Actinic.Outputs
{
	public abstract class AbstractOutput : IComparable <AbstractOutput>
	{

		/// <summary>
		/// Gets a value indicating whether the output system is connected
		/// and set up for use.
		/// </summary>
		/// <value><c>true</c> if output system is set up; otherwise, <c>false</c>.</value>
		public abstract bool Initialized {
			get;
		}

		/// <summary>
		/// Gets the identifier for which output system is connected.
		/// </summary>
		/// <value>Output system connection identifier.</value>
		public abstract string Identifier {
			get;
		}

		/// <summary>
		/// Gets the priority of this output system, with lower numbers being higher priority.
		/// </summary>
		/// <value>The priority.</value>
		public abstract int Priority {
			get;
		}

		/// <summary>
		/// Length of time for an Update command to succeed (may be an average)
		/// </summary>
		/// <value>Delay in milliseconds</value>
		public abstract float ProcessingLatency {
			get;
		}

		/// <summary>
		/// Gets the number of lights controlled by this output.
		/// </summary>
		/// <value>Number of controlled lights</value>
		public abstract int LightCount {
			get;
		}

		public delegate void SystemDataReceivedHandler (object sender,EventArgs e);

		/// <summary>
		/// Occurs when the output system provides data.
		/// </summary>
		public event SystemDataReceivedHandler SystemDataReceived;

		/// <summary>
		/// When true, system is presently resetting
		/// </summary>
		protected bool ResettingSystem;

		public AbstractOutput ()
		{
		}

		/// <summary>
		/// Initializes the output system.
		/// </summary>
		/// <returns><c>true</c>, if system was successfully initialized, <c>false</c> otherwise.</returns>
		public abstract bool InitializeSystem ();

		/// <summary>
		/// Shutdowns the output system.
		/// </summary>
		/// <returns><c>true</c>, if system was successfully shutdown, <c>false</c> otherwise.</returns>
		public abstract bool ShutdownSystem ();

		/// <summary>
		/// Shutdown then re-initialize the output system.
		/// </summary>
		/// <returns><c>true</c>, if system was successfully reset, <c>false</c> otherwise.</returns>
		public bool ResetSystem ()
		{
			bool result;
			ResettingSystem = true;
			result = (ShutdownSystem () && InitializeSystem ());
			ResettingSystem = false;
			return result;
		}

		/// <summary>
		/// Verifies the given LED light set provides the right number of lights, etc
		/// <exception cref="ArgumentException">Thrown when provided with an invalid light set</exception>
		/// </summary>
		/// <param name="Actinic_Light_Set">List of LEDs to verify</param>
		protected void ValidateLightSet (List<Color> Actinic_Light_Set)
		{
			if (Actinic_Light_Set.Count != LightCount)
				throw new ArgumentException (string.Format ("Given Actinic_Light_Set with {0} lights, needed {1}", Actinic_Light_Set.Count, LightCount));
		}

		/// <summary>
		/// Updates the brightness of the lights.
		/// </summary>
		/// <returns><c>true</c>, if brightness was updated, <c>false</c> otherwise.</returns>
		/// <param name="Actinic_Light_Set">LED list representing desired state of lights, ignoring the color component.</param>
		public abstract bool UpdateLightsBrightness (List<Color> Actinic_Light_Set);

		/// <summary>
		/// Updates the color of the lights.
		/// </summary>
		/// <returns><c>true</c>, if color was updated, <c>false</c> otherwise.</returns>
		/// <param name="Actinic_Light_Set">LED list representing desired state of lights, ignoring the brightness components.</param>
		public abstract bool UpdateLightsColor (List<Color> Actinic_Light_Set);

		/// <summary>
		/// Updates both color and brightness of the lights.
		/// </summary>
		/// <returns><c>true</c>, if color and brightness was updated, <c>false</c> otherwise.</returns>
		/// <param name="Actinic_Light_Set">LED list representing desired state of lights.</param>
		public abstract bool UpdateLightsAll (List<Color> Actinic_Light_Set);

		/// <summary>
		/// Raises the system data received event.
		/// </summary>
		/// <param name="sender">Sender.</param>
		/// <param name="e">Event arguments.</param>
		protected void OnSystemDataReceived (object sender, EventArgs e)
		{
			SystemDataReceivedHandler handler = SystemDataReceived;
			if (handler != null)
				handler (this, e);
		}

		/// <summary>
		/// Returns the sort order of this output system compared with another.
		/// </summary>
		/// <returns>The sort order, positive preceding, negative following, zero equals.</returns>
		/// <param name="obj">The AbstractOutput to compare to this instance.</param>
		public int CompareTo (AbstractOutput otherOutput)
		{
			if (otherOutput == null)
				return 1;

			return this.Priority.CompareTo (otherOutput.Priority);
		}
	}
}

