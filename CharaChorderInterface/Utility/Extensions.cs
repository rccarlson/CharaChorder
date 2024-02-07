using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CharaChorderInterface.Utility;

public static class Extensions
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

	public static bool SequenceUnorderedEqual<T>(this IEnumerable<T> source, IEnumerable<T> other)
	{
		var otherArr = other.ToArray();
		BitArray claimed = new BitArray(otherArr.Length);

		int? getFromOther(T item)
		{
			for (int i = 0; i < otherArr.Length; i++)
			{
				if (claimed[i]) continue;
				var otherItem = otherArr[i];
				if (item is null)
				{
					if (otherItem is null) return i;
					else continue;
				}
				else if (item.Equals(otherArr[i]))
					return i;
			}
			return null;
		}

		foreach (var item in source)
		{
			var idx = getFromOther(item);
			if (idx is int i)
			{
				claimed[i] = true;
			}
			else return false;
		}

		foreach(bool item in claimed)
		{
			if (!item) return false;
		}
		return true;
	}
}
