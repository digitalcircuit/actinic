//
//  Main.cs
//
//  Author:
//       Shane Synan <digitalcircuit36939@gmail.com>
//
//  Copyright (c) 2012 - 2015
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

//#define DEBUG_PERFORMANCE
//#define DEBUG_BRIEF_PERFORMANCE
//#define DEBUG_VU_PERFORMANCE
//#define DEBUG_OVERLAY_MANAGEMENT
using System;
using System.IO.Ports;
using System.Collections.Generic;
using FoxSoft.Math;

namespace G35_USB
{
	class MainClass
	{

		private static AbstractOutput ActiveOutputSystem;

		private const int Light_Animation_Target_Latency = 48; // ms
		// Above should at least be slightly longer than the USB processing time, to reduce the risk of the queue getting filled
		// EDIT: With the new layered processing system, 48 ms may be too fast of a target.  Trying 49 ms...
		private static int Light_Animation_Latency = 0; // ms

		private static int G35_Light_Queue_CurrentIdleWait = 1; // ms
		// Current delay when idle
		private const int G35_Light_Queue_IdleWaitMultiplier = 2; //
		// Amount to increase idle delay when no events received
		private const int G35_Light_Queue_MaxIdleWaitTime = 128; // ms
		// To keep reaction times low, this is the maximum amount of time the light queue will sleep when no events are received

#if DEBUG_VU_PERFORMANCE
		private static System.Diagnostics.Stopwatch VU_Processing_PerfStopwatch = new System.Diagnostics.Stopwatch ();
#endif
		private static LED_Queue G35_Lights_Queue = new LED_Queue (LightSystem.LIGHT_COUNT);
		private const string G35_Lights_Queue_Name = "base_layer";

		private static Dictionary<string, LED_Queue> G35_Lights_Overlay_Queues = new Dictionary<string, LED_Queue>();

		private const int G35_Lights_Maximum_Overlays = 5;
		// To avoid performance issues, prevent more than this number of queues existing

		private static System.Threading.Thread G35_Light_Queue_Thread;

		private static List<double> Audio_Volumes = new List<double> ();
		private static System.Diagnostics.Process Audio_Capture_Process;

		private const int Animation_Smoothing_Iterations_DEFAULT = 15;
		private const double Animation_Smoothing_Percentage_DEFAULT = 0.7;
		private static bool Animation_Fading_Enabled = true;


		#region Menu Handling

		private static int G35_Light_Queue_BufferFullWarning {
			get {
				return (1 * 1000) / Light_Animation_Latency;  // First # is in seconds
				// Output a warning if number of frames reaches this amount
				//  Reaching this makes future commands seem slower to respond
			}
		}
		private static bool G35_Light_Queue_BufferFullWarning_Shown = false;
		// And don't spam the console about it; only notify when reached, then when fixed

		/// <summary>
		/// True if entered commands are expected to conflict with ongoing animations ('&&' concatenated or prefaced with '!'), otherwise false
		/// </summary>
		private static bool Command_ConflictsExpected = false;

		private static AbstractAnimation.Style Animation_AnimationStyle = AbstractAnimation.Style.Moderate;
		private const string Main_Command_Help = "color, brightness, white, black, identify, anim, overlay, shift_outwards, vu, clear, queue, reset, quit\n(tip: use '&&' to combine commands)";
		private const string Anim_Command_Help = "anim [play, seek, stop, fade, style]";
		private const string Overlay_Command_Help = "overlay [name_of_layer 'command', OR list, clear_all]";
		private const string Overlay_Layer_Command_Help = "overlay name_of_layer [color, brightness, identify, play, exists, blending, clear]";
		private const string VU_Command_Help = "vu [run, legacy_mode, OR display, hide, set_low, set_mid, set_high]";
		private const string VU_Run_Command_Help = "vu run [beat_pulse, rave_mood, spinner]";
		private const string VU_Legacy_Mode_Command_Help = "vu legacy_mode [auto_fast_beat, rainbow, rainbow_solid, rainbow_beat, rainbow_beat_bass, hueshift, hueshift_beat, moving_bars, moving_bars_spaced, stationary_bars, solid_rainbow_strobe, solid_white_strobe, solid_hueshift_strobe, single_rainbow_strobe, single_white_strobe]";
		private const string Queue_Command_Help = "queue [start, stop, clear, spam, test]";

		private static bool SkipInput = false;

		public static void Main (string[] args)
		{
#if DEBUG_PERFORMANCE
			Console.WriteLine ("DEBUGGING:  Compiled with 'DEBUG_PERFORMANCE' enabled");
#endif
#if DEBUG_BRIEF_PERFORMANCE
			Console.WriteLine ("DEBUGGING:  Compiled with 'DEBUG_BRIEF_PERFORMANCE' enabled");
#endif
#if DEBUG_VU_PERFORMANCE
			Console.WriteLine ("DEBUGGING:  Compiled with 'DEBUG_VU_PERFORMANCE' enabled");
#endif
#if DEBUG_OVERLAY_MANAGEMENT
			Console.WriteLine ("DEBUGGING:  Compiled with 'DEBUG_OVERLAY_MANAGEMENT' enabled");
#endif

			int retriesSinceLastSuccess = 0;
			while (true)
			{
				if (retriesSinceLastSuccess >= 5)
				{
					Console.WriteLine ("Could not reconnect after 5 tries, giving up.");
					return;
				}
				try {
					if (InitializeSystem () == false)
						return;
					retriesSinceLastSuccess = 0;
					RunMenu ();
					ShutdownSystem ();
					break;
				} catch (System.IO.IOException ex) {
					Console.WriteLine (DateTime.Now);
					Console.WriteLine ("\n ! Unexpected connection loss, attempting to reconnect...\n\n{0}\n", ex);
					retriesSinceLastSuccess ++;
					Console.Write ("Waiting 5 seconds...");
					System.Threading.Thread.Sleep (5000);
					Console.WriteLine ("  Retrying.");
					// Try again by nature of not calling break
				}
			}
		}

		/// <summary>
		/// Initializes the lighting manager and output system.
		/// </summary>
		/// <returns><c>true</c>, if system was initialized, <c>false</c> otherwise.</returns>
		/// <param name="RunWithoutInteraction">If set to <c>true</c> run without interaction.</param>
		/// <param name="SkipPreparingSystem">If set to <c>true</c> skip preparing the output system (clearing and starting queue, etc).</param>
		private static bool InitializeSystem (bool RunWithoutInteraction = false, bool SkipPreparingSystem = false)
		{
			Console.WriteLine ("Connecting to output system...");

			bool retryAgain = true;
			while (retryAgain) {
				if (InitializeOutputSystem (new ArduinoOutput ()))
					break;
				if (RunWithoutInteraction == false) {
					Console.WriteLine ("Could not open connection to output system, is device plugged in?\n" +
						"(press 's' for simulation mode, 'r' to retry, any other key to exit)");
					switch (Console.ReadKey ().Key) {
					case ConsoleKey.S:
						retryAgain = false;
						Console.WriteLine ("\nNote:  Running in simulation mode; no commands will be sent to USB");
						InitializeOutputSystem (new DummyOutput ());
						break;
					case ConsoleKey.R:
						Console.WriteLine ("\nTrying again...");
						break;
					default:
						retryAgain = false;
						// Quit called, exit the application
						return false;
					}
				} else {
					// Can't prompt for feedback, just bail out
					return false;
				}
			}

			if (SkipPreparingSystem == false) {
				Console.WriteLine ("Preparing system...");

				// Clear out any flickering in the buffer
				System.Threading.Thread.Sleep (200);
				G35_Lights_Queue.MarkAsProcessed ();
				// UpdateLights_All (G35_Lights_Queue.LightsLastProcessed);
				FillLights_Color (G35_Lights_Queue, LightSystem.Color_MIN, LightSystem.Color_MIN, LightSystem.Color_MIN, true, false);
				// System.Threading.Thread.Sleep (100);
				// FillLights_Color (G35_Lights_Queue, LightSystem.Color_MIN, LightSystem.Color_MIN, LightSystem.Color_MIN, true, false);
				if (UpdateLights_All (G35_Lights_Queue.Lights) == false) {
					Console.WriteLine ("! Error while updating lights, is something wrong?");
					// Can't do anything here, exit the application
					return false;
				}
				G35_Lights_Queue.MarkAsProcessed ();

				// Initialize the higher level system
				G35_Light_Start_Queue ();
				FillLights_Brightness (G35_Lights_Queue, LightSystem.Brightness_MIN, false);
				FillLights_Color (G35_Lights_Queue, LightSystem.Color_MIN, LightSystem.Color_MIN, LightSystem.Color_MIN, false, true);

				G35_Lights_Queue.PushToQueue ();
			}
			// Initialization successful!
			return true;
		}

		/// <summary>
		/// Shuts down the lighting manager and output system.
		/// </summary>
		private static void ShutdownSystem ()
		{
			Console.WriteLine ("Shutting down...");

			HaltActivity (true);
			FillLights_Color (G35_Lights_Queue, LightSystem.Color_MIN, LightSystem.Color_MIN, LightSystem.Color_MIN);
			G35_Light_Stop_Queue ();

			ShutdownOutputSystem ();
		}

