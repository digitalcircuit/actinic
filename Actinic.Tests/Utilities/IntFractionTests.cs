//
//  IntFractionTests.cs
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
using NUnit.Framework;
using System;

using Actinic.Utilities;

namespace Actinic.Tests
{
	[TestFixture ()]
	public class IntFractionTests
	{
		#region Construction

		[Test]
		public void Construct_Default ()
		{
			// [Arrange]
			// Nothing to do

			// [Act]
			IntFraction sampleNumber = new IntFraction ();

			// [Assert]
			Assert.That (sampleNumber.IntValue,
				Is.EqualTo (0)
			);

			Assert.That (sampleNumber.DecimalValue,
				Is.EqualTo (0)
			);
		}

		[Test]
		public void Construct_Direct ()
		{
			// [Arrange]
			const double minValue = -2;
			const double maxValue = 2;
			const double step = 0.25;

			// [Act/Assert]
			for (double value = minValue; value < maxValue; value += step) {
				IntFraction sampleNumber = new IntFraction (value);

				Assert.That (sampleNumber.IntValue,
					Is.EqualTo ((int)Math.Truncate (value)),
					"Integer part does not match"
				);

				Assert.That (sampleNumber.DecimalValue,
					Is.EqualTo (value),
					"Decimal part does not match"
				);
			}
		}

		#endregion

		#region Functions - TakeInt

		[Test]
		public void TakeInt_Test ()
		{
			// [Arrange]
			double[,] testPairs = new double[,] {
				{ -2.01, -2 },
				{ -2, -2 },
				{ -1.99, -1 },
				{ -1.01, -1 },
				{ -1, -1 },
				{ -0.99, 0 },
				{ -0.01, 0 },
				{ 0, 0 },
				{ 0.01, 0 },
				{ 0.99, 0 },
				{ 1, 1 },
				{ 1.01, 1 },
				{ 1.99, 1 },
				{ 2, 2 },
				{ 2.01, 2 },
			};

			// [Act/Assert]
			for (int i = 0; i < testPairs.GetLength (0); i++) {
				// Load test case
				double customValue = testPairs [i, 0];
				double expectedInteger = testPairs [i, 1];
				// Set up expectations
				double expectedDecimal = customValue - expectedInteger;
				int resultInteger;
				double resultDecimal;
				IntFraction sampleValue = new IntFraction (customValue);

				// Act
				resultInteger = sampleValue.TakeInt ();
				resultDecimal = sampleValue.DecimalValue;

				// Assert
				Assert.That (resultInteger,
					Is.EqualTo (expectedInteger),
					"Integer value does not match"
				);

				Assert.That (resultDecimal,
					Is.EqualTo (expectedDecimal),
					"Decimal value does not match"
				);

				Assert.That (sampleValue.IntValue,
					Is.EqualTo (0),
					"Integer value remains after calling TakeInt()"
				);
			}
		}

		#endregion

		#region Functions - ToString

		[Test]
		public void ToString_Test ()
		{
			// [Arrange]
			const double customValue = 1.5;
			IntFraction sampleValue = new IntFraction (customValue);

			// Prepare the expected result of converting to string
			string expectedResult =
				string.Format (
					"[IntFraction: IntValue={0}, DecimalValue={1}]",
					sampleValue.IntValue,
					sampleValue.DecimalValue
				);

			// [Act/Assert]
			Assert.That (sampleValue.ToString (),
				Is.EqualTo (expectedResult)
			);
		}

		#endregion

		#region Functions - Operators (Unary)

		[Test]
		public void OperatorAddition_Valid_Test (
			[Values (-1, 0.1, 1, 1.1)] double customValue)
		{
			// [Arrange]
			const double startingValue = 1.5;
			double expectedDecimalResult = customValue + startingValue;
			IntFraction sampleValue = new IntFraction (startingValue);

			// [Act]
			sampleValue += customValue;

			// [Assert]
			Assert.That (sampleValue.DecimalValue,
				Is.EqualTo (expectedDecimalResult),
				"Decimal value does not match"
			);
		}

