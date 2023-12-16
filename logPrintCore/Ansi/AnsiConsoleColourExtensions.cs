#if DEBUG
//#define DEBUG_ASSEMBLY
#endif

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

using logPrintCore.Utils;

namespace logPrintCore.Ansi;

internal static class AnsiConsoleColourExtensions
{
	public const char FOREGROUND = '~';
	public const char BACKGROUND = '#';

	// ReSharper disable MemberCanBePrivate.Global
	public const char RESET = '!';
	public const char PUSH = '<';
	public const char POP = '>';


	public const string PUSH_FG = "~<~";	//NOTE: char.ToString() isn't considered constant by the compiler...
	public const string PUSH_BG = "#<#";

	public const string POP_FG = "~>~";
	public const string POP_BG = "#>#";
	// ReSharper restore MemberCanBePrivate.Global

	public const string PUSH_COLOURS = PUSH_FG + PUSH_BG;
	public const string POP_COLOURS = POP_BG + POP_FG;


	public const string MATCH_FOREGROUND = "(?>(?>~[^~]+~)+)";
	public const string MATCH_BACKGROUND = "(?>(?>#[^#]+#)+)";
	public const string MATCH_ANY = "(?>(?>~[^~]+~)|(?>#[^#]+#))+";

	// ReSharper disable once MemberCanBePrivate.Global
	// ReSharper disable once NotAccessedField.Global
	public static readonly int OutputWidth = 128;

	static readonly char[] _newlineChars = Environment.NewLine.ToCharArray();

	internal static readonly Pool<TextPart> TextPartPool;
	internal static readonly Pool<ResetPart> ResetPartPool;
	internal static readonly Pool<ForegroundColourPart> ForegroundColourPartPool;
	internal static readonly Pool<BackgroundColourPart> BackgroundColourPartPool;
	internal static readonly Pool<PushPart> PushPartPool;
	internal static readonly Pool<PopPart> PopPartPool;

	static AnsiConsoleColourExtensions()
	{
		try {
			OutputWidth = Console.WindowWidth;
		} catch (Exception) {
			// Ignore.
		}


		TextPartPool = new(128, 64, TextPart.Create);
		ResetPartPool = new(2, 2, ResetPart.Create);
		ForegroundColourPartPool = new(128, 64, ForegroundColourPart.Create);
		BackgroundColourPartPool = new(128, 64, BackgroundColourPart.Create);
		PushPartPool = new(32, 32, PushPart.Create);
		PopPartPool = new(32, 32, PopPart.Create);
	}

	internal static void Return(Part part)
	{
		switch (part) {
			case TextPart textPart:
				TextPartPool.Return(textPart);
				break;

			case ResetPart resetPart:
				ResetPartPool.Return(resetPart);
				break;

			case ForegroundColourPart foregroundColourPart:
				ForegroundColourPartPool.Return(foregroundColourPart);
				break;

			case BackgroundColourPart backgroundColourPart:
				BackgroundColourPartPool.Return(backgroundColourPart);
				break;

			case PushPart pushPart:
				PushPartPool.Return(pushPart);
				break;

			case PopPart popPart:
				PopPartPool.Return(popPart);
				break;

			default:
				throw new ArgumentOutOfRangeException(nameof(part), part, $"Unhandled Part subtype `{part.GetType().FullName}`!");
		}
	}


	public static ConsoleColourOutputMode _outputMode = ConsoleColourOutputMode.ConsoleColor;


	public static string? EscapeColourCodeChars(this string? line)
	{
		const string FG = "~";
		const string BG = "#";
		const string FG2 = "~~";
		const string BG2 = "##";
		return line
			?.Replace(FG, FG2)
			.Replace(BG, BG2);
	}