		/// <summary>
		/// Runs the system menu, prompting for commands and doing actions.
		/// </summary>
		private static void RunMenu ()
		{
			string command = "help";
			// First-time around, show the command help
			int[] identifyLightsToModify = {0, 1, LightSystem.LIGHT_INDEX_MIDDLE - 1, LightSystem.LIGHT_INDEX_MIDDLE, LightSystem.LIGHT_INDEX_MIDDLE + 1,
				LightSystem.LIGHT_INDEX_MIDDLE + 2, LightSystem.LIGHT_COUNT - 2, LightSystem.LIGHT_COUNT - 1
			};
			// Used for the 'identify' command
			int LED_num;
			byte R, G, B, Brightness;
			string[] commands;
			while (command != "quit") {
				if (command != "") {
					if (command.Contains ("&&")) {
						Command_ConflictsExpected = true;
						commands = command.Replace ("@", "LITERAL_AT").Replace (" && ", "@").Split ('@');
						for (int i = 0; i < commands.Length; i++) {
							commands [i] = commands [i].Replace ("LITERAL_AT", "@").Trim ();
						}
					} else {
						if (command.StartsWith ("!")) {
							command = command.TrimStart ('!');
							Command_ConflictsExpected = true;
						} else {
							Command_ConflictsExpected = false;
						}
						commands = new string[] { command };
					}
					foreach (string current_command in commands) {
						if (current_command == "")
							continue;
						string[] cmd_args = current_command.Split (' ');
						if (cmd_args != null & cmd_args.Length > 0) {
							// TODO: Combine the regular and overlay commands into one unified set
							switch (cmd_args [0].ToLowerInvariant ()) {
							case "color":
								if (cmd_args.Length == 5 || cmd_args.Length == 6) {
									HaltActivity (false);
									try {
										R = Convert.ToByte (cmd_args [2]);
										G = Convert.ToByte (cmd_args [3]);
										B = Convert.ToByte (cmd_args [4]);

										if (cmd_args [1].ToLowerInvariant () == "all") {
											if (cmd_args.Length != 6)
												FillLights_Brightness (G35_Lights_Queue, LightSystem.Brightness_MAX, false, true);
											FillLights_Color (G35_Lights_Queue, R, G, B);
										} else {
											// First part can be a range of LEDs
											if (cmd_args [1].Contains ("-")) {
												int LED_start_num = (Convert.ToInt32 (cmd_args [1].Split ('-') [0]) - 1);
												int LED_end_num = (Convert.ToInt32 (cmd_args [1].Split ('-') [1]) - 1);
												if (LED_end_num <= LED_start_num) {
													Console.WriteLine ("(Range of LEDs must start from lower to higher, e.g. '4-20')");
													break;
												}
												for (LED_num = LED_start_num; LED_num <= LED_end_num; LED_num++) {
													if (cmd_args.Length != 6)
														G35_Lights_Queue.Lights [LED_num].Brightness = LightSystem.Brightness_MAX;
													SetLight_Color (G35_Lights_Queue, LED_num, R, G, B);
												}
											} else {
												LED_num = (Convert.ToInt32 (cmd_args [1]) - 1);
												if (cmd_args.Length != 6)
													G35_Lights_Queue.Lights [LED_num].Brightness = LightSystem.Brightness_MAX;
												SetLight_Color (G35_Lights_Queue, LED_num, R, G, B);
											}
										}
									} catch (System.FormatException) {
										Console.WriteLine ("(invalid numbers entered; type 'color' for help)");
									}
								} else {
									Console.WriteLine ("> color [LED # or 'all' or dash-separated range] [R from 0-255] [G from 0-255] [B from 0-255] [optional: keep brightness]");
								}
								break;
							case "brightness":
								if (cmd_args.Length == 3) {
									HaltActivity (false);
									try {
										Brightness = Convert.ToByte (cmd_args [2]);
										if (cmd_args [1].ToLowerInvariant () == "all") {
											FillLights_Brightness (G35_Lights_Queue, Brightness, true);
										} else {
											// First part can be a range of LEDs
											if (cmd_args [1].Contains ("-")) {
												int LED_start_num = (Convert.ToInt32 (cmd_args [1].Split ('-') [0]) - 1);
												int LED_end_num = (Convert.ToInt32 (cmd_args [1].Split ('-') [1]) - 1);
												if (LED_end_num <= LED_start_num) {
													Console.WriteLine ("(Range of LEDs must start from lower to higher, e.g. '4-20')");
													break;
												}
												for (LED_num = LED_start_num; LED_num <= LED_end_num; LED_num++) {
													G35_Lights_Queue.Lights [LED_num].Brightness = Brightness;
												}
											} else {
												LED_num = (Convert.ToInt32 (cmd_args [1]) - 1);
												G35_Lights_Queue.Lights [LED_num].Brightness = Brightness;
											}
											AddToAnimQueue (G35_Lights_Queue);
										}
									} catch (System.FormatException) {
										Console.WriteLine ("(invalid numbers entered; type 'brightness' for help)");
									}
								} else {
									Console.WriteLine ("> brightness [LED # or 'all' or dash-separated range] [brightness from 0-255]");
								}
								break;
							case "white":
								HaltActivity (false);
								FillLights_Brightness (G35_Lights_Queue, LightSystem.Brightness_MAX, false);
								FillLights_Color (G35_Lights_Queue, LightSystem.Color_MAX, LightSystem.Color_MAX, LightSystem.Color_MAX);
								break;
							case "black":
								HaltActivity (false);
								FillLights_Brightness (G35_Lights_Queue, LightSystem.Brightness_MIN, false);
								FillLights_Color (G35_Lights_Queue, LightSystem.Color_MIN, LightSystem.Color_MIN, LightSystem.Color_MIN);
								break;
							case "identify":
								HaltActivity (false);
								FillLights_Brightness (G35_Lights_Queue, LightSystem.Brightness_MIN, false);
								foreach (int ledIndex in identifyLightsToModify) {
									G35_Lights_Queue.Lights [ledIndex].Brightness = LightSystem.Brightness_MAX;
								}

								FillLights_Color (G35_Lights_Queue, LightSystem.Color_MIN, LightSystem.Color_MIN, LightSystem.Color_MIN, false);
								SetLight_Color (G35_Lights_Queue, 0, LightSystem.Color_MAX, LightSystem.Color_MIN, LightSystem.Color_MIN, false);
								SetLight_Color (G35_Lights_Queue, 1, LightSystem.Color_MAX, LightSystem.Color_MAX, LightSystem.Color_MIN, false);

								SetLight_Color (G35_Lights_Queue, LightSystem.LIGHT_INDEX_MIDDLE - 1, LightSystem.Color_MAX, LightSystem.Color_MIN, LightSystem.Color_MIN, false);
								SetLight_Color (G35_Lights_Queue, LightSystem.LIGHT_INDEX_MIDDLE, LightSystem.Color_MAX, LightSystem.Color_MIN, LightSystem.Color_MAX, false);
								SetLight_Color (G35_Lights_Queue, LightSystem.LIGHT_INDEX_MIDDLE + 1, LightSystem.Color_MIN, LightSystem.Color_MIN, LightSystem.Color_MAX, false);
								SetLight_Color (G35_Lights_Queue, LightSystem.LIGHT_INDEX_MIDDLE + 2, LightSystem.Color_MIN, LightSystem.Color_MAX, LightSystem.Color_MAX, false);

								SetLight_Color (G35_Lights_Queue, LightSystem.LIGHT_COUNT - 2, LightSystem.Color_MAX, LightSystem.Color_MAX, LightSystem.Color_MIN, false);
								SetLight_Color (G35_Lights_Queue, LightSystem.LIGHT_COUNT - 1, LightSystem.Color_MIN, LightSystem.Color_MAX, LightSystem.Color_MIN, false);
								AddToAnimQueue (G35_Lights_Queue);
								break;
							case "anim":
								if (cmd_args.Length > 1 && cmd_args [1] != null) {
									switch (cmd_args [1].ToLowerInvariant ()) {
									case "play":
										if (cmd_args.Length > 2 && cmd_args [2] != null) {
											switch (cmd_args [2].ToLowerInvariant ()) {
											case "simple":
												if (cmd_args.Length > 3 && cmd_args [3] != null) {
													switch (cmd_args [3].ToLowerInvariant ()) {
													case "fade":
														HaltActivity (false);
														SimpleFadeAnimation fade_animator = new SimpleFadeAnimation (G35_Lights_Queue.LightsLastProcessed);
														fade_animator.AnimationStyle = Animation_AnimationStyle;
														Animation_Play (G35_Lights_Queue, fade_animator);
														break;
													case "interval":
														if (cmd_args.Length > 4 && cmd_args [4] != null) {
															HaltActivity (false);
															IntervalAnimation time_animator = new IntervalAnimation (G35_Lights_Queue.LightsLastProcessed);
															switch (cmd_args [4].ToLowerInvariant ()) {
															case "time":
																time_animator.SelectedIntervalMode = IntervalAnimation.IntervalMode.Time;
																break;
															case "weather":
																time_animator.SelectedIntervalMode = IntervalAnimation.IntervalMode.Weather;
																Console.WriteLine ("('anim play simple interval weather' NOT YET IMPLEMENTED)");
																throw new System.NotImplementedException ("Tried to create an animation of type IntervalAnimation.IntervalMode.Weather, but that has not yet been implemented in code.");
															//break;
															default:
																Console.WriteLine ("> anim play simple interval [time, weather]");
																break;
															}
															time_animator.AnimationStyle = Animation_AnimationStyle;
															Animation_Play (G35_Lights_Queue, time_animator);
														} else {
															Console.WriteLine ("> anim play simple interval [time, weather]");
														}
														break;
													case "strobe":
														if (cmd_args.Length > 4 && cmd_args [4] != null) {
															HaltActivity (false);
															SimpleStrobeAnimation strobe_animator = new SimpleStrobeAnimation (G35_Lights_Queue.LightsLastProcessed);
															switch (cmd_args [4].ToLowerInvariant ()) {
															case "white":
																strobe_animator.SelectedStrobeMode = SimpleStrobeAnimation.StrobeMode.White;
																break;
															case "color":
																strobe_animator.SelectedStrobeMode = SimpleStrobeAnimation.StrobeMode.Color;
																break;
															case "single":
																strobe_animator.SelectedStrobeMode = SimpleStrobeAnimation.StrobeMode.Single;
																break;
															case "fireflies":
																strobe_animator.SelectedStrobeMode = SimpleStrobeAnimation.StrobeMode.Fireflies;
																break;
															case "rain":
																strobe_animator.SelectedStrobeMode = SimpleStrobeAnimation.StrobeMode.Rain;
																break;
															case "thunderstorm":
																strobe_animator.SelectedStrobeMode = SimpleStrobeAnimation.StrobeMode.Thunderstorm;
																break;
															default:
																Console.WriteLine ("> anim play simple strobe [white, color, single, fireflies, rain, thunderstorm]");
																break;
															}
															strobe_animator.AnimationStyle = Animation_AnimationStyle;
															Animation_Play (G35_Lights_Queue, strobe_animator);
														} else {
															Console.WriteLine ("> anim play simple strobe [white, color, single, fireflies, rain, thunderstorm]");
														}
														break;
													case "spinner":
														HaltActivity (false);
														SimpleSpinnerAnimation spinner_animator = new SimpleSpinnerAnimation (G35_Lights_Queue.LightsLastProcessed);
														spinner_animator.AnimationStyle = Animation_AnimationStyle;
														Animation_Play (G35_Lights_Queue, spinner_animator);
														break;
													default:
														Console.WriteLine ("> anim play simple [fade, interval, strobe, spinner]");
														break;
													}
												} else {
													Console.WriteLine ("> anim play simple [fade, interval, strobe, spinner]");
												}
												break;
											case "file":
												if (cmd_args.Length > 3 && cmd_args [3] != null) {
													switch (cmd_args [3].ToLowerInvariant ()) {
													case "basic":
														if (cmd_args.Length > 4 && cmd_args [4] != null) {
															string file_name = cmd_args [4];
															if (cmd_args.Length > 5) {
																// If there's spaces in the file-name, concatenate them together
																for (int i = 5; i < cmd_args.Length; i++) {
																	file_name += " " + cmd_args [i];
																}
															}
															if (System.IO.File.Exists (file_name)) {
																try {
																	HaltActivity (false);
																	BitmapAnimation imageAnimation = new BitmapAnimation (LightSystem.LIGHT_COUNT, file_name);
																	imageAnimation.AnimationStyle = Animation_AnimationStyle;
																	Console.WriteLine ("(animation successfully loaded)");
																	Animation_Play (G35_Lights_Queue, imageAnimation);	
																} catch (System.IO.InvalidDataException ex) {
																	Console.WriteLine ("(Something went wrong when playing that image...\n{0})", ex);
																}
															} else {
																Console.WriteLine ("(File '{0}' doesn't exist)", file_name);
															}
														} else {
															Console.WriteLine ("> anim play file basic [path to image file, at least {0} pixels wide]", LightSystem.LIGHT_COUNT);
														}
														break;
													case "audio":
														if (cmd_args.Length > 4 && cmd_args [4] != null) {
															string file_name = cmd_args [4];
															if (cmd_args.Length > 5) {
																// If there's spaces in the file-name, concatenate them together
																for (int i = 5; i < cmd_args.Length; i++) {
																	file_name += " " + cmd_args [i];
																}
															}
															if (System.IO.File.Exists (file_name)) {
																try {
																	HaltActivity (false);
																	AudioBitmapAnimation imageAnimation = new AudioBitmapAnimation (LightSystem.LIGHT_COUNT, file_name);
																	imageAnimation.AnimationStyle = Animation_AnimationStyle;
																	Console.WriteLine ("(animation successfully loaded)");
																	Animation_Play (G35_Lights_Queue, imageAnimation);	
																} catch (System.IO.InvalidDataException ex) {
																	Console.WriteLine ("(Something went wrong when playing that image...\n{0})", ex);
																}
															} else {
																Console.WriteLine ("(File '{0}' doesn't exist)", file_name);
															}
														} else {
															Console.WriteLine ("> anim play file audio [path to audio animation playlist, line #1 path to image at least {0} pixels wide, line #2 path to audio file]", LightSystem.LIGHT_COUNT);
														}
														break;
													default:
														Console.WriteLine ("> anim play file [audio, basic]");
														break;
													}
												} else {
													Console.WriteLine ("> anim play file [audio, basic]");
												}
												break;
											default:
												Console.WriteLine ("> anim play [simple, file]");
												break;
											}
										} else {
											Console.WriteLine ("> anim play [simple, file]");
										}
										break;
									case "seek":
										if (cmd_args.Length > 2 && cmd_args [2] != null) {
											if (G35_Lights_Queue.AnimationActive && G35_Lights_Queue.SelectedAnimation is AudioBitmapAnimation) {
												double time_seek;
												double.TryParse (cmd_args [2], out time_seek);
												(G35_Lights_Queue.SelectedAnimation as AudioBitmapAnimation).SeekToPosition (time_seek);
											} else {
												Console.WriteLine ("(Can't seek, no audio animation is playing)");
											}
										} else {
											Console.WriteLine ("> anim seek [time in seconds]");
										}
										break;
									case "stop":
										Animation_Stop (G35_Lights_Queue);
										break;
									case "fade":
										if (cmd_args.Length > 2 && cmd_args [2] != null) {
											switch (cmd_args [2].ToLowerInvariant ()) {
											case "enable":
												Animation_Fading_Enabled = true;
												Console.WriteLine ("(Fading for commands enabled)");
												break;
											case "disable":
												Animation_Fading_Enabled = false;
												Console.WriteLine ("(Fading for commands disabled)");
												break;
											default:
												Console.WriteLine ("> anim fade [enable, disable]");
												break;
											}
										} else {
											Console.WriteLine ("> anim fade [enable, disable]");
										}
										break;
									case "style":
										if (cmd_args.Length > 2 && cmd_args [2] != null) {
											bool style_changed = true;
											switch (cmd_args [2].ToLowerInvariant ()) {
											case "bright":
												Animation_AnimationStyle = AbstractAnimation.Style.Bright;
												Console.WriteLine ("(Animation style brightened)");
												break;
											case "moderate":
												Animation_AnimationStyle = AbstractAnimation.Style.Moderate;
												Console.WriteLine ("(Animation style moderate)");
												break;
											case "soft":
												Animation_AnimationStyle = AbstractAnimation.Style.Soft;
												Console.WriteLine ("(Animation style softened)");
												break;
											default:
												style_changed = false;
												Console.WriteLine ("> anim style [bright, moderate, soft]");
												break;
											}
											if (style_changed == true) {
												foreach (KeyValuePair <string, LED_Queue> queue in GetAllQueues ()) {
													if (queue.Value.AnimationActive) {
														queue.Value.SelectedAnimation.AnimationStyle = Animation_AnimationStyle;
														if ((queue.Value.SelectedAnimation.RequestedAnimationDelay > Light_Animation_Latency) || (queue.Value.SelectedAnimation.RequestSmoothCrossfade))
															queue.Value.AnimationForceFrameRequest = true;
													}
												}
											}
										} else {
											Console.WriteLine ("> anim style [bright, moderate (default), soft]");
										}
										break;
									default:
										Console.WriteLine ("> " + Anim_Command_Help);
										break;
									}
								} else {
									Console.WriteLine ("> " + Anim_Command_Help);
								}
								break;
							case "overlay":
								if (cmd_args.Length > 1 && cmd_args [1] != null) {
									switch (cmd_args [1].ToLowerInvariant ()) {
									case "list":
										lock (G35_Lights_Overlay_Queues) {
											if (G35_Lights_Overlay_Queues.Count == 0) {
												Console.WriteLine ("(no overlay layers active)");
											} else {
												Console.Write ("(currently active layers:");
												foreach (KeyValuePair <string, LED_Queue> queue in G35_Lights_Overlay_Queues) {
													Console.Write (" '{0}'", queue.Key);
												}
												Console.WriteLine (")");
											}
										}
										break;
									case "clear_all":
										lock (G35_Lights_Overlay_Queues) {
											foreach (KeyValuePair <string, LED_Queue> queue in G35_Lights_Overlay_Queues) {
												HaltActivity (queue.Value);
												FillLights_Brightness (queue.Value, 0, false, true);
												FillLights_Color (queue.Value, 0, 0, 0, true, true);
											}
											G35_Lights_Overlay_Queues.Clear ();
										}
										UpdateAllQueues ();
										break;
									default:
										if (cmd_args.Length > 2 && cmd_args [2] != null) {
											string overlay_name = cmd_args [1].ToLowerInvariant ();
											switch (overlay_name) {
											case G35_Lights_Queue_Name:
												Console.WriteLine ("(layer '{0}' already used as the default layer; pick another name)", G35_Lights_Queue_Name);
												break;
											default:
												switch (cmd_args [2].ToLowerInvariant ()) {
												case "color":
													lock (G35_Lights_Overlay_Queues) {
														LED_Queue resulting_queue = GetQueueByName (overlay_name, true);
														if (resulting_queue != null) {
															// Mapping from usual 'color' command:  Numbers 5-6 -> 7-8
															if (cmd_args.Length == 7 || cmd_args.Length == 8) {
																HaltActivity (resulting_queue);
																try {
																	R = Convert.ToByte (cmd_args [4]);
																	G = Convert.ToByte (cmd_args [5]);
																	B = Convert.ToByte (cmd_args [6]);

																	if (cmd_args [3].ToLowerInvariant () == "all") {
																		if (cmd_args.Length != 8)
																			FillLights_Brightness (resulting_queue, LightSystem.Brightness_MAX, false, true);
																		FillLights_Color (resulting_queue, R, G, B);
																	} else {
																		// First part can be a range of LEDs
																		if (cmd_args [3].Contains ("-")) {
																			int LED_start_num = (Convert.ToInt32 (cmd_args [3].Split ('-') [0]) - 1);
																			int LED_end_num = (Convert.ToInt32 (cmd_args [3].Split ('-') [1]) - 1);
																			if (LED_end_num <= LED_start_num) {
																				Console.WriteLine ("(Range of LEDs must start from lower to higher, e.g. '4-20')");
																				break;
																			}
																			for (LED_num = LED_start_num; LED_num <= LED_end_num; LED_num++) {
																				if (cmd_args.Length != 8)
																					resulting_queue.Lights [LED_num].Brightness = LightSystem.Brightness_MAX;
																				SetLight_Color (resulting_queue, LED_num, R, G, B);
																			}
																		} else {
																			LED_num = (Convert.ToInt32 (cmd_args [3]) - 1);
																			if (cmd_args.Length != 8)
																				resulting_queue.Lights [LED_num].Brightness = LightSystem.Brightness_MAX;
																			SetLight_Color (resulting_queue, LED_num, R, G, B);
																		}
																	}
																} catch (System.FormatException) {
																	Console.WriteLine ("(invalid numbers entered; type 'overlay {0} color' for help)", overlay_name);
																}
															} else {
																Console.WriteLine ("> overlay {0} color [LED # or 'all' or dash-separated range] [R from 0-255] [G from 0-255] [B from 0-255] [optional: keep brightness]", overlay_name);
															}
														}
													}
													break;
												case "brightness":
													lock (G35_Lights_Overlay_Queues) {
														LED_Queue resulting_queue = GetQueueByName (overlay_name, true);
														if (resulting_queue != null) {
															// Mapping from usual 'color' command:  Numbers 3 -> 5
															if (cmd_args.Length == 5) {
																HaltActivity (resulting_queue);
																try {
																	Brightness = Convert.ToByte (cmd_args [4]);
																	if (cmd_args [3].ToLowerInvariant () == "all") {
																		FillLights_Brightness (resulting_queue, Brightness, true);
																	} else {
																		// First part can be a range of LEDs
																		if (cmd_args [3].Contains ("-")) {
																			int LED_start_num = (Convert.ToInt32 (cmd_args [3].Split ('-') [0]) - 1);
																			int LED_end_num = (Convert.ToInt32 (cmd_args [3].Split ('-') [1]) - 1);
																			if (LED_end_num <= LED_start_num) {
																				Console.WriteLine ("(Range of LEDs must start from lower to higher, e.g. '4-20')");
																				break;
																			}
																			for (LED_num = LED_start_num; LED_num <= LED_end_num; LED_num++) {
																				resulting_queue.Lights [LED_num].Brightness = Brightness;
																			}
																		} else {
																			LED_num = (Convert.ToInt32 (cmd_args [3]) - 1);
																			resulting_queue.Lights [LED_num].Brightness = Brightness;
																		}
																		AddToAnimQueue (resulting_queue);
																	}
																} catch (System.FormatException) {
																	Console.WriteLine ("(invalid numbers entered; type 'overlay {0} brightness' for help)", overlay_name);
																}
															} else {
																Console.WriteLine ("> overlay {0} brightness [LED # or 'all' or dash-separated range] [brightness from 0-255]", overlay_name);
															}
														}
													}
													break;
												case "identify":
													lock (G35_Lights_Overlay_Queues) {
														LED_Queue resulting_queue = GetQueueByName (overlay_name, true);
														if (resulting_queue != null) {
															// Mapping from usual 'identify' command:  Numbers 1 -> 3
															HaltActivity (resulting_queue);
															FillLights_Brightness (resulting_queue, LightSystem.Brightness_MIN, false);
															foreach (int ledIndex in identifyLightsToModify) {
																resulting_queue.Lights [ledIndex].Brightness = LightSystem.Brightness_MAX;
															}

															FillLights_Color (resulting_queue, LightSystem.Color_MIN, LightSystem.Color_MIN, LightSystem.Color_MIN, false);
															SetLight_Color (resulting_queue, 0, LightSystem.Color_MAX, LightSystem.Color_MIN, LightSystem.Color_MIN, false);
															SetLight_Color (resulting_queue, 1, LightSystem.Color_MAX, LightSystem.Color_MAX, LightSystem.Color_MIN, false);

															SetLight_Color (resulting_queue, LightSystem.LIGHT_INDEX_MIDDLE - 1, LightSystem.Color_MAX, LightSystem.Color_MIN, LightSystem.Color_MIN, false);
															SetLight_Color (resulting_queue, LightSystem.LIGHT_INDEX_MIDDLE, LightSystem.Color_MAX, LightSystem.Color_MIN, LightSystem.Color_MAX, false);
															SetLight_Color (resulting_queue, LightSystem.LIGHT_INDEX_MIDDLE + 1, LightSystem.Color_MIN, LightSystem.Color_MIN, LightSystem.Color_MAX, false);
															SetLight_Color (resulting_queue, LightSystem.LIGHT_INDEX_MIDDLE + 2, LightSystem.Color_MIN, LightSystem.Color_MAX, LightSystem.Color_MAX, false);

															SetLight_Color (resulting_queue, LightSystem.LIGHT_COUNT - 2, LightSystem.Color_MAX, LightSystem.Color_MAX, LightSystem.Color_MIN, false);
															SetLight_Color (resulting_queue, LightSystem.LIGHT_COUNT - 1, LightSystem.Color_MIN, LightSystem.Color_MAX, LightSystem.Color_MIN, false);
															AddToAnimQueue (resulting_queue);
														}
													}
													break;
												case "play":
													lock (G35_Lights_Overlay_Queues) {
														LED_Queue resulting_queue = GetQueueByName (overlay_name, true);
														if (resulting_queue != null) {
															// Mapping: 2 -> 3
															if (cmd_args.Length > 3 && cmd_args [3] != null) {
																switch (cmd_args [3].ToLowerInvariant ()) {
																case "simple":
																	if (cmd_args.Length > 4 && cmd_args [4] != null) {
																		switch (cmd_args [4].ToLowerInvariant ()) {
																		case "strobe":
																			if (cmd_args.Length > 5 && cmd_args [5] != null) {
																				HaltActivity (resulting_queue);
																				SimpleStrobeAnimation strobe_animator = new SimpleStrobeAnimation (resulting_queue.LightsLastProcessed);
																				switch (cmd_args [5].ToLowerInvariant ()) {
																				case "white":
																					strobe_animator.SelectedStrobeMode = SimpleStrobeAnimation.StrobeMode.White;
																					break;
																				case "color":
																					strobe_animator.SelectedStrobeMode = SimpleStrobeAnimation.StrobeMode.Color;
																					break;
																				case "single":
																					strobe_animator.SelectedStrobeMode = SimpleStrobeAnimation.StrobeMode.Single;
																					break;
																				case "fireflies":
																					strobe_animator.SelectedStrobeMode = SimpleStrobeAnimation.StrobeMode.Fireflies;
																					break;
																				case "rain":
																					strobe_animator.SelectedStrobeMode = SimpleStrobeAnimation.StrobeMode.Rain;
																					break;
																				case "thunderstorm":
																					strobe_animator.SelectedStrobeMode = SimpleStrobeAnimation.StrobeMode.Thunderstorm;
																					break;
																				default:
																					Console.WriteLine ("> overlay {0} play simple strobe [white, color, single, fireflies, rain, thunderstorm]", overlay_name);
																					break;
																				}
																				strobe_animator.AnimationStyle = Animation_AnimationStyle;
																				Animation_Play (resulting_queue, strobe_animator);
																			} else {
																				Console.WriteLine ("> overlay {0} play simple strobe [white, color, single, fireflies, rain, thunderstorm]", overlay_name);
																			}
																			break;
																		case "spinner":
																			HaltActivity (resulting_queue);
																			SimpleSpinnerAnimation spinner_animator = new SimpleSpinnerAnimation (resulting_queue.LightsLastProcessed);
																			spinner_animator.AnimationStyle = Animation_AnimationStyle;
																			Animation_Play (resulting_queue, spinner_animator);
																			break;
																		default:
																			Console.WriteLine ("> overlay {0} play simple [fade, interval, strobe, spinner]", overlay_name);
																			break;
																		}
																	} else {
																		Console.WriteLine ("> overlay {0} play simple [fade, interval, strobe, spinner]", overlay_name);
																	}
																	break;
																case "file":
																	if (cmd_args.Length > 4 && cmd_args [4] != null) {
																		switch (cmd_args [4].ToLowerInvariant ()) {
																		case "basic":
																			if (cmd_args.Length > 5 && cmd_args [5] != null) {
																				string file_name = cmd_args [5];
																				if (cmd_args.Length > 6) {
																					// If there's spaces in the file-name, concatenate them together
																					for (int i = 6; i < cmd_args.Length; i++) {
																						file_name += " " + cmd_args [i];
																					}
																				}
																				if (System.IO.File.Exists (file_name)) {
																					try {
																						HaltActivity (resulting_queue);
																						BitmapAnimation imageAnimation = new BitmapAnimation (LightSystem.LIGHT_COUNT, file_name);
																						imageAnimation.AnimationStyle = Animation_AnimationStyle;
																						Console.WriteLine ("(animation successfully loaded)");
																						Animation_Play (resulting_queue, imageAnimation);	
																					} catch (System.IO.InvalidDataException ex) {
																						Console.WriteLine ("(Something went wrong when playing that image...\n{0})", ex);
																					}
																				} else {
																					Console.WriteLine ("(File '{0}' doesn't exist)", file_name);
																				}
																			} else {
																				Console.WriteLine ("> overlay {0} play file basic [path to image file, at least {1} pixels wide]", overlay_name, LightSystem.LIGHT_COUNT);
																			}
																			break;
																		case "audio":
																			if (cmd_args.Length > 5 && cmd_args [5] != null) {
																				string file_name = cmd_args [5];
																				if (cmd_args.Length > 6) {
																					// If there's spaces in the file-name, concatenate them together
																					for (int i = 6; i < cmd_args.Length; i++) {
																						file_name += " " + cmd_args [i];
																					}
																				}
																				if (System.IO.File.Exists (file_name)) {
																					try {
																						HaltActivity (resulting_queue);
																						AudioBitmapAnimation imageAnimation = new AudioBitmapAnimation (LightSystem.LIGHT_COUNT, file_name);
																						imageAnimation.AnimationStyle = Animation_AnimationStyle;
																						Console.WriteLine ("(animation successfully loaded)");
																						Animation_Play (resulting_queue, imageAnimation);	
																					} catch (System.IO.InvalidDataException ex) {
																						Console.WriteLine ("(Something went wrong when playing that image...\n{0})", ex);
																					}
																				} else {
																					Console.WriteLine ("(File '{0}' doesn't exist)", file_name);
																				}
																			} else {
																				Console.WriteLine ("> overlay {0} play file audio [path to audio animation playlist, line #1 path to image at least {1} pixels wide, line #2 path to audio file]", overlay_name, LightSystem.LIGHT_COUNT);
																			}
																			break;
																		default:
																			Console.WriteLine ("> overlay {0} play file [audio, basic]", overlay_name);
																			break;
																		}
																	} else {
																		Console.WriteLine ("> overlay {0} play file [audio, basic]", overlay_name);
																	}
																	break;
																default:
																	Console.WriteLine ("> overlay {0} play [simple, file]", overlay_name);
																	break;
																}
															} else {
																Console.WriteLine ("> overlay {0} play [simple, file]", overlay_name);
															}
														}
													}
													break;
												case "blending":
													if (cmd_args.Length > 3 && cmd_args [3] != null) {
														bool blending_changed = true;
														LED_Queue resulting_queue = GetQueueByName (overlay_name, true);
														if (resulting_queue != null) {
															switch (cmd_args [3].ToLowerInvariant ()) {
															case "combine":
																resulting_queue.BlendMode = LED.BlendingStyle.Combine;
																break;
															case "favor":
																resulting_queue.BlendMode = LED.BlendingStyle.Favor;
																break;
															case "mask":
																resulting_queue.BlendMode = LED.BlendingStyle.Mask;
																break;
															case "replace":
																resulting_queue.BlendMode = LED.BlendingStyle.Replace;
																break;
																// FIXME:  Add a new mode that unselectively replaces everything
																//  Perhaps rename current to 'mask', and new should be 'replace'
															default:
																blending_changed = false;
																Console.WriteLine ("> overlay {0} blending [combine/favor/mask/replace]", overlay_name);
																break;
															}
															WarnQueueLossIfEmpty (resulting_queue);
															if (blending_changed == true) {
																UpdateAllQueues ();
															}
														}
													} else {
														Console.WriteLine ("> overlay {0} blending [combine/favor/mask/replace]", overlay_name);
													}
													break;
												case "exists":
													lock (G35_Lights_Overlay_Queues) {
														if (G35_Lights_Overlay_Queues.ContainsKey (overlay_name)) {
															Console.WriteLine ("(layer '{0}' exists)", overlay_name);
														} else {
															Console.WriteLine ("(layer '{0}' does not exist)", overlay_name);
														}
													}
													break;
												case "clear":
													lock (G35_Lights_Overlay_Queues) {
														if (G35_Lights_Overlay_Queues.ContainsKey (overlay_name)) {
															LED_Queue queue_to_remove = GetQueueByName (overlay_name);
															HaltActivity (queue_to_remove);
															FillLights_Brightness (queue_to_remove, 0, false, true);
															FillLights_Color (queue_to_remove, 0, 0, 0, true, true);
															G35_Lights_Overlay_Queues.Remove (overlay_name);
															Console.WriteLine ("(layer '{0}' removed)", overlay_name);
															UpdateAllQueues ();
														} else {
															Console.WriteLine ("(layer '{0}' does not exist, no change)", overlay_name);
														}
													}
													break;
												default:
													Console.WriteLine ("> " + Overlay_Layer_Command_Help);
													break;
												}
												break;
											}
										} else {
											Console.WriteLine ("> " + Overlay_Layer_Command_Help);
										}
										break;
									}
								} else {
									Console.WriteLine ("> " + Overlay_Command_Help);
								}
								break;
							case "shift_outwards":
								if (cmd_args.Length == 2) {
									HaltActivity (false);
									int Shift_Amount = Convert.ToByte (cmd_args [1]);
									LightProcessing.ShiftLightsOutward (G35_Lights_Queue.Lights, Shift_Amount);
									AddToAnimQueue (G35_Lights_Queue);
								} else {
									Console.WriteLine ("> shift_outwards [number of times]");
								}
								break;
							case "vu":
								if (cmd_args.Length > 1 && cmd_args [1] != null) {
									switch (cmd_args [1].ToLowerInvariant ()) {
									case "run":
										// For the newer AbstractReactiveAnimation types
										//  Automatically starts the system, resetting if an existing animation was running
										if (cmd_args.Length == 3) {
											// Prepare the animation
											//  Animation_Play will automatically enable the VU system
											switch (cmd_args [2]) {
											case "beat_pulse":
												HaltActivity (false);
												BeatPulseReactiveAnimation beatpulse_animator = new BeatPulseReactiveAnimation (G35_Lights_Queue.LightsLastProcessed);
												beatpulse_animator.AnimationStyle = Animation_AnimationStyle;
												Animation_Play (G35_Lights_Queue, beatpulse_animator);
												break;
											case "spinner":
												HaltActivity (false);
												SpinnerReactiveAnimation spinner_animator = new SpinnerReactiveAnimation (G35_Lights_Queue.LightsLastProcessed);
												spinner_animator.AnimationStyle = Animation_AnimationStyle;
												Animation_Play (G35_Lights_Queue, spinner_animator);
												break;
											case "rave_mood":
												HaltActivity (false);
												RaveMoodReactiveAnimation ravemood_animator = new RaveMoodReactiveAnimation (G35_Lights_Queue.LightsLastProcessed);
												ravemood_animator.AnimationStyle = Animation_AnimationStyle;
												Animation_Play (G35_Lights_Queue, ravemood_animator);
												break;
											default:
												Console.WriteLine ("(Not a valid VU meter type)");
												Console.WriteLine ("> " + VU_Run_Command_Help);
												break;
											}
										} else {
											Console.WriteLine ("> " + VU_Run_Command_Help);
										}
										break;
									case "legacy_mode":
										if (cmd_args.Length == 3) {
											if (G35_Lights_Queue.AnimationActive == false || !(G35_Lights_Queue.SelectedAnimation is LegacyReactiveAnimation)) {
												Console.WriteLine ("(Starting legacy VU animation...)");

												HaltActivity (false);
												LegacyReactiveAnimation legacy_animator = new LegacyReactiveAnimation (G35_Lights_Queue.LightsLastProcessed);
												legacy_animator.AnimationStyle = Animation_AnimationStyle;
												Animation_Play (G35_Lights_Queue, legacy_animator);
											}
											switch (cmd_args [2]) {
											case "auto_fast_beat":
												(G35_Lights_Queue.SelectedAnimation as LegacyReactiveAnimation).VU_Selected_Mode = LegacyReactiveAnimation.VU_Meter_Mode.AutomaticFastBeat;
												//VU_Selected_Mode = VU_Meter_Mode.AutomaticFastBeat;
												break;
											case "rainbow":
												(G35_Lights_Queue.SelectedAnimation as LegacyReactiveAnimation).VU_Selected_Mode = LegacyReactiveAnimation.VU_Meter_Mode.Rainbow;
												//VU_Selected_Mode = VU_Meter_Mode.Rainbow;
												break;
											case "rainbow_solid":
												(G35_Lights_Queue.SelectedAnimation as LegacyReactiveAnimation).VU_Selected_Mode = LegacyReactiveAnimation.VU_Meter_Mode.RainbowSolid;
												//VU_Selected_Mode = VU_Meter_Mode.RainbowSolid;
												break;
											case "rainbow_beat":
												(G35_Lights_Queue.SelectedAnimation as LegacyReactiveAnimation).VU_Selected_Mode = LegacyReactiveAnimation.VU_Meter_Mode.RainbowBeat;
												//VU_Selected_Mode = VU_Meter_Mode.RainbowBeat;
												break;
											case "rainbow_beat_bass":
												(G35_Lights_Queue.SelectedAnimation as LegacyReactiveAnimation).VU_Selected_Mode = LegacyReactiveAnimation.VU_Meter_Mode.RainbowBeatBass;
												//VU_Selected_Mode = VU_Meter_Mode.RainbowBeatBass;
												break;
											case "hueshift":
												(G35_Lights_Queue.SelectedAnimation as LegacyReactiveAnimation).VU_Selected_Mode = LegacyReactiveAnimation.VU_Meter_Mode.Hueshift;
												//VU_Selected_Mode = VU_Meter_Mode.Hueshift;
												break;
											case "hueshift_beat":
												(G35_Lights_Queue.SelectedAnimation as LegacyReactiveAnimation).VU_Selected_Mode = LegacyReactiveAnimation.VU_Meter_Mode.HueshiftBeat;
												//VU_Selected_Mode = VU_Meter_Mode.HueshiftBeat;
												break;
											case "moving_bars":
												(G35_Lights_Queue.SelectedAnimation as LegacyReactiveAnimation).VU_Selected_Mode = LegacyReactiveAnimation.VU_Meter_Mode.MovingBars;
												//VU_Selected_Mode = VU_Meter_Mode.MovingBars;
												break;
											case "moving_bars_spaced":
												(G35_Lights_Queue.SelectedAnimation as LegacyReactiveAnimation).VU_Selected_Mode = LegacyReactiveAnimation.VU_Meter_Mode.MovingBarsEquallySpaced;
												//VU_Selected_Mode = VU_Meter_Mode.MovingBarsEquallySpaced;
												break;
											case "stationary_bars":
												(G35_Lights_Queue.SelectedAnimation as LegacyReactiveAnimation).VU_Selected_Mode = LegacyReactiveAnimation.VU_Meter_Mode.StationaryBars;
												//VU_Selected_Mode = VU_Meter_Mode.StationaryBars;
												break;
											case "solid_rainbow_strobe":
												(G35_Lights_Queue.SelectedAnimation as LegacyReactiveAnimation).VU_Selected_Mode = LegacyReactiveAnimation.VU_Meter_Mode.SolidRainbowStrobe;
												//VU_Selected_Mode = VU_Meter_Mode.SolidRainbowStrobe;
												break;
											case "solid_white_strobe":
												(G35_Lights_Queue.SelectedAnimation as LegacyReactiveAnimation).VU_Selected_Mode = LegacyReactiveAnimation.VU_Meter_Mode.SolidWhiteStrobe;
												//VU_Selected_Mode = VU_Meter_Mode.SolidWhiteStrobe;
												break;
											case "solid_hueshift_strobe":
												(G35_Lights_Queue.SelectedAnimation as LegacyReactiveAnimation).VU_Selected_Mode = LegacyReactiveAnimation.VU_Meter_Mode.SolidHueshiftStrobe;
												//VU_Selected_Mode = VU_Meter_Mode.SolidHueshiftStrobe;
												break;
											case "single_rainbow_strobe":
												(G35_Lights_Queue.SelectedAnimation as LegacyReactiveAnimation).VU_Selected_Mode = LegacyReactiveAnimation.VU_Meter_Mode.SolidSingleRainbowStrobe;
												//VU_Selected_Mode = VU_Meter_Mode.SolidSingleRainbowStrobe;
												break;
											case "single_white_strobe":
												(G35_Lights_Queue.SelectedAnimation as LegacyReactiveAnimation).VU_Selected_Mode = LegacyReactiveAnimation.VU_Meter_Mode.SolidSingleWhiteStrobe;
												//VU_Selected_Mode = VU_Meter_Mode.SolidSingleWhiteStrobe;
												break;
											default:
												Console.WriteLine ("(Not a valid legacy VU meter mode)");
												Console.WriteLine ("> " + VU_Legacy_Mode_Command_Help);
												break;
											}
										} else {
											Console.WriteLine ("> " + VU_Legacy_Mode_Command_Help);
										}
										break;
									case "display":
										if (cmd_args.Length > 2 && cmd_args [2] != null) {
											switch (cmd_args [2].ToLowerInvariant ()) {
											case "meter":
												if (cmd_args.Length > 3 && cmd_args [3] != null) {
													switch (cmd_args [3].ToLowerInvariant ()) {
													case "show":
														ReactiveSystem.Processing_Show_Analysis = true;
														break;
													case "hide":
														ReactiveSystem.Processing_Show_Analysis = false;
														break;
													default:
														ReactiveSystem.Processing_Show_Analysis = !ReactiveSystem.Processing_Show_Analysis;
														Console.WriteLine ("(Toggled VU meter display; for specific control, vu display meter [show, hide])");
														break;
													}
												} else {
													ReactiveSystem.Processing_Show_Analysis = !ReactiveSystem.Processing_Show_Analysis;
													Console.WriteLine ("(Toggled VU meter display; for specific control, vu display meter [show, hide])");
												}
												break;
											case "variable":
												if (cmd_args.Length > 3 && cmd_args [3] != null) {
													switch (cmd_args [3].ToLowerInvariant ()) {
													case "show":
														ReactiveSystem.Processing_Show_Variables = true;
														break;
													case "hide":
														ReactiveSystem.Processing_Show_Variables = false;
														break;
													default:
														ReactiveSystem.Processing_Show_Variables = !ReactiveSystem.Processing_Show_Variables;
														Console.WriteLine ("(Toggled VU variable display; for specific control, vu display variable [show, hide])");
														break;
													}
												} else {
													ReactiveSystem.Processing_Show_Variables = !ReactiveSystem.Processing_Show_Variables;
													Console.WriteLine ("(Toggled VU variable display; for specific control, vu display variable [show, hide])");
												}
												break;
											case "freq":
												if (cmd_args.Length > 3 && cmd_args [3] != null) {
													switch (cmd_args [3].ToLowerInvariant ()) {
													case "show":
														ReactiveSystem.Processing_Show_Frequencies = true;
														break;
													case "hide":
														ReactiveSystem.Processing_Show_Frequencies = false;
														break;
													default:
														ReactiveSystem.Processing_Show_Frequencies = !ReactiveSystem.Processing_Show_Frequencies;
														Console.WriteLine ("(Toggled VU frequency display; for specific control, vu display freq [show, hide])");
														break;
													}
												} else {
													ReactiveSystem.Processing_Show_Frequencies = !ReactiveSystem.Processing_Show_Frequencies;
													Console.WriteLine ("(Toggled VU frequency display; for specific control, vu display freq [show, hide])");
												}
												break;
											case "limit":
												if (cmd_args.Length > 3 && cmd_args [3] != null) {
													switch (cmd_args [3].ToLowerInvariant ()) {
													case "enable":
														ReactiveSystem.Processing_Limit_Display = true;
														break;
													case "disable":
														ReactiveSystem.Processing_Limit_Display = false;
														break;
													default:
														ReactiveSystem.Processing_Limit_Display = !ReactiveSystem.Processing_Limit_Display;
														Console.WriteLine ("(Toggled limiting width of VU display; for specific control, vu display limit [enable, disable])");
														break;
													}
												} else {
													ReactiveSystem.Processing_Limit_Display = !ReactiveSystem.Processing_Limit_Display;
													Console.WriteLine ("(Toggled limiting width of VU display; for specific control, vu display limit [enable, disable])");
												}
												break;
											default:
												Console.WriteLine ("> vu display [meter, variable, freq, limit]");
												break;
											}
										} else {
											Console.WriteLine ("> vu display [meter, variable, freq, limit]");
										}
										break;
									case "hide":
										ReactiveSystem.Processing_Show_Analysis = false;
										ReactiveSystem.Processing_Show_Variables = false;
										ReactiveSystem.Processing_Show_Frequencies = false;
										Console.WriteLine ("(All VU display output hidden)");
										break;
									case "set_low":
										if (cmd_args.Length == 3) {
											ReactiveSystem.Audio_Volume_Low_Percentage = Math.Max (Convert.ToDouble (cmd_args [2]), 0);
										} else {
											Console.WriteLine ("> vu set_low [percentage of frequencies for low, current " + ReactiveSystem.Audio_Volume_Low_Percentage.ToString () + ", larger (up to 1) = skip more]");
										}
										break;
									case "set_mid":
										if (cmd_args.Length == 3) {
											ReactiveSystem.Audio_Volume_Mid_Percentage = Math.Max (Convert.ToDouble (cmd_args [2]), 0);
										} else {
											Console.WriteLine ("> vu set_mid [percentage of frequencies for mid, current " + ReactiveSystem.Audio_Volume_Mid_Percentage.ToString () + ", larger (up to 1) = more]");
										}
										break;
									case "set_high":
										if (cmd_args.Length == 3) {
											ReactiveSystem.Audio_Volume_High_Percentage = Math.Max (Convert.ToDouble (cmd_args [2]), 0);
										} else {
											Console.WriteLine ("> vu set_high [percentage of frequencies for high, current " + ReactiveSystem.Audio_Volume_High_Percentage.ToString () + ", smaller (from 0 to 1) = more]");
										}
										break;
									case "set_frequency_start":
										if (cmd_args.Length == 3) {
											ReactiveSystem.Audio_Frequency_Scale_Start = Math.Max (Convert.ToDouble (cmd_args [2]), 0);
										} else {
											Console.WriteLine ("> vu set_frequency_start [starting value for the frequency scaling multiplier, current " + ReactiveSystem.Audio_Frequency_Scale_Start.ToString () + ", larger = shift bars more towards higher frequencies]");
										}
										break;
									default:
										Console.WriteLine ("> " + VU_Command_Help);
										break;
									}
								} else {
									Console.WriteLine ("> " + VU_Command_Help);
								}
								break;
							case "clear":
								Console.Clear ();
								break;
							case "queue":
								if (cmd_args.Length > 1 && cmd_args [1] != null) {
									switch (cmd_args [1].ToLowerInvariant ()) {
									case "start":
										G35_Light_Start_Queue ();
										Console.WriteLine ("(G35 light queue manually started; queued events cleared)");
										break;
									case "stop":
										G35_Light_Stop_Queue ();
										Console.WriteLine ("(G35 light queue forcibly stopped)");
										break;
									case "clear":
										G35_Lights_Queue.ClearQueue ();
										Console.WriteLine ("(G35 light queue cleared)");
										break;
									case "test":
										FillLights_Color (G35_Lights_Queue, LightSystem.Color_MIN, LightSystem.Color_MIN, LightSystem.Color_MIN, false);
										G35_Lights_Queue.PushToQueue ();
										WaitForQueue (G35_Lights_Queue);
										Console.WriteLine ("(G35 light queue test: synchronous)");
										for (int i = 0; i < G35_Lights_Queue.Lights.Count; i++) {
											G35_Lights_Queue.Lights [i].R = LightSystem.Color_MAX;
											G35_Lights_Queue.Lights [i].G = LightSystem.Color_MIN;
											G35_Lights_Queue.Lights [i].B = LightSystem.Color_MAX;
											G35_Lights_Queue.Lights [i].Brightness = LightSystem.Brightness_MAX;
											G35_Lights_Queue.PushToQueue ();
											Console.Write ("X");
											WaitForQueue (G35_Lights_Queue);
											Console.Write ("_");
										}
										Console.WriteLine ("");
										System.Threading.Thread.Sleep (500);
										Console.WriteLine ("(G35 light queue test: asynchronous)");
										Console.WriteLine ("(filling queue)");
										for (int i = 0; i < G35_Lights_Queue.Lights.Count; i++) {

											G35_Lights_Queue.Lights [i].R = LightSystem.Color_MIN;
											G35_Lights_Queue.Lights [i].G = LightSystem.Color_MIN;
											G35_Lights_Queue.Lights [i].B = LightSystem.Color_MAX;
											G35_Lights_Queue.Lights [i].Brightness = LightSystem.Brightness_MAX;
											G35_Lights_Queue.PushToQueue ();
										}
										Console.WriteLine ("(queue filled)");
										WaitForQueue (G35_Lights_Queue);
										Console.WriteLine ("(queue ready)");
										Console.WriteLine ("(G35 light queue test complete)");
										break;
									case "spam":
										FillLights_Color (G35_Lights_Queue, LightSystem.Color_MIN, LightSystem.Color_MIN, LightSystem.Color_MIN, false);
										FillLights_Brightness (G35_Lights_Queue, LightSystem.Brightness_MAX, false);
										G35_Lights_Queue.PushToQueue ();
										WaitForQueue (G35_Lights_Queue);
										Console.WriteLine ("(G35 light queue test: adding 1000 events)");
										for (int loop = 0; loop < (1000 / (LightSystem.LIGHT_COUNT * 2)); loop++) {
											for (int i = 0; i < G35_Lights_Queue.Lights.Count; i++) {
												G35_Lights_Queue.Lights [i].R = LightSystem.Color_MIN;
												G35_Lights_Queue.Lights [i].G = LightSystem.Color_MIN;
												G35_Lights_Queue.Lights [i].B = LightSystem.Color_MAX;
												G35_Lights_Queue.PushToQueue ();
											}
											for (int i = 0; i < G35_Lights_Queue.Lights.Count; i++) {
												G35_Lights_Queue.Lights [i].SetColor (RandomColorGenerator.GetRandomColor ());
												G35_Lights_Queue.PushToQueue ();
											}
										}
										Console.WriteLine ("(queue filled)");
										break;
									default:
										Console.WriteLine ("> " + Queue_Command_Help);
										break;
									}
								} else {
									Console.WriteLine ("> " + Queue_Command_Help);
								}
								break;
							case "reset":
								Console.WriteLine ("(resetting output system...)");
								// FIXME: Resetting the system should not require halting everything, but something deadlocks otherwise...
								HaltActivity (true);
								if (ResetOutputSystem ()) {
									Console.WriteLine ("(reset succeeded)");
									// Re-send the last displayed frame of lights
									G35_Lights_Queue.PushToQueue ();
								} else {
									Console.WriteLine ("(reset failed!)");
								}
								break;
							case "help":
								Console.WriteLine ("> " + Main_Command_Help);
								break;
							default:
								Console.WriteLine ("(unknown command)");
								break;
							}
						}
					}
				}
				try {
					command = Console.ReadLine ();
				} catch (ArgumentOutOfRangeException) {
					Console.WriteLine ("\n(Sorry, but there was a problem reading console input.  You'll have to re-type whatever you were going to say)");
					command = "";
				}
				if (command == null)
					command = "";
			}
		}

