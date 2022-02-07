//
//  CommandReceiver.cs
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
using System.Collections.Generic;
using System.Linq;

using Actinic.Commands.Input;

namespace Actinic.Commands
{
	public class CommandReceiver
	{
		/// <summary>
		/// Collection of asynchronous command input providers.
		/// </summary>
		private List<AbstractInput> commandInputs = new List<AbstractInput> ();

		/// <summary>
		/// Gets a list of identifiers for all enabled input providers.
		/// </summary>
		/// <value>List of identifiers for each input provider.</value>
		public List<string> InputIdentifiers => commandInputs.Select (id => id.Identifier).ToList ();

		/// <summary>
		/// Initializes a new instance of the <see cref="T:ConsoleHttpTest.Commands.CommandReceiver"/> class.
		/// </summary>
		/// <param name="consoleEnabled">If set to <c>true</c> Console input is enabled.</param>
		/// <param name="httpEnabled">If set to <c>true</c> HTTP API server is enabled.</param>
		/// <param name="httpAddress">The HTTP listening address, required if httpEnabled is true.</param>
		public CommandReceiver (bool consoleEnabled, bool httpEnabled, string httpAddress = "")
		{
			if (consoleEnabled) {
				commandInputs.Add (new ConsoleInput ());
			}
			if (httpEnabled) {
				if (string.IsNullOrEmpty (httpAddress)) {
					throw new ArgumentNullException (nameof (httpAddress),
						"httpAddress cannot be null or empty when httpEnabled is true.");
				}
				// This may fail
				try {
					commandInputs.Add (new HTTPInput (httpAddress));
				} catch (Exception ex) {
					throw new InvalidOperationException (string.Format ("Could" +
						" not start HTTP server on specified address '{0}'", httpAddress), ex);
				}
			}

			if (commandInputs.Count <= 0) {
				throw new ArgumentException ("At least one command input " +
					"must be enabled (e.g. consoleEnabled, httpEnabled).");
			}
		}

		/// <summary>
		/// Gets the next available command from any enabled command input
		/// provider, blocking as long as needed.
		/// </summary>
		/// <returns>The command input string.</returns>
		public string GetCommand ()
		{
			if (commandInputs.Count <= 0) {
				throw new InvalidOperationException ("At least one command " +
					"input must be enabled (e.g. Console, HTTP).");
			}
			// Get all available asynchronous command input tasks
			var cmdTasks = commandInputs.Select (cmdInput => cmdInput.GetCommandAsync ());
			// Wait for any command input providers to return a valid result
			return System.Threading.Tasks.Task.WhenAny (cmdTasks).Result.Result;
		}
	}
}