		[Test]
		public void OperatorAddition_Null_Test ()
		{
			// [Arrange]
			IntFraction sampleValue = new IntFraction ();

			// [Act/Assert]
			Assert.That (
				delegate {
					sampleValue = sampleValue + null;
				},
				Throws.TypeOf<ArgumentNullException> ()
				.With.Property ("ParamName").EqualTo ("right")
			);

			Assert.That (
				delegate {
					sampleValue = null + sampleValue;
				},
				Throws.TypeOf<ArgumentNullException> ()
				.With.Property ("ParamName").EqualTo ("left")
			);
		}

		[Test]
		public void OperatorSubtraction_Test (
			[Values (-1, 0.1, 1, 1.1)] double customValue)
		{
			// [Arrange]
			const double startingValue = 1.5;
			double expectedDecimalResult = startingValue - customValue;
			IntFraction sampleValue = new IntFraction (startingValue);

			// [Act]
			sampleValue -= customValue;

			// [Assert]
			Assert.That (sampleValue.DecimalValue,
				Is.EqualTo (expectedDecimalResult),
				"Decimal value does not match"
			);
		}

		[Test]
		public void OperatorSubtraction_Null_Test ()
		{
			// [Arrange]
			IntFraction sampleValue = new IntFraction ();

			// [Act/Assert]
			Assert.That (
				delegate {
					sampleValue = sampleValue - null;
				},
				Throws.TypeOf<ArgumentNullException> ()
				.With.Property ("ParamName").EqualTo ("right")
			);

			Assert.That (
				delegate {
					sampleValue = null - sampleValue;
				},
				Throws.TypeOf<ArgumentNullException> ()
				.With.Property ("ParamName").EqualTo ("left")
			);
		}

		[Test]
		public void OperatorMultiplication_Test (
			[Values (-1, 0.1, 1, 1.1)] double customValue)
		{
			// [Arrange]
			const double startingValue = 1.5;
			double expectedDecimalResult = startingValue * customValue;
			IntFraction sampleValue = new IntFraction (startingValue);

			// [Act]
			sampleValue *= customValue;

			// [Assert]
			Assert.That (sampleValue.DecimalValue,
				Is.EqualTo (expectedDecimalResult),
				"Decimal value does not match"
			);
		}

		[Test]
		public void OperatorMultiplication_Null_Test ()
		{
			// [Arrange]
			IntFraction sampleValue = new IntFraction ();

			// [Act/Assert]
			Assert.That (
				delegate {
					sampleValue = sampleValue * null;
				},
				Throws.TypeOf<ArgumentNullException> ()
				.With.Property ("ParamName").EqualTo ("right")
			);

			Assert.That (
				delegate {
					sampleValue = null * sampleValue;
				},
				Throws.TypeOf<ArgumentNullException> ()
				.With.Property ("ParamName").EqualTo ("left")
			);
		}

		[Test]
		public void OperatorDivision_Test (
			[Values (-1, 0.1, 1, 1.1)] double customValue)
		{
			// [Arrange]
			const double startingValue = 1.5;
			double expectedDecimalResult = startingValue / customValue;
			IntFraction sampleValue = new IntFraction (startingValue);

			// [Act]
			sampleValue /= customValue;

			// [Assert]
			Assert.That (sampleValue.DecimalValue,
				Is.EqualTo (expectedDecimalResult),
				"Decimal value does not match"
			);
		}

		[Test]
		public void OperatorDivision_Null_Test ()
		{
			// [Arrange]
			IntFraction sampleValue = new IntFraction ();

			// [Act/Assert]
			Assert.That (
				delegate {
					sampleValue = sampleValue / null;
				},
				Throws.TypeOf<ArgumentNullException> ()
				.With.Property ("ParamName").EqualTo ("right")
			);

			Assert.That (
				delegate {
					sampleValue = null / sampleValue;
				},
				Throws.TypeOf<ArgumentNullException> ()
				.With.Property ("ParamName").EqualTo ("left")
			);
		}