		/// <summary>
		/// Clear out any keys stuck in the console input while the event loop was hung
		/// </summary>
		private static void FlushKeyboard ()
		{
			while (Console.In.Peek() != -1) {
				Console.In.Read ();
				if (SkipInput != true)
					break;
			}
		}

		#endregion

		#region Queue Management

		private static Dictionary <string, LED_Queue> GetAllQueues ()
		{
			lock (G35_Lights_Overlay_Queues) {
				Dictionary<string, LED_Queue> MergedQueues = new Dictionary<string, LED_Queue> (G35_Lights_Overlay_Queues.Count + 1, G35_Lights_Overlay_Queues.Comparer);
				foreach (KeyValuePair<string, LED_Queue> queue in G35_Lights_Overlay_Queues) {
					MergedQueues.Add (queue.Key, queue.Value);
				}
				MergedQueues.Add (G35_Lights_Queue_Name, G35_Lights_Queue);
				return MergedQueues;
			}
		}

		private static LED_Queue GetQueueByName (string QueueName, bool ShowWarningIfNull = false)
		{
			lock (G35_Lights_Overlay_Queues) {
				if (G35_Lights_Overlay_Queues.ContainsKey (QueueName) == false) {
					if (G35_Lights_Overlay_Queues.Count < G35_Lights_Maximum_Overlays) {
#if DEBUG_OVERLAY_MANAGEMENT
						Console.WriteLine ("-- '{0}' does not exist; adding queue", QueueName);
#endif
						G35_Lights_Overlay_Queues.Add (QueueName, new LED_Queue (LightSystem.LIGHT_COUNT, true));
					} else {
#if DEBUG_OVERLAY_MANAGEMENT
						Console.WriteLine ("-- '{0}' does not exist; cannot create queue as too many already exist", QueueName);
#endif
						if (ShowWarningIfNull)
							Console.WriteLine ("(Cannot create layer '{0}' as {1} queues already exist; delete others using 'overlay name_of_queue clear')", QueueName, G35_Lights_Overlay_Queues.Count);
						return null;
					}
				} else {
#if DEBUG_OVERLAY_MANAGEMENT
					Console.WriteLine ("-- '{0}' already exists; reusing", QueueName);
#endif
				}
				return G35_Lights_Overlay_Queues [QueueName];
			}
		}

