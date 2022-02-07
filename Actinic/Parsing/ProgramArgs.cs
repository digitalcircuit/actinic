//
//  ProgramArgs.cs
//
//  Author:
//       Shane Synan <digitalcircuit36939@gmail.com>
//
//  Copyright (c) 2022
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

namespace Actinic.Parsing
{
	public class ProgramArgs
	{
		/// <summary>
		/// The Actinic project URL.
		/// </summary>
		private const string projectURL = "https://github.com/digitalcircuit/actinic/";

		/// <summary>
		/// Gets a value indicating whether the program should exit immediately.
		/// </summary>
		/// <value><c>true</c> if the app should exit immediately; otherwise, <c>false</c>.</value>
		public bool ExitImmediately {
			get;
			private set;
		}

		/// <summary>
		/// Gets a value indicating whether interactive prompts should be skipped.
		/// </summary>
		/// <value><c>true</c> if prompts are disabled; otherwise, <c>false</c>.</value>
		public bool NoPrompts {
			get;
			private set;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:ConsoleHttpTest.ProgramArgs"/> class.
		/// </summary>
		/// <param name="args">Command line arguments.</param>
		public ProgramArgs (string [] args)
		{
			var appAssembly = System.Reflection.Assembly.GetExecutingAssembly ();
			string appName = System.IO.Path.GetFileName (appAssembly.Location);
			Version appVersion = appAssembly.GetName ().Version;
			string cliHelpPrompt = "Try '" + appName + " --help'" +
				"for more information.";

			string cliHelp = @"Usage: " + appName + @" [OPTION]...

Actinic manages light strands according to animations and music.

--no-prompt        Exit on system errors instead of prompting to retry

-h, --help         Show this help
-v, --version      Show version information";

			var cli_args = new System.Collections.Generic.List<string> (args);
			while (cli_args.Count > 0) {
				// Parse command line arguments
				switch (cli_args [0]) {
				case "/?":
				case "-?":
				case "-h":
				case "--help":
					Console.WriteLine (cliHelp);
					ExitImmediately = true;
					return;
				case "-v":
				case "--version":
					// See https://stackoverflow.com/questions/36351866/getting-the-version-of-my-c-sharp-app
					Console.WriteLine ("Actinic version: {0}", appVersion);
					Console.WriteLine (projectURL);
					ExitImmediately = true;
					return;
				case "--no-prompt":
					NoPrompts = true;
					cli_args.RemoveAt (0);
					break;
				default:
					string errorMessage = string.Format ("{0}: unrecognized " +
						"option '{1}'", appName, cli_args [0]);
					Console.Error.WriteLine (errorMessage);
					Console.Error.WriteLine (cliHelpPrompt);
					throw new ArgumentException (errorMessage, cli_args [0]);
				}
			}
		}
	}
}
