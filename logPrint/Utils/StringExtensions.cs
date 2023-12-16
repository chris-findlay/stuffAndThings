using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

// {Ctrl+M, O} is your friend...

// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBePrivate.Global

namespace logPrint.Utils;

internal static class StringExtensions
{
	public static T? NullIfDefault<T>(this T thing)
		where T : struct
	{
		return Equals(thing, default)
			? null
			: thing;
	}

	public static T? NullIf<T>(this T thing, T nullWhen)
		where T : struct
	{
		return Equals(thing, nullWhen)
			? null
			: thing;
	}

	public static T NullWhen<T>(this T thing, T nullWhen)
		where T : class
	{
		return Equals(thing, nullWhen)
			? null
			: thing;
	}

	#region Strings

	public static string NullIfEmpty(this string str)
	{
		// ReSharper disable once NullIfEmpty
		return string.IsNullOrEmpty(str)
			? null
			: str;
	}

	public static string NullIfWhitespace(this string str)
	{
		// ReSharper disable once NullIfWhitespace
		return string.IsNullOrWhiteSpace(str)
			? null
			: str;
	}

	public static string SafeTrim(this string str, params char[] chars)
	{
		return str?.Trim(chars);
	}

	public static string TrimToNull(this string str, params char[] chars)
	{
		return str?.Trim(chars).NullIfEmpty();
	}

	public static bool FuzzyContains(this string str, string substr, StringComparison stringComparison)
	{
		return (str.IndexOf(substr, stringComparison) != -1);
	}

	/// <summary>Like <see cref="string.Concat(IEnumerable{string})"/> except that if any part is null, the entire result is null.</summary>
	/// <param name="str">The first part.</param>
	/// <param name="parts">The rest of the parts.</param>
	/// <returns>The concatenated result, or null.</returns>
	/// <example>var greeting = "Hello ".RCoalesce(FirstName, "!") ?? "Hi,";</example>
	public static string RCoalesce(this string str, params string[] parts)
	{
		var all = new List<string> {
			str
		};

		all.AddRange(parts);

		return all.Any(s => s == null)
			? null
			: string.Concat(all);
	}

	#endregion

	#region Numbers

	#region Integers

	public static byte? TryParseByte(this string str, NumberStyles numberStyle = NumberStyles.Integer, IFormatProvider formatProvider = null)
	{
		return byte.TryParse(str, numberStyle, formatProvider ?? NumberFormatInfo.CurrentInfo, out byte value)
			? value
			: null;
	}
	public static byte TryParseByte(this string str, byte defaultValue, NumberStyles numberStyle = NumberStyles.Integer, IFormatProvider formatProvider = null)
	{
		return str.TryParseByte(numberStyle, formatProvider) ?? defaultValue;
	}


	public static short? TryParseShort(this string str, NumberStyles numberStyle = NumberStyles.Integer, IFormatProvider formatProvider = null)
	{
		return short.TryParse(str, numberStyle, formatProvider ?? NumberFormatInfo.CurrentInfo, out short value)
			? value
			: null;
	}
	public static short TryParseShort(this string str, short defaultValue, NumberStyles numberStyle = NumberStyles.Integer, IFormatProvider formatProvider = null)
	{
		return str.TryParseShort(numberStyle, formatProvider) ?? defaultValue;
	}

	public static ushort? TryParseUShort(this string str, NumberStyles numberStyle = NumberStyles.Integer, IFormatProvider formatProvider = null)
	{
		return ushort.TryParse(str, numberStyle, formatProvider ?? NumberFormatInfo.CurrentInfo, out ushort value)
			? value
			: null;
	}
	public static ushort TryParseUShort(this string str, ushort defaultValue, NumberStyles numberStyle = NumberStyles.Integer, IFormatProvider formatProvider = null)
	{
		return str.TryParseUShort(numberStyle, formatProvider) ?? defaultValue;
	}


	public static int? TryParseInt(this string str, NumberStyles numberStyle = NumberStyles.Integer, IFormatProvider formatProvider = null)
	{
		return int.TryParse(str, numberStyle, formatProvider ?? NumberFormatInfo.CurrentInfo, out int value)
			? value
			: null;
	}
	public static int TryParseInt(this string str, int defaultValue, NumberStyles numberStyle = NumberStyles.Integer, IFormatProvider formatProvider = null)
	{
		return str.TryParseInt(numberStyle, formatProvider) ?? defaultValue;
	}

	public static uint? TryParseUInt(this string str, NumberStyles numberStyle = NumberStyles.Integer, IFormatProvider formatProvider = null)
	{
		return uint.TryParse(str, numberStyle, formatProvider ?? NumberFormatInfo.CurrentInfo, out uint value)
			? value
			: null;
	}
	public static uint TryParseUInt(this string str, uint defaultValue, NumberStyles numberStyle = NumberStyles.Integer, IFormatProvider formatProvider = null)
	{
		return str.TryParseUInt(numberStyle, formatProvider) ?? defaultValue;
	}


	public static long? TryParseLong(this string str, NumberStyles numberStyle = NumberStyles.Integer, IFormatProvider formatProvider = null)
	{
		return long.TryParse(str, numberStyle, formatProvider ?? NumberFormatInfo.CurrentInfo, out long value)
			? value
			: null;
	}
	public static long TryParseLong(this string str, long defaultValue, NumberStyles numberStyle = NumberStyles.Integer, IFormatProvider formatProvider = null)
	{
		return str.TryParseLong(numberStyle, formatProvider) ?? defaultValue;
	}