		private static void WarnQueueLossIfEmpty (LED_Queue QueueToModify)
		{
			if (QueueToModify.LightsHaveNoEffect) {
				Console.WriteLine ("(Warning: queue has no effect and will be automatically deleted!  Set a color, brightness, or animation to avoid this.)");
			}
		}

		/// <summary>
		/// Forces all queues to update at least once, useful to propagate overlay queue changes.
		/// </summary>
		private static void UpdateAllQueues ()
		{
			// Force an update on all existing layers
			lock (G35_Lights_Overlay_Queues) {
				foreach (KeyValuePair <string, LED_Queue> queue in G35_Lights_Overlay_Queues) {
					queue.Value.PushToQueue (true);
				}
			}
			lock (G35_Lights_Queue) {
				G35_Lights_Queue.PushToQueue (true);
			}
		}

		private static void HaltActivity (bool IncludeOverlayQueues)
		{
			// As some animations depend partially on the VU volume system, the animation framework should be shut down first.
			//  That will also disable the VU volume system if the animation had requested it.
			if (IncludeOverlayQueues) {
				foreach (KeyValuePair <string, LED_Queue> queue in GetAllQueues ()) {
					HaltActivity (queue.Value);
				}
			} else {
				HaltActivity (G35_Lights_Queue);
			}
		}

