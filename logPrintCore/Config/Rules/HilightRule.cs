using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

using logPrintCore.Ansi;

namespace logPrintCore.Config.Rules;

internal sealed class HilightRule : Rule
{
	readonly RuleSet _ruleSet;
	readonly bool _matchStrippedOutput;

	readonly Func<Range, string, string, string> _applyHilight = (range, line, popFixup) =>
		$"{line[..range.Start]}{AnsiConsoleColourExtensions.PUSH_COLOURS}~k~#W#{line[range].StripColourCodes()}{AnsiConsoleColourExtensions.POP_COLOURS}{popFixup}{line[range.End..]}";


	public HilightRule(RuleSet ruleSet, Regex match, bool matchStrippedOutput)
	{
		_ruleSet = ruleSet;
		_matchStrippedOutput = matchStrippedOutput;
		Match = match;
	}


	public override string Process(string? line)
	{
		if (line == null) {
			return "";
		}


		return _matchStrippedOutput
			? HilightStripped(line)
			: HilightFormatted(line);
	}


	string HilightStripped(string line)
	{
		var match = Match.Match(line.StripColourCodes());
		return match.Success
			? HilightFormattedForStrippedMatch(line, match)
			: line;
	}

	string HilightFormattedForStrippedMatch(string line, Capture match)
	{
		_ruleSet.DidHilight = true;

		var strippedRange = (start: match.Index, end: match.Index + match.Length);
		var strippedCursor = (start: 0, end: 0);
		var cursor = (start: 0, end: 0);
		var pushes = new Stack<char>();
		char type;

		// Adjust cursor start up to matching range start, counting stack changes:
		while (strippedCursor.start < strippedRange.start) {
			switch (type = line[cursor.start]) {
				case AnsiConsoleColourExtensions.FOREGROUND:
				case AnsiConsoleColourExtensions.BACKGROUND:
					SkipFormat(ref cursor.start, ref strippedCursor.start, skipStack: false);
					break;

				default:
					cursor.start++;
					strippedCursor.start++;
					break;
			}
		}

		cursor.end = cursor.start;
		strippedCursor.end = strippedCursor.start;

		// Adjust cursor end up to matching range end, NOT counting stack changes as we will be stripping them:
		while (strippedCursor.end < strippedRange.end) {
			switch (type = line[cursor.end]) {
				case AnsiConsoleColourExtensions.FOREGROUND:
				case AnsiConsoleColourExtensions.BACKGROUND:
					SkipFormat(ref cursor.end, ref strippedCursor.end, skipStack: true);
					break;

				default:
					cursor.end++;
					strippedCursor.end++;
					break;
			}
		}

		// Adjust end up to endOfInput, counting stack changes so we can see how far out we are:
		int end = cursor.end;
		while (end < line.Length) {
			int dummy = 0;
			switch (type = line[end]) {
				case AnsiConsoleColourExtensions.FOREGROUND:
				case AnsiConsoleColourExtensions.BACKGROUND:
					SkipFormat(ref end, ref dummy, skipStack: false);


					break;

				default:
					end++;
					break;
			}
		}

		string popFixup = GetStackFixup(pushes);

		return _applyHilight(new(cursor.start, cursor.end), line, popFixup);


		void SkipFormat(ref int cursorPos, ref int strippedCursorPos, bool skipStack)
		{
			cursorPos++;

			switch (line[cursorPos]) {
				case AnsiConsoleColourExtensions.FOREGROUND:
				case AnsiConsoleColourExtensions.BACKGROUND:
					// Escaped:
					strippedCursorPos++;
					cursorPos++;
					return;

				case AnsiConsoleColourExtensions.PUSH:
					if (!skipStack) {
						cursorPos += 2;
						pushes.Push(type);

						return;
					}

					break;

				case AnsiConsoleColourExtensions.POP:
					if (!skipStack) {
						cursorPos += 2;
						// ReSharper disable once RedundantAssignment - it is in Debug.
						var popped = pushes.Pop();
						// ReSharper disable once InvocationIsSkipped - not in Debug.
						Debug.Assert(type == popped);

						return;
					}

					break;
			}

			while (!(line[cursorPos] == AnsiConsoleColourExtensions.FOREGROUND || line[cursorPos] == AnsiConsoleColourExtensions.BACKGROUND)) {
				cursorPos++;
			}

			cursorPos++;
		}
	}

	string HilightFormatted(string line)
	{
		var match = Match.Match(line);
		return match.Success
			? HilightFormattedForFormattedMatch(line, match)
			: line;
	}

	string HilightFormattedForFormattedMatch(string line, Capture match)
	{
		_ruleSet.DidHilight = true;

		var cursor = 0;
		var pushes = new Stack<char>();
		char type;

		// Adjust cursor start up to matching range start, counting stack changes:
		while (cursor < match.Index) {
			switch (type = line[cursor]) {
				case AnsiConsoleColourExtensions.FOREGROUND:
				case AnsiConsoleColourExtensions.BACKGROUND:
					SkipFormat(ref cursor, skipStack: false);
					break;

				default:
					cursor++;
					break;
			}
		}


		// Adjust cursor end up to matching range end, NOT counting stack changes as we will be stripping them:
		cursor = match.Index + match.Length;

		// Adjust end up to endOfInput, counting stack changes so we can see how far out we are:
		while (cursor < line.Length) {
			switch (type = line[cursor]) {
				case AnsiConsoleColourExtensions.FOREGROUND:
				case AnsiConsoleColourExtensions.BACKGROUND:
					SkipFormat(ref cursor, skipStack: false);
					break;

				default:
					cursor++;
					break;
			}
		}

		string popFixup = GetStackFixup(pushes);

		return _applyHilight(new(match.Index, match.Index + match.Length), line, popFixup);


		void SkipFormat(ref int cursorPos, bool skipStack)
		{
			cursorPos++;

			switch (line[cursorPos]) {
				case AnsiConsoleColourExtensions.FOREGROUND:
				case AnsiConsoleColourExtensions.BACKGROUND:
					// Escaped:
					cursorPos++;
					return;

				case AnsiConsoleColourExtensions.PUSH:
					if (!skipStack) {
						cursorPos += 2;
						pushes.Push(type);

						return;
					}

					break;

				case AnsiConsoleColourExtensions.POP:
					if (!skipStack) {
						cursorPos += 2;
						// ReSharper disable once RedundantAssignment - it is in Debug.
						var popped = pushes.Pop();
						// ReSharper disable once InvocationIsSkipped - not in Debug.
						Debug.Assert(type == popped);

						return;
					}

					break;
			}

			while (!(line[cursorPos] == AnsiConsoleColourExtensions.FOREGROUND || line[cursorPos] == AnsiConsoleColourExtensions.BACKGROUND)) {
				cursorPos++;
			}

			cursorPos++;
		}
	}

	static string GetStackFixup(Stack<char> pushes)
	{
		var popFixup = new StringBuilder(pushes.Count * 3);
		while (pushes.Any()) {
			popFixup.AppendFormat("{0}>{0}", pushes.Pop());
		}

		return popFixup.ToString();
	}
}
