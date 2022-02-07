//
//  HTTPInput.cs
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
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Actinic.Commands.Input
{
	public class HTTPInput : AbstractInput
	{
		/// <summary>
		/// The Actinic project URL.
		/// </summary>
		private const string projectURL = "https://github.com/digitalcircuit/actinic/";

		/// <summary>
		/// The current API path.
		/// </summary>
		private const string apiPath = "/api/v0/";

		/// <summary>
		/// The ongoing HTTP API listen operation, or null if not set up.
		/// </summary>
		private Task<string> queuedHTTPRead;

		/// <summary>
		/// Underlying HTTP listener for API requests.
		/// </summary>
		private HttpListener httpProvider;

		/// <summary>
		/// Gets the prefix the HTTP listener responds to.
		/// </summary>
		/// <value>The HTTP listener prefix, e.g. "http://localhost:1234/".</value>
		private string HttpPrefix {
			get {
				if (httpProvider?.Prefixes?.Count > 0) {
					// httpProvider, Prefixes are not null, and at least one prefix is registered
					return httpProvider.Prefixes.First ();
				}
				// Fallback value
				return "http://<host>:<port>/";
			}
		}

		/// <summary>
		/// Gets the identifier for the connected command input system.
		/// </summary>
		/// <value>Command input system identifier.</value>
		public override string Identifier => HttpPrefix;

		/// <summary>
		/// Gets an input command asynchronously, returning it as one string.
		/// </summary>
		/// <returns>The command string.</returns>
		public override async Task<string> GetCommandAsync ()
		{
			if (!IsTaskPending (queuedHTTPRead)) {
				queuedHTTPRead = GetHttpCommandAsync ();
			}
			return await queuedHTTPRead;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:ConsoleHttpTest.Commands.Input.HTTPInput"/> class.
		/// </summary>
		/// <param name="listenAddress">HTTP server listening address, e.g. "localhost:1234".</param>
		public HTTPInput (string listenAddress)
		{
			httpProvider = new HttpListener ();
			httpProvider.Prefixes.Add (string.Format ("http://{0}/", listenAddress));
			// This may throw an exception if the port is in use
			httpProvider.Start ();
		}

		/// <summary>
		/// Gets the HTTP POST request data asynchronously.
		/// </summary>
		/// <returns>The HTTP POST request data.</returns>
		/// <param name="request">HTTP Listener Request object.</param>
		private async Task<string> GetPostRequestDataAsync (HttpListenerRequest request)
		{
			// See https://stackoverflow.com/questions/5197579/getting-form-data-from-httplistenerrequest
			if (!request.HasEntityBody) {
				return null;
			}
			System.IO.Stream body = request.InputStream;
			using (var reader = new System.IO.StreamReader (body, request.ContentEncoding)) {
				return await reader.ReadToEndAsync ();
			}
		}

		/// <summary>
		/// Gets the next input command via HTTP asynchronously and serves the
		/// API welcome page.
		/// </summary>
		/// <returns>The next input command POSTed via HTTP.</returns>
		private async Task<string> GetHttpCommandAsync ()
		{
			// Command result from HTTP
			string command = null;

			while (string.IsNullOrEmpty (command)) {
				// Clear command
				command = null;

				var cmdContext = await httpProvider.GetContextAsync ();
				var cmdRequest = cmdContext.Request;
				var cmdResponse = cmdContext.Response;

				string responseString = "";
				if (cmdRequest.Url.AbsolutePath == "/favicon.ico") {
					// No favicon
					cmdResponse.StatusCode = 404;
				} else if (cmdRequest.Url.AbsolutePath != apiPath) {
					// Only a very rudimentary API is supported at the moment
					cmdResponse.Redirect (apiPath);
				} else if (cmdRequest.HttpMethod == "POST") {
					if (!cmdRequest.HasEntityBody) {
						// Needs body
						cmdResponse.StatusCode = 100;
					} else if (cmdRequest.ContentLength64 > 1 * 1024) {
						// Don't allow more than 1 KB
						responseString = "Response invalid, exceeded 1 KB";
						cmdResponse.StatusCode = 400;
					} else {
						command = await GetPostRequestDataAsync (cmdRequest);
						if (command == null) {
							// Needs body
							cmdResponse.StatusCode = 100;
						} else {
							command = command.Trim ();
							command = WebUtility.UrlDecode (command);

							// Remove basic non-printing characters
							// See https://stackoverflow.com/questions/40564692/c-sharp-regex-to-remove-non-printable-characters-and-control-characters-in-a
							command = System.Text.RegularExpressions.Regex.Replace (command, @"\p{Cc}+", string.Empty);

							// Remove "cmd=" prefix
							if (command.StartsWith ("cmd=", StringComparison.Ordinal)) {
								command = command.Substring ("cmd=".Length);
							}
							// Keep the same page
							cmdResponse.StatusCode = 204;
							// Alternatively, clear the HTTP form (slower)
							//cmdResponse.Redirect (apiPath);
						}
					}
				} else {
					// Multiline verbatim string literal makes this easier to read
					// Use "" to represent "
					// See https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/strings/#regular-and-verbatim-string-literals
					responseString = @"<!DOCTYPE html>
<html>
<head>
  <meta charset=""UTF-8"">
  <meta name=""viewport"" content=""width=device-width, initial-scale=1"">
  <title>Actinic web API v0</title>
  <style>:root { color-scheme: light dark; }</style>
</head>
<body>
<h3>Actinic web API v0</h3>
<form method=""post"">
  <label for=""cmd"">Command:</label><br/>
  <input type=""text"" id=""cmd"" name=""cmd"" value="""" autofocus style=""width: 100%; max-width: 50em;""> 
  <input type=""submit"" value=""Submit""> 
  <input type=""reset"" value=""Clear"">
</form>
<p>
  Enter Actinic command, or <code>POST</code>
  commands directly to <code>" + apiPath + @"</code>.<br/>
  Example:</br>
  <code>curl """ + HttpPrefix.TrimEnd ('/') + apiPath +
  @""" --data ""example command " + "\ud83d\udca1" +
  @"""</code>
</p>
<p>
  For more information, type <code>help</code> in
  the Actinic console window, or see
  <a rel=""external noreferrer noopener nofollow"" href=""" + projectURL + @""">
  the Actinic repository</a>.
</p>
</body></html>";
				}

				byte [] buffer = System.Text.Encoding.UTF8.GetBytes (responseString);
				// Get a response stream and write the response to it

				cmdResponse.ContentLength64 = buffer.Length;
				System.IO.Stream output = cmdResponse.OutputStream;
				output.Write (buffer, 0, buffer.Length);
				// Close the output stream
				output.Close ();
			}

			// Valid command found
			if (string.IsNullOrEmpty (command)) {
				// This should not be possible
				throw new InvalidOperationException ("Internal HTTP 'command'" +
					"should not be null or empty.");
			}

			// Return current command
			return command;
		}
	}
}