		[Test]
		public void OperatorDivision_DivideByZero_Test ()
		{
			// [Arrange]
			IntFraction sampleValueNonZero = new IntFraction (1.5);
			IntFraction sampleValueZero = new IntFraction (0);

			// [Act/Assert]
			Assert.That (
				delegate {
					sampleValueNonZero = sampleValueNonZero / sampleValueZero;
				},
				Throws.TypeOf<DivideByZeroException> ()
			);
		}

		[Test]
		public void OperatorIncrement_Prefix_Test ()
		{
			// [Arrange]
			const double customValue = 1.5;
			double customValueModifiable = customValue;
			double expectedDecimalResultCall = ++customValueModifiable;
			double expectedDecimalResultAfter = customValueModifiable;

			IntFraction sampleValue = new IntFraction (customValue);
			IntFraction sampleValueCall;
			IntFraction sampleValueAfter;

			// [Act]
			sampleValueCall = ++sampleValue;
			sampleValueAfter = sampleValue;

			// [Assert]
			Assert.That (sampleValueCall.DecimalValue,
				Is.EqualTo (expectedDecimalResultCall),
				"Decimal value from call does not match"
			);

			Assert.That (sampleValueAfter.DecimalValue,
				Is.EqualTo (expectedDecimalResultAfter),
				"Decimal value after call does not match"
			);
		}

		[Test]
		public void OperatorIncrement_Postfix_Test ()
		{
			// [Arrange]
			const double customValue = 1.5;
			double customValueModifiable = customValue;
			double expectedDecimalResultCall = customValueModifiable++;
			double expectedDecimalResultAfter = customValueModifiable;

			IntFraction sampleValue = new IntFraction (customValue);
			IntFraction sampleValueCall;
			IntFraction sampleValueAfter;

			// [Act]
			sampleValueCall = sampleValue++;
			sampleValueAfter = sampleValue;

			// [Assert]
			Assert.That (sampleValueCall.DecimalValue,
				Is.EqualTo (expectedDecimalResultCall),
				"Decimal value from call does not match"
			);

			Assert.That (sampleValueAfter.DecimalValue,
				Is.EqualTo (expectedDecimalResultAfter),
				"Decimal value after call does not match"
			);
		}

		[Test]
		public void OperatorIncrement_Null_Test ()
		{
			// [Arrange]
			IntFraction sampleValueNull = null;

			// [Act/Assert]
			Assert.That (
				delegate {
					sampleValueNull++;
				},
				Throws.TypeOf<ArgumentNullException> ()
				.With.Property ("ParamName").EqualTo ("value")
			);
		}

		[Test]
		public void OperatorDecrement_Prefix_Test ()
		{
			// [Arrange]
			const double customValue = 1.5;
			double customValueModifiable = customValue;
			double expectedDecimalResultCall = --customValueModifiable;
			double expectedDecimalResultAfter = customValueModifiable;

			IntFraction sampleValue = new IntFraction (customValue);
			IntFraction sampleValueCall;
			IntFraction sampleValueAfter;

			// [Act]
			sampleValueCall = --sampleValue;
			sampleValueAfter = sampleValue;

			// [Assert]
			Assert.That (sampleValueCall.DecimalValue,
				Is.EqualTo (expectedDecimalResultCall),
				"Decimal value from call does not match"
			);

			Assert.That (sampleValueAfter.DecimalValue,
				Is.EqualTo (expectedDecimalResultAfter),
				"Decimal value after call does not match"
			);
		}