		private static void HaltActivity (LED_Queue QueueToModify, bool ForceQueueCleanup = false)
		{
			if (ForceQueueCleanup) {
				QueueToModify.ClearQueue ();
			}
			if (QueueToModify.AnimationActive)
				Animation_Stop (QueueToModify);
		}

		private static void G35_Light_Start_Queue ()
		{
			if (G35_Light_Queue_Thread != null)
				G35_Light_Stop_Queue ();
			G35_Light_Queue_Thread = new System.Threading.Thread (G35_Light_Run_Queue);
			G35_Light_Queue_Thread.IsBackground = true;
			G35_Light_Queue_Thread.Priority = System.Threading.ThreadPriority.BelowNormal;
			G35_Light_Queue_Thread.Start ();
		}

		private static void G35_Light_Stop_Queue ()
		{
			if (G35_Light_Queue_Thread != null)
				G35_Light_Queue_Thread.Abort ();
		}

		private static void G35_Light_Run_Queue ()
		{
			G35_Lights_Queue.ClearQueue ();

			// Current LED frame to send to output
			LED_Set Light_Snapshot = null;
			// All individual frames to send to output
			List<LED_Set> Light_Snapshots = new List<LED_Set> ();
			// Current VU volumes as collected from the VU input system; only used for AbstractReactiveAnimation
			List<double> Audio_Volumes_Snapshot = new List<double> ();

			// Keeps track of queue performance to maintain consistent FPS
			System.Diagnostics.Stopwatch Queue_PerfStopwatch = new System.Diagnostics.Stopwatch ();
#if DEBUG_PERFORMANCE
			bool wasIdle = false;
#endif
			while (true) {
				// Keep track of how long these steps take to maintain a consistent FPS
				Queue_PerfStopwatch.Restart ();

				// -- ReactiveAnimation-specific queue management --
				// Note: For now, only the base layer can be a ReactiveAnimation; overlays will not get updated
				AbstractReactiveAnimation selected_reactive_animation = G35_Lights_Queue.SelectedAnimation as AbstractReactiveAnimation;
				if (G35_Lights_Queue.AnimationActive && selected_reactive_animation != null) {
#if DEBUG_VU_PERFORMANCE
					VU_Processing_PerfStopwatch.Restart ();
#endif
#if DEBUG_PERFORMANCE
					Console.WriteLine ("{0} ms - updating audio snapshot", Queue_PerfStopwatch.ElapsedMilliseconds);
#endif
					// Grab a snapshot of the current audio volumes, locking the array to prevent modification
					lock (Audio_Volumes) {
						while (Audio_Volumes_Snapshot.Count < Audio_Volumes.Count) {
							Audio_Volumes_Snapshot.Add (0);
						}
						while (Audio_Volumes_Snapshot.Count > Audio_Volumes.Count) {
							Audio_Volumes_Snapshot.RemoveAt (Audio_Volumes_Snapshot.Count - 1);
						}
						for (int i = 0; i < Audio_Volumes.Count; i++) {
							Audio_Volumes_Snapshot [i] = Audio_Volumes [i];
							Audio_Volumes [i] = 0;
						}
					}
					
					// Update the current reactive animation with this volume snapshot
					selected_reactive_animation.UpdateAudioSnapshot (Audio_Volumes_Snapshot);
					ReactiveSystem.PrintAudioInformationToConsole (selected_reactive_animation);
#if DEBUG_VU_PERFORMANCE
					Console.WriteLine ("# Time until acknowledged: {0}", VU_Processing_PerfStopwatch.ElapsedMilliseconds);
#endif
				}

				// -- Animation-specific queue management --
				foreach (KeyValuePair <string, LED_Queue> queue in GetAllQueues ()) {
					if (queue.Value.AnimationActive) {
						UpdateAnimationStackForQueue (queue.Value, Queue_PerfStopwatch.ElapsedMilliseconds, queue.Key);
					}
				}

				// -- Generic Light Queue output --
				// Fixed: with multi-threaded timing, the light-queue could become empty between checking the count and pulling a snapshot

				if (ActiveOutputSystemReady ()) {
					LED_Set QueueLightSnapshot = null;
					bool update_needed = false;
					foreach (KeyValuePair <string, LED_Queue> queue in GetAllQueues ()) {
						if (queue.Value.QueueEmpty == false) {
							update_needed = true;
							break;
						}
					}
					foreach (KeyValuePair <string, LED_Queue> queue in GetAllQueues ()) {
						QueueLightSnapshot = queue.Value.PopFromQueue ();
						if (QueueLightSnapshot != null) {
#if DEBUG_PERFORMANCE
							Console.WriteLine ("{0} ms - grabbing snapshot from light queue ({1})", Queue_PerfStopwatch.ElapsedMilliseconds, queue.Key);
#endif
							Light_Snapshots.Add (QueueLightSnapshot);
							queue.Value.QueueIdleTime = 0;
							queue.Value.Lights = QueueLightSnapshot.LED_Values;
#if DEBUG_PERFORMANCE
							Console.WriteLine ("{0} ms - updating last processed ({1})", Queue_PerfStopwatch.ElapsedMilliseconds, queue.Key);
#endif
							queue.Value.MarkAsProcessed ();
						} else if (update_needed) {
							// Nothing new in the queue, but it must be added to the snapshot for it to be blended down again
							LED_Set last_processed_set = new LED_Set (queue.Value.LightsLastProcessed);
							last_processed_set.BlendMode = queue.Value.BlendMode;
							Light_Snapshots.Add (last_processed_set);
						}
					}
				}

				// Do this after the above to ensure any remaining queue entries will be pushed out
				bool update_after_deletion_needed = false;
				lock (G35_Lights_Overlay_Queues) {
					List<string> EmptyQueues = new List<string> ();
					IAnimationOneshot selected_oneshot_animation = null;
					bool deletionRequested = false;
					bool queueWithMaskBlendingActive = false;
					foreach (KeyValuePair <string, LED_Queue> queue in G35_Lights_Overlay_Queues) {
						selected_oneshot_animation = queue.Value.SelectedAnimation as IAnimationOneshot;
						if (queue.Value.BlendMode == LED.BlendingStyle.Mask || queue.Value.BlendMode == LED.BlendingStyle.Replace)
							queueWithMaskBlendingActive = true;
						if ((queue.Value.LightsHaveNoEffect) || (selected_oneshot_animation != null && selected_oneshot_animation.AnimationFinished)) {
							HaltActivity (queue.Value, true);
							EmptyQueues.Add (queue.Key);
							deletionRequested = true;
						}
					}

					if (deletionRequested && queueWithMaskBlendingActive)
						update_after_deletion_needed = true;
					// Test case:
					//  color all 0 255 0
					//  brightness all 100
					//  overlay test identify && overlay test blending replace
					//  overlay voice-notif color all 255 0 0 keep && overlay voice-notif blending favor && overlay voice-notif brightness all 255
					// [wait a little while]
					//  overlay voice-notif brightness all 0 && overlay voice-notif color all 0 0 0 keep
					// [test layer should reappear -and- have the correct color/brightness values]
					// Continuing the test case:
					//  overlay clear_all
					// [only green as set above should show]

					foreach (string key in EmptyQueues) {
#if DEBUG_OVERLAY_MANAGEMENT
						Console.WriteLine (" {0} ms - removing overlay '{1}' as it is empty", Queue_PerfStopwatch.ElapsedMilliseconds, key);
#endif
						G35_Lights_Overlay_Queues.Remove (key);
					}
				}
				if (update_after_deletion_needed)
				{
					UpdateAllQueues ();
				}

				// Merge all layers together...
				Light_Snapshot = MergeSnapshotsDown (Light_Snapshots);
				// And clear the now out-of-date values for next run
				Light_Snapshots.Clear ();

				if (Light_Snapshot != null && ActiveOutputSystemReady ()) {
#if DEBUG_PERFORMANCE
					wasIdle = false;
#endif
					// There's a frame to play and the system is ready
					G35_Light_Queue_CurrentIdleWait = 1;
					// Reset the idle counter, used for implementing interval animations

#if DEBUG_PERFORMANCE
					Console.WriteLine ("{0} ms - frame generated", Queue_PerfStopwatch.ElapsedMilliseconds);
#endif
					int retriesSinceLastSuccess = 0;
					while (true) {
						if (retriesSinceLastSuccess >= 5) {
							throw new System.IO.IOException ("Could not reconnect to output system in background after 5 tries, giving up");
						}
						try {
							if (UpdateLights_All (Light_Snapshot.LED_Values) == false) {
								Console.WriteLine ("(Error while updating lights in the animation queue!)");
							}
							// It at least didn't throw an exception...
							retriesSinceLastSuccess = 0;
							// Exit this loop to allow further processing
							break;
						} catch (System.IO.IOException ex) {
							Console.WriteLine (DateTime.Now);
							Console.WriteLine ("\n ! Unexpected connection loss, attempting to reconnect...\n\n{0}\n", ex);
							ShutdownOutputSystem ();
							retriesSinceLastSuccess ++;
							Console.Write ("Waiting 5 seconds...");
							System.Threading.Thread.Sleep (5000);
							Console.WriteLine ("  Retrying.");

							// RunWithoutInteraction:  Usually someone won't be providing input when this happens
							// SkipPreparingSystem:  Queue and all that is already running
							if (InitializeSystem (true, true) == false)
							{
								throw new System.IO.IOException ("Could not reconnect to output system in background, giving up");
							}
							// Try again by nature of not calling break
						}
					}
#if DEBUG_PERFORMANCE
					Console.WriteLine ("{0} ms - frame sent", Queue_PerfStopwatch.ElapsedMilliseconds);
#endif
#if DEBUG_BRIEF_PERFORMANCE
					if (Queue_PerfStopwatch.ElapsedMilliseconds > Light_Animation_Latency)
						Console.WriteLine ("# {0} ms - frame finished ({1})", Queue_PerfStopwatch.ElapsedMilliseconds, DateTime.Now.ToLongTimeString ());
#endif
					// Attempt to keep each frame spaced 'Light_Animation_Delay' time apart, default 50ms
					SleepForAnimation ((int)Queue_PerfStopwatch.ElapsedMilliseconds);
#if DEBUG_PERFORMANCE
					Console.WriteLine ("# {0} ms - frame finished", Queue_PerfStopwatch.ElapsedMilliseconds);
#endif
#if DEBUG_VU_PERFORMANCE
					Console.WriteLine ("# {0} ms - frame finished", VU_Processing_PerfStopwatch.ElapsedMilliseconds);
					VU_Processing_PerfStopwatch.Stop ();
#endif
				} else {
#if DEBUG_PERFORMANCE
					if (wasIdle == false)
						Console.WriteLine ("# Idle ({0} ms loop)", G35_Light_Queue_CurrentIdleWait);
					if (G35_Light_Queue_CurrentIdleWait == G35_Light_Queue_MaxIdleWaitTime)
						wasIdle = true;
#endif
					System.Threading.Thread.Sleep (G35_Light_Queue_CurrentIdleWait);
					foreach (KeyValuePair <string, LED_Queue> queue in GetAllQueues ()) {
						queue.Value.QueueIdleTime += G35_Light_Queue_CurrentIdleWait;
					}
					if (G35_Light_Queue_CurrentIdleWait < G35_Light_Queue_MaxIdleWaitTime) {
						// Each loop spent in idle without events, increase the delay
						G35_Light_Queue_CurrentIdleWait *= G35_Light_Queue_IdleWaitMultiplier;
					} else if (G35_Light_Queue_CurrentIdleWait > G35_Light_Queue_MaxIdleWaitTime) {
						// ...but don't go above the maximum idle wait time
						G35_Light_Queue_CurrentIdleWait = G35_Light_Queue_MaxIdleWaitTime;
					}
				}
				if (G35_Lights_Queue.QueueCount >= G35_Light_Queue_BufferFullWarning && G35_Light_Queue_BufferFullWarning_Shown == false) {
					Console.WriteLine ("(Warning: the LED output queue holds over {0} frames, which will cause a delay in" +
						" response after a command)", G35_Light_Queue_BufferFullWarning);
					G35_Light_Queue_BufferFullWarning_Shown = true;
				} else if (G35_Lights_Queue.QueueCount < G35_Light_Queue_BufferFullWarning && G35_Light_Queue_BufferFullWarning_Shown == true) {
					Console.WriteLine ("(LED output queue now holds less than {0} frames)", G35_Light_Queue_BufferFullWarning);
					G35_Light_Queue_BufferFullWarning_Shown = false;
				}
				Queue_PerfStopwatch.Stop ();
			}
		}

