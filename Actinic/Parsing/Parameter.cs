//
//  Parameter.cs
//
//  Author:
//       Shane Synan <digitalcircuit36939@gmail.com>
//
//  Copyright (c) 2016
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
using System.Linq;

// Rendering
using Actinic.Rendering;

namespace Actinic.Parsing
{
	public static class Parameter
	{

		/// <summary>
		/// Given a string of arguments, gets a range of integers representing indexes of selected lights.
		/// </summary>
		/// <returns><c>true</c>, if range was successfully gotten, <c>false</c> otherwise.</returns>
		/// <param name="Arguments">Remaining arguments, including for this command, e.g. 5-10:2.  Valid arguments will be removed from this reference variable.</param>
		/// <param name="MaximumLights">Number of available lights.</param>
		/// <param name="SelectedLights">List of selected lights.</param>
		/// <param name="ExplainErrors">If set to <c>true</c> explain via console when errors happen.</param>
		public static bool GetRange (ref List<string> Arguments,
		                             int MaximumLights,
		                             out List<int> SelectedLights,
		                             bool ExplainErrors = false)
		{
			if (Arguments == null)
				throw new ArgumentException ("Provided arguments must not be null", "Arguments");

			// Help message with two spaces of indentation
			const string HELP_Examples =
				"  Use combinations of" +
				" [all], [index], [start]-[end], [start]-[end]:[increment]" +
				"\n  Example: 1,5,10-20,30-40:2,40-50:5";

			// Which lights are selected?
			SelectedLights = new List<int> ();

			if (Arguments.Count < 1) {
				if (ExplainErrors)
					Console.Error.WriteLine ("> Specify a range\n{0}", HELP_Examples);
				return false;
			}

			// Get the first item from arguments, split on separator, then remove it from the list
			string[] selections = Arguments [0].Split (',');
			Arguments.RemoveAt (0);

			// For each potential choice...
			foreach (string selection in selections) {
				// ...ignore blank
				if (selection.Trim () == "")
					continue;

				// ...check the type...
				if (selection.ToLowerInvariant () == "all") {
					// Clean up redundancies, then toss in everything and the kitchen sink
					SelectedLights.Clear ();
					// Note: second part of .Range is a count, -not- index, and should not be subtracted by one
					SelectedLights.AddRange (Enumerable.Range (0, MaximumLights));
					// Can't get any more lights than 'all', so break out of the loop now
					break;
				} else if (selection.Contains ("-")) {
					// Selection is a range, time to parse
					string range_selection = selection;
					int range_start, range_end, range_step = 1;

					if (selection.Contains (":")) {
						// A step value is specified, parse and validate
						string[] selection_steps = range_selection.Split (':');
						// First portion is the range as normal
						range_selection = selection_steps [0];
						// Second portion is the step value
						if (int.TryParse (selection_steps [1], out range_step)) {
							if (!(range_step >= 1 && range_step <= MaximumLights)) {
								// Not a valid step amount (within light count), bail out
								if (ExplainErrors)
									Console.Error.WriteLine (
										"> Range increment '{3}' is out of range, must be from '{1}' to '{2}'\n{0}",
										HELP_Examples, 1, MaximumLights, range_step
									);
								return false;
							}
							// If valid, the step amount is already stored, no further action required
						} else {
							// Not an integer, bail out
							if (ExplainErrors)
								Console.Error.WriteLine (
									"> Range increment must be a whole number from '{1}' to '{2}'\n{0}",
									HELP_Examples, 1, MaximumLights
								);
							return false;
						}
					}

					// A range of some sort has been specified, parse and validate
					string[] selection_boundaries = range_selection.Split ('-');

					// Starting index
					if (int.TryParse (selection_boundaries [0], out range_start)) {
						if (!(range_start >= 1 && range_start <= MaximumLights)) {
							// Not a valid start position (within light count), bail out
							if (ExplainErrors)
								Console.Error.WriteLine (
									"> Range start '{3}' is out of range, must be from '{1}' to '{2}'\n{0}",
									HELP_Examples, 1, MaximumLights, range_start
								);
							return false;
						}
						// If valid, the range start is already stored, no further action required
					} else {
						// Not an integer, bail out
						if (ExplainErrors)
							Console.Error.WriteLine (
								"> Range start must be a whole number from '{1}' to '{2}'\n{0}",
								HELP_Examples, 1, MaximumLights
							);
						return false;
					}

					// Ending index
					if (int.TryParse (selection_boundaries [1], out range_end)) {
						if (!(range_end >= range_start && range_end <= MaximumLights)) {
							// Not a valid start position (higher than start, within light count), bail out
							if (ExplainErrors)
								Console.Error.WriteLine (
									"> Range end '{3}' is out of range, must be from '{1}' (range start) to '{2}'\n {0}",
									HELP_Examples, range_start, MaximumLights, range_end
								);
							return false;
						}
						// If valid, the range start is already stored, no further action required
					} else {
						// Not an integer, bail out
						if (ExplainErrors)
							Console.Error.WriteLine (
								"> Range end must be a whole number from '{1}' to '{2}'\n{0}",
								HELP_Examples, 1, MaximumLights
							);
						return false;
					}

					// Convert from one- to zero-based index
					--range_start;
					--range_end;

					// Add the range of lights using a Linq enumeration
					// Use (end − start)÷step to get number of integers, biasing towards too few rather than too many
					// E.g. range_start = 6, range_end = 29, range_step = 3, result = 7.66, numbers end at 27
					// Entire count is + 1 in order to include both start -and- ending numbers, when possible
					// Inside enumerable, x will range from range_start to stop, and before multiplying must be
					// subtracted to convert the range into a 0 to end scale.
					// See:  https://msdn.microsoft.com/en-us/library/system.linq.enumerable.range.aspx
					SelectedLights.AddRange (Enumerable.Range (range_start, ((range_end - range_start) / range_step) + 1)
					                         .Select (x => range_start + (x - range_start) * range_step));
				} else {
					// Selection is a single light, try it directly
					int single_light;
					if (int.TryParse (selection, out single_light)) {
						if (single_light >= 1 && single_light <= MaximumLights) {
							// Convert from one- to zero-based index, add light only if not already selected
							--single_light;
							if (!SelectedLights.Contains (single_light))
								SelectedLights.Add (single_light);
						} else {
							// Not a valid index (within light count), bail out
							if (ExplainErrors)
								Console.Error.WriteLine (
									"> Light index '{3}' is out of range, must be from '{1}' to '{2}'\n {0}",
									HELP_Examples, 1, MaximumLights, single_light
								);
							return false;
						}
					} else {
						// Not an integer, bail out
						if (ExplainErrors)
							Console.Error.WriteLine (
								"> Light index must be a whole number from '{1}' to '{2}'\n{0}",
								HELP_Examples, 1, MaximumLights
							);
						return false;
					}
				}
			}

			// Nothing invalid, so sort the results (for easier verification), remove duplicates and return success
			SelectedLights.Sort ();
			SelectedLights = SelectedLights.Distinct ().ToList ();
			return true;
		}