		[Test]
		public void OperatorDecrement_Postfix_Test ()
		{
			// [Arrange]
			const double customValue = 1.5;
			double customValueModifiable = customValue;
			double expectedDecimalResultCall = customValueModifiable--;
			double expectedDecimalResultAfter = customValueModifiable;

			IntFraction sampleValue = new IntFraction (customValue);
			IntFraction sampleValueCall;
			IntFraction sampleValueAfter;

			// [Act]
			sampleValueCall = sampleValue--;
			sampleValueAfter = sampleValue;

			// [Assert]
			Assert.That (sampleValueCall.DecimalValue,
				Is.EqualTo (expectedDecimalResultCall),
				"Decimal value from call does not match"
			);

			Assert.That (sampleValueAfter.DecimalValue,
				Is.EqualTo (expectedDecimalResultAfter),
				"Decimal value after call does not match"
			);
		}

		[Test]
		public void OperatorDecrement_Null_Test ()
		{
			// [Arrange]
			IntFraction sampleValueNull = null;

			// [Act/Assert]
			Assert.That (
				delegate {
					sampleValueNull--;
				},
				Throws.TypeOf<ArgumentNullException> ()
				.With.Property ("ParamName").EqualTo ("value")
			);
		}

		#endregion

		#region Functions - Operators (Comparison: Equality)

		[Test]
		public void Equals_Same_Test ()
		{
			// [Arrange]
			const double customValue = 1.5;
			IntFraction sampleValue = new IntFraction (customValue);
			IntFraction sampleValueSame = new IntFraction (customValue);

			// [Act/Assert]
			Assert.That (sampleValue,
				Is.EqualTo (sampleValueSame),
				"Values do not match"
			);
		}

		[Test]
		public void Equals_Different_Test ()
		{
			// [Arrange]
			const double customValue = 1.5;
			IntFraction sampleValue = new IntFraction (customValue);
			IntFraction sampleValueDifferent =
				new IntFraction (customValue + 1);

			// [Act/Assert]
			Assert.That (sampleValue,
				Is.Not.EqualTo (sampleValueDifferent),
				"Values incorrectly match"
			);
		}

		[Test]
		public void Equals_SameCastedObj_Test ()
		{
			// [Arrange]
			const double customValue = 1.5;
			IntFraction sampleValue = new IntFraction (customValue);
			IntFraction sampleValueSame = new IntFraction (customValue);

			// [Act/Assert]
			Assert.That (sampleValue,
				Is.EqualTo ((object)sampleValueSame),
				"Values casted by (object) do not match"
			);
		}

		// Unable to test .Equals on a null object.  For a similar test, see
		// OperatorEquals test below

		[Test]
		public void Equals_DifferentNull_Test ()
		{
			// [Arrange]
			IntFraction sampleValueNotNull = new IntFraction (1.5);

			// [Act/Assert]
			Assert.That (sampleValueNotNull,
				Is.Not.EqualTo (null),
				"Non-null matches null value"
			);
		}

		[Test]
		public void OperatorEquals_Matches_Same_Test ()
		{
			// [Arrange]
			const double customValue = 1.5;
			IntFraction sampleValue = new IntFraction (customValue);
			IntFraction sampleValueSame = new IntFraction (customValue);

			bool resultSameOperator;
			bool resultSameEquals;

			// [Act]
			resultSameOperator = (sampleValue == sampleValueSame);
			resultSameEquals = (sampleValue.Equals (sampleValueSame));

			// [Assert]
			Assert.That (resultSameOperator,
				Is.EqualTo (resultSameEquals),
				"== operator does not match .Equals() for equal values"
			);
		}

		[Test]
		public void OperatorEquals_Matches_Different_Test ()
		{
			// [Arrange]
			const double customValue = 1.5;
			IntFraction sampleValue = new IntFraction (customValue);
			IntFraction sampleValueDifferent =
				new IntFraction (customValue + 1);

			bool resultDifferentOperator;
			bool resultDifferentEquals;

			// [Act]
			resultDifferentOperator = (sampleValue == sampleValueDifferent);
			resultDifferentEquals = (sampleValue.Equals (sampleValueDifferent));

			// [Assert]
			Assert.That (resultDifferentOperator,
				Is.EqualTo (resultDifferentEquals),
				"== operator does not match .Equals() for differing values"
			);
		}

