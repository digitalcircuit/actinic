//
//  LED_Queue.cs
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
	public class LED_Queue
	{
		/// <summary>
		/// Modifiable list of LEDs representing the desired output state
		/// </summary>
		public List<LED> Lights = new List<LED> ();

		/// <summary>
		/// Gets a list of LEDs representing the last state processed by the output system, useful for fades
		/// </summary>
		/// <value>Read-only list of LEDs</value>
		public List<LED> LightsLastProcessed {
			get;
			private set;
		}

		/// <summary>
		/// Gets the number of lights
		/// </summary>
		/// <value>Number of lights</value>
		public int LightCount {
			get { return Lights.Count; }
		}

		/// <summary>
		/// Gets a value indicating whether the selected animation is active.
		/// </summary>
		/// <value><c>true</c> if an animation is active; otherwise, <c>false</c>.</value>
		public bool AnimationActive {
			get { return (SelectedAnimation != null); }
		}

		/// <summary>
		/// If <c>true</c>, force an update for the next frame request in the output system loop
		/// </summary>
		public bool AnimationForceFrameRequest = false;

		/// <summary>
		/// The currently selected animation.
		/// </summary>
		public AbstractAnimation SelectedAnimation = null;

		/// <summary>
		/// How long the output queue has been idle.
		/// </summary>
		public int QueueIdleTime = 0;

		/// <summary>
		/// Gets a value indicating whether the output queue is empty.
		/// </summary>
		/// <value><c>true</c> if queue is empty; otherwise, <c>false</c>.</value>
		public bool QueueEmpty {
			get { return (OutputQueue.Count <= 0); }
		}

		/// <summary>
		/// Gets the number of frames currently in the output queue.
		/// </summary>
		/// <value>Number representing frames waiting in output queue.</value>
		public int QueueCount {
			get { return OutputQueue.Count; }
		}

		/// <summary>
		/// Gets a value indicating whether this <see cref="G35_USB.LED_Queue"/> has any effect on ouput, i.e. LEDs
		/// are not all black with no brightness.
		/// </summary>
		/// <value><c>true</c> if lights have no effect; otherwise, <c>false</c>.</value>
		public bool LightsHaveNoEffect {
			get {
				if (AnimationActive == true || QueueEmpty == false)
					return false;
				foreach (LED light in Lights) {
					if (light.HasNoEffect == false)
						return false;
				}
				return true;
			}
		}

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

		private Queue<LED_Set> OutputQueue = new Queue<LED_Set> ();

		public LED_Queue (int LED_Light_Count)
		{
			InitializeFromBlanks (LED_Light_Count, false);
		}

		public LED_Queue (int LED_Light_Count, bool ClearAllLEDs)
		{
			InitializeFromBlanks (LED_Light_Count, ClearAllLEDs);
		}

		private void InitializeFromBlanks (int LED_Light_Count, bool ClearAllLEDs)
		{
			LightsLastProcessed = new List<LED> ();

			byte brightness = (ClearAllLEDs ? LightSystem.Brightness_MIN : LightSystem.Brightness_MAX);
			for (int i = 0; i < LED_Light_Count; i++) {
				Lights.Add (new LED (0, 0, 0, brightness));
				LightsLastProcessed.Add (new LED (0, 0, 0, brightness));
			}
		}

		public LED_Queue (List<LED> PreviouslyShownFrame)
		{
			LightsLastProcessed = new List<LED> ();

			Lights.AddRange (PreviouslyShownFrame);
			LightsLastProcessed.AddRange (PreviouslyShownFrame);
		}


		/// <summary>
		/// Marks the current queue as processed, copying it to LightsLastProcessed
		/// </summary>
		public void MarkAsProcessed ()
		{
			lock (Lights) {
				lock (LightsLastProcessed) {
					for (int index = 0; index < Lights.Count; index++) {
						LightsLastProcessed [index].SetColor (Lights [index].GetColor ());
					}
				}
			}
		}

		/// <summary>
		/// Grabs the first frame from the queue if entries are queued, otherwise returns null
		/// </summary>
		/// <returns>If multiple frames are queued, returns an LED_Set, otherwise null</returns>
		public LED_Set PopFromQueue ()
		{
			lock (OutputQueue) {
				if (OutputQueue.Count > 0) {
					LED_Set result = OutputQueue.Dequeue ();
					result.BlendMode = BlendMode;
					return result;
				} else {
					return null;
				}
			}
		}

		/// <summary>
		/// Adds the current state of the Lights frame to the end of the output queue
		/// </summary>
		public void PushToQueue ()
		{
			PushToQueue (false);
		}

		/// <summary>
		/// Adds a frame to the end of the output queue
		/// </summary>
		/// <param name="NextFrame">An LED_Set representing the desired frame.</param>
		public void PushToQueue (LED_Set NextFrame)
		{
			if (NextFrame.LightCount != LightCount)
				throw new ArgumentOutOfRangeException (string.Format ("NextFrame must contain same number of LEDs (has {0}, expected {1})", NextFrame.LightCount, LightCount));
			lock (OutputQueue) {
				OutputQueue.Enqueue (NextFrame.Clone ());
			}
		}

		/// <summary>
		/// Adds a list of LEDs representing a frame to the end of the output queue
		/// </summary>
		/// <param name="NextFrame">A list of LEDs representing the desired frame.</param>
		public void PushToQueue (List<LED> NextFrame)
		{
			if (NextFrame.Count != LightCount)
				throw new ArgumentOutOfRangeException (string.Format ("NextFrame must contain same number of LEDs (has {0}, expected {1})", NextFrame.Count, LightCount));
			lock (OutputQueue) {
				OutputQueue.Enqueue (new LED_Set (NextFrame).Clone ());
			}
		}

		/// <summary>
		/// Adds the current state of the Lights frame to the end of the output queue
		/// </summary>
		/// <param name="UseLastFrame">If set to <c>true</c> use the last entry in the queue instead of the current state (useful if an animation is running).</param>
		public void PushToQueue (bool UseLastFrame)
		{
			if (QueueEmpty || UseLastFrame == false) {
				PushToQueue (Lights);
			} else if (UseLastFrame) {
				PushToQueue (OutputQueue.ToArray ()[OutputQueue.Count - 1]);
			}
		}

		/// <summary>
		/// Clears the output queue
		/// </summary>
		public void ClearQueue ()
		{
			lock (OutputQueue) {
				OutputQueue.Clear ();
			}
		}
	}
}

