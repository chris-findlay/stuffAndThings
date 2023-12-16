using System;
using System.Collections.Generic;

using logPrintCore.Utils;

namespace logPrintCore.Ansi;

internal sealed class TextPart : Part, IEquatable<TextPart>, IRentable<TextPart>
{
	public string _text = null!;


	TextPart() { }


	public TextPart Init(string text)
	{
		Init();
		_text = text;
		return this;
	}


	public static TextPart Create()
	{
		return new();
	}


	public LinkedListNode<TextPart>? Node { get; set; }
#if DEBUG
	public string Here { get; set; } = null!;
#endif


	public override bool MergeWith(Part previous, out Part merged)
	{
		merged = null!;
		return false;
	}

	public override string ToAnsi()
	{
		return _text;
	}


	protected override string DebugOutput()
	{
		return _text;
	}


	/// <summary>Indicates whether the current object is equal to another object of the same type.</summary>
	/// <param name="other">An object to compare with this object.</param>
	/// <returns>
	/// <see langword="true" /> if the current object is equal to the <paramref name="other" /> parameter; otherwise, <see langword="false" />.</returns>
	public bool Equals(TextPart? other)
	{
		if (ReferenceEquals(null, other)) {
			return false;
		}


		if (ReferenceEquals(this, other)) {
			return true;
		}


		return base.Equals(other) && _text == other._text;
	}

	/// <summary>Determines whether the specified object is equal to the current object.</summary>
	/// <param name="obj">The object to compare with the current object.</param>
	/// <returns>
	/// <see langword="true" /> if the specified object  is equal to the current object; otherwise, <see langword="false" />.</returns>
	public override bool Equals(object? obj)
	{
		return ReferenceEquals(this, obj) || obj is TextPart other && Equals(other);
	}

	/// <summary>Serves as the default hash function.</summary>
	/// <returns>A hash code for the current object.</returns>
	public override int GetHashCode()
	{
		return HashCode.Combine(base.GetHashCode(), _text);
	}

	/// <summary>Returns a value that indicates whether the values of two <see cref="T:logPrintCore.Ansi.TextPart" /> objects are equal.</summary>
	/// <param name="left">The first value to compare.</param>
	/// <param name="right">The second value to compare.</param>
	/// <returns>true if the <paramref name="left" /> and <paramref name="right" /> parameters have the same value; otherwise, false.</returns>
	public static bool operator ==(TextPart? left, TextPart? right)
	{
		return Equals(left, right);
	}

	/// <summary>Returns a value that indicates whether two <see cref="T:logPrintCore.Ansi.TextPart" /> objects have different values.</summary>
	/// <param name="left">The first value to compare.</param>
	/// <param name="right">The second value to compare.</param>
	/// <returns>true if <paramref name="left" /> and <paramref name="right" /> are not equal; otherwise, false.</returns>
	public static bool operator !=(TextPart? left, TextPart? right)
	{
		return !Equals(left, right);
	}
}