		[Test]
		public void OperatorEquals_SameNull_Test ()
		{
			// [Arrange]
			IntFraction sampleValueNull = null;
			bool result;

			// [Act]
			result = (sampleValueNull == null);

			// [Assert]
			Assert.That (result,
				Is.True,
				"Null values do not match"
			);
		}

		#endregion

		#region Functions - Operators (Comparison: General)

		[Test]
		public void OperatorGreaterThan_Test ()
		{
			// [Arrange]
			IntFraction sampleValueSmall = new IntFraction (1.5);
			IntFraction sampleValueSmallSame = new IntFraction (1.5);
			IntFraction sampleValueLarge = new IntFraction (2.5);

			// [Act/Assert]
			Assert.That ((sampleValueLarge > sampleValueSmall),
				Is.True,
				"Larger value not greater than smaller value"
			);

			Assert.That ((sampleValueSmall > sampleValueSmallSame),
				Is.Not.True,
				"Same value greater than itself"
			);

			Assert.That ((sampleValueSmall > sampleValueLarge),
				Is.Not.True,
				"Smaller value greater than larger value"
			);
		}

		[Test]
		public void OperatorGreaterThanOrEqualTo_Test ()
		{
			// [Arrange]
			IntFraction sampleValueSmall = new IntFraction (1.5);
			IntFraction sampleValueSmallSame = new IntFraction (1.5);
			IntFraction sampleValueLarge = new IntFraction (2.5);

			// [Act/Assert]
			Assert.That ((sampleValueLarge >= sampleValueSmall),
				Is.True,
				"Larger value not greater than smaller value"
			);

			Assert.That ((sampleValueSmall >= sampleValueSmallSame),
				Is.True,
				"Same value not equal to itself"
			);

			Assert.That ((sampleValueSmall >= sampleValueLarge),
				Is.Not.True,
				"Smaller value greater than larger value"
			);
		}

		[Test]
		public void OperatorLessThan_Test ()
		{
			// [Arrange]
			IntFraction sampleValueSmall = new IntFraction (1.5);
			IntFraction sampleValueSmallSame = new IntFraction (1.5);
			IntFraction sampleValueLarge = new IntFraction (2.5);

			// [Act/Assert]
			Assert.That ((sampleValueSmall < sampleValueLarge),
				Is.True,
				"Smaller value not less than larger value"
			);

			Assert.That ((sampleValueSmall < sampleValueSmallSame),
				Is.Not.True,
				"Same value smaller than itself"
			);

			Assert.That ((sampleValueLarge < sampleValueSmall),
				Is.Not.True,
				"Larger value less than smaller value"
			);
		}

		[Test]
		public void OperatorLessThanOrEqualTo_Test ()
		{
			// [Arrange]
			IntFraction sampleValueSmall = new IntFraction (1.5);
			IntFraction sampleValueSmallSame = new IntFraction (1.5);
			IntFraction sampleValueLarge = new IntFraction (2.5);

			// [Act/Assert]
			Assert.That ((sampleValueSmall <= sampleValueLarge),
				Is.True,
				"Smaller value not less than larger value"
			);

			Assert.That ((sampleValueSmall <= sampleValueSmallSame),
				Is.True,
				"Same value not equal to itself"
			);

			Assert.That ((sampleValueLarge <= sampleValueSmall),
				Is.Not.True,
				"Larger value less than smaller value"
			);
		}

		#endregion

		#region Functions - CompareTo