	// ReSharper disable once UnusedMember.Global
	public static void Clear(this TextWriter writer, ClearMode clearMode = ClearMode.ToEnd)
	{
		switch (_outputMode) {
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
		switch (_outputMode) {
			case ConsoleColourOutputMode.ConsoleColor:
				Console.ResetColor();
				Console.Out.Write($"\r{new string(' ', OutputWidth - 1)}\r");
				break;

			case ConsoleColourOutputMode.Ansi:
				writer.Write($"{Part.PREFIX}37;40{Part.SUFFIX}{Part.PREFIX}{(int)clearMode}K");
				break;

			default:
				throw new ArgumentOutOfRangeException(nameof(clearMode), clearMode, $"Unhandled ClearMode: '{clearMode}'");
		}
	}


	// ReSharper disable once UnusedMember.Global
	public static string StripColourCodes(this string? str)
	{
		return Regex.Replace(
			str ?? "",
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
			(builder, part) => {
				Return(part);
				return builder.Append(part.ToAnsi().Dump());
			},
#else
			(builder, part) => {
				Return(part);
				return builder.Append(part.ToAnsi());
			},
#endif
			builder => builder.ToString()
		);
	}


	// ReSharper disable UnusedMember.Global
	// ReSharper disable once MemberCanBePrivate.Global
	public static void WriteColours(this TextWriter writer, string? text, bool? resetAtEnd = null)
	{
		if (_outputMode == ConsoleColourOutputMode.Ansi) {
			writer.Write(text?.Colourise(resetAtEnd));
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
				Return(textPart);
				continue;
			}


			var colourPart = (ColourPart)part;
			colourPart.ApplyConsoleColor();
			Return(colourPart);
		}
	}
	public static void WriteColours(this TextWriter writer, bool? resetAtEnd = null, string? format = null, params object[] args)
	{
		ArgumentNullException.ThrowIfNull(format);


		writer.WriteColours(string.Format(format, args), resetAtEnd);
	}
	public static void WriteColours(this TextWriter writer, FormattableString format, bool? resetAtEnd = null)
	{
		writer.WriteColours(format.ToString(), resetAtEnd);
	}

	// ReSharper disable once MemberCanBePrivate.Global
	public static void WriteLineColours(this TextWriter writer, string? text, bool? resetAtEnd = null)
	{
		writer.WriteColours(text, resetAtEnd);
		writer.WriteLine();
	}
	public static void WriteLineColours(this TextWriter writer, bool? resetAtEnd = null, string? format = null, params object[] args)
	{
		ArgumentNullException.ThrowIfNull(format);


		writer.WriteLineColours(string.Format(format, args), resetAtEnd);
	}
	public static void WriteLineColours(this TextWriter writer, FormattableString format, bool? resetAtEnd = null)
	{
		writer.WriteColours(format.ToString(), resetAtEnd);
		writer.WriteLine();
	}
	// ReSharper restore UnusedMember.Global