		/// <summary>
		/// Given a string of arguments, gets a color by name or RGB values
		/// </summary>
		/// <returns><c>true</c>, if color was successfully gotten, <c>false</c> otherwise.</returns>
		/// <param name="Arguments">All arguments, including for this command, e.g. orange.  Valid arguments will be removed from this reference variable.</param>
		/// <param name="SelectedColor">Selected color.</param>
		/// <param name="ExplainErrors">If set to <c>true</c> explain via console when errors happen.</param>
		public static bool GetColor (ref List<string> Arguments,
		                             out Color SelectedColor,
		                             bool ExplainErrors = false)
		{
			if (Arguments == null)
				throw new ArgumentException ("Provided arguments must not be null", "Arguments");

			// Help message with two spaces of indentation
			const string HELP_Examples =
				"  Use" +
				" [color name], [red] [green] [blue], or [red] [green] [blue] [brightness]" +
				"\n  Type 'color list' for a list of named colors" +
				"\n  Examples: white, black, blue, 0 255 0, 0 128 255 100";

			// Choose an unusual color for failure cases (blue screen of death)
			SelectedColor = new Color (0, 0, 255);

			if (Arguments.Count < 1) {
				if (ExplainErrors)
					Console.Error.WriteLine ("> Specify a color\n{0}", HELP_Examples);
				return false;
			}

			// This might be a named color, check all names first.
			if (Color.Named.ContainsKey (Arguments [0].Trim ())) {
				// Match found, use this color
				SelectedColor = Color.Named [Arguments [0].Trim ()];

				// Remove this argument from the list and return success
				Arguments.RemoveAt (0);
				return true;
			} else if (Arguments.Count >= 3) {
				// It's not a named color.  Maybe it's a multi-argument RGB value?
				byte selected_r, selected_g, selected_b, selected_brightness;
				if (byte.TryParse (Arguments [0], out selected_r) &&
				    byte.TryParse (Arguments [1], out selected_g) &&
				    byte.TryParse (Arguments [2], out selected_b) &&
				    selected_r >= LightSystem.Color_MIN && selected_r <= LightSystem.Color_MAX &&
				    selected_g >= LightSystem.Color_MIN && selected_g <= LightSystem.Color_MAX &&
				    selected_b >= LightSystem.Color_MIN && selected_b <= LightSystem.Color_MAX) {
					// Try to parse each of the three colors, RGB, then check if they're within
					// range of minimum and maximum color.  If so, we've at least got a color.

					if (Arguments.Count >= 4 && byte.TryParse (Arguments [3], out selected_brightness) &&
					    selected_brightness >= LightSystem.Brightness_MIN &&
					    selected_brightness <= LightSystem.Brightness_MAX) {
						// Brightness specified (argument count >=4), successfully parsed, and within range of
						// minimum and maximum.  Conditionally remove the extra argument here.
						Arguments.RemoveAt (0);
					} else {
						// Brightness not specified, treat as maximum
						selected_brightness = LightSystem.Brightness_MAX;
					}

					// Create a new color with the desired RGB values
					SelectedColor = new Color (selected_r, selected_g, selected_b, selected_brightness);

					// All parsed successfully, remove the RGB arguments from the list and return success
					// Note: brightness is handled separately
					Arguments.RemoveRange (0, 3);
					return true;
				} else {
					// Something went wrong parsing the colors, return unsuccessful
					if (ExplainErrors)
						Console.Error.WriteLine (
							"> Color value is out of range, must be from '{1}' to '{2}'\n{0}",
							HELP_Examples, LightSystem.Color_MIN, LightSystem.Color_MAX
						);
					return false;
				}
			} else {
				// All attempts to make sense have failed, return unsuccessful
				if (ExplainErrors)
					Console.Error.WriteLine (
						"> Unknown color\n{0}", HELP_Examples
					);
				return false;
			}
		}
	}
}

