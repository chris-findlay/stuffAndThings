#if DEBUG
//#define DEBUG_ASSEMBLY
#endif

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace logPrint.Ansi;

internal static class AnsiConsoleColourExtensions
{
	const char FOREGROUND = '~';
	const char BACKGROUND = '#';
	const char RESET = '!';
	const char PUSH = '<';
	const char POP = '>';


	// ReSharper disable MemberCanBePrivate.Global
	public const string PUSH_FG = "~<~";
	public const string PUSH_BG = "#<#";

	public const string POP_FG = "~>~";
	public const string POP_BG = "#>#";
	// ReSharper restore MemberCanBePrivate.Global

	public const string PUSH_COLOURS = PUSH_FG + PUSH_BG;
	public const string POP_COLOURS = POP_BG + POP_FG;


	// ReSharper disable once MemberCanBePrivate.Global
	// ReSharper disable once NotAccessedField.Global
	public static int outputWidth = 128;


	static AnsiConsoleColourExtensions()
	{
		try {
			outputWidth = Console.WindowWidth;
		} catch (Exception) {
			// Ignore.
		}
	}


	public static ConsoleColourOutputMode outputMode = ConsoleColourOutputMode.ConsoleColor;


	public static string EscapeColourCodeChars(this string line)
	{
		return line
			?.Replace(FOREGROUND.ToString(), FOREGROUND.ToString() + FOREGROUND)
			.Replace(BACKGROUND.ToString(), BACKGROUND.ToString() + BACKGROUND);
	}


	// ReSharper disable once UnusedMember.Global
	public static void Clear(this TextWriter writer, ClearMode clearMode = ClearMode.ToEnd)
	{
		switch (outputMode) {
			case ConsoleColourOutputMode.ConsoleColor:
				Console.ResetColor();
				Console.Clear();
				break;

			case ConsoleColourOutputMode.Ansi:
				writer.Write($"{Part.PREFIX}37;40{Part.SUFFIX}{Part.PREFIX}{(int)clearMode}J");
				break;

			default:
				throw new ArgumentOutOfRangeException(nameof(clearMode), clearMode, $"Unhandled ClearMode: '{clearMode}'");
		}
	}

	public static void ClearLine(this TextWriter writer, ClearMode clearMode = ClearMode.ToEnd)
	{
		switch (outputMode) {
			case ConsoleColourOutputMode.ConsoleColor:
				Console.ResetColor();
				Console.Out.Write($"\r{new string(' ', Console.WindowWidth - 1)}\r");
				break;

			case ConsoleColourOutputMode.Ansi:
				writer.Write($"{Part.PREFIX}37;40{Part.SUFFIX}{Part.PREFIX}{(int)clearMode}K");
				break;

			default:
				throw new ArgumentOutOfRangeException(nameof(clearMode), clearMode, $"Unhandled ClearMode: '{clearMode}'");
		}
	}


