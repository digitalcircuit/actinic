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
		/// The default port for the HTTP API server if not specified
		/// </summary>
		/// <remarks>"54448" is "light" on a phone keypad.</remarks>
		private const int HTTPServerDefaultPort = 54448;

		/// <summary>
		/// Gets a value indicating whether the HTTP API server is enabled.
		/// </summary>
		/// <value><c>true</c> if HTTP server enabled; otherwise, <c>false</c>.</value>
		public bool HTTPServerEnabled {
			get;
			private set;
		}

		/// <summary>
		/// Gets the HTTP API address and port used for the HTTP API server.
		/// </summary>
		/// <value>The HTTP API server listening address and port.</value>
		public string HTTPServerAddress {
			get;
			private set;
		} = "localhost:" + HTTPServerDefaultPort;

		/// <summary>
		/// Gets a value indicating whether all console input is ignored.
		/// </summary>
		/// <value><c>true</c> if console input is ignored; otherwise, <c>false</c>.</value>
		public bool NoConsole {
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

-l, --listen       Start a simple HTTP API server for remote control
--listen-addr      Listening address for HTTP API server
                   Specifying this implies '--listen'
                   Use '*' to listen on all interfaces
                   [default: '" + HTTPServerAddress + @"']

-T, --no-console   Ignore all console input
                   Specifying this implies '--no-prompt'
                   Requires enabling another input method (e.g. '--listen')
                   Useful for running Actinic as a service

--no-prompt        Exit on system errors instead of prompting to retry

-h, --help         Show this help
-v, --version      Show version information";

			// Selected "--no-console" alias, if specified
			// Used when validating options
			var optNoConsoleAlias = "";

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
				case "-l":
				case "--listen":
					HTTPServerEnabled = true;
					cli_args.RemoveAt (0);
					break;
				case "--listen-addr":
					// Enable listener if not yet enabled
					HTTPServerEnabled = true;
					// Remove argument
					cli_args.RemoveAt (0);
					// Get listen address
					HTTPServerAddress = cli_args [0];
					cli_args.RemoveAt (0);
					// Add default port if missing
					if (!HTTPServerAddress.Contains (":")) {
						HTTPServerAddress += ":54448";
					}
					break;
				case "-T":
				case "--no-console":
					NoConsole = true;
					optNoConsoleAlias = cli_args [0];
					cli_args.RemoveAt (0);
					// Also disable prompts if not already set
					NoPrompts = true;
					break;
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

			// Validate options
			if (NoConsole) {
				// Another option must be enabled
				if (!HTTPServerEnabled) {
					var errorMessage = string.Format ("{0}: option " +
						"'{1}' requires another input method" +
						" (e.g. '--listen')", appName, optNoConsoleAlias);
					Console.Error.WriteLine (errorMessage);
					Console.Error.WriteLine (cliHelpPrompt);
					throw new ArgumentException (errorMessage,
						optNoConsoleAlias);
				}
			}
		}
	}
}
