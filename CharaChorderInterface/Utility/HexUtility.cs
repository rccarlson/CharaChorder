using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CharaChorderInterface.Utility;

internal static class HexUtility
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

	public static long BinToDec(string bin) => Convert.ToInt64(bin, 2);
}
