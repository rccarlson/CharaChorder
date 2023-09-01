using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CharaChorderInterface.Utility;

internal static class Extensions
{
	public static T Clamp<T>(this IComparable<T> value, T min, T max)
	{
		if (value.CompareTo(min) < 0) return min;
		else if (value.CompareTo(max) > 0) return max;
		else return (T)value;
	}

	public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T?> source)
	{
		return source
			.Where(x => x is not null)
			.Cast<T>();
	}
}
