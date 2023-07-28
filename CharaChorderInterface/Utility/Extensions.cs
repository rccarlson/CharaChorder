using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CharaChorder.Utility;

internal static class Extensions
{
	public static T Clamp<T>(this IComparable<T> value, T min, T max)
	{
		if (value.CompareTo(min) < 0) return min;
		else if (value.CompareTo(max) > 0) return max;
		else return (T)value;
	}
}