		private static void UpdateAnimationStackForQueue (LED_Queue QueueToModify, long PerfTracking_TimeElapsed, string PerfTracking_QueueName)
		{
			if (QueueToModify.AnimationActive && QueueToModify.QueueEmpty) {
				// Only add an animation frame if enabled, and the queue is empty
				if ((QueueToModify.SelectedAnimation.RequestedAnimationDelay <= Light_Animation_Latency) ||
					(QueueToModify.QueueIdleTime >= QueueToModify.SelectedAnimation.RequestedAnimationDelay) ||
					(QueueToModify.AnimationForceFrameRequest == true)) {
					// Only add an animation frame if less than default delay is requested, or enough time elapsed in idle
#if DEBUG_PERFORMANCE
						Console.WriteLine ("{0} ms - queuing frame from active animation ({1})", PerfTracking_TimeElapsed, PerfTracking_QueueName);
#endif
					try {
						// In all of the below, you must set QueueToModify to the new, intended output, otherwise
						//  animation transitions will contain old values.
						if (QueueToModify.SelectedAnimation.EnableSmoothing && Animation_Fading_Enabled) {
							// Get the next frame, apply smoothing
							ApplySmoothing (QueueToModify.SelectedAnimation.SmoothingAmount, true, QueueToModify.SelectedAnimation.GetNextFrame (), QueueToModify);
							// ...and insert it into the queue.
							QueueToModify.PushToQueue ();
						} else if ((QueueToModify.AnimationForceFrameRequest == true) &&
							(QueueToModify.SelectedAnimation.RequestSmoothCrossfade)) {
							// Animation has a potentially-sharp change and requests a smooth cross-fade
							// Get the next frame...
							QueueToModify.Lights = QueueToModify.SelectedAnimation.GetNextFrame ();
							// ...and insert it into the queue with an animated transition.
							AddToAnimQueue (QueueToModify);
						} else {
							// Without smoothing, just directly add to the queue
							QueueToModify.Lights = QueueToModify.SelectedAnimation.GetNextFrame ();
							// But ensure that the local QueueToModify list is accurate
							QueueToModify.PushToQueue ();
						}
					} catch (System.InvalidOperationException) {
						Console.WriteLine ("(error playing animation, invalid operation encountered - setting queue to black)");
						// System.InvalidOperationException: AudioPlayer not running, can not determine current track position.
						// TODO: Better method of fixing this?
						//  For now, just stop the animation, and let it do whatever it was going to do.
						Animation_Stop (QueueToModify);
						FillLights_Color (QueueToModify, LightSystem.Color_MIN, LightSystem.Color_MIN, LightSystem.Color_MIN, false, true);
						FillLights_Brightness (QueueToModify, LightSystem.Brightness_MIN, false, true);
					}

					QueueToModify.AnimationForceFrameRequest = false;
					// Frame has been sent, reset the ForceFrameRequest flag
				}
			}
		}

