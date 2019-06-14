//
//  ImpulseAudioInput.cs
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

//#define DEBUG_IMPULSE_PERFORMANCE

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Actinic.AudioInputs
{
	public class ImpulseAudioInput : AbstractAudioInput
	{
		#region Impulse Library Interop

		[DllImport ("impulse", EntryPoint = "im_getSnapshot")]
		private static extern IntPtr IM_GetSnapshot (int fft);

		[DllImport ("impulse", EntryPoint = "im_start")]
		private static extern void IM_Start ();

		[DllImport ("impulse", EntryPoint = "im_stop")]
		private static extern void IM_Stop ();

		private const int Impulse_VOLUME_COUNT = 256;
		private double[] AudioVolumesInstantaneous = new double[Impulse_VOLUME_COUNT];

		private const int Impulse_ENABLE_FFT = 1;
		private const int Impulse_DISABLE_FFT = 0;

		#endregion

		public override bool Running {
			get {
				return (ResettingSystem == false && SystemActive == true);
			}
		}

		public override string Identifier {
			get {
				return "Impulse PulseAudio library";
			}
		}

		public override int Priority {
			get {
				return Impulse_Priority;
			}
		}

		public ImpulseAudioInput ()
		{
		}

		public override bool InitializeSystem ()
		{
			try {
				Action prelinkInfo = IM_Start;
				Marshal.Prelink (prelinkInfo.Method);
				prelinkInfo = IM_Stop;
				Marshal.Prelink (prelinkInfo.Method);
				Func<int, IntPtr> desiredDelagate = IM_GetSnapshot;
				Marshal.Prelink (desiredDelagate.Method);
				// All methods linked, this library should be good
				return true;
			} catch (EntryPointNotFoundException) {
				// Library exists but does not have this entry point, consider this output method invalid
				return false;
			} catch (DllNotFoundException) {
				// Library does not exist, consider this output method invalid
				return false;
			}
		}

		public override bool ShutdownSystem ()
		{
			return StopAudioCapture ();
		}

		public override bool StartAudioCapture ()
		{
			if (AudioCaptureThread != null && AudioCaptureThread.IsAlive) {
				// Capture already running, no need to restart
				return false;
			}
			AudioCaptureThread = new System.Threading.Thread (RunAudioCapture);
			AudioCaptureThread.IsBackground = true;
			AudioCaptureThread.Priority = System.Threading.ThreadPriority.BelowNormal;

			IM_Start ();
			// Start audio capture before starting the thread
			ClearAllAudio ();
			AudioCaptureThread.Start ();
			SystemActive = true;
			return true;
		}

		public override bool StopAudioCapture ()
		{
			// Gracefully stop audio capture thread
			SystemActive = false;
			AudioCaptureThread.Join ();

			IM_Stop ();
			// Stop audio capture after ending the thread
			ClearAllAudio ();
			SystemActive = false;
			return true;
		}

		public override double[] GetSnapshot ()
		{
			// Take a rolling maximum that includes the current value
			double[] audioCurrent = new double[Impulse_VOLUME_COUNT];
			lock (AudioVolumesSync) {
				// Copy the current volume
				Array.Copy (AudioVolumesBuffered, audioCurrent, Impulse_VOLUME_COUNT);
				// Take the maximum values from current and each snapshot chunk
				// of the rolling window
				foreach (var rollingChunk in AudioVolumesRolling) {
					// Take individual maximum of each current and rolling
					// volume within the list
					for (int i = 0; i < audioCurrent.Length; i++) {
						audioCurrent [i] =
							Math.Max (rollingChunk [i], audioCurrent [i]);
					}
				}
			}
			// Return the final maxima
			return audioCurrent;
		}

		#region Internal

		/// <summary>
		/// Runs the audio capture.
		/// </summary>
		private void RunAudioCapture ()
		{
			#if DEBUG_IMPULSE_PERFORMANCE
			int update_count = 0;
			const int MAX_UPDATE_COUNT = 1000;
			Impulse_PerfStopwatch.Start ();
			#endif
			// Track timing for rolling snapshots, start immediately
			System.Diagnostics.Stopwatch rollingVolumesStopwatch =
				System.Diagnostics.Stopwatch.StartNew ();

			// Run as long as the system is active
			while (AudioCaptureThread.IsAlive && SystemActive) {
				#if DEBUG_IMPULSE_PERFORMANCE
				if (update_count > MAX_UPDATE_COUNT) {
					update_count = 0;
					Console.WriteLine (
						"# Impulse performance (average {0} runs): {1,6:F3} ms",
						MAX_UPDATE_COUNT,
						Impulse_PerfStopwatch.Elapsed.TotalMilliseconds / (float)MAX_UPDATE_COUNT
					);
					Impulse_PerfStopwatch.Restart ();
				} else {
					++update_count;
				}
				#endif
				IntPtr result = IM_GetSnapshot (Impulse_ENABLE_FFT);
				System.Runtime.InteropServices.Marshal.Copy (result, AudioVolumesInstantaneous, 0, AudioVolumesInstantaneous.Length);
				lock (AudioVolumesSync) {
					// Take the maximum of the input and buffered volumes
					for (int i = 0; i < AudioVolumesInstantaneous.Length; i++) {
						AudioVolumesBuffered [i] = Math.Max (AudioVolumesBuffered [i], AudioVolumesInstantaneous [i]);
					}

					if (rollingVolumesStopwatch.ElapsedMilliseconds >= AudioRollingSampleInterval) {
						// Move the buffered volumes into a rolling snapshot
						AppendAudioRolling (AudioVolumesBuffered);
						ClearAudioBuffer ();
						// Reset timer
						rollingVolumesStopwatch.Restart ();
					}
				}
			}

			// Stop tracking timing
			rollingVolumesStopwatch.Stop ();

			#if DEBUG_IMPULSE_PERFORMANCE
			Impulse_PerfStopwatch.Stop ();
			#endif
		}

		/// <summary>
		/// Appends the audio snapshot to the rolling audio volumes list.
		/// <remarks>
		/// This does NOT lock the audio volume list, and is only suitable for
		/// internal use with a function that locks.
		/// </remarks>
		/// </summary>
		/// <param name="AudioVolumes">Current audio volumes.</param>
		private void AppendAudioRolling (double[] AudioVolumes)
		{
			if (AudioVolumes.Length != Impulse_VOLUME_COUNT) {
				throw new ArgumentOutOfRangeException ("AudioVolumes",
					string.Format (
						"AudioVolumes.Length does not match {0}",
						Impulse_VOLUME_COUNT
					)
				);
			}

			// Make a copy (to avoid clearing later)
			double[] audioSnapshot = new double[AudioVolumes.Length];
			Array.Copy (AudioVolumes, audioSnapshot, AudioVolumes.Length);

			// Add to the beginning as a copy
			AudioVolumesRolling.AddFirst (audioSnapshot);

			// Prune snapshots if needed
			while (AudioVolumesRolling.Count > AudioVolumesRollingSize) {
				AudioVolumesRolling.RemoveLast ();
			}
		}

		/// <summary>
		/// Clears the audio buffer.
		/// <remarks>
		/// This does NOT lock the audio volume list, and is only suitable for
		/// internal use with a function that locks.
		/// </remarks>
		/// </summary>
		private void ClearAudioBuffer ()
		{
			Array.Clear (AudioVolumesBuffered, 0, AudioVolumesBuffered.Length);
		}

		/// <summary>
		/// Clears all audio tracking and buffers.
		/// <remarks>
		/// This does NOT lock the audio volume list, and is only suitable for
		/// internal use with a function that locks.
		/// </remarks>
		/// </summary>
		private void ClearAllAudio ()
		{
			ClearAudioBuffer ();
			AudioVolumesRolling.Clear ();
		}

		/// <summary>
		/// Priority of this system to other output systems, lower numbers result in higher priority.
		/// </summary>
		private const int Impulse_Priority = 36943;
		// Feel free to change this as desired

		/// <summary>
		/// Set to true if the audio capture system is active, otherwise false.
		/// </summary>
		private bool SystemActive = false;

		/// <summary>
		/// The Impulse audio input capturing thread.
		/// </summary>
		private System.Threading.Thread AudioCaptureThread;

		/// <summary>
		/// Synchronization object for access to audio volumes.
		/// </summary>
		private object AudioVolumesSync = new object ();

		/// <summary>
		/// Current array of audio volumes.
		/// </summary>
		private double[] AudioVolumesBuffered = new double[Impulse_VOLUME_COUNT];

		/// <summary>
		/// List of audio volumes in a rolling list, with newest first.
		/// </summary>
		private LinkedList<double[]> AudioVolumesRolling =
			new LinkedList<double[]> ();

		/// <summary>
		/// The interval in milliseconds of how often to snapshot a rolling
		/// value of the audio volumes.
		/// </summary>
		private const long AudioRollingSampleInterval = 10;

		/// <summary>
		/// The duration of the window for tracking rolling audio volumes.
		/// Any input volumes older than this will no longer have any effect.
		/// <remarks>
		/// This should be large enough that slower lighting systems have time
		/// to react to all peak values, e.g. greater than average latency.
		/// Note that adjusting this affects the apparent intensity of input
		/// audio.  Changing this may require changes to the reactive animations
		/// to keep the same look and feel.
		/// </remarks>
		/// </summary>
		private const long AudioRollingWindowDuration = 50;

		/// <summary>
		/// The desired size of the rolling audio volume list.
		/// </summary>
		private const long AudioVolumesRollingSize =
			AudioRollingWindowDuration / AudioRollingSampleInterval;

		#if DEBUG_IMPULSE_PERFORMANCE
		private System.Diagnostics.Stopwatch Impulse_PerfStopwatch = new System.Diagnostics.Stopwatch ();
		#endif

		#endregion
	}
}

