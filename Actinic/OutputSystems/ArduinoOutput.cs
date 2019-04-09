//
//  ArduinoOutput.cs
//
//  Author:
//       Shane Synan <digitalcircuit36939@gmail.com>
//
//  Copyright (c) 2015 - 2016
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

//#define DEBUG_USB_PERFORMANCE
//#define DEBUG_ASYNC_USB_PERFORMANCE

using System;
using System.IO.Ports;
using System.Collections.Generic;
using FoxSoft.Utilities;

// Output systems (transitioning legacy to modern)
using Actinic.Output;

// Rendering
using Actinic.Rendering;

namespace Actinic.Outputs
{
	public class ArduinoOutput:AbstractOutput
	{

		/// <summary>
		/// Priority of this system to other output systems, lower numbers result in higher priority.
		/// </summary>
		private const int Arduino_Priority = 36943;
		// Feel free to change this as desired

		#region Protocol

		private const string Protocol_Firmware_Identifier =
			"ActinicArduino";
		private const string Protocol_Firmware_Negotiation_Version = "version:";
		private const string Protocol_Firmware_Negotiation_Light_Count = "light_count:";
		private const string Protocol_Firmware_Negotiation_Strand_Length = "strand_length:";
		private const string Protocol_Firmware_Negotiation_Color_Max = "color_max:";
		private const string Protocol_Firmware_Negotiation_Brightness_Max = "bright_max:";
		private const string Protocol_Firmware_Negotiation_End = "end_init";

		/// <summary>
		/// Compatible major version of the firmware
		/// </summary>
		private const int Protocol_Firmware_Compatible_Version_Major = 3;

		/// <summary>
		/// Version string for compatible firmware
		/// </summary>
		private readonly string Protocol_Firmware_Compatible_Version_String =
			Protocol_Firmware_Negotiation_Version
			+ Protocol_Firmware_Compatible_Version_Major
			+ ".";

		/// <summary>
		/// The major version of the firmware, breaking changes
		/// </summary>
		private decimal Protocol_Version;

		/// <summary>
		/// The number of lights.
		/// </summary>
		private int Protocol_Light_Count;

		/// <summary>
		/// The lighted length of the strand in meters.
		/// </summary>
		private float Protocol_Strand_Length;

		private int Protocol_Color_MAX;
		private const int Protocol_Color_MIN = 0;

		private int Protocol_Brightness_MAX;
		private const int Protocol_Brightness_MIN = 0;

		private const byte Protocol_QUERY_INFO = (byte)'?';
		private const byte Protocol_SET_ALL = (byte)'A';
		private const byte Protocol_SET_BRIGHTNESS = (byte)'B';
		private const byte Protocol_SET_COLOR = (byte)'C';
		private const byte Protocol_SET_HUE = (byte)'H';

		#endregion

		public override bool Initialized {
			get {
				return (ResettingSystem == false && USB_Serial != null && USB_Serial.IsOpen && ConnectionStatus == ConnectionState.Ready);
			}
		}

		public override string Identifier {
			get {
				return Arduino_TTY;
			}
		}

		public override string VersionIdentifier {
			get {
				if (Protocol_Version != 0) {
					return Protocol_Version.ToString ();
				} else {
					return "unknown";
				}
			}
		}

		public override int Priority {
			get {
				return Arduino_Priority;
			}
		}

		/// <summary>
		/// Measured processing latency
		/// </summary>
		private double measuredLatency = 0;

		/// <summary>
		/// Active device configuration
		/// </summary>
		private DeviceConfiguration deviceConfig;

		/// <summary>
		/// Read-only version of the active device configuration
		/// </summary>
		private ReadOnlyDeviceConfiguration deviceConfigRO;

		public override ReadOnlyDeviceConfiguration Configuration {
			get {
				if (deviceConfigRO == null) {
					deviceConfigRO =
						new ReadOnlyDeviceConfiguration (deviceConfig);
				}
				return deviceConfigRO;
			}
		}

		private enum ConnectionState
		{
			Disconnected,
			Connecting,
			FirmwareFound,
			CompatibleVersionFound,
			ProtocolFound,
			Ready
		}

