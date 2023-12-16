using System.Collections.Generic;
using System.Linq;

namespace logPrint.Utils;

internal static class DictionaryExtensions
{
	// ReSharper disable UnusedMember.Global
	public static int IndexOfKey<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key)
	{
		return dictionary.Keys
				.Select(
					(k, i) => dictionary.Comparer.Equals(k, key)
						? i
						: (int?)-1
				)
				.FirstOrDefault(i => i != -1)
			?? -1;
	}
	public static int IndexOfKey<TKey, TValue>(this OrderedDictionary<TKey, TValue> dictionary, TKey key)
	{
		return dictionary.Keys
				.Select(
					(k, i) => dictionary.Comparer.Equals(k, key)
						? i
						: (int?)-1
				)
				.FirstOrDefault(i => i != -1)
			?? -1;
	}

	public static int IndexOfValue<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TValue value)
	{
		return dictionary.Values
				.Select(
					(v, i) => Equals(v, value)
						? i
						: (int?)-1
				)
				.FirstOrDefault(i => i != -1)
			?? -1;
	}
	public static int IndexOfValue<TKey, TValue>(this OrderedDictionary<TKey, TValue> dictionary, TValue value)
	{
		return dictionary.Values
				.Select(
					(v, i) => Equals(v, value)
						? i
						: (int?)-1
				)
				.FirstOrDefault(i => i != -1)
			?? -1;
	}
	// ReSharper restore UnusedMember.Global
}
