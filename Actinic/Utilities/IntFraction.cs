//
//  IntFraction.cs
//
//  Author:
//       Shane Synan <digitalcircuit36939@gmail.com>
//
//  Copyright (c) 2019
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

namespace Actinic.Utilities
{
	/// <summary>
	/// Integer that tracks fractional progress towards the next integer value.
	/// </summary>
	public class IntFraction : IEquatable<IntFraction>, IComparable<IntFraction>
	{

		#region Constructors

		/// <summary>
		/// Initializes a new fractional integer.
		/// </summary>
		/// <param name="Value">Starting value.</param>
		public IntFraction (double Value = 0)
		{
			fullValue = Value;
		}

		#endregion

		#region Properties

		/// <summary>
		/// Gets the whole part of the stored value.
		/// </summary>
		/// <value>The whole part of the value.</value>
		public int IntValue {
			get {
				// Chop off below the decimal point
				return (int)Math.Truncate (fullValue);
			}
		}

		/// <summary>
		/// Gets the full stored value.
		/// </summary>
		/// <value>The full value.</value>
		public double DecimalValue {
			get {
				return fullValue;
			}
		}

		#endregion

		#region Shared Functions

		/// <summary>
		/// Takes the whole part of the number, subtracting it from the stored
		/// value.
		/// </summary>
		/// <returns>The whole part of the number.</returns>
		public int TakeInt ()
		{
			// Store the whole part
			int intValue = IntValue;

			// Subtract the whole from the fraction
			fullValue -= intValue;

			return intValue;
		}

		/// <summary>
		/// Returns a string that represents the current
		/// <see cref="IntFraction"/>.
		/// </summary>
		/// <returns>A string that represents the current <see cref="IntFraction"/>.</returns>
		public override string ToString ()
		{
			return string.Format (
				"[IntFraction: IntValue={0}, DecimalValue={1}]",
				IntValue, DecimalValue);
		}

		#endregion

		#region Operators

		public static IntFraction operator + (
			IntFraction left, IntFraction right)
		{
			if (ReferenceEquals (left, null)) {
				throw new ArgumentNullException ("left");
			}
			if (ReferenceEquals (right, null)) {
				throw new ArgumentNullException ("right");
			}
			return new IntFraction (left.fullValue + right.fullValue);
		}

		public static IntFraction operator - (
			IntFraction left, IntFraction right)
		{
			if (ReferenceEquals (left, null)) {
				throw new ArgumentNullException ("left");
			}
			if (ReferenceEquals (right, null)) {
				throw new ArgumentNullException ("right");
			}
			return new IntFraction (left.fullValue - right.fullValue);
		}

		public static IntFraction operator * (
			IntFraction left, IntFraction right)
		{
			if (ReferenceEquals (left, null)) {
				throw new ArgumentNullException ("left");
			}
			if (ReferenceEquals (right, null)) {
				throw new ArgumentNullException ("right");
			}
			return new IntFraction (left.fullValue * right.fullValue);
		}

		public static IntFraction operator / (
			IntFraction left, IntFraction right)
		{
			if (ReferenceEquals (left, null)) {
				throw new ArgumentNullException ("left");
			}
			if (ReferenceEquals (right, null)) {
				throw new ArgumentNullException ("right");
			}
			if (right.fullValue == 0) {
				throw new DivideByZeroException ();
			}
			return new IntFraction (left.fullValue / right.fullValue);
		}

		public static IntFraction operator ++ (IntFraction value)
		{
			if (ReferenceEquals (value, null)) {
				throw new ArgumentNullException ("value");
			}
			return new IntFraction (value.fullValue + 1);
		}

		public static IntFraction operator -- (IntFraction value)
		{
			if (ReferenceEquals (value, null)) {
				throw new ArgumentNullException ("value");
			}
			return new IntFraction (value.fullValue - 1);
		}

		// Provide the rest of the operators, as per the following
		// See https://docs.microsoft.com/en-us/visualstudio/code-quality/ca1013-overload-operator-equals-on-overloading-add-and-subtract?view=vs-2017
		// And https://docs.microsoft.com/en-us/previous-versions/dotnet/netframework-4.0/336aedhh(v=vs.100)
		// And https://stackoverflow.com/questions/25461585/operator-overloading-equals

		public override bool Equals (object obj)
		{
			// Check for null values
			if (ReferenceEquals (null, obj)) {
				return false;
			}
			if (ReferenceEquals (this, obj)) {
				return true;
			}

			return obj.GetType () == GetType () && Equals ((IntFraction)obj);
		}

		public bool Equals (IntFraction other)
		{
			if (ReferenceEquals (null, other)) {
				return false;
			}
			if (ReferenceEquals (this, other)) {
				return true;
			}

			// Check the underlying type
			return fullValue.Equals (other.fullValue);
		}

		public override int GetHashCode ()
		{
			return fullValue.GetHashCode ();
		}

		public static bool operator == (IntFraction left, IntFraction right)
		{
			return IntFraction.Equals (left, right);
		}

		public static bool operator != (IntFraction left, IntFraction right)
		{
			return !IntFraction.Equals (left, right);
		}

		public static bool operator > (IntFraction left, IntFraction right)
		{
			if (ReferenceEquals (left, null)) {
				throw new ArgumentNullException ("left");
			}
			if (ReferenceEquals (right, null)) {
				throw new ArgumentNullException ("right");
			}
			return left.fullValue > right.fullValue;
		}

		public static bool operator < (IntFraction left, IntFraction right)
		{
			if (ReferenceEquals (left, null)) {
				throw new ArgumentNullException ("left");
			}
			if (ReferenceEquals (right, null)) {
				throw new ArgumentNullException ("right");
			}
			return left.fullValue < right.fullValue;
		}

		public static bool operator >= (IntFraction left, IntFraction right)
		{
			if (ReferenceEquals (left, null)) {
				throw new ArgumentNullException ("left");
			}
			if (ReferenceEquals (right, null)) {
				throw new ArgumentNullException ("right");
			}
			return left.fullValue >= right.fullValue;
		}

		public static bool operator <= (IntFraction left, IntFraction right)
		{
			if (ReferenceEquals (left, null)) {
				throw new ArgumentNullException ("left");
			}
			if (ReferenceEquals (right, null)) {
				throw new ArgumentNullException ("right");
			}
			return left.fullValue <= right.fullValue;
		}

		public int CompareTo (IntFraction other)
		{
			return this.fullValue.CompareTo (other.fullValue);
		}

		// Allow easy operator usage
		public static implicit operator IntFraction (int i)
		{
			return new IntFraction (i);
		}

		public static implicit operator IntFraction (double d)
		{
			return new IntFraction (d);
		}

		#endregion

		#region Internal

		/// <summary>
		/// The full value, whole and fractional parts
		/// </summary>
		private double fullValue = 0;

		#endregion
	}
}

