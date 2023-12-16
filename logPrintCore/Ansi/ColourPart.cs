using System;

namespace logPrintCore.Ansi;

internal abstract class ColourPart : Part, IEquatable<ColourPart>
{
	protected void Init(bool? isForeground, byte colour)
	{
		Init();

		_isForeground = isForeground;
		if (!isForeground.HasValue) {
			return;
		}


		if (isForeground.Value) {
			_currentForeground = colour;
		} else {
			_currentBackground = colour;
		}
	}


	protected internal bool? _isForeground;
	protected internal bool HasForeground => _isForeground != false;
	protected internal bool HasBackground => _isForeground != true;


	public override bool MergeWith(Part previous, out Part merged)
	{
		if (previous is TextPart or PushPart or PopPart) {
			merged = null!;
			return false;
		}


		merged = this;
		if (previous is not ColourPart previousColour) {
			return true;
		}


		if (previousColour.HasBackground && !HasBackground) {
			_currentBackground = previousColour._currentBackground;
			_isForeground = null;
		}

		// ReSharper disable once InvertIf
		if (previousColour.HasForeground && !HasForeground) {
			_currentForeground = previousColour._currentForeground;
			_isForeground = null;
		}

		return true;
	}

	public override string ToAnsi()
	{
		if (_isForeground.HasValue) {
			return string.Concat(
				PREFIX,
				_isForeground.Value
					? FOREGROUND_FIELD
					: BACKGROUND_FIELD,
				ToAnsiPart(
					_isForeground.Value
						? _currentForeground
						: _currentBackground
				),
				SUFFIX
			);
		}


		if (this is ResetPart) {
			return PREFIX + SUFFIX;
		}


		if ((_currentForeground | BOLD_BIT) == (_currentBackground | BOLD_BIT)) {
			return string.Concat(
				PREFIX,
				FOREGROUND_FIELD,
				ToAnsiPart((byte)(_currentForeground & ~BOLD_BIT)),
				JOINER,
				ToAnsiPart(_currentBackground),
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
			ToAnsiPart(_currentForeground),
			JOINER,
			BACKGROUND_FIELD,
			ToAnsiPart(_currentForeground),
			SUFFIX
		);
	}

	public virtual void ApplyConsoleColor()
	{
		if (HasForeground) {
			Console.ForegroundColor = AnsiToConsoleColorMap[_currentForeground];
		}

		if (HasBackground) {
			Console.BackgroundColor = AnsiToConsoleColorMap[_currentBackground];
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
		return $"(iF={_isForeground.ToString()?.PadRight(5)}) F={(HasForeground ? _currentForeground.ToString("X2") : "  ")}, B={(HasBackground ? _currentBackground.ToString("X2") : "  ")}";
	}


	/// <summary>Indicates whether the current object is equal to another object of the same type.</summary>
	/// <param name="other">An object to compare with this object.</param>
	/// <returns>
	/// <see langword="true" /> if the current object is equal to the <paramref name="other" /> parameter; otherwise, <see langword="false" />.</returns>
	public bool Equals(ColourPart? other)
	{
		if (ReferenceEquals(null, other)) {
			return false;
		}


		if (ReferenceEquals(this, other)) {
			return true;
		}


		return base.Equals(other) && _isForeground == other._isForeground;
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


		return obj.GetType() == GetType() && Equals((ColourPart)obj);
	}

	/// <summary>Serves as the default hash function.</summary>
	/// <returns>A hash code for the current object.</returns>
	public override int GetHashCode()
	{
		// ReSharper disable once NonReadonlyMemberInGetHashCode
		return HashCode.Combine(base.GetHashCode(), _isForeground);
	}

	/// <summary>Returns a value that indicates whether the values of two <see cref="T:logPrintCore.Ansi.ColourPart" /> objects are equal.</summary>
	/// <param name="left">The first value to compare.</param>
	/// <param name="right">The second value to compare.</param>
	/// <returns>true if the <paramref name="left" /> and <paramref name="right" /> parameters have the same value; otherwise, false.</returns>
	public static bool operator ==(ColourPart? left, ColourPart? right)
	{
		return Equals(left, right);
	}

	/// <summary>Returns a value that indicates whether two <see cref="T:logPrintCore.Ansi.ColourPart" /> objects have different values.</summary>
	/// <param name="left">The first value to compare.</param>
	/// <param name="right">The second value to compare.</param>
	/// <returns>true if <paramref name="left" /> and <paramref name="right" /> are not equal; otherwise, false.</returns>
	public static bool operator !=(ColourPart? left, ColourPart? right)
	{
		return !Equals(left, right);
	}
}
