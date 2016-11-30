//
//  MathUtilities.cs
//
//  Author:
//       Shane Synan <digitalcircuit36939@gmail.com>
//
//  Copyright (c) 2013 - 2016
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

namespace FoxSoft.Utilities
{
	/// <summary>
	/// A collection of useful mathematics utilities.
	/// </summary>
	public static class MathUtilities
	{
		#region Generic Processing
		/// <summary>
		/// Converts an input from one range of values to another.
		/// </summary>
		/// <returns>The new output within the range of new values.</returns>
		/// <param name="OldValue">Old value.</param>
		/// <param name="OldMin">Old minimum range.</param>
		/// <param name="OldMax">Old maximum range.</param>
		/// <param name="NewMin">New minimum range.</param>
		/// <param name="NewMax">New maximum range.</param>
		public static int ConvertRange (int OldValue, int OldMin, int OldMax, int NewMin, int NewMax)
		{
			int OldRange = (OldMax - OldMin);
			if (OldRange == 0)
				return 0;
			int NewRange = (NewMax - NewMin);
			// Form a ratio from the old value, the old minimum, with the new range
			int NewValue = (((OldValue - OldMin) * NewRange) / OldRange) + NewMin;
			return NewValue;
		}

		/// <summary>
		/// Converts an input from one range of values to another.
		/// </summary>
		/// <returns>The new output within the range of new values.</returns>
		/// <param name="OldValue">Old value.</param>
		/// <param name="OldMin">Old minimum range.</param>
		/// <param name="OldMax">Old maximum range.</param>
		/// <param name="NewMin">New minimum range.</param>
		/// <param name="NewMax">New maximum range.</param>
		public static double ConvertRange (double OldValue, double OldMin, double OldMax, double NewMin, double NewMax)
		{
			double OldRange = (OldMax - OldMin);
			if (OldRange == 0)
				return 0;
			double NewRange = (NewMax - NewMin);
			// Form a ratio from the old value, the old minimum, with the new range
			double NewValue = (((OldValue - OldMin) * NewRange) / OldRange) + NewMin;
			return NewValue;
		}

		/// <summary>
		/// Caps a given value to range of minimum and maximum.
		/// </summary>
		/// <returns>The value within the specified range.</returns>
		/// <param name="Value">Value to cap.</param>
		/// <param name="Min">Minimum range.</param>
		/// <param name="Max">Maximum range.</param>
		public static int CapToRange (int Value, int Min, int Max)
		{
			// Return the smaller number of value, max, and return the bigger number of value, min
			return System.Math.Max (System.Math.Min (Value, Max), Min);
		}

		/// <summary>
		/// Caps a given value to range of minimum and maximum.
		/// </summary>
		/// <returns>The value within the specified range.</returns>
		/// <param name="Value">Value to cap.</param>
		/// <param name="Min">Minimum range.</param>
		/// <param name="Max">Maximum range.</param>
		public static double CapToRange (double Value, double Min, double Max)
		{
			// Return the smaller number of value, max, and return the bigger number of value, min
			return System.Math.Max (System.Math.Min (Value, Max), Min);
		}

		// Wrapping equation thanks to ares_games on StackOverflow.
		// See https://stackoverflow.com/questions/14415753/wrap-value-into-range-min-max-without-division

		/// <summary>
		/// Wraps a given number around to fit within the minimum and maximum, rather than chopping it off.
		/// </summary>
		/// <returns>The value wrapped around to the specified range.</returns>
		/// <param name="Value">Value to wrap around.</param>
		/// <param name="Min">Minimum range.</param>
		/// <param name="Max">Maximum range.</param>
		public static double WrapAround (double Value, double Min, double Max)
		{
			if (Value > Max || Value < Min) {
				// Outside range, wrap number around
				return Value - (Max - Min) * Math.Floor (Value / (Max - Min));
			} else {
				return Value;
			}
		}

		/// <summary>
		/// Wraps a given number around to fit within the minimum and maximum, rather than chopping it off.
		/// </summary>
		/// <returns>The value wrapped around to the specified range.</returns>
		/// <param name="Value">Value to wrap around.</param>
		/// <param name="Min">Minimum range.</param>
		/// <param name="Max">Maximum range.</param>
		public static int WrapAround (int Value, int Min, int Max)
		{
			if (Value > Max || Value < Min) {
				// Outside range, wrap number around
				return Value - (Max - Min) * (int)Math.Floor ((double)Value / (Max - Min));
			} else {
				return Value;
			}
		}

		/// <summary>
		/// Averages the values biased towards the new or old value based on freshness.
		/// </summary>
		/// <returns>The averaged result.</returns>
		/// <param name="Old">Old value.</param>
		/// <param name="New">New value.</param>
		/// <param name="Freshness">How much the newer value is favored as a percentage from 0 - 1.</param>
		public static double AverageValues (double Old, double New, double Freshness)
		{
			return (New * Freshness) + (Old * (1 - Freshness));
		}

		#endregion

		#region Display

		/// <summary>
		/// Generates a printable meter bar representing the given values.
		/// </summary>
		/// <returns>The meter bar as a string, usable for console or text-file saving.</returns>
		/// <param name="Number">Number.</param>
		/// <param name="NumberMin">Minimum range of the number.</param>
		/// <param name="NumberMax">Maximum range of the number.</param>
		/// <param name="BarWidth">Width of the generated meter bar.</param>
		/// <param name="ShowNumber">If set to <c>true</c> include the number as part of the meter bar.</param>
		public static string GenerateMeterBar (double Number, double NumberMin, double NumberMax,
			int BarWidth, bool ShowNumber)
		{
			int measuredBarWidth = (BarWidth - 1);
			// 1 is exact, added 1 more for negative situations
			if (NumberMin < 0) {
				measuredBarWidth -= 1;
			}
			if (ShowNumber) {
				measuredBarWidth = (measuredBarWidth - 8);
				// 8 is exact, added 1 more for negative situations
				if (NumberMin < 0) {
					measuredBarWidth -= 1;
				}
			}
			if (measuredBarWidth < 1) {
				throw new ArgumentOutOfRangeException ("BarWidth", measuredBarWidth,
					"Number must be large enough to print a bar (approximately 5 - 8 characters)");
			}
			string result = "";
			if (NumberMin < 0) {
				measuredBarWidth = measuredBarWidth / 2;
				if (Number < 0) {
					result = new String (' ',
						(int)ConvertRange (Number, NumberMin, 0, 0, measuredBarWidth)).PadRight (measuredBarWidth, '=')
						+ "#" + new String (' ', measuredBarWidth) + "|";
				} else {
					result = new String (' ', measuredBarWidth) + "#" + new String ('=',
						(int)ConvertRange (Number, 0, NumberMax, 0, measuredBarWidth)).PadRight (measuredBarWidth, ' ')
						+ "|";
				}
			} else {
				result = new String ('=',
					(int)ConvertRange (Number, NumberMin, NumberMax, 0, measuredBarWidth))
					.PadRight (measuredBarWidth, ' ') + "|";
			}
			if (ShowNumber) {
				if (NumberMin < 0) {
					string Number_Sign = " ";
					if (Number < 0) {
						Number_Sign = "-";
					}
					result += " (" + Number_Sign + System.Math.Abs (System.Math.Round (Number, 3)).ToString ()
						.PadRight (5, ' ') + ")";
				} else {
					result += " (" + System.Math.Round (Number, 3).ToString ().PadRight (5, ' ') + ")";
				}
			}
			return result;
		}

		#endregion

	}
}

