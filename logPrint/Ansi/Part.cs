#if DEBUG
//#define DEBUG_ASSEMBLY
#endif

using System;
using System.Collections.Generic;
using System.Linq;

namespace logPrint.Ansi;

internal abstract class Part
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
		{ "WHITE", 7 | BOLD_BIT }
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
		{ 7 | BOLD_BIT, ConsoleColor.White }
	};

	static readonly Dictionary<ConsoleColor, byte> consoleColorToAnsiMap = AnsiToConsoleColorMap.ToDictionary(pair => pair.Value, pair => pair.Key);

	#endregion

	protected static readonly byte DefaultForeground = consoleColorToAnsiMap[Console.ForegroundColor];
	protected static readonly byte DefaultBackground = consoleColorToAnsiMap[Console.BackgroundColor];

	protected internal byte currentForeground = DefaultForeground;
	protected internal byte currentBackground = DefaultBackground;

#if DEBUG_ASSEMBLY
	private static uint nextID;
	private readonly uint _id = ++nextID;
#endif

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
}