	// ReSharper disable once UnusedMember.Global
	public static string StripColourCodes(this string str)
	{
		return Regex.Replace(
			str,
			@"(?x)
					~
					(?:
							(?<unescape>~)
						|
							[^~]+
							~
					)
				|
					\#
					(?:
							(?<unescape>\#)
						|
							[^\#]+
							\#
					)
			",
			"${unescape}"
		);
	}


	// ReSharper disable once MemberCanBePrivate.Global
	public static string Colourise(this string text, bool? resetAtEnd = null)
	{
#if DEBUG_ASSEMBLY
		var inputParts = Parse(text, resetAtEnd).ToList().DumpList(true);
		var parts = Normalise(inputParts).ToList().DumpList(true);
#else
		var parts = Normalise(Parse(text, resetAtEnd));
#endif
		return parts.Aggregate(
			new StringBuilder(),
#if DEBUG_ASSEMBLY
			(builder, part) => builder.Append(part.ToAnsi().Dump()),
#else
			(builder, part) => builder.Append(part.ToAnsi()),
#endif
			builder => builder.ToString()
		);
	}


	// ReSharper disable UnusedMember.Global
	// ReSharper disable once MemberCanBePrivate.Global
	public static void WriteColours(this TextWriter writer, string text, bool? resetAtEnd = null)
	{
		if (outputMode == ConsoleColourOutputMode.Ansi) {
			writer.Write(text.Colourise(resetAtEnd));
			return;
		}


		var parts = Normalise(Parse(text, resetAtEnd))
#if DEBUG_ASSEMBLY
			.ToList().DumpList(true)
#endif
			;

		foreach (var part in parts) {
			if (part is TextPart textPart) {
				writer.Write(textPart._text);
				continue;
			}


			var colourPart = (ColourPart)part;
			colourPart.ApplyConsoleColor();
		}
	}

	public static void WriteColours(this TextWriter writer, bool? resetAtEnd = null, string format = null, params object[] args)
	{
		if (format == null) {
			throw new ArgumentNullException(nameof(format));
		}


		writer.WriteColours(string.Format(format, args), resetAtEnd);
	}

	// ReSharper disable once MemberCanBePrivate.Global
	public static void WriteLineColours(this TextWriter writer, string text, bool? resetAtEnd = null)
	{
		writer.WriteColours(text, resetAtEnd);
		writer.WriteLine();
	}

	public static void WriteLineColours(this TextWriter writer, bool? resetAtEnd = null, string format = null, params object[] args)
	{
		if (format == null) {
			throw new ArgumentNullException(nameof(format));
		}


		writer.WriteLineColours(string.Format(format, args), resetAtEnd);
	}
	// ReSharper restore UnusedMember.Global


	static IEnumerable<Part> Parse(string text, bool? resetAtEnd)
	{
#if DEBUG_ASSEMBLY
		Console.Error.WriteLine("----[Parse]-" + new string('-', outputWidth - 13));
		Console.Error.WriteLine(text);
		Console.Error.WriteLine(new string('-', outputWidth - 1));
#endif
		var sawColourCodes = false;

		var currentText = new StringBuilder();

		Func<string> CalculateLocation(int index)
		{
			return () => {
				var lineNum = text.Count(ch => ch == '\n');
				var truncated = text.Substring(0, index);
				var position = truncated.Length - truncated.LastIndexOf('\n') - 1;
				truncated = text.Substring(truncated.Length - position).TrimStart(Environment.NewLine.ToCharArray());
				int endOfLine = truncated.IndexOf("\n", StringComparison.Ordinal);
				if (endOfLine < 0) {
					endOfLine = Math.Min(truncated.Length, position + 10);
				}

				var line = truncated.Substring(0, endOfLine).Trim(Environment.NewLine.ToCharArray()).Replace('\t', ' ');
				return $"{lineNum + 1}:{position + 1}{Environment.NewLine}{line}{(endOfLine == truncated.Length ? "" : "...")}{Environment.NewLine}{new string('-', position)}^{Environment.NewLine}";
			};
		}

		for (var i = 0; i < text.Length;) {
			int j;

			char c = text[i++];
			switch (c) {
				case FOREGROUND:
					j = text.IndexOf(FOREGROUND, i);
					if (j == -1 || j == i) {
						currentText.Append(c);
						i++;
					} else {
						sawColourCodes = true;
						if (currentText.Length > 0) {
							yield return new TextPart(currentText.ToString());


							currentText.Clear();
						}


						var code = text.Substring(i, j - i);
						if (j == i + 1) {
							yield return text[i] switch {
								RESET => new ResetPart(isForeground: true),
								PUSH => new PushPart(isForeground: true),
								POP => new PopPart(isForeground: true),
								_ => new ForegroundColourPart(CodeToAnsi(code, CalculateLocation(i)))
							};
						} else if (code.Equals("RESET", StringComparison.OrdinalIgnoreCase)) {
							yield return new ResetPart(isForeground: true);
						} else {
							yield return new ForegroundColourPart(CodeToAnsi(code, CalculateLocation(i)));
						}


						i = j + 1;
					}


					break;

				case BACKGROUND:
					j = text.IndexOf(BACKGROUND, i);
					if (j == -1 || j == i) {
						currentText.Append(c);
						i++;
					} else {
						sawColourCodes = true;
						if (currentText.Length > 0) {
							yield return new TextPart(currentText.ToString());


							currentText.Clear();
						}


						var code = text.Substring(i, j - i);
						if (j == i + 1) {
							yield return text[i] switch {
								RESET => new ResetPart(isForeground: false),
								PUSH => new PushPart(isForeground: false),
								POP => new PopPart(isForeground: false),
								_ => new BackgroundColourPart(CodeToAnsi(code, CalculateLocation(i)))
							};
						} else if (code.Equals("RESET", StringComparison.OrdinalIgnoreCase)) {
							yield return new ResetPart(isForeground: false);
						} else {
							yield return new BackgroundColourPart(CodeToAnsi(code, CalculateLocation(i)));
						}


						i = j + 1;
					}


					break;

				default:
					currentText.Append(c);
					break;
			}
		}


		if (currentText.Length > 0) {
			yield return new TextPart(currentText.ToString());
		}


		if (resetAtEnd ?? sawColourCodes) {
			yield return new ResetPart();
		}
	}

	static byte CodeToAnsi(string code, Func<string> calculateLocation)
	{
		try {
			return Part.CodeToAnsiMap[code];
		} catch (KeyNotFoundException exception) {
			throw new KeyNotFoundException($"Unknown colour code '{code}' at {calculateLocation()}", exception);
		}
	}

	static IEnumerable<Part> Normalise(IEnumerable<Part> inputParts)
	{
		var parts = inputParts.ToList();
#if DEBUG_ASSEMBLY
		Console.Error.WriteLine("----[Normalise]" + new string('-', outputWidth - 16));
		parts.DumpList(true);
		Console.Error.WriteLine(new string('-', outputWidth - 1));
#endif
		if (parts.Count == 0) {
			yield break;
		}


		ColourPart lastColourPart = new ResetPart();
#if DEBUG_ASSEMBLY
		Console.Error.WriteLine($"Starting with {lastColourPart}");
#endif

		var pushStack = new Stack<PushPart>();
		foreach (var part in parts) {
			if (part is PushPart pushPart) {
#if DEBUG_ASSEMBLY
				Console.Error.WriteLine($":<: Got: {pushPart}");
#endif
				if (pushPart.HasForeground) {
					pushPart.pushedForeground = lastColourPart.currentForeground;
				}

				if (pushPart.HasBackground) {
					pushPart.pushedBackground = lastColourPart.currentBackground;
				}

				pushStack.Push(pushPart);
#if DEBUG_ASSEMBLY
				Console.Error.WriteLine($"\tpushing {pushPart} => {pushStack.Count}");
				pushStack.ToList().DumpList(true);
#endif
			} else {
				var popPart = part as PopPart;
#if DEBUG_ASSEMBLY
				if (popPart != null)
				{
					Console.Error.WriteLine($":>: Got: {popPart}; popping <- {pushStack.Count}");
					var push = pushStack.Pop();
					popPart.Link(push);
					Console.Error.WriteLine($"\t={popPart}");
					pushStack.ToList().DumpList(true);
				}
#else
				popPart?.Link(pushStack.Pop());
#endif

				// Do this for pop as well:
				if (part is not ColourPart colourPart) {
					continue;
				}


				if (colourPart.HasForeground) {
					lastColourPart.currentForeground = colourPart.currentForeground;
				}

				if (colourPart.HasBackground) {
					lastColourPart.currentBackground = colourPart.currentBackground;
				}
#if DEBUG_ASSEMBLY

				Console.Error.WriteLine($"Updating with {colourPart} ==> {lastColourPart}");
#endif
			}
		}

#if DEBUG_ASSEMBLY
		parts.DumpList(true);
#endif
		var length = parts.Count - 1;
		var i = 0;

		Part mergedPart = null;

		while (i < length) {
			var previousPart = parts[i];
#if DEBUG_ASSEMBLY
			Console.Error.WriteLine($"Previous := {previousPart}");
#endif
			mergedPart = null;
			while (i < length) {
				var nextPart = parts[++i];
#if DEBUG_ASSEMBLY
				Console.Error.WriteLine($"Next     := {nextPart}");
				Console.Error.WriteLine($"Merging\t  {previousPart}{Environment.NewLine}\t& {nextPart}");
#endif
				if (!nextPart.MergeWith(previousPart, out mergedPart)) {
#if DEBUG_ASSEMBLY
					Console.Error.WriteLine("\t=> No Merge.");
#endif
					break;
				}


#if DEBUG_ASSEMBLY
				Console.Error.WriteLine($"\t=>{mergedPart}");
#endif
				previousPart = mergedPart;
				mergedPart = null;
			}

#if DEBUG_ASSEMBLY
			Console.Error.WriteLine($"<<< {previousPart}");
#endif
			yield return previousPart;
		}


		if (mergedPart == null && i == length) {
			yield return parts[i];
		}
	}
}
