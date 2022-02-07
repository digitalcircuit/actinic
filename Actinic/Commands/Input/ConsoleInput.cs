//
//  ConsoleInput.cs
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
using System.Threading.Tasks;

namespace Actinic.Commands.Input
{
	public class ConsoleInput : AbstractInput
	{
		/// <summary>
		/// The ongoing Console read operation, or null if not set up.
		/// </summary>
		private Task<string> queuedConsoleRead;

		/// <summary>
		/// Gets the identifier for the connected command input system.
		/// </summary>
		/// <value>Command input system identifier.</value>
		public override string Identifier => "console";

		/// <summary>
		/// Gets an input command asynchronously, returning it as one string.
		/// </summary>
		/// <returns>The command string.</returns>
		public override async Task<string> GetCommandAsync ()
		{
			if (!IsTaskPending (queuedConsoleRead)) {
				queuedConsoleRead = Task.Run (() => Console.ReadLine ());
			}
			return await queuedConsoleRead;
			//return await Task.Run (() => Console.ReadLine ());
		}

		// No constructor required
	}
}
