//
//  AudioBitmapAnimation.cs
//
//  Author:
//       Shane Synan <digitalcircuit36939@gmail.com>
//
//  Copyright (c) 2014 - 2016
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

//#define DEBUG_MPLAYER

using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;

// Rendering
using Actinic.Rendering;

namespace Actinic.Animations
{
	public class AudioBitmapAnimation:AbstractAnimation, IAnimationOneshot
	{
		private const string Player_Program = "mplayer";
		private const string Player_Arguments = "-slave -quiet -input nodefault-bindings -idle -ao alsa '{0}'";
		// Don't read user config: -noconfig all
		// Stay running at end of file: -idle
		// Use ALSA over PulseAudio: -ao alsa
		// (PulseAudio 1.8 seems to introduce delay with mplayer's command interface)
		private const string CMD_SeekToTime = "seek {0} 2";
		//seek 12.1 2
		private const string CMD_RequestTime = "get_property time_pos";
		private const string CMD_RequestTime_Response_Prefix = "ANS_time_pos=";
		//ANS_time_pos=64.511275
		private const string CMD_RequestTime_NotAvailableResponse_Prefix = "ANS_ERROR=";
		//ANS_ERROR=PROPERTY_UNAVAILABLE
		private const string CMD_RequestTime_ErrorResponse_Prefix = "Failed to get value of property 'time_pos'";
		//Failed to get value of property 'time_pos'.

		private Process AudioPlayer;
		private double AudioPlayer_CurrentTime = 0;
		private bool AudioPlayer_CurrentTime_Updated = false;

		/// <summary>
		/// If true, the current time has been requested, otherwise no request is pending.
		/// </summary>
		private bool AudioPlayer_CurrentTime_Pending = false;

		/// <summary>
		/// If true, the current time is unavailable (e.g. error), otherwise false.
		/// </summary>
		private bool AudioPlayer_CurrentTime_Unavailable = false;
		private List<Layer> AnimationFrames = new List<Layer> ();
		private int animation_frame = 0;

		public int AnimationFrame {
			get { return animation_frame; }
			set {
				if (value < 0) {
					animation_frame = 0;
				} else if (value >= AnimationFrames.Count) {
					animation_frame = AnimationFrames.Count - 1;
				} else {
					animation_frame = value;
				}
			}
		}

		public string ImageFilePath {
			get;
			private set;
		}

		public string AudioFilePath {
			get;
			private set;
		}

		public bool AnimationFinished {
			get {
#if DEBUG_MPLAYER
				Console.WriteLine ("AnimationFinished = {0} (frame {1} of {2}, error: {3})",
					(AnimationFrame >= (AnimationFrames.Count - 2) ||
						(AnimationFrame > 0 && AudioPlayer_CurrentTime_Unavailable)),
					AnimationFrame, AnimationFrames.Count, AudioPlayer_CurrentTime_Unavailable);
#endif
				return (AnimationFrame >= (AnimationFrames.Count - 2) ||
				(AnimationFrame > 0 && AudioPlayer_CurrentTime_Unavailable));
				// Sometimes it gets stuck on the next to last frame :/
				// Consider as finished if it's near the last frame, or past frame 1 and time is unavaible.
			}
		}

		public AudioBitmapAnimation (int Light_Count, string AudioAnimationFilePath) : base (Light_Count)
		{
			if (File.Exists (AudioAnimationFilePath) == false)
				throw new System.IO.FileNotFoundException ("AudioAnimationFilePath must point to an image.", AudioAnimationFilePath);
			ParseAnimationFile (AudioAnimationFilePath);

			System.Drawing.Bitmap bitmapImage = new System.Drawing.Bitmap (ImageFilePath);
			if (bitmapImage.Width < Light_Count)
				throw new System.IO.InvalidDataException (String.Format (
					"The animation file's provided image [ImageFilePath = '{0}'] " +
					"is not wide enough for the current number of lights.  " +
					"Expected '{1}' width, but got '{2}' width.",
					ImageFilePath, Light_Count, bitmapImage.Width)
				);
			AnimationFrames = AnimationUtilities.ConvertImageToLEDArray (Light_Count, bitmapImage);
			InitAudioSystem ();
		}

		private void ParseAnimationFile (string FilePath)
		{
			System.IO.StreamReader file_read = new StreamReader (FilePath);
			ImageFilePath = file_read.ReadLine ();
			AudioFilePath = file_read.ReadLine ();
			file_read.Close ();

			if (File.Exists (ImageFilePath) == false)
				throw new System.IO.FileNotFoundException (string.Format ("The first line in the animation file must point to a valid image.  See line #1 in '{0}', points to '{1}'", FilePath, ImageFilePath));
			if (File.Exists (AudioFilePath) == false)
				throw new System.IO.FileNotFoundException (string.Format ("The second line in the animation file must point to a valid audio file.  See line #2 in '{0}', points to '{1}'", FilePath, AudioFilePath));
		}