		private ConnectionState ConnectionStatus = ConnectionState.Disconnected;
		private string Arduino_TTY = "";
		private SerialPort USB_Serial;

		public ArduinoOutput ()
		{
		}


		public override bool InitializeSystem ()
		{
			// Find all valid serial ports
			string[] serial_ports = System.IO.Ports.SerialPort.GetPortNames ();

			// NOTE: On older versions of Mono (e.g. in Ubuntu 14.04), try/catch does not catch the SerialPort.Open()
			// exception.  If this crashes, try commenting out the above line.
			// See https://bugzilla.xamarin.com/show_bug.cgi?id=15514
			//   Unhandled Exception: System.IO.IOException: Bad file descriptor
			//     at System.IO.Ports.SerialPortStream.Dispose (Boolean disposing) [0x00000] in <filename unknown>:0
			// You'll also need to manually get a shorter list of valid serial ports, e.g. finding all Arduinos with
			// string[] serial_ports = System.IO.Directory.GetFiles ("/dev", "ttyACM*");

			if (serial_ports.Length == 0) {
				return false;
				// None found
			}

			// The Arduino firmware responds with Protocol_Firmware_Version to indicate it's valid
			//  This checks each available serial port and tests for that response
			for (int i = 0; i < serial_ports.Length; i++) {
				if (serial_ports [i] == null || serial_ports [i].Trim () == "")
					continue;
				if (CheckSerialPort (serial_ports [i])) {
					// Valid serial port found, system set-up
					return true;
				}
			}
			// Couldn't find a serial port, back out
			return false;
		}

