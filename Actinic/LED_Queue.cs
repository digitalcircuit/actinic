//
//  LED_Queue.cs
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

// Animation management
using Actinic.Animations;

// Rendering
using Actinic.Rendering;

namespace Actinic
{
	public class LED_Queue
	{
		/// <summary>
		/// Modifiable list of LEDs representing the desired output state
		/// </summary>
		public Layer Lights;

		/// <summary>
		/// Gets a list of LEDs representing the last state processed by the output system, useful for fades
		/// </summary>
		/// <value>Read-only list of LEDs</value>
		public Layer LightsLastProcessed {
			get;
			private set;
		}

		/// <summary>
		/// Gets the number of lights
		/// </summary>
		/// <value>Number of lights</value>
		public int LightCount {
			get { return Lights.PixelCount; }
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
		/// Gets a value indicating whether this <see cref="Actinic.LED_Queue"/> has any effect on ouput, i.e. LEDs
		/// are not all black with no brightness.
		/// </summary>
		/// <value><c>true</c> if lights have no effect; otherwise, <c>false</c>.</value>
		public bool LightsHaveNoEffect {
			get {
				if (AnimationActive == true || QueueEmpty == false)
					return false;
				// Check if the lights -don't- have an effect.
				return !Lights.HasEffect;
			}
		}

		// FIXME: Revisit queue-wide blend-mode after LED Queue update
		private Color.BlendMode blending_mode = Color.BlendMode.Combine;

		/// <summary>
		/// When merged down, this defines how the layer should be handled, default of Combine.
		/// </summary>
		/// <value>The blending mode.</value>
		public Color.BlendMode BlendMode {
			get {
				return blending_mode;
			}
			set {
				blending_mode = value;
			}
		}

		private Queue<Layer> OutputQueue = new Queue<Layer> ();

		public LED_Queue (int LED_Light_Count)
		{
			InitializeFromBlanks (LED_Light_Count, false);
		}

		public LED_Queue (int LED_Light_Count, bool ClearAllLEDs)
		{
			InitializeFromBlanks (LED_Light_Count, ClearAllLEDs);
		}

		private void InitializeFromBlanks (
			int LED_Light_Count, bool ClearAllLEDs)
		{
			byte brightness = (ClearAllLEDs ? (byte)0 : Color.MAX);
			Color fillColor = new Color (0, 0, 0, brightness);
			// Fill the layer with the given color
			Lights =
				new Layer (LED_Light_Count, Color.BlendMode.Combine, fillColor);

			// Copy to the processed list.  When first initializing, skip
			// locking.
			LightsLastProcessed = Lights.Clone ();
		}

		public LED_Queue (Layer PreviouslyShownFrame)
		{
			Lights = PreviouslyShownFrame.Clone ();
			// Copy to the processed list.  When first initializing, skip
			// locking.
			LightsLastProcessed = Lights.Clone ();
		}


		/// <summary>
		/// Marks the current queue as processed, copying it to LightsLastProcessed
		/// </summary>
		public void MarkAsProcessed ()
		{
			lock (Lights) {
				lock (LightsLastProcessed) {
					// Clone the layer over to avoid any reference links
					LightsLastProcessed = Lights.Clone ();
				}
			}
		}

		/// <summary>
		/// Grabs the first frame from the queue if entries are queued, otherwise returns null
		/// </summary>
		/// <returns>If multiple frames are queued, returns a Layer, otherwise null</returns>
		public Layer PopFromQueue ()
		{
			lock (OutputQueue) {
				if (OutputQueue.Count > 0) {
					Layer result = OutputQueue.Dequeue ();
					// Update the layer blending mode to the queue default
					// FIXME: Revisit blend-mode coercion after LED Queue update
					result.Blending = BlendMode;
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
		/// <param name="NextFrame">A Layer representing the desired frame.</param>
		public void PushToQueue (Layer NextFrame)
		{
			if (NextFrame.PixelCount != LightCount)
				throw new ArgumentOutOfRangeException ("NextFrame",
					string.Format (
						"NextFrame must contain same number of LEDs (has {0}," +
						" expected {1})", NextFrame.PixelCount, LightCount
					)
				);
			lock (OutputQueue) {
				OutputQueue.Enqueue (NextFrame.Clone ());
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
				PushToQueue (OutputQueue.ToArray () [OutputQueue.Count - 1]);
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