		[Test]
		public void CompareToGreaterThan_Test ()
		{
			// [Arrange]
			IntFraction sampleValueSmall = new IntFraction (1.5);
			IntFraction sampleValueLarge = new IntFraction (2.5);

			// [Act/Assert]
			Assert.That (sampleValueLarge,
				Is.GreaterThan (sampleValueSmall),
				"Larger value not greater than smaller value"
			);

			Assert.That (sampleValueSmall,
				Is.Not.GreaterThan (sampleValueSmall),
				"Same value greater than itself"
			);

			Assert.That (sampleValueSmall,
				Is.Not.GreaterThan (sampleValueLarge),
				"Smaller value greater than larger value"
			);
		}

		[Test]
		public void CompareToGreaterThanOrEqualTo_Test ()
		{
			// [Arrange]
			IntFraction sampleValueSmall = new IntFraction (1.5);
			IntFraction sampleValueLarge = new IntFraction (2.5);

			// [Act/Assert]
			Assert.That (sampleValueLarge,
				Is.GreaterThanOrEqualTo (sampleValueSmall),
				"Larger value not greater than smaller value"
			);

			Assert.That (sampleValueSmall,
				Is.GreaterThanOrEqualTo (sampleValueSmall),
				"Same value not equal to itself"
			);

			Assert.That (sampleValueSmall,
				Is.Not.GreaterThanOrEqualTo (sampleValueLarge),
				"Smaller value greater than larger value"
			);
		}

		[Test]
		public void CompareToLessThan_Test ()
		{
			// [Arrange]
			IntFraction sampleValueSmall = new IntFraction (1.5);
			IntFraction sampleValueLarge = new IntFraction (2.5);

			// [Act/Assert]
			Assert.That (sampleValueSmall,
				Is.LessThan (sampleValueLarge),
				"Smaller value not less than larger value"
			);

			Assert.That (sampleValueSmall,
				Is.Not.LessThan (sampleValueSmall),
				"Same value smaller than itself"
			);

			Assert.That (sampleValueLarge,
				Is.Not.LessThan (sampleValueSmall),
				"Larger value less than smaller value"
			);
		}

		[Test]
		public void CompareToLessThanOrEqualTo_Test ()
		{
			// [Arrange]
			IntFraction sampleValueSmall = new IntFraction (1.5);
			IntFraction sampleValueLarge = new IntFraction (2.5);

			// [Act/Assert]
			Assert.That (sampleValueSmall,
				Is.LessThanOrEqualTo (sampleValueLarge),
				"Smaller value not less than larger value"
			);

			Assert.That (sampleValueSmall,
				Is.LessThanOrEqualTo (sampleValueSmall),
				"Same value not equal to itself"
			);

			Assert.That (sampleValueLarge,
				Is.Not.LessThanOrEqualTo (sampleValueSmall),
				"Larger value less than smaller value"
			);
		}

		#endregion

		#region Functions - GetHashCode

		[Test]
		public void GetHashCode_Reasonable_Test ()
		{
			// [Arrange]
			const double customValue = 1.5;
			IntFraction sampleValue = new IntFraction (customValue);

			bool result;

			// [Act]
			// Check that something reasonable happens (non-exhaustive test)
			result = (sampleValue.GetHashCode () == customValue.GetHashCode ());

			// [Assert]
			Assert.That (result,
				Is.True,
				"Hash codes do not match"
			);
		}

		#endregion

		#region Functions - Operators (Conversion)

		[Test]
		public void OperatorCastDouble_Test ()
		{
			// [Arrange]
			const double customValue = 1.5;
			IntFraction sampleValue;

			// [Act]
			sampleValue = customValue;

			// [Assert]
			Assert.That (sampleValue.DecimalValue,
				Is.EqualTo (customValue),
				"Decimal value does not match"
			);
		}

		[Test]
		public void OperatorCastInt_Test ()
		{
			// [Arrange]
			const int customValue = 1;
			IntFraction sampleValue;

			// [Act]
			sampleValue = customValue;

			// [Assert]
			Assert.That (sampleValue.DecimalValue,
				Is.EqualTo (customValue),
				"Decimal value does not match"
			);
		}

		#endregion
	}
}