		private static LED_Set MergeSnapshotsDown (List<LED_Set> LightSnapshots)
		{
			if (LightSnapshots.Count <= 0) {
				return null;
			}
			LED_Set MergedLayers = new LED_Set();
			for (int index = 0; index < LightSnapshots[0].LightCount; index++) {
				MergedLayers.LED_Values.Add (new LED (0, 0, 0, 0));
			}

			List<int> snapshots_requesting_replacement = new List<int> ();

			for (int i = 0; i < LightSnapshots.Count; i++) {
				if (LightSnapshots [i].BlendMode == LED.BlendingStyle.Favor || LightSnapshots [i].BlendMode == LED.BlendingStyle.Mask || LightSnapshots [i].BlendMode == LED.BlendingStyle.Replace) {
					snapshots_requesting_replacement.Add (i);
				} else {
					// Blend the layers together
					LightProcessing.MergeLayerDown (LightSnapshots [i].LED_Values, MergedLayers.LED_Values, LightSnapshots[i].BlendMode);
				}
			}

			// These must be done after standard blending modes due to the possibility of overriding the above
			foreach (int snapshot_index in snapshots_requesting_replacement) {
				LightProcessing.MergeLayerDown (LightSnapshots [snapshot_index].LED_Values, MergedLayers.LED_Values, LightSnapshots[snapshot_index].BlendMode);
			}
			return MergedLayers;
		}

		private static void WaitForQueue (LED_Queue QueueToModify)
		{
			WaitForQueue (QueueToModify, 0);
		}

		private static void WaitForQueue (LED_Queue QueueToModify, int MaximumRemainingEvents)
		{
			if (G35_Light_Queue_Thread != null) {
				if (G35_Light_Queue_Thread.IsAlive == true) {
#if DEBUG_PERFORMANCE
					if (QueueToModify.QueueCount > MaximumRemainingEvents)
						Console.WriteLine ("-- {0} events in queue, waiting until {1}...", QueueToModify.QueueCount, MaximumRemainingEvents);
#endif
					while (QueueToModify.QueueCount > MaximumRemainingEvents) {
						System.Threading.Thread.Sleep (5);
					}
				}
			}
		}

		private static void SleepForAnimation ()
		{
			SleepForAnimation (0);
		}

		private static void SleepForAnimation (int AlreadySleptAmount)
		{
			SleepForAnimation (AlreadySleptAmount, Light_Animation_Latency);
		}

		private static void SleepForAnimation (int AlreadySleptAmount, int RequestedDelay)
		{
#if DEBUG_PERFORMANCE
			Console.WriteLine ("{0} ms - already waited above requested (latency)", Math.Max ((AlreadySleptAmount - RequestedDelay), 0));
#endif
			if (Math.Max (RequestedDelay - AlreadySleptAmount, 0) > 0)
				System.Threading.Thread.Sleep (Math.Max (RequestedDelay - AlreadySleptAmount, 0));

			// Wait a preset amount of time.  This controls the animation speed
#if DEBUG_PERFORMANCE
			Console.WriteLine ("{0} ms - attempt at delay request", RequestedDelay + Math.Max ((AlreadySleptAmount - RequestedDelay), 0));
#endif
			// Don't let events build up, or there will be excessive lag between music and lights
		}

		/// <summary>
		/// Add the current Lights collection of LEDs to the queue, automatically smoothly transitioning into it.
		/// </summary>
		/// <param name="QueueToModify">Queue to add animation to</param>
		private static void AddToAnimQueue (LED_Queue QueueToModify)
		{
			AddToAnimQueue (QueueToModify, QueueToModify.Lights);
		}

		/// <summary>
		/// Add the collection of LEDs to the queue, automatically smoothly transitioning into it.
		/// </summary>
		/// <param name="QueueToModify">Queue to add animation to</param>
		/// <param name="LED_Collection">Desired LED appearance</param>
		private static void AddToAnimQueue (LED_Queue QueueToModify, List<LED> LED_Collection)
		{
			if (QueueToModify.QueueCount > 0 && Command_ConflictsExpected == false)
				Console.WriteLine ("(Warning: interrupting fade, appearance may vary.  If intended, prefix with '!')");
			// Don't warn about clearing the output queue if it's expected by running multiple commands at once
			QueueToModify.ClearQueue ();
			if (Animation_Fading_Enabled) {
				double Avg_OldPercent = Math.Min (Animation_Smoothing_Percentage_DEFAULT, 1);
				double Avg_NewPercent = Math.Max (1 - Animation_Smoothing_Percentage_DEFAULT, 0);
				List<LED> LED_Intermediate = new List<LED> ();
				lock (QueueToModify.LightsLastProcessed) {
					for (int i = 0; i < LightSystem.LIGHT_COUNT; i++) {
						LED_Intermediate.Add (new LED (QueueToModify.LightsLastProcessed [i].R, QueueToModify.LightsLastProcessed [i].G, QueueToModify.LightsLastProcessed [i].B, QueueToModify.LightsLastProcessed [i].Brightness));
					}
				}
				for (int i_fades = 0; i_fades < Animation_Smoothing_Iterations_DEFAULT; i_fades++) {
					for (int i = 0; i < LightSystem.LIGHT_COUNT; i++) {
						LED_Intermediate [i].R = (byte)((LED_Collection [i].R * Avg_NewPercent) + (LED_Intermediate [i].R * Avg_OldPercent));
						LED_Intermediate [i].G = (byte)((LED_Collection [i].G * Avg_NewPercent) + (LED_Intermediate [i].G * Avg_OldPercent));
						LED_Intermediate [i].B = (byte)((LED_Collection [i].B * Avg_NewPercent) + (LED_Intermediate [i].B * Avg_OldPercent));
						LED_Intermediate [i].Brightness = (byte)((LED_Collection [i].Brightness * Avg_NewPercent) + (LED_Intermediate [i].Brightness * Avg_OldPercent));
					}
					QueueToModify.PushToQueue (LED_Intermediate);
				}
				// Just in case the fade did not finish completely, ensure the desired state is sent, too
				QueueToModify.PushToQueue (LED_Collection);
			} else {
				QueueToModify.PushToQueue (LED_Collection);
			}
		}
		
		#endregion

		#region Animation Management

		private static void Animation_Play (LED_Queue QueueToModify, AbstractAnimation DesiredAnimation)
		{
			if (DesiredAnimation == null)
				throw new System.ArgumentNullException ("DesiredAnimation", "You must specify a valid animation to play.");
			if (QueueToModify.AnimationActive)
				Animation_Stop (QueueToModify);

			QueueToModify.SelectedAnimation = DesiredAnimation;
			if ((QueueToModify.SelectedAnimation.RequestedAnimationDelay > Light_Animation_Latency) || (QueueToModify.SelectedAnimation.RequestSmoothCrossfade))
				QueueToModify.AnimationForceFrameRequest = true;
			// If animation requests a longer delay or a smooth cross-fade, force the first frame to happen now

			if (QueueToModify.SelectedAnimation is AbstractReactiveAnimation) {
				// If animation reacts to music, start up the VU system
				Audio_Start_Capture ();
			}

			//Animation_Active = true;
		}