	public static ulong? TryParseULong(this string str, NumberStyles numberStyle = NumberStyles.Integer, IFormatProvider formatProvider = null)
	{
		return ulong.TryParse(str, numberStyle, formatProvider ?? NumberFormatInfo.CurrentInfo, out ulong value)
			? value
			: null;
	}
	public static ulong TryParseULong(this string str, ulong defaultValue, NumberStyles numberStyle = NumberStyles.Integer, IFormatProvider formatProvider = null)
	{
		return str.TryParseULong(numberStyle, formatProvider) ?? defaultValue;
	}

	#endregion

	#region Floating Point

	public static float? TryParseFloat(this string str, NumberStyles numberStyle = NumberStyles.Integer, IFormatProvider formatProvider = null)
	{
		return float.TryParse(str, numberStyle, formatProvider ?? NumberFormatInfo.CurrentInfo, out float value)
			? value
			: null;
	}
	public static float TryParseFloat(this string str, float defaultValue, NumberStyles numberStyle = NumberStyles.Float | NumberStyles.AllowThousands, IFormatProvider formatProvider = null)
	{
		return str.TryParseFloat(numberStyle, formatProvider) ?? defaultValue;
	}

	public static double? TryParseDouble(this string str, NumberStyles numberStyle = NumberStyles.Integer, IFormatProvider formatProvider = null)
	{
		return double.TryParse(str, numberStyle, formatProvider ?? NumberFormatInfo.CurrentInfo, out double value)
			? value
			: null;
	}
	public static double TryParseDouble(this string str, double defaultValue, NumberStyles numberStyle = NumberStyles.Float | NumberStyles.AllowThousands, IFormatProvider formatProvider = null)
	{
		return str.TryParseDouble(numberStyle, formatProvider) ?? defaultValue;
	}

	#endregion

	#endregion

	#region Date & Time

	public static TimeSpan? TryParseTimeSpan(this string str, IFormatProvider formatProvider = null)
	{
		return TimeSpan.TryParse(str, formatProvider, out TimeSpan value)
			? value
			: null;
	}
	public static TimeSpan TryParseTimeSpan(this string str, TimeSpan defaultValue, IFormatProvider formatProvider = null)
	{
		return str.TryParseTimeSpan(formatProvider) ?? defaultValue;
	}

	public static DateTimeOffset? TryParseDateTimeOffset(this string str, IFormatProvider formatProvider = null, DateTimeStyles dateTimeStyles = DateTimeStyles.None)
	{
		return DateTimeOffset.TryParse(str, formatProvider, dateTimeStyles, out DateTimeOffset value)
			? value
			: null;
	}
	public static DateTimeOffset TryParseDateTimeOffset(this string str, DateTimeOffset defaultValue, IFormatProvider formatProvider = null, DateTimeStyles dateTimeStyles = DateTimeStyles.None)
	{
		return str.TryParseDateTimeOffset(formatProvider, dateTimeStyles) ?? defaultValue;
	}

	public static DateTime? TryParseDateTime(this string str, IFormatProvider formatProvider = null, DateTimeStyles dateTimeStyles = DateTimeStyles.None)
	{
		return DateTime.TryParse(str, formatProvider, dateTimeStyles, out DateTime value)
			? value
			: null;
	}
	public static DateTime TryParseDateTime(this string str, DateTime defaultValue, IFormatProvider formatProvider = null, DateTimeStyles dateTimeStyles = DateTimeStyles.None)
	{
		return str.TryParseDateTime(formatProvider, dateTimeStyles) ?? defaultValue;
	}

	public static DateTime? TryParseDateTimeExact(this string str, string format, IFormatProvider formatProvider = null, DateTimeStyles style = DateTimeStyles.None)
	{
		return DateTime.TryParseExact(str, format, formatProvider, style, out DateTime value)
			? value
			: null;
	}
	public static DateTime? TryParseDateTimeExact(this string str, string[] formats, IFormatProvider formatProvider = null, DateTimeStyles style = DateTimeStyles.None)
	{
		return DateTime.TryParseExact(str, formats, formatProvider, style, out DateTime value)
			? value
			: null;
	}
	public static DateTime TryParseDateTimeExact(this string str, DateTime defaultValue, string format, IFormatProvider formatProvider = null, DateTimeStyles dateTimeStyles = DateTimeStyles.None)
	{
		return str.TryParseDateTimeExact(format, formatProvider, dateTimeStyles) ?? defaultValue;
	}
	public static DateTime TryParseDateTimeExact(this string str, DateTime defaultValue, string[] formats, IFormatProvider formatProvider = null, DateTimeStyles dateTimeStyles = DateTimeStyles.None)
	{
		return str.TryParseDateTimeExact(formats, formatProvider, dateTimeStyles) ?? defaultValue;
	}

	#endregion

	#region Uri

	public static Uri TryParseUri(this string str, UriKind uriKind = UriKind.RelativeOrAbsolute)
	{
		return Uri.TryCreate(str, uriKind, out Uri value)
			? value
			: null;
	}

	#endregion
}
