/*******************************************************************************
Copyright 2017 Technische Hochschule Ingolstadt

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"),
to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, 
and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions: 

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS 
IN THE SOFTWARE.
***********************************************************************************/

using System;

namespace VRLate
{
	public class NumeralSystem
	{
		private const string DIGITS = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
		private const int LONG_BITS = 64;

		public static long ArbitraryToDecimal (string number, int radix)
		{

			if (radix < 2 || radix > DIGITS.Length) {
				throw new ArgumentException ("Radix must be greater or equals 2 and smaller then "
				+ DIGITS.Length.ToString ());
			}

			if (String.IsNullOrEmpty (number)) {
				return 0;
			}

			number = number.ToUpperInvariant ();

			long result = 0;
			long multiplier = 1;
			for (int i = number.Length - 1; i >= 0; i--) {
				char c = number [i];
				if (i == 0 && c == '-') {
					result = -result;
					break;
				}

				int digit = DIGITS.IndexOf (c);
				if (digit == -1) {
					throw new ArgumentException ("Invalid character in numeral system");
				}

				result += digit * multiplier;
				multiplier *= radix;
			}

			return result;
		}

		public static string DecimalToArbitrary (long decimalNumber, int radix)
		{
			if (radix < 2 || radix > DIGITS.Length) {
				throw new ArgumentException ("Radix must be greater or equals 2 and smaller then "
				+ DIGITS.Length.ToString ());
			}

			if (decimalNumber == 0) {
				return "0";
			}

			int index = LONG_BITS - 1;
			long currentNumber = Math.Abs (decimalNumber);
			char[] charArray = new char[LONG_BITS];

			while (currentNumber != 0) {
				int remainder = (int)(currentNumber % radix);
				charArray [index--] = DIGITS [remainder];
				currentNumber = currentNumber / radix;
			}

			string result = new String (charArray, index + 1, LONG_BITS - index - 1);
			if (decimalNumber < 0) {
				result = "-" + result;
			}

			return result;
		}
	}
}