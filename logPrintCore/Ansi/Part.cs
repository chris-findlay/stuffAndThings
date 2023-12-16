#if DEBUG
//#define DEBUG_ASSEMBLY
#endif

using System;
using System.Collections.Generic;
using System.Linq;

namespace logPrintCore.Ansi;

internal abstract class Part : IEquatable<Part>
{
	#region Constants and Lookups

	protected internal const string PREFIX = "\u001B[";
	protected const string BOLD = "1";
	protected const string FOREGROUND_FIELD = "3";

	protected const string BACKGROUND_FIELD = "4";

	//protected const string INVERSE = "7";

	protected const string JOINER = ";";

	protected internal const string SUFFIX = "m";

	protected const byte BOLD_BIT = 8;

	protected internal static readonly Dictionary<string, byte> CodeToAnsiMap = new() {
		{ "k", 0 },
		{ "black", 0 },

		{ "r", 1 },
		{ "red", 1 },

		{ "g", 2 },
		{ "green", 2 },

		{ "y", 3 },
		{ "yellow", 3 },

		{ "b", 4 },
		{ "blue", 4 },

		{ "m", 5 },
		{ "magenta", 5 },

		{ "c", 6 },
		{ "cyan", 6 },

		{ "w", 7 },
		{ "white", 7 },

		{ "K", 0 | BOLD_BIT },
		{ "BLACK", 0 | BOLD_BIT },

		{ "R", 1 | BOLD_BIT },
		{ "RED", 1 | BOLD_BIT },

		{ "G", 2 | BOLD_BIT },
		{ "GREEN", 2 | BOLD_BIT },

		{ "Y", 3 | BOLD_BIT },
		{ "YELLOW", 3 | BOLD_BIT },

		{ "B", 4 | BOLD_BIT },
		{ "BLUE", 4 | BOLD_BIT },

		{ "M", 5 | BOLD_BIT },
		{ "MAGENTA", 5 | BOLD_BIT },

		{ "C", 6 | BOLD_BIT },
		{ "CYAN", 6 | BOLD_BIT },

		{ "W", 7 | BOLD_BIT },
		{ "WHITE", 7 | BOLD_BIT },
	};

	protected static readonly Dictionary<byte, ConsoleColor> AnsiToConsoleColorMap = new() {
		{ 0, ConsoleColor.Black },
		{ 1, ConsoleColor.DarkRed },
		{ 2, ConsoleColor.DarkGreen },
		{ 3, ConsoleColor.DarkYellow },
		{ 4, ConsoleColor.DarkBlue },
		{ 5, ConsoleColor.DarkMagenta },
		{ 6, ConsoleColor.DarkCyan },
		{ 7, ConsoleColor.Gray },
		{ 0 | BOLD_BIT, ConsoleColor.DarkGray },
		{ 1 | BOLD_BIT, ConsoleColor.Red },
		{ 2 | BOLD_BIT, ConsoleColor.Green },
		{ 3 | BOLD_BIT, ConsoleColor.Yellow },
		{ 4 | BOLD_BIT, ConsoleColor.Blue },
		{ 5 | BOLD_BIT, ConsoleColor.Magenta },
		{ 6 | BOLD_BIT, ConsoleColor.Cyan },
		{ 7 | BOLD_BIT, ConsoleColor.White },
	};

	static readonly Dictionary<ConsoleColor, byte> _consoleColorToAnsiMap = AnsiToConsoleColorMap.ToDictionary(pair => pair.Value, pair => pair.Key);

	#endregion

	protected internal static readonly byte DefaultForeground = _consoleColorToAnsiMap[Console.ForegroundColor];
	protected internal static readonly byte DefaultBackground = _consoleColorToAnsiMap[Console.BackgroundColor];

	protected internal byte _currentForeground = DefaultForeground;
	protected internal byte _currentBackground = DefaultBackground;

#if DEBUG_ASSEMBLY
	private static uint nextID;
	private readonly uint _id = ++nextID;
#endif

	protected void Init()
	{
		_currentForeground = DefaultForeground;
		_currentBackground = DefaultBackground;
	}

	public abstract bool MergeWith(Part previous, out Part merged);

	public abstract string ToAnsi();


	public override string ToString()
	{
#if DEBUG_ASSEMBLY
		return $"{{{GetType().Name}#{_id:000}: {DebugOutput()}}}";
#else
		return $"{{{GetType().Name}: {DebugOutput()}}}";
#endif
	}

	protected abstract string DebugOutput();


	/// <summary>Indicates whether the current object is equal to another object of the same type.</summary>
	/// <param name="other">An object to compare with this object.</param>
	/// <returns>
	/// <see langword="true" /> if the current object is equal to the <paramref name="other" /> parameter; otherwise, <see langword="false" />.</returns>
	public bool Equals(Part? other)
	{
		if (ReferenceEquals(null, other)) {
			return false;
		}


		if (ReferenceEquals(this, other)) {
			return true;
		}


		return _currentForeground == other._currentForeground && _currentBackground == other._currentBackground;
	}

	/// <summary>Determines whether the specified object is equal to the current object.</summary>
	/// <param name="obj">The object to compare with the current object.</param>
	/// <returns>
	/// <see langword="true" /> if the specified object  is equal to the current object; otherwise, <see langword="false" />.</returns>
	public override bool Equals(object? obj)
	{
		if (ReferenceEquals(null, obj)) {
			return false;
		}


		if (ReferenceEquals(this, obj)) {
			return true;
		}


		return obj.GetType() == typeof(Part) && Equals((Part)obj);
	}

	/// <summary>Serves as the default hash function.</summary>
	/// <returns>A hash code for the current object.</returns>
	public override int GetHashCode()
	{
		// ReSharper disable NonReadonlyMemberInGetHashCode
		return HashCode.Combine(_currentForeground, _currentBackground);
		// ReSharper restore NonReadonlyMemberInGetHashCode
	}

	/// <summary>Returns a value that indicates whether the values of two <see cref="T:logPrintCore.Ansi.Part" /> objects are equal.</summary>
	/// <param name="left">The first value to compare.</param>
	/// <param name="right">The second value to compare.</param>
	/// <returns>true if the <paramref name="left" /> and <paramref name="right" /> parameters have the same value; otherwise, false.</returns>
	public static bool operator ==(Part? left, Part? right)
	{
		return Equals(left, right);
	}

	/// <summary>Returns a value that indicates whether two <see cref="T:logPrintCore.Ansi.Part" /> objects have different values.</summary>
	/// <param name="left">The first value to compare.</param>
	/// <param name="right">The second value to compare.</param>
	/// <returns>true if <paramref name="left" /> and <paramref name="right" /> are not equal; otherwise, false.</returns>
	public static bool operator !=(Part? left, Part? right)
	{
		return !Equals(left, right);
	}
}
