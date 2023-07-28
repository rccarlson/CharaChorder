using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace CharaChorderInterface.Utility;

internal static class NumberUtility
{
	public static string HexToBin(string hex)
	{
		// https://stackoverflow.com/a/6617321/11069086
		return string.Join(string.Empty,
			hex.Select(
				c => {
					var componentInt = Convert.ToInt32(c.ToString(), 16);
					var binStr = Convert.ToString(componentInt, 2);
					return binStr.PadLeft(4, '0');
				}
			)
		);
	}
	public static string HexToDec(string hex)
	{
		// https://stackoverflow.com/a/16967286/11069086

		List<int> dec = new() { 0 };   // decimal result

		foreach (char c in hex)
		{
			int carry = Convert.ToInt32(c.ToString(), 16);
			// initially holds decimal value of current hex digit;
			// subsequently holds carry-over for multiplication

			for (int i = 0; i < dec.Count; ++i)
			{
				int val = dec[i] * 16 + carry;
				dec[i] = val % 10;
				carry = val / 10;
			}

			while (carry > 0)
			{
				dec.Add(carry % 10);
				carry /= 10;
			}
		}

		var chars = dec.Select(d => (char)('0' + d));
		var cArr = chars.Reverse().ToArray();
		return new string(cArr);
	}

	public static string BinToDec(string bin)
	{
		BigInteger decimalValue = 0;
		int power = 0;
		for (int i = bin.Length - 1; i >= 0; i--)
		{
			if (bin[i] == '1')
			{
				decimalValue += BigInteger.Pow(2, power);
			}
			power++;
		}
		return decimalValue.ToString();
	}
	public static string BinToHex(string bin)
	{
		// Pad the binary number with leading zeros to ensure it is multiple of 4
		while (bin.Length % 4 != 0)
		{
			bin = "0" + bin;
		}

		string hexadecimalNumber = "";

		// Convert binary to hexadecimal
		for (int i = 0; i < bin.Length; i += 4)
		{
			string binaryPart = bin.Substring(i, 4);
			int decimalValue = Convert.ToInt32(binaryPart, 2);
			string hexPart = decimalValue.ToString("X");
			hexadecimalNumber += hexPart;
		}

		return hexadecimalNumber;
	}

	public static string DecToBin(int dec) => DecToBin(dec.ToString());
	public static string DecToBin(string dec)
	{
		BigInteger number = BigInteger.Parse(dec);
		StringBuilder binary = new();
		while (number != 0)
		{
			BigInteger remainder = number % 2;
			binary.Insert(0, remainder);
			number /= 2;
		}
		return binary.ToString();
	}
}