		private static void Animation_Stop (LED_Queue QueueToModify)
		{
			WaitForQueue (QueueToModify);
			if (QueueToModify.SelectedAnimation is AudioBitmapAnimation)
				(QueueToModify.SelectedAnimation as AudioBitmapAnimation).StopAudioSystem ();
			if (QueueToModify.SelectedAnimation is AbstractReactiveAnimation || Audio_Capture_Process != null) {
				// If animation reacts to music, shut down the VU system
				Audio_Stop_Capture ();
				Audio_Clear_Captured_Volumes ();
			}
			QueueToModify.SelectedAnimation = null;
		}

		#endregion

		#region VU Meter Management

		private static void Audio_Clear_Captured_Volumes ()
		{
			lock (Audio_Volumes) {
				if (Audio_Volumes.Count > 0) {
					if (ReactiveSystem.Processing_Show_Analysis)
						Console.WriteLine ();
					Console.WriteLine ("(Audio volume processing queue cleared)");
					Audio_Volumes.Clear ();
				}
			}
		}

		private static void Audio_Start_Capture ()
		{
			if (Audio_Capture_Process != null)
				Audio_Stop_Capture ();
			Audio_Capture_Process = new System.Diagnostics.Process ();
			Audio_Capture_Process.StartInfo = new System.Diagnostics.ProcessStartInfo ("python", "print_volume.py");

			Audio_Capture_Process.StartInfo.UseShellExecute = false;
			//Note: added to fix error
			
			Audio_Capture_Process.StartInfo.CreateNoWindow = true;

			// set up output redirection
			Audio_Capture_Process.StartInfo.RedirectStandardInput = true;
			// above prevents the process from grabbing Ctrl+C events
			Audio_Capture_Process.StartInfo.RedirectStandardOutput = true;
			Audio_Capture_Process.EnableRaisingEvents = true;
			// see below for output handler
			Audio_Capture_Process.OutputDataReceived += Audio_Capture_Process_DataReceived;
			
			
			Audio_Capture_Process.Start ();

			Audio_Capture_Process.BeginOutputReadLine ();

		}

		private static void Audio_Stop_Capture ()
		{
			if (Audio_Capture_Process != null)
			if (Audio_Capture_Process.HasExited == false)
				Audio_Capture_Process.Kill ();
		}

		private static void Audio_Capture_Process_DataReceived (object sender, System.Diagnostics.DataReceivedEventArgs e)
		{
			if (e.Data != null)
			if (e.Data.StartsWith ("audio_data:"))
				Audio_Add_Volumes (ParsePythonDoubleArray (e.Data.Replace ("audio_data:", "")));
		}

		private static void Audio_Add_Volumes (List<double> Current_VU_Volumes)
		{
			if (G35_Lights_Queue.AnimationActive == false || !(G35_Lights_Queue.SelectedAnimation is AbstractReactiveAnimation))
				return;
			lock (Audio_Volumes) {
				while (Audio_Volumes.Count < Current_VU_Volumes.Count) {
					Audio_Volumes.Add (0);
				}
				while (Audio_Volumes.Count > Current_VU_Volumes.Count) {
					Audio_Volumes.RemoveAt (Audio_Volumes.Count - 1);
				}
				for (int i = 0; i < Current_VU_Volumes.Count; i++) {
					Audio_Volumes [i] = Math.Max (Audio_Volumes [i], Current_VU_Volumes [i]);
				}
			}
		}

		/// <summary>
		/// Parses a Python array of doubles to a List<double>.
		/// </summary>
		/// <returns>
		/// A List<double>.
		/// </returns>
		/// <param name='PrintedArray'>
		/// The Python array, converted to a string.
		/// </param>
		private static List<double> ParsePythonDoubleArray (string PrintedArray)
		{
			if (String.IsNullOrEmpty (PrintedArray))
				return null;

			PrintedArray = PrintedArray.Replace (" ", "").Replace ("(", "").Replace (")", "");

			List<double> result = new List<double> ();
			foreach (string s in PrintedArray.Split(',')) {
				if (s != "")
					result.Add (Convert.ToDouble (s));
			}
			return result;
		}

		#endregion

		#region Generic Light Processing

		/// <summary>
		/// Applies smoothing to the provided queue from colors specified.
		/// </summary>
		/// <param name='SmoothingAmount'>
		/// Smoothing amount, percentage from 0 to 1, higher numbers = longer fade.
		/// </param>
		/// <param name='OnlySmoothBrightnessDecrease'>
		/// Only smooth brightness when it is decreasing.
		/// </param>
		/// <param name='QueueToModify'>
		/// Queue to apply the smoothed results to.
		/// </param> 
		private static void ApplySmoothing (double SmoothingAmount, bool OnlySmoothBrightnessDecrease, List<LED> Lights_Unsmoothed, LED_Queue QueueToModify)
		{
			double Avg_OldPercent = Math.Min (SmoothingAmount, 1);
			double Avg_NewPercent = Math.Max (1 - SmoothingAmount, 0);

			for (int i = 0; i < QueueToModify.LightCount; i++) {
				QueueToModify.Lights [i].R = (byte)((Lights_Unsmoothed [i].R * Avg_NewPercent) + (QueueToModify.Lights [i].R * Avg_OldPercent));
				QueueToModify.Lights [i].G = (byte)((Lights_Unsmoothed [i].G * Avg_NewPercent) + (QueueToModify.Lights [i].G * Avg_OldPercent));
				QueueToModify.Lights [i].B = (byte)((Lights_Unsmoothed [i].B * Avg_NewPercent) + (QueueToModify.Lights [i].B * Avg_OldPercent));
				QueueToModify.Lights [i].Brightness = (byte)((Lights_Unsmoothed [i].Brightness * Avg_NewPercent) + (QueueToModify.Lights [i].Brightness * Avg_OldPercent));
				if (OnlySmoothBrightnessDecrease) {
					QueueToModify.Lights [i].Brightness = (byte)Math.Max (Lights_Unsmoothed [i].Brightness, QueueToModify.Lights [i].Brightness);
				}
			}
		}


		#endregion

		private static void FillLights_Brightness (LED_Queue QueueToModify, byte Brightness)
		{
			FillLights_Brightness (QueueToModify, Brightness, true);
		}

		private static void FillLights_Brightness (LED_Queue QueueToModify, byte Brightness, bool ApplyNow)
		{
			FillLights_Brightness (QueueToModify, Brightness, ApplyNow, false);
		}

		private static void FillLights_Brightness (LED_Queue QueueToModify, byte Brightness, bool ApplyNow, bool SkipAnimationQueue)
		{
			for (int i = 0; i < QueueToModify.LightCount; i++) {
				QueueToModify.Lights [i].Brightness = Brightness;
			}
			if (ApplyNow) {
				if (SkipAnimationQueue) {
					QueueToModify.PushToQueue ();
				} else {
					AddToAnimQueue (QueueToModify);
				}
			}
		}

		private static void FillLights_Color (LED_Queue QueueToModify, G35_USB.Color SelectedColor)
		{
			FillLights_Color (QueueToModify, SelectedColor, true);
		}

		private static void FillLights_Color (LED_Queue QueueToModify, G35_USB.Color SelectedColor, bool ApplyNow)
		{
			FillLights_Color (QueueToModify, SelectedColor, ApplyNow, false);
		}

		private static void FillLights_Color (LED_Queue QueueToModify, G35_USB.Color SelectedColor, bool ApplyNow, bool SkipAnimationQueue)
		{
			FillLights_Color (QueueToModify, SelectedColor.R, SelectedColor.G, SelectedColor.B, ApplyNow, SkipAnimationQueue);
		}

		private static void FillLights_Color (LED_Queue QueueToModify, byte R, byte G, byte B)
		{
			FillLights_Color (QueueToModify, R, G, B, true);
		}

		private static void FillLights_Color (LED_Queue QueueToModify, byte R, byte G, byte B, bool ApplyNow)
		{
			FillLights_Color (QueueToModify, R, G, B, ApplyNow, false);
		}

		private static void FillLights_Color (LED_Queue QueueToModify, byte R, byte G, byte B, bool ApplyNow, bool SkipAnimationQueue)
		{
			for (int i = 0; i < QueueToModify.LightCount; i++) {
				QueueToModify.Lights [i].R = R;
				QueueToModify.Lights [i].G = G;
				QueueToModify.Lights [i].B = B;
			}
			if (ApplyNow) {
				if (SkipAnimationQueue) {
					QueueToModify.PushToQueue ();
				} else {
					AddToAnimQueue (QueueToModify);
				}
			}
		}

		private static void SetLight_Color (LED_Queue QueueToModify, int Index, byte R, byte G, byte B)
		{
			SetLight_Color (QueueToModify, Index, R, G, B, true);
		}

		private static void SetLight_Color (LED_Queue QueueToModify, int Index, byte R, byte G, byte B, bool ApplyNow)
		{
			SetLight_Color (QueueToModify, Index, R, G, B, ApplyNow, false);
		}

		private static void SetLight_Color (LED_Queue QueueToModify, int Index, byte R, byte G, byte B, bool ApplyNow, bool SkipAnimationQueue)
		{
			if (Index >= 0 & Index < QueueToModify.LightCount) {
				QueueToModify.Lights [Index].R = R;
				QueueToModify.Lights [Index].G = G;
				QueueToModify.Lights [Index].B = B;
			}
			if (ApplyNow) {
				if (SkipAnimationQueue) {
					QueueToModify.PushToQueue ();
				} else {
					AddToAnimQueue (QueueToModify);
				}
			}
		}

		private static bool UpdateLights_Brightness (List<LED> G35_Light_Set)
		{
			if (ActiveOutputSystemReady () == false)
				return false;

			return ActiveOutputSystem.UpdateLightsBrightness (G35_Light_Set);
		}

		private static bool UpdateLights_Color (List<LED> G35_Light_Set)
		{
			if (ActiveOutputSystemReady () == false)
				return false;

			return ActiveOutputSystem.UpdateLightsColor (G35_Light_Set);
		}

		private static bool UpdateLights_All (List<LED> G35_Light_Set)
		{
			if (ActiveOutputSystemReady () == false)
				return false;

			return ActiveOutputSystem.UpdateLightsAll (G35_Light_Set);
		}


		private static bool ActiveOutputSystemReady ()
		{
			return (ActiveOutputSystem != null && ActiveOutputSystem.Initialized);
		}

		private static bool InitializeOutputSystem (AbstractOutput DesiredOutputSystem)
		{
			if (DesiredOutputSystem == null)
				throw new ArgumentNullException ("DesiredOutputSystem", "DesiredOutputSystem must be specified to initialize the system");

			bool success = false;

			SkipInput = true;
			System.Threading.Thread skip_input_buffer = new System.Threading.Thread (FlushKeyboard);
			skip_input_buffer.IsBackground = true;
			skip_input_buffer.Start ();

			ActiveOutputSystem = DesiredOutputSystem;
			success = ActiveOutputSystem.InitializeSystem ();

			if (success) {
				// Don't allow a shorter animation time than the output system processing can manage
				Light_Animation_Latency = Math.Max (ActiveOutputSystem.ProcessingLatency + 1, Light_Animation_Target_Latency);

				string outputType = ActiveOutputSystem.GetType ().Name.Trim ();
				if (outputType == "") {
					outputType = "Unknown";
				}
				Console.WriteLine ("Connected to '{0}' via '{1}'", outputType, ActiveOutputSystem.Identifier);
			}

			SkipInput = false;
			if (skip_input_buffer.IsAlive)
				skip_input_buffer.Abort ();

			return success;
		}

		private static bool ShutdownOutputSystem ()
		{
			if (ActiveOutputSystem != null)
				return ActiveOutputSystem.ShutdownSystem ();
			return false;
		}

		private static bool ResetOutputSystem ()
		{
			if (ActiveOutputSystem == null)
				return false;

			if (ActiveOutputSystem.ResetSystem ()) {
				string outputType = ActiveOutputSystem.GetType ().Name.Trim ();
				if (outputType == "") {
					outputType = "Unknown";
				}
				Console.WriteLine ("Connected to '{0}' via '{1}'", outputType, ActiveOutputSystem.Identifier);

				return true;
			} else {
				return false;
			}
		}

	}
}