		/// <summary>
		/// Attempts to open a connection to the serial port and negotiate the protocol.
		/// </summary>
		/// <returns><c>true</c>, if connection successful, <c>false</c> otherwise.</returns>
		/// <param name="SerialDevice">Connection string for USB serial device.</param>
		private bool CheckSerialPort (string SerialDevice)
		{
			Arduino_TTY = SerialDevice;

			ConnectionStatus = ConnectionState.Connecting;

			USB_Serial = new SerialPort (Arduino_TTY);
			USB_Serial.BaudRate = 115200;
			USB_Serial.Parity = Parity.None;
			USB_Serial.StopBits = StopBits.One;
			USB_Serial.DataBits = 8;
			USB_Serial.Handshake = Handshake.None;

			// This is required on Windows for some Arduino boards.
			// See https://stackoverflow.com/questions/26929153/c-sharp-serial-communcation-with-arduino#26929382
			USB_Serial.DtrEnable = true;
			USB_Serial.RtsEnable = true;

			try {
				USB_Serial.Open ();

				System.Text.StringBuilder receiveBuffer = new System.Text.StringBuilder ();
				string result = "";
				const int bufSize = 20;

				// Prepare query for information
				Byte[] writeQueryBuffer = { Protocol_QUERY_INFO };
				// Query for information
				USB_Serial.Write (writeQueryBuffer, 0, writeQueryBuffer.Length);

				// Check for a valid response twenty times; assume failed if no success
				for (int retry = 0; retry < 20; retry++) {
					System.Threading.Thread.Sleep (200);
					if (USB_Serial.BytesToRead > 0) {
						Byte[] dataBuffer = new Byte[bufSize];

						USB_Serial.Read (dataBuffer, 0, bufSize);
						string s = System.Text.ASCIIEncoding.ASCII.GetString (dataBuffer);

						receiveBuffer.Append (s);
					}
					result = receiveBuffer.ToString ();

					switch (ConnectionStatus) {
					case ConnectionState.Connecting:
						if (result.Contains (Protocol_Firmware_Identifier) == true) {
							ConnectionStatus = ConnectionState.FirmwareFound;
						} else {
							// Retry sending the query for information
							USB_Serial.Write (writeQueryBuffer, 0, writeQueryBuffer.Length);
						}
						break;
					case ConnectionState.FirmwareFound:
						if (result.Contains (Protocol_Firmware_Compatible_Version_String) == true)
							ConnectionStatus = ConnectionState.CompatibleVersionFound;
						break;
					case ConnectionState.CompatibleVersionFound:
						if (result.Contains (Protocol_Firmware_Negotiation_End) == true)
							ConnectionStatus = ConnectionState.ProtocolFound;
						break;
					default:
						// Most likely still waiting for results
						break;
					}

					// Break down here, for otherwise it only exits the switch
					if (ConnectionStatus == ConnectionState.ProtocolFound)
						break;
				}

				if (ConnectionStatus == ConnectionState.ProtocolFound && result != "") {
					// Reset protocol values
					Protocol_Version = 0;
					Protocol_Light_Count = 0;
					Protocol_Color_MAX = 0;
					Protocol_Brightness_MAX = 0;

					// Reset calculated values
					measuredLatency = 0;

					// Remove stale device configuration
					deviceConfig = null;
					deviceConfigRO = null;

					// Load protocol information
					string[] negotiation_results = result.Split ('\n');
					foreach (string protocol_entry in negotiation_results) {
						if (protocol_entry.Trim () == "")
							continue;
						if (protocol_entry.StartsWith (Protocol_Firmware_Negotiation_Version)) {
							decimal.TryParse (protocol_entry.Substring (Protocol_Firmware_Negotiation_Version.Length),
								out Protocol_Version);
						}
						if (protocol_entry.StartsWith (Protocol_Firmware_Negotiation_Light_Count)) {
							int.TryParse (protocol_entry.Substring (Protocol_Firmware_Negotiation_Light_Count.Length),
								out Protocol_Light_Count);
						}
						if (protocol_entry.StartsWith (Protocol_Firmware_Negotiation_Strand_Length)) {
							float.TryParse (protocol_entry.Substring (Protocol_Firmware_Negotiation_Strand_Length.Length),
								out Protocol_Strand_Length);
						}
						if (protocol_entry.StartsWith (Protocol_Firmware_Negotiation_Color_Max)) {
							int.TryParse (protocol_entry.Substring (Protocol_Firmware_Negotiation_Color_Max.Length),
								out Protocol_Color_MAX);
						}
						if (protocol_entry.StartsWith (Protocol_Firmware_Negotiation_Brightness_Max)) {
							int.TryParse (protocol_entry.Substring (Protocol_Firmware_Negotiation_Brightness_Max.Length),
								out Protocol_Brightness_MAX);
						}
					}

					if ((Protocol_Version <= 0)
					    || (Protocol_Light_Count < 1)
					    || (Protocol_Strand_Length <= 0)
					    || (Protocol_Color_MAX < 1)
					    || (Protocol_Brightness_MAX < 1)) {
						// Something's wrong with the connection or the Arduino firmware's configuration
						// Just treat this as a non-existent device, but print a warning
						Console.Error.WriteLine ("Arduino on '{0}' provided invalid configuration, ignoring device", Arduino_TTY);
						ShutdownSystem ();
						return false;
					} else {
						// Set up device configuration
						deviceConfig = new DeviceConfiguration (
							Protocol_Light_Count, Protocol_Strand_Length);

						// Wait to connect DataReceived until now so others don't nom the protocol negotiation data
						USB_Serial.DataReceived += new SerialDataReceivedEventHandler (DataReceivedHandler);
						// All is good, mark system as ready
						ConnectionStatus = ConnectionState.Ready;
						return true;
					}
				} else {
					// Something went wrong, treat this as a non-existent device

					// Connection state represents the last successful
					// negotiation step
					switch (ConnectionStatus) {
					case ConnectionState.Disconnected:
					case ConnectionState.Connecting:
						// Don't warn for unexpected states; might be a random
						// device
						break;
					case ConnectionState.FirmwareFound:
						// Firmware found, but with an incompatible version
						Console.Error.WriteLine (
							"Arduino on '{0}' is not compatible with version " +
							"{1}.x, ignoring device\n" +
							"(Try upgrading Arduino firmware or Actinic " +
							"software..?)",
							Arduino_TTY,
							Protocol_Firmware_Compatible_Version_Major);
						break;
					case ConnectionState.CompatibleVersionFound:
						// Firmware version compatible, but negotiation didn't
						// finish - interrupted connection?
						Console.Error.WriteLine (
							"Arduino on '{0}' has compatible version {1}.x, " +
							"but negotiation didn't finish, ignoring device",
							Arduino_TTY,
							Protocol_Firmware_Compatible_Version_Major);
						break;
					case ConnectionState.ProtocolFound:
					case ConnectionState.Ready:
						// Something went really wrong - this shouldn't happen
						Console.Error.WriteLine (
							"Arduino on '{0}' has compatible version {1}.x, " +
							"but something broke after negotiation, ignoring " +
							"device",
							Arduino_TTY,
							Protocol_Firmware_Compatible_Version_Major);
						break;
					}
					ShutdownSystem ();
					return false;
				}
			} catch (System.IO.IOException) {
				ConnectionStatus = ConnectionState.Disconnected;
				return false;
			}
		}


