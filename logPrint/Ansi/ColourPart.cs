using System;

namespace logPrint.Ansi;

internal abstract class ColourPart : Part
{
	protected ColourPart(bool? isForeground, byte colour)
	{
		IsForeground = isForeground;
		if (!isForeground.HasValue) {
			return;
		}


		if (isForeground.Value) {
			currentForeground = colour;
		} else {
			currentBackground = colour;
		}
	}


	protected internal bool? IsForeground;
	protected internal bool HasForeground => IsForeground != false;
	protected internal bool HasBackground => IsForeground != true;


	public override bool MergeWith(Part previous, out Part merged)
	{
		if (previous is TextPart || previous is PushPart || previous is PopPart) {
			merged = null;
			return false;
		}


		merged = this;

		var previousColour = (ColourPart)previous;
		if (previousColour.HasBackground && !HasBackground) {
			currentBackground = previousColour.currentBackground;
			IsForeground = null;
		}

		// ReSharper disable once InvertIf
		if (previousColour.HasForeground && !HasForeground) {
			currentForeground = previousColour.currentForeground;
			IsForeground = null;
		}

		return true;
	}

	public override string ToAnsi()
	{
		if (IsForeground.HasValue) {
			return string.Concat(
				PREFIX,
				IsForeground.Value
					? FOREGROUND_FIELD
					: BACKGROUND_FIELD,
				ToAnsiPart(
					IsForeground.Value
						? currentForeground
						: currentBackground
				),
				SUFFIX
			);
		}


		if (this is ResetPart) {
			return PREFIX + SUFFIX;
		}


		if ((currentForeground | BOLD_BIT) == (currentBackground | BOLD_BIT)) {
			return string.Concat(
				PREFIX,
				FOREGROUND_FIELD,
				ToAnsiPart((byte)(currentForeground & ~BOLD_BIT)),
				JOINER,
				ToAnsiPart(currentBackground),
				SUFFIX
			);
		}


		//if ((CurrentBackground & BOLD_BIT) > 0)
		//{
		//	return string.Concat(
		//		AnsiConsoleColourExtensions.PREFIX,
		//		AnsiConsoleColourExtensions.FOREGROUND_FIELD,
		//		ToAnsiPart(CurrentBackground),
		//		AnsiConsoleColourExtensions.JOINER,
		//		AnsiConsoleColourExtensions.BACKGROUND_FIELD,
		//		ToAnsiPart(CurrentForeground),
		//		AnsiConsoleColourExtensions.JOINER,
		//		AnsiConsoleColourExtensions.INVERSE,	// '7'
		//		AnsiConsoleColourExtensions.SUFFIX
		//	);
		//}		//BUG: we never reset INVERSE.


		return string.Concat(
			PREFIX,
			FOREGROUND_FIELD,
			ToAnsiPart(currentForeground),
			JOINER,
			BACKGROUND_FIELD,
			ToAnsiPart(currentForeground),
			SUFFIX
		);
	}

	public virtual void ApplyConsoleColor()
	{
		if (HasForeground) {
			Console.ForegroundColor = AnsiToConsoleColorMap[currentForeground];
		}

		if (HasBackground) {
			Console.BackgroundColor = AnsiToConsoleColorMap[currentBackground];
		}
	}


	static string ToAnsiPart(byte colour)
	{
		var colourString = (colour & ~BOLD_BIT).ToString();
		return (colour & BOLD_BIT) > 0
			? string.Concat(colourString, JOINER, BOLD)
			: colourString;
	}


	protected override string DebugOutput()
	{
		return $"(iF={IsForeground.ToString(),-5}) F={(HasForeground ? currentForeground.ToString("X2") : "  ")}, B={(HasBackground ? currentBackground.ToString("X2") : "  ")}";
	}
}
