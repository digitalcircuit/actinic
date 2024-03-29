//
//  Main.cs
//
//  Author:
//       Shane Synan <digitalcircuit36939@gmail.com>
//
//  Copyright (c) 2012 - 2016
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
//#define DEBUG_FORCE_DUMMYOUTPUT
//#define DEBUG_FORCE_DUMMYAUDIOINPUT

using System;
using System.IO.Ports;
using System.Collections.Generic;
using FoxSoft.Utilities;

// Animations
using Actinic.Animations;

// Input systems
using Actinic.AudioInputs;

// Output systems
using Actinic.Outputs;

// Rendering
using Actinic.Rendering;

namespace Actinic
{
	class MainClass
	{
		/// <summary>
		/// The parsed application arguments from launching the program.
		/// </summary>
		private static Parsing.ProgramArgs AppArgs;

		/// <summary>
		/// Exit code when everything goes well.
		/// </summary>
		private const int EXIT_SUCCESS = 0;
		/// <summary>
		/// Exit code when invalid command line arguments are given.
		/// </summary>
		private const int EXIT_ERROR_BAD_ARGS = 1;
		/// <summary>
		/// Exit code when a connected input or output device fails.
		/// </summary>
		private const int EXIT_ERROR_DEVICE_FAILURE = 2;
		/// <summary>
		/// Exit code when the input receiver for commands fails.
		/// </summary>
		private const int EXIT_ERROR_CMD_RECEIVER_FAILURE = 3;

		/// <summary>
		/// The input manager/receiver for all commands.
		/// </summary>
		private static Commands.CommandReceiver AppCommands;

		private static AbstractAudioInput ActiveAudioInputSystem;

		private static AbstractOutput ActiveOutputSystem;

		private static System.Threading.ManualResetEvent ActiveOutputSystemReadyEvent =
			new System.Threading.ManualResetEvent (false);

		/// <summary>
		/// The amount of additional time allowed for rendering before
		/// considering it an issue.  Used for debugging and performance tuning.
		/// <remarks>
		/// This depends on the speed of the computer and may need adjusted.
		/// </remarks>
		/// </summary>
		private const double Render_MaxLatency = 2;

		// Above should at least be slightly longer than the USB processing
		// time, to reduce the risk of the queue getting filled.
		// EDIT: With the new layered processing system, 48 ms may be too fast
		// of a target.  Trying 49 ms...

		/// <summary>
		/// If true, display average latency, otherwise hide it.
		/// </summary>
		private static bool Debug_Display_Latency = false;

		/// <summary>
		/// How often to print the average performance value.
		/// </summary>
		private static TimeSpan Debug_Perf_PrintInterval =
			new TimeSpan (0, 0, 1);

		/// <summary>
		/// The last time the average performance value was printed.
		/// </summary>
		private static DateTime Debug_Perf_LastPrint = DateTime.UtcNow;

		/// <summary>
		/// The current delay when idle in ms.
		/// </summary>
		private static int Actinic_Light_Queue_CurrentIdleWait = 1;

		/// <summary>
		/// The amount to increase idle delay when no events received in ms.
		/// </summary>
		private const int Actinic_Light_Queue_IdleWaitMultiplier = 2;

		/// <summary>
		/// The maximum amount of time the light queue will sleep when no events
		/// are received in ms.  Keeps reaction times low.
		/// </summary>
		private const int Actinic_Light_Queue_MaxIdleWaitTime = 128;

#if DEBUG_VU_PERFORMANCE
		private static System.Diagnostics.Stopwatch VU_Processing_PerfStopwatch = new System.Diagnostics.Stopwatch ();
#endif
		private static LED_Queue Actinic_Lights_Queue;
		private const string Actinic_Lights_Queue_Name = "base_layer";

		private static Dictionary<string, LED_Queue> Actinic_Lights_Overlay_Queues = new Dictionary<string, LED_Queue> ();

		private static System.Threading.Thread Actinic_Light_Queue_Thread;

		/// <summary>
		/// The number of animations making use of the audio input system.
		/// </summary>
		private static int Animation_Audio_Users = 0;

		/// <summary>
		/// Function that takes in a given layer and modifies it.
		/// </summary>
		delegate void LayerTransformer (ref Layer lights);

		/// <summary>
		/// The duration of the animation smoothing in milliseconds.
		/// </summary>
		private const int Animation_Smoothing_Duration = 350;

		private static bool Animation_Fading_Enabled = true;


		#region Menu Handling

		private static int Actinic_Light_Queue_BufferFullWarning {
			get {
				if (ActiveOutputSystem?.Configuration != null
					&& ActiveOutputSystem.Configuration.AverageLatency > 0) {
					return (int)((1 * 1000) / ActiveOutputSystem.Configuration.AverageLatency);
					// First # is in seconds
				} else {
					// Make a good guess
					return 100;
				}
				// Output a warning if number of frames reaches this amount
				// Reaching this makes future commands seem slower to respond
			}
		}

		private static bool Actinic_Light_Queue_BufferFullWarning_Shown = false;
		// And don't spam the console about it; only notify when reached, then when fixed

		private static AbstractAnimation.Style Animation_AnimationStyle = AbstractAnimation.Style.Moderate;
		private const string Main_Command_Help = "color, brightness, white, black, identify, anim, overlay, shift_outwards, vu, clear, queue, debug, reset, quit\n(tip: use '&&' to combine commands)";
		private const string Anim_Command_Help = "anim [play, seek, stop, fade, style]";
		private const string Overlay_Command_Help = "overlay [name_of_layer 'command', OR list, clear_all]";
		private const string Overlay_Layer_Command_Help = "overlay name_of_layer [color, brightness, identify, play, exists, blending, clear]";
		private const string VU_Command_Help = "vu [run, legacy_mode, OR display, hide, set_low, set_mid, set_high]";
		private const string VU_Run_Command_Help = "vu run [beat_pulse, rave_mood, spinner]";
		private const string VU_Legacy_Mode_Command_Help = "vu legacy_mode [auto_fast_beat, rainbow, rainbow_solid, rainbow_beat, rainbow_beat_bass, hueshift, hueshift_beat, moving_bars, moving_bars_spaced, stationary_bars, solid_rainbow_strobe, solid_white_strobe, solid_hueshift_strobe, single_rainbow_strobe, single_white_strobe]";
		private const string Debug_Command_Help = "debug [display]";
		private const string Queue_Command_Help = "queue [start, stop, clear, spam, test]";

