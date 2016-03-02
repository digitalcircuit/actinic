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

namespace FoxSoft.Math
{
	public class MathUtilities
	{
		public MathUtilities ()
		{
		}

		#region Generic Processing

		public static Random RandomProvider = new Random ();

		public static int ConvertRange (int OldValue, int OldMin, int OldMax, int NewMin, int NewMax)
		{
			int OldRange = (OldMax - OldMin);
			if (OldRange == 0)
				return 0;
			int NewRange = (NewMax - NewMin);
			int NewValue = (((OldValue - OldMin) * NewRange) / OldRange) + NewMin;
			return NewValue;
		}

		public static double ConvertRange (double OldValue, double OldMin, double OldMax, double NewMin, double NewMax)
		{
			double OldRange = (OldMax - OldMin);
			if (OldRange == 0)
				return 0;
			double NewRange = (NewMax - NewMin);
			double NewValue = (((OldValue - OldMin) * NewRange) / OldRange) + NewMin;
			return NewValue;
		}

		public static int CapToRange (int Value, int Min, int Max)
		{
			// Return the smaller number of value, max, and return the bigger number of value, min
			return System.Math.Max (System.Math.Min (Value, Max), Min);
		}

		public static double CapToRange (double Value, double Min, double Max)
		{
			// Return the smaller number of value, max, and return the bigger number of value, min
			return System.Math.Max (System.Math.Min (Value, Max), Min);
		}

		public static double WrapNumberAround (double Min, double Max, double Number)
		{
			if (Number > Max) {
				while (Number > Max) {
					Number = Number - Max;
				}
			} else if (Number < Min) {
				while (Number < Min) {
					Number = Number + Min;
				}
			}
			return Number;
		}

		public static int WrapNumberAround (int Min, int Max, int Number)
		{
			int StepAmount;
			if (Number > Max) {
				if (Max != 0) {
					StepAmount = Max;
				} else {
					StepAmount = 1;
				}
				while (Number > Max) {
					Number = Number - StepAmount;
				}
			} else if (Number < Min) {
				if (Min != 0) {
					StepAmount = Min;
				} else {
					StepAmount = 1;
				}
				while (Number < Min) {
					Number = Number + StepAmount;
				}
			}
			return Number;
		}

		/// <summary>
		/// Averages the values, biased towards the new or old value based on freshness.
		/// </summary>
		/// <returns>
		/// The averaged result.
		/// </returns>
		/// <param name='Old'>
		/// Old value.
		/// </param>
		/// <param name='New'>
		/// New value.
		/// </param>
		/// <param name='Freshness'>
		/// How much the newer value is favored, as a percentage from 0 - 1.
		/// </param>
		public static double AverageValues (double Old, double New, double Freshness)
		{
			return (New * Freshness) + (Old * (1 - Freshness));
		}

		#endregion

		#region Display

		public static string GenerateMeterBar (double Number, double NumberMin, double NumberMax, int BarWidth, bool PrintNumber)
		{
			int measuredBarWidth = (BarWidth - 1);
			// 1 is exact, added 1 more for negative situations
			if (NumberMin < 0) {
				measuredBarWidth -= 1;
			}
			if (PrintNumber) {
				measuredBarWidth = (measuredBarWidth - 8);
				// 8 is exact, added 1 more for negative situations
				if (NumberMin < 0) {
					measuredBarWidth -= 1;
				}
			}
			if (measuredBarWidth < 1) {
				throw new ArgumentOutOfRangeException ("BarWidth", measuredBarWidth, "Number must be large enough to print a bar (approximately 5 - 8 characters)");
			}
			string result = "";
			if (NumberMin < 0) {
				measuredBarWidth = measuredBarWidth / 2;
				if (Number < 0) {
					result = new String (' ', (int)ConvertRange (Number, NumberMin, 0, 0, measuredBarWidth)).PadRight (measuredBarWidth, '=') + "#" + new String (' ', measuredBarWidth) + "|";
				} else {
					result = new String (' ', measuredBarWidth) + "#" + new String ('=', (int)ConvertRange (Number, 0, NumberMax, 0, measuredBarWidth)).PadRight (measuredBarWidth, ' ') + "|";
				}
			} else {
				result = new String ('=', (int)ConvertRange (Number, NumberMin, NumberMax, 0, measuredBarWidth)).PadRight (measuredBarWidth, ' ') + "|";
			}
			if (PrintNumber) {
				if (NumberMin < 0) {
					string Number_Sign = " ";
					if (Number < 0) {
						Number_Sign = "-";
					}
					result += " (" + Number_Sign + System.Math.Abs (System.Math.Round (Number, 3)).ToString ().PadRight (5, ' ') + ")";
				} else {
					result += " (" + System.Math.Round (Number, 3).ToString ().PadRight (5, ' ') + ")";
				}
			}
			return result;
		}

	#endregion

	}
}

