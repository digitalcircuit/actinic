//
//  LED_Set.cs
//
//  Author:
//       Shane Synan <digitalcircuit36939@gmail.com>
//
//  Copyright (c) 2013 - 2016
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
using Actinic;

namespace Actinic
{
	public class LED_Set
	{
		public List<LED> LED_Values = new List<LED> ();

		private LED.BlendingStyle blending_mode = LED.BlendingStyle.Combine;
		/// <summary>
		/// When merged down, this defines how the layer should be handled, default of Combine.
		/// </summary>
		/// <value>The blending mode.</value>
		public LED.BlendingStyle BlendMode {
			get {
				return blending_mode;
			}
			set {
				blending_mode = value;
			}
		}

		/// <summary>
		/// Gets a value indicating whether this <see cref="Actinic.LED_Queue"/> has any effect on ouput, i.e. LEDs
		/// are not all black with no brightness.
		/// </summary>
		/// <value><c>true</c> if lights have no effect; otherwise, <c>false</c>.</value>
		public bool LightsHaveNoEffect {
			get {
				foreach (LED light in LED_Values) {
					if (light.HasNoEffect == false)
						return false;
				}
				return true;
			}
		}

		public int LightCount {
			get { return LED_Values.Count; }
		}

		public LED_Set ()
		{
		}

		public LED_Set (int NumberOfLights)
		{
			for (int i = 0; i < NumberOfLights; i++) {
				LED_Values.Add (new LED ());
			}
		}

		public LED_Set (List<LED> Actinic_Light_Set)
		{
			LED_Values.AddRange (Actinic_Light_Set);
		}

		public LED_Set Clone ()
		{
			LED_Set cloned_set = new LED_Set (0);
			cloned_set.BlendMode = BlendMode;
			foreach (LED LED_to_clone in LED_Values) {
				cloned_set.LED_Values.Add (LED_to_clone.Clone ());
			}
			return cloned_set;
		}
	}
}