	static IEnumerable<Part> Parse(string? text, bool? resetAtEnd)
	{
		if (string.IsNullOrEmpty(text)) {
			yield break;
		}


#if DEBUG_ASSEMBLY
		Console.Error.WriteLine("----[Parse]-" + new string('-', outputWidth - 13));
		Console.Error.WriteLine(text);
		Console.Error.WriteLine(new string('-', outputWidth - 1));
#endif
		var sawColourCodes = false;

		var currentText = new StringBuilder();

		static Func<string> LazyCalculateLocation(int index, string str)
		{
			return () => {
				var lineNum = str.Occurrences('\n');
				var truncated = str[..index];
				var position = truncated.Length - truncated.LastIndexOf('\n') - 1;
				truncated = str[(truncated.Length - position)..].TrimStart(_newlineChars);
				int endOfLine = truncated.IndexOf("\n", StringComparison.Ordinal);
				if (endOfLine < 0) {
					endOfLine = Math.Min(truncated.Length, position + 10);
				}

				var line = truncated[..endOfLine].Trim(_newlineChars).Replace('\t', ' ');
				return $"{lineNum + 1}:{position + 1}{Environment.NewLine}{line}{(endOfLine == truncated.Length ? "" : "...")}{Environment.NewLine}{new('-', position)}^{Environment.NewLine}";
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
							yield return TextPartPool.Rent().Init(currentText.ToString());


							currentText.Clear();
						}


						var code = text[i..j];
						if (j == i + 1) {
							yield return text[i] switch {
								RESET => ResetPartPool.Rent().Init(isForeground: true),
								PUSH => PushPartPool.Rent().Init(isForeground: true),
								POP => PopPartPool.Rent().Init(isForeground: true),
								_ => ForegroundColourPartPool.Rent().Init(CodeToAnsi(code, LazyCalculateLocation(i, text))),
							};
						} else if (code.Equals("RESET", StringComparison.OrdinalIgnoreCase)) {
							yield return ResetPartPool.Rent().Init(isForeground: true);
						} else {
							yield return ForegroundColourPartPool.Rent().Init(CodeToAnsi(code, LazyCalculateLocation(i, text)));
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
							yield return TextPartPool.Rent().Init(currentText.ToString());


							currentText.Clear();
						}


						var code = text[i..j];
						if (j == i + 1) {
							yield return text[i] switch {
								RESET => ResetPartPool.Rent().Init(isForeground: false),
								PUSH => PushPartPool.Rent().Init(isForeground: false),
								POP => PopPartPool.Rent().Init(isForeground: false),
								_ => BackgroundColourPartPool.Rent().Init(CodeToAnsi(code, LazyCalculateLocation(i, text))),
							};
						} else if (code.Equals("RESET", StringComparison.OrdinalIgnoreCase)) {
							yield return ResetPartPool.Rent().Init(isForeground: false);
						} else {
							yield return BackgroundColourPartPool.Rent().Init(CodeToAnsi(code, LazyCalculateLocation(i, text)));
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
			yield return TextPartPool.Rent().Init(currentText.ToString());
		}


		if (resetAtEnd ?? sawColourCodes) {
			yield return ResetPartPool.Rent().Init();
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


		ColourPart lastColourPart = ResetPartPool.Rent().Init();
#if DEBUG_ASSEMBLY
		Console.Error.WriteLine($"Starting with {lastColourPart}");
#endif

		var pushStack = new Stack<PushPart>();
		int i;
		for (i = 0; i < parts.Count; i++) {
			Part part = parts[i];
			if (part is PushPart pushPart) {
#if DEBUG_ASSEMBLY
				Console.Error.WriteLine($":<: Got: {pushPart}");
#endif
				if (pushPart.HasForeground) {
					pushPart._pushedForeground = lastColourPart._currentForeground;
				}

				if (pushPart.HasBackground) {
					pushPart._pushedBackground = lastColourPart._currentBackground;
				}

				pushStack.Push(pushPart);
#if DEBUG_ASSEMBLY
				Console.Error.WriteLine($"\tpushing {pushPart} => {pushStack.Count}");
				pushStack.ToList().DumpList(true);
#endif
			} else {
				var popPart = part as PopPart;
#if DEBUG_ASSEMBLY
				if (popPart != null){
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


				if (part is ResetPart && i == parts.Count - 1) {
					break;
				}


				if (colourPart.HasForeground) {
					lastColourPart._currentForeground = colourPart._currentForeground;
				}

				if (colourPart.HasBackground) {
					lastColourPart._currentBackground = colourPart._currentBackground;
				}
#if DEBUG_ASSEMBLY
				Console.Error.WriteLine($"Updating with {colourPart} ==> {lastColourPart}");
#endif
			}
		}

#if DEBUG_ASSEMBLY
		parts.DumpList(true);
#endif
		var length = parts.Count;
		if (parts[^1] is ResetPart { _isForeground: null } reset && lastColourPart._currentForeground == reset._currentForeground && lastColourPart._currentBackground == reset._currentBackground) {
			length--;
			Return(parts[^1]);
		}

		Return(lastColourPart);

		i = 0;
		while (i < length) {
			var previousPart = parts[i];
#if DEBUG_ASSEMBLY
			Console.Error.WriteLine($"Previous := {previousPart}");
#endif
			while (++i < length) {
				var nextPart = parts[i];
#if DEBUG_ASSEMBLY
				Console.Error.WriteLine($"Next     := {nextPart}");
				Console.Error.WriteLine($"Merging\t  {previousPart}{Environment.NewLine}\t& {nextPart}");
#endif
				if (nextPart.MergeWith(previousPart, out Part mergedPart)) {
					Return(previousPart);
					previousPart = mergedPart;
#if DEBUG_ASSEMBLY
					Console.Error.WriteLine($"\t=>{mergedPart}");
#endif
				} else {
#if DEBUG_ASSEMBLY
					Console.Error.WriteLine("\t=> No Merge.");
#endif
					break;
				}
			}

#if DEBUG_ASSEMBLY
			Console.Error.WriteLine($"<<< {previousPart}");
#endif
			yield return previousPart;
		}
	}
}