		private void InitAudioSystem ()
		{
			StopAudioSystem ();
			AudioPlayer = new Process ();
			AudioPlayer.StartInfo.FileName = Player_Program;
			AudioPlayer.StartInfo.Arguments = String.Format (Player_Arguments, AudioFilePath);
			AudioPlayer.StartInfo.UseShellExecute = false;
			AudioPlayer.StartInfo.CreateNoWindow = true;
			AudioPlayer.StartInfo.RedirectStandardInput = true;
			AudioPlayer.StartInfo.RedirectStandardOutput = true;
			AudioPlayer.StartInfo.RedirectStandardError = true;
			AudioPlayer.EnableRaisingEvents = true;
			// see below for output handler
			AudioPlayer.OutputDataReceived += new DataReceivedEventHandler (AudioPlayer_OutputCallback);
			AudioPlayer.ErrorDataReceived += new DataReceivedEventHandler (AudioPlayer_ErrorCallback);

			AudioPlayer.Start ();
			AudioPlayer.BeginOutputReadLine ();
		}

		public void StopAudioSystem ()
		{
			if (AudioPlayer != null && AudioPlayer.HasExited == false)
				AudioPlayer.Kill ();
		}

		private void AudioPlayer_OutputCallback (object sender, System.Diagnostics.DataReceivedEventArgs e)
		{
			if (e?.Data == null || e.Data == "")
				return;
#if DEBUG_MPLAYER
			Console.WriteLine ("MPlayer output: " + e.Data);
#endif
			if (e.Data.StartsWith (CMD_RequestTime_Response_Prefix)) {
				AudioPlayer_CurrentTime = double.Parse (e.Data.Split ('=') [1]);
				AudioPlayer_CurrentTime_Pending = false;
				AudioPlayer_CurrentTime_Updated = true;
				AudioPlayer_CurrentTime_Unavailable = false;
			} else if (e.Data.StartsWith (CMD_RequestTime_NotAvailableResponse_Prefix)) {
				AudioPlayer_CurrentTime_Pending = false;
				AudioPlayer_CurrentTime_Unavailable = true;
			}
		}

		private void AudioPlayer_ErrorCallback (object sender, System.Diagnostics.DataReceivedEventArgs e)
		{
			if (e?.Data == null || e.Data == "")
				return;
#if DEBUG_MPLAYER
			Console.WriteLine ("MPlayer error: " + e.Data);
#endif
			if (e.Data.StartsWith (CMD_RequestTime_ErrorResponse_Prefix)) {
				AudioPlayer_CurrentTime_Pending = false;
				AudioPlayer_CurrentTime_Unavailable = true;
			}
		}

		public void SeekToPosition (double Position)
		{
			if (AudioPlayer == null || AudioPlayer.HasExited == true)
				throw new InvalidOperationException ("AudioPlayer not running, can not seek to a position.");
			AudioPlayer.StandardInput.WriteLine (CMD_SeekToTime, Position);
		}

		private int getPlayingFrame ()
		{
			if (AudioPlayer == null || AudioPlayer.HasExited == true)
				throw new InvalidOperationException ("AudioPlayer not running, can not determine current track position.");

			AudioPlayer_CurrentTime_Updated = false;
			if (!AudioPlayer_CurrentTime_Pending) {
				// Don't request multiple times
				AudioPlayer_CurrentTime_Pending = true;
				AudioPlayer.StandardInput.WriteLine (CMD_RequestTime);
			}

//			while (AudioPlayer_CurrentTime_Updated == false) {
//				// Just wait for it, it should be instantaneous
//			}

			int desiredFrame = (int)((AudioPlayer_CurrentTime * 1000) / 50);
			if (desiredFrame < 0)
				desiredFrame = 0;
			if (desiredFrame >= AnimationFrames.Count)
				desiredFrame = AnimationFrames.Count - 1;

			return desiredFrame;
		}

		public override Layer GetNextFrame ()
		{
			animation_frame = getPlayingFrame ();
			if (animation_frame > -1 && animation_frame < AnimationFrames.Count) {
				int frame_index = animation_frame;
				if (AudioPlayer_CurrentTime_Updated == false
				    && (animation_frame + 1) < AnimationFrames.Count) {
					// Predictively skip one over to the next frame
					frame_index++;
				}
				// Don't allow the calling code to modify the animation
				return AnimationFrames [frame_index].Clone ();
			} else {
				throw new System.ArgumentOutOfRangeException (
					"animation_frame",
					animation_frame,
					"animation_frame must " +
					"be within range of AnimationFrames, e.g. [0, " + AnimationFrames.Count.ToString () + "]"
				);
			}
		}


	}
}