		public override bool ShutdownSystem ()
		{
			if (USB_Serial != null) {
				ConnectionStatus = ConnectionState.Disconnected;
				USB_Serial.Close ();
				USB_Serial = null;

				// Remove stale device configuration
				deviceConfig = null;
				deviceConfigRO = null;
				return true;
			} else {
				return true;
			}
		}


		public override bool UpdateLightsBrightness (
			Layer Actinic_Light_Set, double ProcessingOverhead = 0)
		{
			if (Initialized == false)
				return false;
			ValidateLightSet (Actinic_Light_Set);

			try {
				List<byte> output_all = new List<byte> ();
				output_all.Add (Protocol_SET_BRIGHTNESS);
				foreach (Color LED_Light in Actinic_Light_Set.GetEnumerator()) {
					output_all.Add (
						ConvertBrightnessValue (LED_Light.Brightness)
					);
				}

				USB_Serial.Write (output_all.ToArray (), 0, output_all.ToArray ().Length);
				return USB_Serial_WaitAcknowledge (ProcessingOverhead);
			} catch (System.TimeoutException ex) {
				throw new System.IO.IOException ("Connection to output system lost (timeout)", ex);
			} catch (System.InvalidOperationException ex) {
				throw new System.IO.IOException ("Connection to output system lost (invalid operation)", ex);
			}
		}

		public override bool UpdateLightsColor (
			Layer Actinic_Light_Set, double ProcessingOverhead = 0)
		{
			if (Initialized == false)
				return false;
			ValidateLightSet (Actinic_Light_Set);

			try {
				List<byte> output_all = new List<byte> ();
				output_all.Add (Protocol_SET_HUE);
				foreach (Color LED_Light in Actinic_Light_Set.GetEnumerator()) {
					byte[] output = new byte[] {
						ConvertColorValue (LED_Light.B),
						ConvertColorValue (LED_Light.G),
						ConvertColorValue (LED_Light.R)
					};
					output_all.AddRange (output);
				}

				USB_Serial.Write (output_all.ToArray (), 0, output_all.ToArray ().Length);
				return USB_Serial_WaitAcknowledge (ProcessingOverhead);
			} catch (System.TimeoutException ex) {
				throw new System.IO.IOException ("Connection to output system lost (timeout)", ex);
			} catch (System.InvalidOperationException ex) {
				throw new System.IO.IOException ("Connection to output system lost (invalid operation)", ex);
			}
		}

		public override bool UpdateLightsAll (
			Layer Actinic_Light_Set, double ProcessingOverhead = 0)
		{
			if (Initialized == false)
				return false;
			ValidateLightSet (Actinic_Light_Set);

			try {
				List<byte> output_all = new List<byte> ();
				output_all.Add (Protocol_SET_ALL);
				foreach (Color LED_Light in Actinic_Light_Set.GetEnumerator()) {
					byte[] output = new byte[] {
						ConvertColorValue (LED_Light.B),
						ConvertColorValue (LED_Light.G),
						ConvertColorValue (LED_Light.R),
						ConvertBrightnessValue (LED_Light.Brightness)
					};
					output_all.AddRange (output);
				}

				USB_Serial.Write (output_all.ToArray (), 0, output_all.ToArray ().Length);
				return USB_Serial_WaitAcknowledge (ProcessingOverhead);
			} catch (System.TimeoutException ex) {
				throw new System.IO.IOException ("Connection to output system lost (timeout)", ex);
			} catch (System.InvalidOperationException ex) {
				throw new System.IO.IOException ("Connection to output system lost (invalid operation)", ex);
			}
		}


