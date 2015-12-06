//
//  ImpulseAudioInput.cs
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

//#define DEBUG_IMPULSE_PERFORMANCE

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace G35_USB
{
	public class ImpulseAudioInput : AbstractAudioInput
	{
		#region Impulse Library Interop
		[DllImport ("impulse", EntryPoint="im_getSnapshot")]
		private static extern IntPtr IM_GetSnapshot (int fft);

		[DllImport ("impulse", EntryPoint="im_start")]
		private static extern void IM_Start ();

		[DllImport ("impulse", EntryPoint="im_stop")]
		private static extern void IM_Stop ();

		private const int Impulse_VOLUME_COUNT = 256;
		private double[] AudioVolumesInstantaneous = new double[Impulse_VOLUME_COUNT];

		private const int Impulse_ENABLE_FFT = 1;
		private const int Impulse_DISABLE_FFT = 0;
		#endregion

		/// <summary>
		/// Priority of this system to other output systems, lower numbers result in higher priority.
		/// </summary>
		private const int Impulse_Priority = 36943;
		// Feel free to change this as desired

		private bool SystemActive = false;

		private System.Threading.Thread AudioCaptureThread;
		private double[] AudioVolumesBuffered = new double[Impulse_VOLUME_COUNT];

#if DEBUG_IMPULSE_PERFORMANCE
		private System.Diagnostics.Stopwatch Impulse_PerfStopwatch = new System.Diagnostics.Stopwatch ();
#endif

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
			ClearAudioBuffer ();
			AudioCaptureThread.Start ();
			SystemActive = true;
			return true;
		}

		/// <summary>
		/// Runs the audio capture.
		/// </summary>
		private void RunAudioCapture () {
#if DEBUG_IMPULSE_PERFORMANCE
			int update_count = 0;
			const int MAX_UPDATE_COUNT = 1000;
#endif
			while (AudioCaptureThread.IsAlive) {
#if DEBUG_IMPULSE_PERFORMANCE
				if (update_count > MAX_UPDATE_COUNT) {
					update_count = 0;
					Console.WriteLine ("# Impulse performance (average {0} runs): {1} ms", MAX_UPDATE_COUNT,
					                   Impulse_PerfStopwatch.ElapsedMilliseconds / (float)MAX_UPDATE_COUNT);
					Impulse_PerfStopwatch.Restart ();
				} else {
					++update_count;
				}
#endif
				IntPtr result = IM_GetSnapshot (Impulse_ENABLE_FFT);
				System.Runtime.InteropServices.Marshal.Copy (result, AudioVolumesInstantaneous, 0, AudioVolumesInstantaneous.Length);
				lock (AudioVolumesBuffered) {
					for (int i = 0; i < AudioVolumesInstantaneous.Length; i++) {
						AudioVolumesBuffered [i] = Math.Max (AudioVolumesBuffered [i], AudioVolumesInstantaneous [i]);
					}
				}
			}
		}

		/// <summary>
		/// Clears the audio buffer.
		/// </summary>
		/// <remarks>Does NOT acquire a lock; you might need to lock AudioVolumesBuffered before calling this.</remarks>
		private void ClearAudioBuffer ()
		{
			for (int i = 0; i < AudioVolumesBuffered.Length; i++) {
				AudioVolumesBuffered [i] = 0;
			}
		}

		public override bool StopAudioCapture ()
		{
			if (AudioCaptureThread != null)
				AudioCaptureThread.Abort ();

			IM_Stop ();
			// Stop audio capture after ending the thread
			ClearAudioBuffer ();
			SystemActive = false;
			return true;
		}

		public override double[] GetSnapshot ()
		{
			double[] audioSnapshot = new double[Impulse_VOLUME_COUNT];
			lock (AudioVolumesBuffered) {
				Array.Copy (AudioVolumesBuffered, audioSnapshot, Impulse_VOLUME_COUNT);
				ClearAudioBuffer ();
			}
			return audioSnapshot;
		}
	
	}
}