		public static int Main (string [] args)
		{
			// Parse command-line arguments
			try {
				AppArgs = new Parsing.ProgramArgs (args);
			} catch (ArgumentException) {
				return EXIT_ERROR_BAD_ARGS;
			}
			if (AppArgs.ExitImmediately) {
				// Exit if the command-line arguments requested it
				return EXIT_SUCCESS;
			}

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
#if DEBUG_FORCE_DUMMYOUTPUT
			Console.WriteLine ("DEBUGGING:  Compiled with 'DEBUG_FORCE_DUMMYOUTPUT' enabled");
#endif

			Console.WriteLine ("Registering methods to receive commands...");
			try {
				AppCommands = new Commands.CommandReceiver (!AppArgs.NoConsole,
					AppArgs.HTTPServerEnabled, AppArgs.HTTPServerAddress);
			} catch (InvalidOperationException ex) {
				Console.Error.WriteLine ("Unable to listen for commands!");
				Console.Error.WriteLine ("Reason: {0}", ex.Message);
				if (ex.InnerException != null) {
					Console.Error.WriteLine ();
					Console.Error.WriteLine ("Error code: {0}", ex.InnerException);
				}
				return EXIT_ERROR_CMD_RECEIVER_FAILURE;
			}
			Console.WriteLine ("- Listening for commands from {0}",
				string.Join (", ", AppCommands.InputIdentifiers));

			if (PrepareAudioCapture (AppArgs.NoPrompts) == false) {
				return EXIT_ERROR_DEVICE_FAILURE;
			}

			int retriesSinceLastSuccess = 0;
			while (true) {
				if (retriesSinceLastSuccess >= 5) {
					Console.WriteLine ("Could not reconnect after 5 tries, giving up.");
					return EXIT_ERROR_DEVICE_FAILURE;
				}
				try {
					if (PrepareLightingOutput (AppArgs.NoPrompts) == false) {
						return EXIT_ERROR_DEVICE_FAILURE;
					}
					retriesSinceLastSuccess = 0;
					RunMenu ();
					ShutdownSystem ();
					return EXIT_SUCCESS;
				} catch (System.IO.IOException ex) {
					Console.WriteLine (DateTime.Now);
					Console.WriteLine ("\n ! Unexpected connection loss, attempting to reconnect...\n\n{0}\n", ex);
					retriesSinceLastSuccess++;
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
		private static bool PrepareLightingOutput (bool RunWithoutInteraction = false, bool SkipPreparingSystem = false)
		{
			Console.WriteLine ("Connecting to output system...");

#if DEBUG_FORCE_DUMMYOUTPUT
			Console.WriteLine ("DEBUGGING:  Forcing DummyOutput, not checking other options!");
			InitializeOutputSystem (new DummyOutput ());
#else
			bool retryAgain = true;
			bool success = false;
			while (retryAgain && success == false) {
				success = false;
				foreach (AbstractOutput output_system in ReflectiveEnumerator.GetFilteredEnumerableOfType
						 <AbstractOutput, IOutputDummy> (false)) {
					if (InitializeOutputSystem (output_system)) {
						success = true;
						break;
					}
				}
				if (success == false) {
					if (RunWithoutInteraction == false) {
						Console.WriteLine (
							"Could not open connection to output system, is " +
							"device plugged in?\n" +
							"(press 's' for simulation mode, 'r' to retry, " +
							"any other key to exit)"
						);
						switch (Console.ReadKey ().Key) {
						case ConsoleKey.S:
							foreach (AbstractOutput output_system in ReflectiveEnumerator.GetFilteredEnumerableOfType
									 <AbstractOutput, IOutputDummy> (true)) {
								if (InitializeOutputSystem (output_system)) {
									success = true;
									retryAgain = false;
									Console.WriteLine ("\n[Note]  Running in simulation mode; no commands will be sent to USB");
									break;
								}
							}
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
			}
#endif

			if (SkipPreparingSystem == false) {
				Console.WriteLine ("Preparing system...");

				Actinic_Lights_Queue.MarkAsProcessed ();
				Actinic_Lights_Queue.Lights.Fill (Color.Named ["black"]);
				if (UpdateLights_All (Actinic_Lights_Queue.Lights) == false) {
					Console.WriteLine ("! Error while updating lights, is something wrong?");
					// Can't do anything here, exit the application
					return false;
				}
				Actinic_Lights_Queue.MarkAsProcessed ();

				// Initialize the higher level system
				Actinic_Light_Start_Queue ();
			} else {
				// Clean up any existing animations as the output system has
				// been recreated
				HaltActivity (true);
			}

			// Set to a known state
			Actinic_Lights_Queue.Lights.Fill (Color.Named ["black"]);
			Actinic_Lights_Queue.PushToQueue ();

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
			Actinic_Lights_Queue.Lights.Fill (Color.Named ["black"]);
			UpdateLights_All (Actinic_Lights_Queue.Lights);
			Actinic_Light_Stop_Queue ();

			ShutdownOutputSystem ();
		}

		/// <summary>
		/// Runs the system menu, prompting for commands and doing actions.
		/// </summary>
		private static void RunMenu ()
		{
			// First-time around, show the command help
			string command = "help";
			// Used for range and color parsing
			List<int> selected_lights;
			Color selected_color;
			byte Brightness;
			// General
			List<string> commands = new List<string> ();

			// Main menu
			// Don't place anything after this loop
			while (true) {
				if (command != "") {
					if (command.Contains ("&&")) {
						commands = new List<string> (command.Replace ("@", "LITERAL_AT")
													 .Replace (" && ", "@")
													 .Split ('@'));
						for (int i = 0; i < commands.Count; i++) {
							commands [i] = commands [i].Replace ("LITERAL_AT", "@").Trim ();
						}
					} else {
						commands.Clear ();
						commands.Add (command);
					}
					foreach (string current_command in commands) {
						if (current_command == "")
							continue;
						// Ignore commands if output system isn't ready
						if (!ActiveOutputSystemReady ()) {
							Console.Write (
								"Waiting for output system to be ready...");
							ActiveOutputSystemReadyEvent.WaitOne ();
						}

						List<string> cmd_args = new List<string> (current_command.Split (' '));
						// Note: cmd_args will be modified by any parameter parsers.  If original count is needed, save
						// it here.
						if (cmd_args != null & cmd_args.Count > 0) {
							// TODO: Combine the regular and overlay commands into one unified set
							switch (cmd_args [0].ToLowerInvariant ()) {
							case "quit":
								// Exit from the menu function
								return;
							case "color":
								// Command selected, remove it from the arguments
								cmd_args.RemoveAt (0);

								if (cmd_args.Count == 1 && cmd_args [0].ToLowerInvariant () == "list") {
									// List available colors
									System.Text.StringBuilder color_list = new System.Text.StringBuilder ();
									int colors_since_newline = 0;
									foreach (string color_name in Color.Named.Keys) {
										color_list.Append (color_name + ",\t");
										if (colors_since_newline < 6) {
											++colors_since_newline;
										} else {
											colors_since_newline = 0;
											color_list.Append ("\n  ");
										}
									}
									// Trim off the ', ' at the end
									color_list = color_list.Remove (color_list.Length - 2, 2);
									Console.WriteLine ("> Named colors:\n  {0}\n> Custom colors can be specified with [red] [green] [blue]", color_list.ToString ());
								} else {
									// Get the selected range and color
									if (Actinic.Parsing.Parameter.GetRange (ref cmd_args, LightSystem.LIGHT_COUNT, out selected_lights, true) &&
										Actinic.Parsing.Parameter.GetColor (ref cmd_args, out selected_color, true)) {
										// Stop any running animations
										HaltActivity (false);
										// If there's an argument at the end, don't touch brightness
										bool keep_brightness = (cmd_args.Count > 0);

										AddToAnimQueue (Actinic_Lights_Queue,
											delegate (ref Layer lights) {
												// For each selected light, set the color and brightness (if not preserved)
												foreach (int light_index in selected_lights) {
													lights [light_index].SetColor (
														selected_color.R,
														selected_color.G,
														selected_color.B,
														// Preserve brightness by default
														lights [light_index].Brightness
													);
													if (!keep_brightness)
														lights [light_index].Brightness = (selected_color.Brightness);
												}
											}
										);
									} else {
										Console.WriteLine ("> color [range] [color] [optional: keep brightness]");
									}
								}
								break;
							case "brightness":
								// Command selected, remove it from the arguments
								cmd_args.RemoveAt (0);

								// Get the selected range and color
								if (Actinic.Parsing.Parameter.GetRange (ref cmd_args, LightSystem.LIGHT_COUNT, out selected_lights, true)) {
									// Stop any running animations
									HaltActivity (false);

									// Parse brightness if specified
									if (cmd_args.Count >= 1 && byte.TryParse (cmd_args [0], out Brightness)) {
										AddToAnimQueue (Actinic_Lights_Queue,
											delegate (ref Layer lights) {
												// For each selected light, set the brightness
												foreach (int light_index in selected_lights) {
													lights [light_index].Brightness = Brightness;
												}
											}
										);
									} else {
										Console.WriteLine ("(invalid numbers entered; type 'brightness' for help)");
									}
								} else {
									Console.WriteLine (
										"> brightness [range] [brightness from '{0}' to '{1}']",
										LightSystem.Brightness_MIN, LightSystem.Brightness_MAX
									);
								}
								break;
							case "white":
								HaltActivity (false);
								AddToAnimQueue (Actinic_Lights_Queue,
									delegate (ref Layer lights) {
										lights.Fill (Color.Named ["white"]);
									}
								);
								break;
							case "black":
								HaltActivity (false);
								AddToAnimQueue (Actinic_Lights_Queue,
									delegate (ref Layer lights) {
										lights.Fill (Color.Transparent);
									}
								);
								break;
							case "identify":
								HaltActivity (false);

								AddToAnimQueue (Actinic_Lights_Queue,
									delegate (ref Layer lights) {
										lights.Fill (Color.Named ["black"]);
										lights [0].SetColor (Color.Named ["red"]);
										lights [1].SetColor (Color.Named ["yellow"]);

										lights [LightSystem.LIGHT_INDEX_MIDDLE - 1]
											.SetColor (Color.Named ["yellow"]);
										lights [LightSystem.LIGHT_INDEX_MIDDLE]
											.SetColor (Color.Named ["purple"]);
										lights [LightSystem.LIGHT_INDEX_MIDDLE + 1]
											.SetColor (Color.Named ["blue"]);
										lights [LightSystem.LIGHT_INDEX_MIDDLE + 2]
											.SetColor (Color.Named ["yellow"]);

										lights [LightSystem.LIGHT_COUNT - 2]
											.SetColor (Color.Named ["yellow"]);
										lights [LightSystem.LIGHT_COUNT - 1]
											.SetColor (Color.Named ["green"]);
									}
								);
								break;
							case "anim":
								if (cmd_args.Count > 1 && cmd_args [1] != null) {
									switch (cmd_args [1].ToLowerInvariant ()) {
									case "play":
										if (cmd_args.Count > 2 && cmd_args [2] != null) {
											switch (cmd_args [2].ToLowerInvariant ()) {
											case "simple":
												if (cmd_args.Count > 3 && cmd_args [3] != null) {
													switch (cmd_args [3].ToLowerInvariant ()) {
													case "fade":
														HaltActivity (false);
														SimpleFadeAnimation fade_animator = new SimpleFadeAnimation (ActiveOutputSystem.Configuration, Actinic_Lights_Queue.LightsLastProcessed);
														fade_animator.AnimationStyle = Animation_AnimationStyle;
														Animation_Play (Actinic_Lights_Queue, fade_animator);
														break;
													case "interval":
														if (cmd_args.Count > 4 && cmd_args [4] != null) {
															HaltActivity (false);
															IntervalAnimation time_animator = new IntervalAnimation (ActiveOutputSystem.Configuration, Actinic_Lights_Queue.LightsLastProcessed);
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
															Animation_Play (Actinic_Lights_Queue, time_animator);
														} else {
															Console.WriteLine ("> anim play simple interval [time, weather]");
														}
														break;
													case "strobe":
														if (cmd_args.Count > 4 && cmd_args [4] != null) {
															HaltActivity (false);
															SimpleStrobeAnimation strobe_animator = new SimpleStrobeAnimation (ActiveOutputSystem.Configuration, Actinic_Lights_Queue.LightsLastProcessed);
															switch (cmd_args [4].ToLowerInvariant ()) {
															case "white":
																strobe_animator.SelectedStrobeMode = SimpleStrobeAnimation.StrobeMode.White;
																break;
															case "color":
																strobe_animator.SelectedStrobeMode = SimpleStrobeAnimation.StrobeMode.Color;
																break;
															case "single":
																strobe_animator.SelectedStrobeMode = SimpleStrobeAnimation.StrobeMode.SingleWhite;
																break;
															case "single_color":
																strobe_animator.SelectedStrobeMode = SimpleStrobeAnimation.StrobeMode.SingleColor;
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
																Console.WriteLine ("> anim play simple strobe [white, color, single, single_color, fireflies, rain, thunderstorm]");
																break;
															}
															strobe_animator.AnimationStyle = Animation_AnimationStyle;
															Animation_Play (Actinic_Lights_Queue, strobe_animator);
														} else {
															Console.WriteLine ("> anim play simple strobe [white, color, single, single_color, fireflies, rain, thunderstorm]");
														}
														break;
													case "spinner":
														HaltActivity (false);
														SimpleSpinnerAnimation spinner_animator = new SimpleSpinnerAnimation (ActiveOutputSystem.Configuration, Actinic_Lights_Queue.LightsLastProcessed);
														spinner_animator.AnimationStyle = Animation_AnimationStyle;
														Animation_Play (Actinic_Lights_Queue, spinner_animator);
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
												if (cmd_args.Count > 3 && cmd_args [3] != null) {
													switch (cmd_args [3].ToLowerInvariant ()) {
													case "basic":
														if (cmd_args.Count > 4 && cmd_args [4] != null) {
															string file_name = cmd_args [4];
															if (cmd_args.Count > 5) {
																// If there's spaces in the file-name, concatenate them together
																for (int i = 5; i < cmd_args.Count; i++) {
																	file_name += " " + cmd_args [i];
																}
															}
															if (System.IO.File.Exists (file_name)) {
																try {
																	HaltActivity (false);
																	BitmapAnimation imageAnimation = new BitmapAnimation (ActiveOutputSystem.Configuration, file_name);
																	imageAnimation.AnimationStyle = Animation_AnimationStyle;
																	Console.WriteLine ("(animation successfully loaded)");
																	Animation_Play (Actinic_Lights_Queue, imageAnimation);
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
														if (cmd_args.Count > 4 && cmd_args [4] != null) {
															string file_name = cmd_args [4];
															if (cmd_args.Count > 5) {
																// If there's spaces in the file-name, concatenate them together
																for (int i = 5; i < cmd_args.Count; i++) {
																	file_name += " " + cmd_args [i];
																}
															}
															if (System.IO.File.Exists (file_name)) {
																try {
																	HaltActivity (false);
																	AudioBitmapAnimation imageAnimation = new AudioBitmapAnimation (ActiveOutputSystem.Configuration, file_name);
																	imageAnimation.AnimationStyle = Animation_AnimationStyle;
																	Console.WriteLine ("(animation successfully loaded)");
																	Animation_Play (Actinic_Lights_Queue, imageAnimation);
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
										if (cmd_args.Count > 2 && cmd_args [2] != null) {
											if (Actinic_Lights_Queue.AnimationActive && Actinic_Lights_Queue.SelectedAnimation is AudioBitmapAnimation) {
												double time_seek;
												double.TryParse (cmd_args [2], out time_seek);
												(Actinic_Lights_Queue.SelectedAnimation as AudioBitmapAnimation).SeekToPosition (time_seek);
											} else {
												Console.WriteLine ("(Can't seek, no audio animation is playing)");
											}
										} else {
											Console.WriteLine ("> anim seek [time in seconds]");
										}
										break;
									case "stop":
										Animation_Stop (Actinic_Lights_Queue);
										break;
									case "fade":
										if (cmd_args.Count > 2 && cmd_args [2] != null) {
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
										if (cmd_args.Count > 2 && cmd_args [2] != null) {
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
												foreach (KeyValuePair<string, LED_Queue> queue in GetAllQueues ()) {
													if (queue.Value.AnimationActive) {
														queue.Value.SelectedAnimation.AnimationStyle = Animation_AnimationStyle;
														// Force a new frame if the animation delay is greater, or a smooth crossfade is requested
														if ((queue.Value.SelectedAnimation.RequestedAnimationDelay > 0) || (queue.Value.SelectedAnimation.RequestSmoothCrossfade))
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
								if (cmd_args.Count > 1 && cmd_args [1] != null) {
									switch (cmd_args [1].ToLowerInvariant ()) {
									case "list":
										lock (Actinic_Lights_Overlay_Queues) {
											if (Actinic_Lights_Overlay_Queues.Count == 0) {
												Console.WriteLine ("(no overlay layers active)");
											} else {
												Console.Write ("(currently active layers:");
												foreach (KeyValuePair<string, LED_Queue> queue in Actinic_Lights_Overlay_Queues) {
													Console.Write (" '{0}'", queue.Key);
												}
												Console.WriteLine (")");
											}
										}
										break;
									case "clear_all":
										lock (Actinic_Lights_Overlay_Queues) {
											foreach (KeyValuePair<string, LED_Queue> queue in Actinic_Lights_Overlay_Queues) {
												HaltActivity (queue.Value);
											}
											Actinic_Lights_Overlay_Queues.Clear ();
										}
										UpdateAllQueues ();
										break;
									default:
										if (cmd_args.Count > 2 && cmd_args [2] != null) {
											string overlay_name = cmd_args [1].ToLowerInvariant ();
											switch (overlay_name) {
											case Actinic_Lights_Queue_Name:
												Console.WriteLine ("(layer '{0}' already used as the default layer; pick another name)", Actinic_Lights_Queue_Name);
												break;
											default:
												switch (cmd_args [2].ToLowerInvariant ()) {
												case "color":
													lock (Actinic_Lights_Overlay_Queues) {
														LED_Queue resulting_queue = GetQueueByName (overlay_name);
														if (resulting_queue != null) {
															// Mapping from usual 'color' command:  Numbers 5-6 -> 7-8
															// Command selected, remove 'overlay layer_name command' from the arguments
															cmd_args.RemoveRange (0, 3);

															// Get the selected range and color
															if (Actinic.Parsing.Parameter.GetRange (ref cmd_args, LightSystem.LIGHT_COUNT, out selected_lights, true) &&
																Actinic.Parsing.Parameter.GetColor (ref cmd_args, out selected_color, true)) {
																// Stop any running animations on this queue
																HaltActivity (resulting_queue);
																// If there's an argument at the end, don't touch brightness
																bool keep_brightness = (cmd_args.Count > 0);

																AddToAnimQueue (resulting_queue,
																	delegate (ref Layer lights) {
																		// For each selected light, set the color and brightness (if not preserved)
																		foreach (int light_index in selected_lights) {
																			lights [light_index].SetColor (
																				selected_color.R,
																				selected_color.G,
																				selected_color.B,
																				// Preserve brightness by default
																				lights [light_index].Brightness
																			);
																			if (!keep_brightness)
																				lights [light_index].Brightness = (selected_color.Brightness);
																		}
																	}
																);
															} else {
																Console.WriteLine (
																	"> overlay {0} color [range] [color] [optional: keep brightness]",
																	overlay_name
																);
															}
														}
													}
													break;
												case "brightness":
													lock (Actinic_Lights_Overlay_Queues) {
														LED_Queue resulting_queue = GetQueueByName (overlay_name);
														if (resulting_queue != null) {
															// Mapping from usual 'brightness' command:  Numbers 3 -> 5
															// Command selected, remove 'overlay layer_name command' from the arguments
															cmd_args.RemoveRange (0, 3);

															// Get the selected range and color
															if (Actinic.Parsing.Parameter.GetRange (ref cmd_args, LightSystem.LIGHT_COUNT, out selected_lights, true)) {
																// Stop any running animations
																HaltActivity (resulting_queue);

																// Parse brightness if specified
																if (cmd_args.Count >= 1 && byte.TryParse (cmd_args [0], out Brightness)) {
																	AddToAnimQueue (resulting_queue,
																		delegate (ref Layer lights) {
																			// For each selected light, set the brightness
																			foreach (int light_index in selected_lights) {
																				lights [light_index].Brightness = Brightness;
																			}
																		}
																	);
																} else {
																	Console.WriteLine ("(invalid numbers entered; type 'brightness' for help)");
																}
															} else {
																Console.WriteLine (
																	"> overlay {2} brightness [range] [brightness from '{0}' to '{1}']",
																	LightSystem.Brightness_MIN, LightSystem.Brightness_MAX,
																	overlay_name
																);
															}
														}
													}
													break;
												case "identify":
													lock (Actinic_Lights_Overlay_Queues) {
														LED_Queue resulting_queue = GetQueueByName (overlay_name);
														if (resulting_queue != null) {
															// Mapping from usual 'identify' command:  Numbers 1 -> 3
															HaltActivity (resulting_queue);

															AddToAnimQueue (resulting_queue,
																delegate (ref Layer lights) {
																	lights.Fill (Color.Named ["black"]);
																	lights [0].SetColor (Color.Named ["red"]);
																	lights [1].SetColor (Color.Named ["yellow"]);

																	lights [LightSystem.LIGHT_INDEX_MIDDLE - 1]
																		.SetColor (Color.Named ["yellow"]);
																	lights [LightSystem.LIGHT_INDEX_MIDDLE]
																		.SetColor (Color.Named ["purple"]);
																	lights [LightSystem.LIGHT_INDEX_MIDDLE + 1]
																		.SetColor (Color.Named ["blue"]);
																	lights [LightSystem.LIGHT_INDEX_MIDDLE + 2]
																		.SetColor (Color.Named ["yellow"]);

																	lights [LightSystem.LIGHT_COUNT - 2]
																		.SetColor (Color.Named ["yellow"]);
																	lights [LightSystem.LIGHT_COUNT - 1]
																		.SetColor (Color.Named ["green"]);
																}
															);
														}
													}
													break;
												case "play":
													lock (Actinic_Lights_Overlay_Queues) {
														LED_Queue resulting_queue = GetQueueByName (overlay_name);
														if (resulting_queue != null) {
															// Mapping: 2 -> 3
															if (cmd_args.Count > 3 && cmd_args [3] != null) {
																switch (cmd_args [3].ToLowerInvariant ()) {
																case "simple":
																	if (cmd_args.Count > 4 && cmd_args [4] != null) {
																		switch (cmd_args [4].ToLowerInvariant ()) {
																		case "strobe":
																			if (cmd_args.Count > 5 && cmd_args [5] != null) {
																				HaltActivity (resulting_queue);
																				SimpleStrobeAnimation strobe_animator = new SimpleStrobeAnimation (ActiveOutputSystem.Configuration, resulting_queue.LightsLastProcessed);
																				switch (cmd_args [5].ToLowerInvariant ()) {
																				case "white":
																					strobe_animator.SelectedStrobeMode = SimpleStrobeAnimation.StrobeMode.White;
																					break;
																				case "color":
																					strobe_animator.SelectedStrobeMode = SimpleStrobeAnimation.StrobeMode.Color;
																					break;
																				case "single":
																					strobe_animator.SelectedStrobeMode = SimpleStrobeAnimation.StrobeMode.SingleWhite;
																					break;
																				case "single_color":
																					strobe_animator.SelectedStrobeMode = SimpleStrobeAnimation.StrobeMode.SingleColor;
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
																					Console.WriteLine ("> overlay {0} play simple strobe [white, color, single, single_color, fireflies, rain, thunderstorm]", overlay_name);
																					break;
																				}
																				strobe_animator.AnimationStyle = Animation_AnimationStyle;
																				Animation_Play (resulting_queue, strobe_animator);
																			} else {
																				Console.WriteLine ("> overlay {0} play simple strobe [white, color, single, single_color, fireflies, rain, thunderstorm]", overlay_name);
																			}
																			break;
																		case "spinner":
																			HaltActivity (resulting_queue);
																			SimpleSpinnerAnimation spinner_animator = new SimpleSpinnerAnimation (ActiveOutputSystem.Configuration, resulting_queue.LightsLastProcessed);
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
																	if (cmd_args.Count > 4 && cmd_args [4] != null) {
																		switch (cmd_args [4].ToLowerInvariant ()) {
																		case "basic":
																			if (cmd_args.Count > 5 && cmd_args [5] != null) {
																				string file_name = cmd_args [5];
																				if (cmd_args.Count > 6) {
																					// If there's spaces in the file-name, concatenate them together
																					for (int i = 6; i < cmd_args.Count; i++) {
																						file_name += " " + cmd_args [i];
																					}
																				}
																				if (System.IO.File.Exists (file_name)) {
																					try {
																						HaltActivity (resulting_queue);
																						BitmapAnimation imageAnimation = new BitmapAnimation (ActiveOutputSystem.Configuration, file_name);
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
																			if (cmd_args.Count > 5 && cmd_args [5] != null) {
																				string file_name = cmd_args [5];
																				if (cmd_args.Count > 6) {
																					// If there's spaces in the file-name, concatenate them together
																					for (int i = 6; i < cmd_args.Count; i++) {
																						file_name += " " + cmd_args [i];
																					}
																				}
																				if (System.IO.File.Exists (file_name)) {
																					try {
																						HaltActivity (resulting_queue);
																						AudioBitmapAnimation imageAnimation = new AudioBitmapAnimation (ActiveOutputSystem.Configuration, file_name);
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
													if (cmd_args.Count > 3 && cmd_args [3] != null) {
														bool blending_changed = true;
														LED_Queue resulting_queue = GetQueueByName (overlay_name);
														if (resulting_queue != null) {
															switch (cmd_args [3].ToLowerInvariant ()) {
															case "combine":
																resulting_queue.BlendMode = Color.BlendMode.Combine;
																break;
															case "favor":
																resulting_queue.BlendMode = Color.BlendMode.Favor;
																break;
															case "mask":
																resulting_queue.BlendMode = Color.BlendMode.Mask;
																break;
															case "replace":
																resulting_queue.BlendMode = Color.BlendMode.Replace;
																break;
															case "sum":
																resulting_queue.BlendMode = Color.BlendMode.Sum;
																break;
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
													lock (Actinic_Lights_Overlay_Queues) {
														if (Actinic_Lights_Overlay_Queues.ContainsKey (overlay_name)) {
															Console.WriteLine ("(layer '{0}' exists)", overlay_name);
														} else {
															Console.WriteLine ("(layer '{0}' does not exist)", overlay_name);
														}
													}
													break;
												case "clear":
													lock (Actinic_Lights_Overlay_Queues) {
														if (Actinic_Lights_Overlay_Queues.ContainsKey (overlay_name)) {
															LED_Queue queue_to_remove = GetQueueByName (overlay_name);
															HaltActivity (queue_to_remove);
															Actinic_Lights_Overlay_Queues.Remove (overlay_name);
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
								if (cmd_args.Count == 2) {
									HaltActivity (false);
									int Shift_Amount = Convert.ToByte (cmd_args [1]);
									AddToAnimQueue (Actinic_Lights_Queue,
										delegate (ref Layer lights) {
											LightProcessing.ShiftLightsOutward (
												lights, Shift_Amount);
										}
									);
								} else {
									Console.WriteLine ("> shift_outwards [number of times]");
								}
								break;
							case "vu":
								if (cmd_args.Count > 1 && cmd_args [1] != null) {
									switch (cmd_args [1].ToLowerInvariant ()) {
									case "run":
										// For the newer AbstractReactiveAnimation types
										//  Automatically starts the system, resetting if an existing animation was running
										if (cmd_args.Count == 3) {
											// Prepare the animation
											//  Animation_Play will automatically enable the VU system
											switch (cmd_args [2]) {
											case "beat_pulse":
												// Keep audio input active
												HaltActivity (false, true);
												BeatPulseReactiveAnimation beatpulse_animator = new BeatPulseReactiveAnimation (ActiveOutputSystem.Configuration, Actinic_Lights_Queue.LightsLastProcessed);
												beatpulse_animator.AnimationStyle = Animation_AnimationStyle;
												Animation_Play (Actinic_Lights_Queue, beatpulse_animator);
												break;
											case "spinner":
												// Keep audio input active
												HaltActivity (false, true);
												SpinnerReactiveAnimation spinner_animator = new SpinnerReactiveAnimation (ActiveOutputSystem.Configuration, Actinic_Lights_Queue.LightsLastProcessed);
												spinner_animator.AnimationStyle = Animation_AnimationStyle;
												Animation_Play (Actinic_Lights_Queue, spinner_animator);
												break;
											case "rave_mood":
												// Keep audio input active
												HaltActivity (false, true);
												RaveMoodReactiveAnimation ravemood_animator = new RaveMoodReactiveAnimation (ActiveOutputSystem.Configuration, Actinic_Lights_Queue.LightsLastProcessed);
												ravemood_animator.AnimationStyle = Animation_AnimationStyle;
												Animation_Play (Actinic_Lights_Queue, ravemood_animator);
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
										if (cmd_args.Count == 3) {
											if (Actinic_Lights_Queue.AnimationActive == false || !(Actinic_Lights_Queue.SelectedAnimation is LegacyReactiveAnimation)) {
												Console.WriteLine ("(Starting legacy VU animation...)");

												// Keep audio input active
												HaltActivity (false, true);
												LegacyReactiveAnimation legacy_animator = new LegacyReactiveAnimation (ActiveOutputSystem.Configuration, Actinic_Lights_Queue.LightsLastProcessed);
												legacy_animator.AnimationStyle = Animation_AnimationStyle;
												Animation_Play (Actinic_Lights_Queue, legacy_animator);
											}
											switch (cmd_args [2]) {
											case "auto_fast_beat":
												(Actinic_Lights_Queue.SelectedAnimation as LegacyReactiveAnimation).VU_Selected_Mode = LegacyReactiveAnimation.VU_Meter_Mode.AutomaticFastBeat;
												//VU_Selected_Mode = VU_Meter_Mode.AutomaticFastBeat;
												break;
											case "rainbow":
												(Actinic_Lights_Queue.SelectedAnimation as LegacyReactiveAnimation).VU_Selected_Mode = LegacyReactiveAnimation.VU_Meter_Mode.Rainbow;
												//VU_Selected_Mode = VU_Meter_Mode.Rainbow;
												break;
											case "rainbow_solid":
												(Actinic_Lights_Queue.SelectedAnimation as LegacyReactiveAnimation).VU_Selected_Mode = LegacyReactiveAnimation.VU_Meter_Mode.RainbowSolid;
												//VU_Selected_Mode = VU_Meter_Mode.RainbowSolid;
												break;
											case "rainbow_beat":
												(Actinic_Lights_Queue.SelectedAnimation as LegacyReactiveAnimation).VU_Selected_Mode = LegacyReactiveAnimation.VU_Meter_Mode.RainbowBeat;
												//VU_Selected_Mode = VU_Meter_Mode.RainbowBeat;
												break;
											case "rainbow_beat_bass":
												(Actinic_Lights_Queue.SelectedAnimation as LegacyReactiveAnimation).VU_Selected_Mode = LegacyReactiveAnimation.VU_Meter_Mode.RainbowBeatBass;
												//VU_Selected_Mode = VU_Meter_Mode.RainbowBeatBass;
												break;
											case "hueshift":
												(Actinic_Lights_Queue.SelectedAnimation as LegacyReactiveAnimation).VU_Selected_Mode = LegacyReactiveAnimation.VU_Meter_Mode.Hueshift;
												//VU_Selected_Mode = VU_Meter_Mode.Hueshift;
												break;
											case "hueshift_beat":
												(Actinic_Lights_Queue.SelectedAnimation as LegacyReactiveAnimation).VU_Selected_Mode = LegacyReactiveAnimation.VU_Meter_Mode.HueshiftBeat;
												//VU_Selected_Mode = VU_Meter_Mode.HueshiftBeat;
												break;
											case "moving_bars":
												(Actinic_Lights_Queue.SelectedAnimation as LegacyReactiveAnimation).VU_Selected_Mode = LegacyReactiveAnimation.VU_Meter_Mode.MovingBars;
												//VU_Selected_Mode = VU_Meter_Mode.MovingBars;
												break;
											case "moving_bars_spaced":
												(Actinic_Lights_Queue.SelectedAnimation as LegacyReactiveAnimation).VU_Selected_Mode = LegacyReactiveAnimation.VU_Meter_Mode.MovingBarsEquallySpaced;
												//VU_Selected_Mode = VU_Meter_Mode.MovingBarsEquallySpaced;
												break;
											case "stationary_bars":
												(Actinic_Lights_Queue.SelectedAnimation as LegacyReactiveAnimation).VU_Selected_Mode = LegacyReactiveAnimation.VU_Meter_Mode.StationaryBars;
												//VU_Selected_Mode = VU_Meter_Mode.StationaryBars;
												break;
											case "solid_rainbow_strobe":
												(Actinic_Lights_Queue.SelectedAnimation as LegacyReactiveAnimation).VU_Selected_Mode = LegacyReactiveAnimation.VU_Meter_Mode.SolidRainbowStrobe;
												//VU_Selected_Mode = VU_Meter_Mode.SolidRainbowStrobe;
												break;
											case "solid_white_strobe":
												(Actinic_Lights_Queue.SelectedAnimation as LegacyReactiveAnimation).VU_Selected_Mode = LegacyReactiveAnimation.VU_Meter_Mode.SolidWhiteStrobe;
												//VU_Selected_Mode = VU_Meter_Mode.SolidWhiteStrobe;
												break;
											case "solid_hueshift_strobe":
												(Actinic_Lights_Queue.SelectedAnimation as LegacyReactiveAnimation).VU_Selected_Mode = LegacyReactiveAnimation.VU_Meter_Mode.SolidHueshiftStrobe;
												//VU_Selected_Mode = VU_Meter_Mode.SolidHueshiftStrobe;
												break;
											case "single_rainbow_strobe":
												(Actinic_Lights_Queue.SelectedAnimation as LegacyReactiveAnimation).VU_Selected_Mode = LegacyReactiveAnimation.VU_Meter_Mode.SolidSingleRainbowStrobe;
												//VU_Selected_Mode = VU_Meter_Mode.SolidSingleRainbowStrobe;
												break;
											case "single_white_strobe":
												(Actinic_Lights_Queue.SelectedAnimation as LegacyReactiveAnimation).VU_Selected_Mode = LegacyReactiveAnimation.VU_Meter_Mode.SolidSingleWhiteStrobe;
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
										if (cmd_args.Count > 2 && cmd_args [2] != null) {
											switch (cmd_args [2].ToLowerInvariant ()) {
											case "meter":
												if (cmd_args.Count > 3 && cmd_args [3] != null) {
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
												if (cmd_args.Count > 3 && cmd_args [3] != null) {
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
												if (cmd_args.Count > 3 && cmd_args [3] != null) {
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
												if (cmd_args.Count > 3 && cmd_args [3] != null) {
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
										if (cmd_args.Count == 3) {
											ReactiveSystem.Audio_Volume_Low_Percentage = Math.Max (Convert.ToDouble (cmd_args [2]), 0);
										} else {
											Console.WriteLine ("> vu set_low [percentage of frequencies for low, current " + ReactiveSystem.Audio_Volume_Low_Percentage.ToString () + ", larger (up to 1) = skip more]");
										}
										break;
									case "set_mid":
										if (cmd_args.Count == 3) {
											ReactiveSystem.Audio_Volume_Mid_Percentage = Math.Max (Convert.ToDouble (cmd_args [2]), 0);
										} else {
											Console.WriteLine ("> vu set_mid [percentage of frequencies for mid, current " + ReactiveSystem.Audio_Volume_Mid_Percentage.ToString () + ", larger (up to 1) = more]");
										}
										break;
									case "set_high":
										if (cmd_args.Count == 3) {
											ReactiveSystem.Audio_Volume_High_Percentage = Math.Max (Convert.ToDouble (cmd_args [2]), 0);
										} else {
											Console.WriteLine ("> vu set_high [percentage of frequencies for high, current " + ReactiveSystem.Audio_Volume_High_Percentage.ToString () + ", smaller (from 0 to 1) = more]");
										}
										break;
									case "set_frequency_start":
										if (cmd_args.Count == 3) {
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
								if (cmd_args.Count > 1 && cmd_args [1] != null) {
									switch (cmd_args [1].ToLowerInvariant ()) {
									case "start":
										Actinic_Light_Start_Queue ();
										Console.WriteLine ("(Actinic light queue manually started; queued events cleared)");
										break;
									case "stop":
										Actinic_Light_Stop_Queue ();
										Console.WriteLine ("(Actinic light queue forcibly stopped)");
										break;
									case "clear":
										Actinic_Lights_Queue.ClearQueue ();
										Console.WriteLine ("(Actinic light queue cleared)");
										break;
									case "test":
										Actinic_Lights_Queue.Lights.Fill (Color.Named ["black"]);
										Actinic_Lights_Queue.PushToQueue ();
										WaitForQueue (Actinic_Lights_Queue);
										Console.WriteLine ("(Actinic light queue test: synchronous)");
										for (int i = 0; i < Actinic_Lights_Queue.Lights.PixelCount; i++) {
											Actinic_Lights_Queue.Lights [i].R = LightSystem.Color_MAX;
											Actinic_Lights_Queue.Lights [i].G = LightSystem.Color_MIN;
											Actinic_Lights_Queue.Lights [i].B = LightSystem.Color_MAX;
											Actinic_Lights_Queue.Lights [i].Brightness = LightSystem.Brightness_MAX;
											Actinic_Lights_Queue.PushToQueue ();
											Console.Write ("X");
											WaitForQueue (Actinic_Lights_Queue);
											Console.Write ("_");
										}
										Console.WriteLine ("");
										System.Threading.Thread.Sleep (500);
										Console.WriteLine ("(Actinic lights queue test: asynchronous)");
										Console.WriteLine ("(filling queue)");
										for (int i = 0; i < Actinic_Lights_Queue.Lights.PixelCount; i++) {

											Actinic_Lights_Queue.Lights [i].R = LightSystem.Color_MIN;
											Actinic_Lights_Queue.Lights [i].G = LightSystem.Color_MIN;
											Actinic_Lights_Queue.Lights [i].B = LightSystem.Color_MAX;
											Actinic_Lights_Queue.Lights [i].Brightness = LightSystem.Brightness_MAX;
											Actinic_Lights_Queue.PushToQueue ();
										}
										Console.WriteLine ("(queue filled)");
										WaitForQueue (Actinic_Lights_Queue);
										Console.WriteLine ("(queue ready)");
										Console.WriteLine ("(Actinic light queue test complete)");
										break;
									case "spam":
										Actinic_Lights_Queue.Lights.Fill (Color.Named ["black"]);
										Actinic_Lights_Queue.PushToQueue ();
										WaitForQueue (Actinic_Lights_Queue);
										Console.WriteLine ("(Actinic light queue test: adding 1000 events)");
										for (int loop = 0; loop < (1000 / (LightSystem.LIGHT_COUNT * 2)); loop++) {
											for (int i = 0; i < Actinic_Lights_Queue.Lights.PixelCount; i++) {
												Actinic_Lights_Queue.Lights [i].R = LightSystem.Color_MIN;
												Actinic_Lights_Queue.Lights [i].G = LightSystem.Color_MIN;
												Actinic_Lights_Queue.Lights [i].B = LightSystem.Color_MAX;
												Actinic_Lights_Queue.PushToQueue ();
											}
											for (int i = 0; i < Actinic_Lights_Queue.Lights.PixelCount; i++) {
												Actinic_Lights_Queue.Lights [i].SetColor (RandomColorGenerator.GetRandomColor ());
												Actinic_Lights_Queue.PushToQueue ();
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
							case "debug":
								if (cmd_args.Count > 1 && cmd_args [1] != null) {
									switch (cmd_args [1].ToLowerInvariant ()) {
									case "display":
										if (cmd_args.Count > 2 && cmd_args [2] != null) {
											switch (cmd_args [2].ToLowerInvariant ()) {
											case "latency":
												if (cmd_args.Count > 3 && cmd_args [3] != null) {
													switch (cmd_args [3].ToLowerInvariant ()) {
													case "show":
														Debug_Display_Latency = true;
														break;
													case "hide":
														Debug_Display_Latency = false;
														break;
													default:
														Debug_Display_Latency = !Debug_Display_Latency;
														Console.WriteLine ("(Toggled debug latency; for specific control, debug display latency [show, hide])");
														break;
													}
												} else {
													Debug_Display_Latency = !Debug_Display_Latency;
													Console.WriteLine ("(Toggled debug latency; for specific control, debug display latency [show, hide])");
												}
												break;
											default:
												Console.WriteLine ("> debug display [latency]");
												break;
											}
										} else {
											Console.WriteLine ("> debug display [latency]");
										}
										break;
									case "hide":
										Debug_Display_Latency = false;
										Console.WriteLine ("(All debug display output hidden)");
										break;
									default:
										Console.WriteLine ("> " + Debug_Command_Help);
										break;
									}
								} else {
									Console.WriteLine ("> " + Debug_Command_Help);
								}
								break;
							case "reset":
								Console.WriteLine ("(resetting output system...)");
								if (ResetOutputSystem ()) {
									Console.WriteLine ("(reset succeeded)");
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
					command = AppCommands.GetCommand ();
				} catch (ArgumentOutOfRangeException) {
					Console.WriteLine ("\n(Sorry, but there was a problem reading console input.  You'll have to re-type whatever you were going to say)");
					command = "";
				}
				if (command == null) {
					// Ctrl+D (Linux) and Ctrl+Z (Windows) results in a null
					// command input, commonly used for quickly exiting
					command = "quit";
				}
			}
		}

		#endregion

		#region Queue Management

		private static Dictionary<string, LED_Queue> GetAllQueues ()
		{
			lock (Actinic_Lights_Overlay_Queues) {
				Dictionary<string, LED_Queue> MergedQueues = new Dictionary<string, LED_Queue> (Actinic_Lights_Overlay_Queues.Count + 1, Actinic_Lights_Overlay_Queues.Comparer);
				foreach (KeyValuePair<string, LED_Queue> queue in Actinic_Lights_Overlay_Queues) {
					MergedQueues.Add (queue.Key, queue.Value);
				}
				MergedQueues.Add (Actinic_Lights_Queue_Name, Actinic_Lights_Queue);
				return MergedQueues;
			}
		}

		private static LED_Queue GetQueueByName (string QueueName)
		{
			lock (Actinic_Lights_Overlay_Queues) {
				if (Actinic_Lights_Overlay_Queues.ContainsKey (QueueName) == false) {
#if DEBUG_OVERLAY_MANAGEMENT
					Console.WriteLine ("-- '{0}' does not exist; adding queue", QueueName);
#endif
					Actinic_Lights_Overlay_Queues.Add (QueueName, new LED_Queue (LightSystem.LIGHT_COUNT, true));
				} else {
#if DEBUG_OVERLAY_MANAGEMENT
					Console.WriteLine ("-- '{0}' already exists; reusing", QueueName);
#endif
				}
				return Actinic_Lights_Overlay_Queues [QueueName];
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
			lock (Actinic_Lights_Overlay_Queues) {
				foreach (KeyValuePair<string, LED_Queue> queue in Actinic_Lights_Overlay_Queues) {
					queue.Value.PushToQueue (true);
				}
			}
			lock (Actinic_Lights_Queue) {
				Actinic_Lights_Queue.PushToQueue (true);
			}
		}

		/// <summary>
		/// Halts the animation activity, cleaning up.
		/// </summary>
		/// <param name="IncludeOverlayQueues">If set to <c>true</c> include overlay queues.</param>
		/// <param name="KeepAudioInput">If set to <c>true</c> keep audio input system active if already running.</param>
		private static void HaltActivity (
			bool IncludeOverlayQueues, bool KeepAudioInput = false)
		{
			// As some animations depend partially on the VU volume system, the animation framework should be shut down first.
			//  That will also disable the VU volume system if the animation had requested it.
			if (IncludeOverlayQueues) {
				foreach (KeyValuePair<string, LED_Queue> queue in GetAllQueues ()) {
					HaltActivity (queue.Value, KeepAudioInput);
				}
			} else {
				if (Actinic_Lights_Queue != null)
					HaltActivity (Actinic_Lights_Queue, KeepAudioInput);
			}
		}

		/// <summary>
		/// Halts the animation activity, cleaning up.
		/// </summary>
		/// <param name="QueueToModify">Queue to modify.</param>
		/// <param name="KeepAudioInput">If set to <c>true</c> keep audio input system active if already running.</param>
		/// <param name="ForceQueueCleanup">If set to <c>true</c> fully clear the layer queue.</param>
		private static void HaltActivity (
			LED_Queue QueueToModify,
			bool KeepAudioInput = false,
			bool ForceQueueCleanup = false)
		{
			if (Actinic_Lights_Queue == null)
				return;
			if (ForceQueueCleanup) {
				QueueToModify.ClearQueue ();
			}
			if (QueueToModify.AnimationActive)
				Animation_Stop (QueueToModify, KeepAudioInput);
		}

		/// <summary>
		/// Recreates the light queues with the specified number of lights, halting animations if changes are needed.
		/// </summary>
		/// <param name="LightCount">Light count.  If specified number of lights differs from before, halts animations and recreates queues; otherwise, nothing happens.</param>
		private static void CreateLightQueues (int LightCount)
		{
			if (Actinic_Lights_Queue != null && Actinic_Lights_Queue.LightCount == LightCount) {
				// No update needed
				// Note: other queues cannot become de-synced as this queue always exists and will need updates if inaccurate.
				return;
			} else {
				if (Actinic_Lights_Queue != null) {
					Console.WriteLine ("Detected {0} lights, stopping animations and updating configuration...", LightCount);
				} else {
					Console.WriteLine ("Detected {0} lights", LightCount);
				}
				HaltActivity (true);
				lock (Actinic_Lights_Overlay_Queues) {
					Actinic_Lights_Overlay_Queues.Clear ();
				}
				Actinic_Lights_Queue = new LED_Queue (LightCount);
			}
		}

		private static void Actinic_Light_Start_Queue ()
		{
			if (Actinic_Light_Queue_Thread != null)
				Actinic_Light_Stop_Queue ();
			Actinic_Light_Queue_Thread = new System.Threading.Thread (Actinic_Light_Run_Queue);
			Actinic_Light_Queue_Thread.IsBackground = true;
			Actinic_Light_Queue_Thread.Priority = System.Threading.ThreadPriority.BelowNormal;
			Actinic_Light_Queue_Thread.Start ();
		}

		private static void Actinic_Light_Stop_Queue ()
		{
			if (Actinic_Light_Queue_Thread != null)
				Actinic_Light_Queue_Thread.Abort ();
		}

		private static void Actinic_Light_Run_Queue ()
		{
			Actinic_Lights_Queue.ClearQueue ();

			// Current LED frame to send to output
			Layer Light_Snapshot = null;
			// All individual frames to send to output
			List<Layer> Light_Snapshots = new List<Layer> ();
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
				AbstractReactiveAnimation selected_reactive_animation = Actinic_Lights_Queue.SelectedAnimation as AbstractReactiveAnimation;
				if (Actinic_Lights_Queue.AnimationActive && selected_reactive_animation != null) {
#if DEBUG_VU_PERFORMANCE
					VU_Processing_PerfStopwatch.Restart ();
#endif
#if DEBUG_PERFORMANCE
					Console.WriteLine ("{0,6:F2} ms - updating audio snapshot", Queue_PerfStopwatch.Elapsed.TotalMilliseconds);
#endif
					// Grab a snapshot of the current audio volumes
					if (ActiveAudioInputSystem.Running) {
						double [] Audio_Volumes = ActiveAudioInputSystem.GetSnapshot ();
						while (Audio_Volumes_Snapshot.Count < Audio_Volumes.Length) {
							Audio_Volumes_Snapshot.Add (0);
						}
						while (Audio_Volumes_Snapshot.Count > Audio_Volumes.Length) {
							Audio_Volumes_Snapshot.RemoveAt (Audio_Volumes_Snapshot.Count - 1);
						}
						for (int i = 0; i < Audio_Volumes.Length; i++) {
							Audio_Volumes_Snapshot [i] = Audio_Volumes [i];
						}
					}

					// Update the current reactive animation with this volume snapshot
					selected_reactive_animation.UpdateAudioSnapshot (Audio_Volumes_Snapshot);
					ReactiveSystem.PrintAudioInformationToConsole (selected_reactive_animation);
#if DEBUG_VU_PERFORMANCE
					Console.WriteLine ("# Time until acknowledged: {0,6:F2}", VU_Processing_PerfStopwatch.Elapsed.TotalMilliseconds);
#endif
				}

				// -- Animation-specific queue management --
				foreach (KeyValuePair<string, LED_Queue> queue in GetAllQueues ()) {
					if (queue.Value.AnimationActive) {
						UpdateAnimationStackForQueue (queue.Value, Queue_PerfStopwatch.Elapsed.TotalMilliseconds, queue.Key);
					}
				}

				// -- Generic Light Queue output --
				// Fixed: with multi-threaded timing, the light-queue could become empty between checking the count and pulling a snapshot

				if (ActiveOutputSystemReady ()) {
					Layer QueueLightSnapshot = null;
					bool update_needed = false;
					foreach (KeyValuePair<string, LED_Queue> queue in GetAllQueues ()) {
						if (queue.Value.QueueEmpty == false) {
							update_needed = true;
							break;
						}
					}
					foreach (KeyValuePair<string, LED_Queue> queue in GetAllQueues ()) {
						QueueLightSnapshot = queue.Value.PopFromQueue ();
						if (QueueLightSnapshot != null) {
#if DEBUG_PERFORMANCE
							Console.WriteLine ("{0,6:F2} ms - grabbing snapshot from light queue ({1})", Queue_PerfStopwatch.Elapsed.TotalMilliseconds, queue.Key);
#endif
							Light_Snapshots.Add (QueueLightSnapshot);
							queue.Value.QueueIdleTime = 0;
							queue.Value.Lights = QueueLightSnapshot;
#if DEBUG_PERFORMANCE
							Console.WriteLine ("{0,6:F2} ms - updating last processed ({1})", Queue_PerfStopwatch.Elapsed.TotalMilliseconds, queue.Key);
#endif
							queue.Value.MarkAsProcessed ();
						} else if (update_needed) {
							// Nothing new in the queue, but it must be added to
							// the snapshot for it to be blended down again
							Light_Snapshots.Add (
								queue.Value.LightsLastProcessed.Clone ()
							);
						}
					}
				}

				// Do this after the above to ensure any remaining queue entries will be pushed out
				bool update_after_deletion_needed = false;
				lock (Actinic_Lights_Overlay_Queues) {
					List<string> EmptyQueues = new List<string> ();
					IAnimationOneshot selected_oneshot_animation = null;
					bool deletionRequested = false;
					bool queueWithMaskBlendingActive = false;
					foreach (KeyValuePair<string, LED_Queue> queue in Actinic_Lights_Overlay_Queues) {
						selected_oneshot_animation = queue.Value.SelectedAnimation as IAnimationOneshot;
						if (queue.Value.BlendMode == Color.BlendMode.Mask || queue.Value.BlendMode == Color.BlendMode.Replace)
							queueWithMaskBlendingActive = true;
						if ((queue.Value.LightsHaveNoEffect) || (selected_oneshot_animation != null && selected_oneshot_animation.AnimationFinished)) {
							HaltActivity (queue.Value, false, true);
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
						Console.WriteLine (" {0,6:F2} ms - removing overlay '{1}' as it is empty", Queue_PerfStopwatch.Elapsed.TotalMilliseconds, key);
#endif
						Actinic_Lights_Overlay_Queues.Remove (key);
					}
				}
				if (update_after_deletion_needed) {
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
					Actinic_Light_Queue_CurrentIdleWait = 1;
					// Reset the idle counter, used for implementing interval animations

#if DEBUG_PERFORMANCE
					Console.WriteLine ("{0,6:F2} ms - frame generated", Queue_PerfStopwatch.Elapsed.TotalMilliseconds);
#endif
					int retriesSinceLastSuccess = 0;
					while (true) {
						if (retriesSinceLastSuccess >= 5) {
							throw new System.IO.IOException ("Could not reconnect to output system in background after 5 tries, giving up");
						}
						try {
							bool success = UpdateLights_All (
								Light_Snapshot,
								Queue_PerfStopwatch.Elapsed.TotalMilliseconds
							);
							if (success == false) {
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
							retriesSinceLastSuccess++;
							Console.Write ("Waiting 5 seconds...");
							System.Threading.Thread.Sleep (5000);
							Console.WriteLine ("  Retrying.");

							// RunWithoutInteraction:  Usually someone won't be providing input when this happens
							// SkipPreparingSystem:  Queue and all that is already running
							if (PrepareLightingOutput (true, true) == false) {
								throw new System.IO.IOException ("Could not reconnect to output system in background, giving up");
							}
							// Try again by nature of not calling break
						}
					}

					if (Debug_Display_Latency &&
						DateTime.UtcNow > (Debug_Perf_LastPrint + Debug_Perf_PrintInterval)) {
						Console.WriteLine (
							"Avg: {0,6:F2} ms = {1,6:F2} render + {2,6:F2} " +
							"device ({3})",
							ActiveOutputSystem.Configuration.AverageLatency,
							ActiveOutputSystem.Configuration.AverageRenderLatency,
							ActiveOutputSystem.Configuration.AverageDeviceLatency,
							DateTime.Now.ToLongTimeString ()
						);
						Debug_Perf_LastPrint = DateTime.UtcNow;
					}

#if DEBUG_BRIEF_PERFORMANCE
					double allowedLatency = Render_MaxLatency +
						ActiveOutputSystem.Configuration.AverageDeviceLatency;
					if (ActiveOutputSystem.Configuration.AverageLatency > allowedLatency)
						Console.WriteLine ("Avg: {0,6:F2} ms total - {1,6:F2} ms render, exceeds {2} ms ({3})",
							ActiveOutputSystem.Configuration.AverageLatency,
							ActiveOutputSystem.Configuration.AverageRenderLatency,
							Render_MaxLatency,
							DateTime.Now.ToLongTimeString ());
#endif
#if DEBUG_PERFORMANCE
					Console.WriteLine ("# {0,6:F2} ms - frame finished", Queue_PerfStopwatch.Elapsed.TotalMilliseconds);
#endif
#if DEBUG_VU_PERFORMANCE
					Console.WriteLine ("# {0,6:F2} ms - frame finished", VU_Processing_PerfStopwatch.Elapsed.TotalMilliseconds);
					VU_Processing_PerfStopwatch.Stop ();
#endif
				} else {
#if DEBUG_PERFORMANCE
					if (wasIdle == false)
						Console.WriteLine ("# Idle ({0} ms loop)", Actinic_Light_Queue_CurrentIdleWait);
					if (Actinic_Light_Queue_CurrentIdleWait == Actinic_Light_Queue_MaxIdleWaitTime)
						wasIdle = true;
#endif
					System.Threading.Thread.Sleep (Actinic_Light_Queue_CurrentIdleWait);
					foreach (KeyValuePair<string, LED_Queue> queue in GetAllQueues ()) {
						queue.Value.QueueIdleTime += Actinic_Light_Queue_CurrentIdleWait;
					}
					if (Actinic_Light_Queue_CurrentIdleWait < Actinic_Light_Queue_MaxIdleWaitTime) {
						// Each loop spent in idle without events, increase the delay
						Actinic_Light_Queue_CurrentIdleWait *= Actinic_Light_Queue_IdleWaitMultiplier;
					} else if (Actinic_Light_Queue_CurrentIdleWait > Actinic_Light_Queue_MaxIdleWaitTime) {
						// ...but don't go above the maximum idle wait time
						Actinic_Light_Queue_CurrentIdleWait = Actinic_Light_Queue_MaxIdleWaitTime;
					}
				}
				if (Actinic_Lights_Queue.QueueCount >= Actinic_Light_Queue_BufferFullWarning && Actinic_Light_Queue_BufferFullWarning_Shown == false) {
					Console.WriteLine (
						"(Warning: the LED output queue holds over {0} " +
						"frames, which will cause a delay in response after a " +
						"command)",
						Actinic_Light_Queue_BufferFullWarning
					);
					Actinic_Light_Queue_BufferFullWarning_Shown = true;
				} else if (Actinic_Lights_Queue.QueueCount < Actinic_Light_Queue_BufferFullWarning && Actinic_Light_Queue_BufferFullWarning_Shown == true) {
					Console.WriteLine ("(LED output queue now holds less than {0} frames)", Actinic_Light_Queue_BufferFullWarning);
					Actinic_Light_Queue_BufferFullWarning_Shown = false;
				}
				Queue_PerfStopwatch.Stop ();
			}
		}

		private static void UpdateAnimationStackForQueue (LED_Queue QueueToModify, double PerfTracking_TimeElapsed, string PerfTracking_QueueName)
		{
			if (QueueToModify.AnimationActive && QueueToModify.QueueEmpty) {
				// Only add an animation frame if enabled, and the queue is empty
				if ((QueueToModify.SelectedAnimation.RequestedAnimationDelay <= 0) ||
					(QueueToModify.QueueIdleTime >= QueueToModify.SelectedAnimation.RequestedAnimationDelay) ||
					(QueueToModify.AnimationForceFrameRequest == true)) {
					// Only add an animation frame if default delay is requested, or enough time elapsed in idle
#if DEBUG_PERFORMANCE
					Console.WriteLine ("{0,6:F2} ms - queuing frame from active animation ({1})", PerfTracking_TimeElapsed, PerfTracking_QueueName);
#endif
					try {
						// In all of the below, you must set QueueToModify to the new, intended output, otherwise
						//  animation transitions will contain old values.
						if (QueueToModify.SelectedAnimation.EnableSmoothing && Animation_Fading_Enabled) {
							// Get the next frame, filter it onto the current,
							// frame, and add it to the queue
							QueueToModify.PushToQueue (
								QueueToModify.SelectedAnimation.GetNextFrameFiltered (QueueToModify.Lights)
							);
						} else if ((QueueToModify.AnimationForceFrameRequest == true) &&
								   (QueueToModify.SelectedAnimation.RequestSmoothCrossfade)) {
							// Animation has a potentially-sharp change and requests a smooth cross-fade
							// Get the next frame and insert it into the queue
							// with an animated transition.
							AddToAnimQueue (QueueToModify,
								delegate (ref Layer lights) {
									lights = QueueToModify.SelectedAnimation.GetNextFrame ();
								}
							);
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
						QueueToModify.Lights.Fill (Color.Transparent);
					}

					QueueToModify.AnimationForceFrameRequest = false;
					// Frame has been sent, reset the ForceFrameRequest flag
				}
			}
		}

		private static Layer MergeSnapshotsDown (List<Layer> LightSnapshots)
		{
			if (LightSnapshots.Count <= 0) {
				return null;
			}
			Layer MergedLayers =
				new Layer (
					LightSnapshots [0].PixelCount,
					Color.BlendMode.Combine,
					Color.Transparent
				);

			List<int> snapshots_requesting_replacement = new List<int> ();

			for (int i = 0; i < LightSnapshots.Count; i++) {
				if (LightSnapshots [i].Blending == Color.BlendMode.Favor
					|| LightSnapshots [i].Blending == Color.BlendMode.Mask
					|| LightSnapshots [i].Blending == Color.BlendMode.Replace) {
					snapshots_requesting_replacement.Add (i);
				} else {
					// Blend the layers together
					MergedLayers.Blend (LightSnapshots [i],
						LightSnapshots [i].Blending);
				}
			}

			// These must be done after standard blending modes due to the
			// possibility of overriding the above
			foreach (int snapshot_index in snapshots_requesting_replacement) {
				MergedLayers.Blend (LightSnapshots [snapshot_index],
					LightSnapshots [snapshot_index].Blending);
			}
			return MergedLayers;
		}

		private static void WaitForQueue (LED_Queue QueueToModify)
		{
			WaitForQueue (QueueToModify, 0);
		}

		private static void WaitForQueue (LED_Queue QueueToModify, int MaximumRemainingEvents)
		{
			if (Actinic_Light_Queue_Thread != null) {
				if (Actinic_Light_Queue_Thread.IsAlive == true) {
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

		/// <summary>
		/// Add the current Lights collection of LEDs to the queue, automatically smoothly transitioning into it.
		/// </summary>
		/// <param name="TransformLayer">Function applying modifications to the layer</param>
		/// <param name="QueueToModify">Queue to add animation to</param>
		private static void AddToAnimQueue (
			LED_Queue QueueToModify, LayerTransformer TransformLayer)
		{
			if (TransformLayer == null) {
				throw new ArgumentNullException ("TransformLayer");
			}

			if (ActiveOutputSystem?.Initialized != true) {
				// Ignore if the output system isn't ready yet
				return;
			}
;           // Get current lights
			Layer CurrentLayer;
			lock (QueueToModify.LightsLastProcessed) {
				CurrentLayer = QueueToModify.LightsLastProcessed.Clone ();
			}

			// Copy the current layer...
			Layer TargetLayer = CurrentLayer.Clone ();

			// ...unless values exist in the queue - transform the last item
			// from the queue if so.
			while (QueueToModify.QueueCount > 0) {
				// Clear all from queue, saving the last one
				TargetLayer = QueueToModify.PopFromQueue ();
			}

			// Queue now cleared

			// Apply transformation to end result
			TransformLayer (ref TargetLayer);

			if (Animation_Fading_Enabled) {
				// Interpolate from old values to new values within the time
				// range
				//
				// TODO: Replace this with a real-time animation system that
				// doesn't stuff the queue with precalculated interpolated
				// values.

				// Percentage change for each frame
				double percentFrame =
					ActiveOutputSystem.Configuration.FactorTime
					/ Animation_Smoothing_Duration;

				// Smoothly interpolate if able
				if (percentFrame > 0) {
					for (double percent = 0; percent < 1; percent += percentFrame) {
						Layer LED_Frame = new Layer (CurrentLayer);
						LED_Frame.Blend (TargetLayer, percent, true);
						QueueToModify.PushToQueue (LED_Frame);
					}
				}

				// Just in case the fade did not finish completely, ensure the desired state is sent, too
				QueueToModify.PushToQueue (TargetLayer);
			} else {
				QueueToModify.PushToQueue (TargetLayer);
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
			if ((QueueToModify.SelectedAnimation.RequestedAnimationDelay > 0) || (QueueToModify.SelectedAnimation.RequestSmoothCrossfade))
				QueueToModify.AnimationForceFrameRequest = true;
			// If animation requests a longer delay or a smooth cross-fade, force the first frame to happen now

			if (QueueToModify.SelectedAnimation is AbstractReactiveAnimation) {
				// If animation reacts to music, start up the VU system
				if (!ActiveAudioInputSystem.Running) {
					if (!ActiveAudioInputSystem.StartAudioCapture ()) {
						Console.Error.WriteLine (
							"(Something went wrong while starting the audio " +
							"capture system)"
						);
					}
				}
				// Increase the count of audio users
				Animation_Audio_Users++;
			}
		}

		/// <summary>
		/// Stop the active animation on the given queue.
		/// </summary>
		/// <param name="QueueToModify">Queue to modify.</param>
		/// <param name="KeepAudioInput">If set to <c>true</c> keep audio input system active if already running.</param>
		private static void Animation_Stop (LED_Queue QueueToModify, bool KeepAudioInput = false)
		{
			WaitForQueue (QueueToModify);
			if (QueueToModify.SelectedAnimation is AudioBitmapAnimation)
				(QueueToModify.SelectedAnimation as AudioBitmapAnimation).StopAudioSystem ();
			if (QueueToModify.SelectedAnimation is AbstractReactiveAnimation) {
				// Decrease the number of users
				Animation_Audio_Users--;
			}

			// Check if audio system should be stopped
			if (Animation_Audio_Users < 0) {
				// Validate user count
				throw new ArgumentOutOfRangeException (
					"Animation_Audio_Users",
					"Animation_Audio_Users should not be less than zero."
				);
			} else if (Animation_Audio_Users == 0) {
				// No more audio users
				if (!KeepAudioInput && ActiveAudioInputSystem.Running) {
					// If audio input system is running and not requested to be
					// kept, shut it down
					if (!ActiveAudioInputSystem.StopAudioCapture ()) {
						Console.Error.WriteLine (
							"(Something went wrong while stopping the audio " +
							"capture system)"
						);
					}
				}
			}

			// Clear selected animation
			QueueToModify.SelectedAnimation = null;
		}

		#endregion

		#region VU Meter Management

		/// <summary>
		/// Initializes the audio input capture system.
		/// </summary>
		/// <returns><c>true</c>, if system was initialized, <c>false</c> otherwise.</returns>
		/// <param name="RunWithoutInteraction">If set to <c>true</c> run without interaction.</param>
		private static bool PrepareAudioCapture (bool RunWithoutInteraction = false)
		{
			Console.WriteLine ("Connecting to audio capture system...");

#if DEBUG_FORCE_DUMMYAUDIOINPUT
			Console.WriteLine ("DEBUGGING:  Forcing DummyAudioInput, not checking other options!");
			InitializeAudioInputSystem (new DummyAudioInput ());
#else
			bool retryAgain = true;
			bool success = false;
			while (retryAgain && success == false) {
				success = false;
				foreach (AbstractAudioInput audio_input_system in ReflectiveEnumerator.GetFilteredEnumerableOfType
						 <AbstractAudioInput, IAudioInputDummy> (false)) {
					if (InitializeAudioInputSystem (audio_input_system)) {
						success = true;
						break;
					}
				}
				if (success == false) {
					if (RunWithoutInteraction == false) {
						Console.WriteLine (
							"Could not open connection to audio capture " +
							"system, are needed files in place?\n" +
							"(press 's' for simulation mode, 'r' to retry, " +
							"any other key to exit)"
						);
						switch (Console.ReadKey ().Key) {
						case ConsoleKey.S:
							foreach (AbstractAudioInput audio_input_system in ReflectiveEnumerator.GetFilteredEnumerableOfType
									 <AbstractAudioInput, IAudioInputDummy> (true)) {
								if (InitializeAudioInputSystem (audio_input_system)) {
									success = true;
									retryAgain = false;
									Console.WriteLine (
										"\n[Note]  Running in simulation " +
										"mode; no audio will be captured"
									);
									break;
								}
							}
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
			}
#endif
			// Initialization successful!
			return true;
		}

		private static bool InitializeAudioInputSystem (AbstractAudioInput DesiredAudioInputSystem)
		{
			if (DesiredAudioInputSystem == null)
				throw new ArgumentNullException ("DesiredAudioInputSystem", "DesiredAudioInputSystem must be specified to initialize the system");

			bool success = false;

			ActiveAudioInputSystem = DesiredAudioInputSystem;
			success = ActiveAudioInputSystem.InitializeSystem ();

			string outputType = ActiveAudioInputSystem.GetType ().Name.Trim ();
			if (outputType == "") {
				outputType = "Unknown";
			}

			if (success) {
				Console.WriteLine ("- Connected to '{0}' via '{1}'", outputType, ActiveAudioInputSystem.Identifier);
			} else {
				Console.WriteLine ("- Could not connect to '{0}'", outputType);
			}

			return success;
		}

		#endregion

		private static bool UpdateLights_Brightness (
			Layer Actinic_Light_Set, double ProcessingOverhead = 0)
		{
			if (ActiveOutputSystemReady () == false)
				return false;

			return ActiveOutputSystem.UpdateLightsBrightness (
				Actinic_Light_Set, ProcessingOverhead);
		}

		private static bool UpdateLights_Color (
			Layer Actinic_Light_Set, double ProcessingOverhead = 0)
		{
			if (ActiveOutputSystemReady () == false)
				return false;

			return ActiveOutputSystem.UpdateLightsColor (
				Actinic_Light_Set, ProcessingOverhead);
		}

		private static bool UpdateLights_All (
			Layer Actinic_Light_Set, double ProcessingOverhead = 0)
		{
			if (ActiveOutputSystemReady () == false)
				return false;

			return ActiveOutputSystem.UpdateLightsAll (
				Actinic_Light_Set, ProcessingOverhead);
		}


		private static bool ActiveOutputSystemReady ()
		{
			return (ActiveOutputSystem?.Initialized == true);
		}

		private static bool InitializeOutputSystem (AbstractOutput DesiredOutputSystem)
		{
			if (DesiredOutputSystem == null)
				throw new ArgumentNullException ("DesiredOutputSystem", "DesiredOutputSystem must be specified to initialize the system");

			bool success = false;

			ActiveOutputSystem = DesiredOutputSystem;
			success = ActiveOutputSystem.InitializeSystem ();

			string outputType = ActiveOutputSystem.GetType ().Name.Trim ();
			if (outputType == "") {
				outputType = "Unknown";
			}

			if (success) {
				Console.WriteLine (
					"- Connected to '{0}' via '{1}' (ver. {2})",
					outputType,
					ActiveOutputSystem.Identifier,
					ActiveOutputSystem.VersionIdentifier);

				// Update number of lights, (re-)initialize the light queues
				LightSystem.SetLightCount (
					ActiveOutputSystem.Configuration.LightCount);
				CreateLightQueues (LightSystem.LIGHT_COUNT);

				ActiveOutputSystemReadyEvent.Set ();
			} else {
				Console.WriteLine ("- Could not connect to '{0}'", outputType);
			}

			return success;
		}

		private static bool ShutdownOutputSystem ()
		{
			if (ActiveOutputSystem != null) {
				ActiveOutputSystemReadyEvent.Reset ();
				return ActiveOutputSystem.ShutdownSystem ();
			}
			return false;
		}

		private static bool ResetOutputSystem ()
		{
			if (ActiveOutputSystem == null)
				return false;

			ActiveOutputSystemReadyEvent.Reset ();
			if (ActiveOutputSystem.ResetSystem ()) {
				string outputType = ActiveOutputSystem.GetType ().Name.Trim ();
				if (outputType == "") {
					outputType = "Unknown";
				}
				Console.WriteLine (
					"- Connected to '{0}' via '{1}' (ver. {2})",
					outputType,
					ActiveOutputSystem.Identifier,
					ActiveOutputSystem.VersionIdentifier);

				ActiveOutputSystemReadyEvent.Set ();
				return true;
			} else {
				return false;
			}
		}

	}
}