		private void DataReceivedHandler (object sender, SerialDataReceivedEventArgs e)
		{
//			// Usage example:
//			SerialPort sp = (SerialPort)sender;
//			string indata = sp.ReadExisting();
//			Console.WriteLine ("Received: {0}", indata);

			// FIXME: Should output a generic item representing the data, e.g. a string
			OnSystemDataReceived (sender, e);
		}

		private bool USB_Serial_WaitAcknowledge (double ProcessingOverhead)
		{
			if (Initialized == false)
				return false;

			bool result;
			var commandAckStopwatch = System.Diagnostics.Stopwatch.StartNew ();

			try {
				string pendingInput;
				var otherReply = new System.Text.StringBuilder ();
				do {
					pendingInput = char.ConvertFromUtf32 (USB_Serial.ReadByte ());
					if (pendingInput != "#") {
						otherReply.Append (pendingInput);
					}
					// Wait for Arduino to write the data to the LED string
				} while (pendingInput != "#");

				if (USB_Serial.BytesToRead > 0) {
					// Add the rest of the bytes
					otherReply.Append (USB_Serial.ReadExisting ());
				}

				if (otherReply.Length > 0
				    && otherReply.ToString ().Trim ().Length > 0) {
					// Print unexpected results, excluding ending newline, etc
					Console.WriteLine ("[Arduino] {0}",
						otherReply.ToString ().Trim ());
				}

				result = true;
			} catch (System.ArgumentOutOfRangeException ex) {
				Console.WriteLine ("! Error while waiting for Arduino\n\t{0}", ex.ToString ());
				result = false;
			} catch (System.IO.IOException ex) {
				Console.WriteLine ("! Error while waiting for Arduino\n\t{0}", ex.ToString ());
				result = false;
			} catch (System.TimeoutException ex_time) {
				Console.WriteLine ("! Error while waiting for Arduino\n\t{0}", ex_time.ToString ());
				result = false;
			}

			commandAckStopwatch.Stop ();
			measuredLatency = commandAckStopwatch.Elapsed.TotalMilliseconds;
			// Add in any processing delays to the total update rate
			deviceConfig.SetUpdateRate (measuredLatency, ProcessingOverhead);
#if DEBUG_USB_PERFORMANCE
			Console.WriteLine (
				"[Arduino] Acknowledged in {0,6:F2} ms ({1,6:F2} ms total)",
				measuredLatency, ProcessingOverhead + measuredLatency);
#endif
			return result;
		}

		/// <summary>
		/// Converts the given <see cref="Color"/> color component value to fit
		/// within the range of the Arduino protocol.
		/// </summary>
		/// <returns>The color value within protocol limits.</returns>
		/// <param name="ColorValue">Color color component value.</param>
		private byte ConvertColorValue (byte ColorValue)
		{
			return (byte)MathUtilities.ConvertRange (
				ColorValue,
				LightSystem.Color_MIN,
				LightSystem.Color_MAX,
				Protocol_Color_MIN,
				Protocol_Color_MAX
			);
		}

		/// <summary>
		/// Converts the given <see cref="Color.Brightness"/> value to fit
		/// within the range of the Arduino protocol.
		/// </summary>
		/// <returns>The brightness value within protocol limits.</returns>
		/// <param name="BrightnessValue">Color.Brightness value.</param>
		private byte ConvertBrightnessValue (byte BrightnessValue)
		{
			return (byte)MathUtilities.ConvertRange (
				BrightnessValue,
				LightSystem.Brightness_MIN,
				LightSystem.Brightness_MAX,
				Protocol_Brightness_MIN,
				Protocol_Brightness_MAX
			);
		}

	}
}

