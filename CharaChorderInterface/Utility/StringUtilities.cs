using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CharaChorder.Utility;

internal static class StringUtilities
{
	public static string SubStringBefore(this string value, ReadOnlySpan<char> split)
	{
		var valueSpan = value.AsSpan();
		for (int i = 0; i < value.Length - split.Length + 1; i++)
		{
			var substr = valueSpan.Slice(i, split.Length);
			if (substr.SequenceEqual(split)) return value[..i];
		}
		return value;
	}
	public static string ReadToLastInstanceOf(this string value, ReadOnlySpan<char> split)
	{
		var valueSpan = value.AsSpan();
		for (int i = value.Length - split.Length; i >= 0; i--)
		{
			var substr = valueSpan.Slice(i, split.Length);
			if (substr.SequenceEqual(split)) return value[..i];
		}
		return value;
	}
}
