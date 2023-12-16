using System;
using System.Collections.Generic;
using logPrintCore.Utils;

namespace logPrintCore.Ansi;

internal sealed class PushPart : ColourPart, IEquatable<PushPart>, IRentable<PushPart>
{
	PushPart() { }


	public PushPart Init(bool? isForeground)
	{
		Init(isForeground, colour: 0xF0);
		_pushedForeground = 0xCC;
		_pushedBackground = 0xCC;
		return this;
	}


	public static PushPart Create()
	{
		return new();
	}


	public byte _pushedForeground = 0xCC;
	public byte _pushedBackground = 0xCC;


	public LinkedListNode<PushPart>? Node { get; set; }


	public override bool MergeWith(Part previous, out Part merged)
	{
		var push = previous as PushPart;
		if (push?._isForeground == null || push._isForeground == _isForeground) {
			merged = null!;
			return false;
		}


		merged = this;

		if (HasForeground) {
			_pushedBackground = push._pushedBackground;
		} else {
			_pushedForeground = push._pushedForeground;
		}

		_isForeground = null;
		_currentBackground = _currentForeground = 0xE0;

		return true;
	}


	public override string ToAnsi()
	{
		return "";
	}

	public override void ApplyConsoleColor() { }


	protected override string DebugOutput()
	{
		return $"{base.DebugOutput()} PF={(HasForeground ? _pushedForeground.ToString("X2") : "  ")}, PB={(HasBackground ? _pushedBackground.ToString("X2") : "  ")}";
	}


	/// <summary>Indicates whether the current object is equal to another object of the same type.</summary>
	/// <param name="other">An object to compare with this object.</param>
	/// <returns>
	/// <see langword="true" /> if the current object is equal to the <paramref name="other" /> parameter; otherwise, <see langword="false" />.</returns>
	public bool Equals(PushPart? other)
	{
		if (ReferenceEquals(null, other)) {
			return false;
		}


		if (ReferenceEquals(this, other)) {
			return true;
		}


		return base.Equals(other) && _pushedForeground == other._pushedForeground && _pushedBackground == other._pushedBackground;
	}

	/// <summary>Determines whether the specified object is equal to the current object.</summary>
	/// <param name="obj">The object to compare with the current object.</param>
	/// <returns>
	/// <see langword="true" /> if the specified object  is equal to the current object; otherwise, <see langword="false" />.</returns>
	public override bool Equals(object? obj)
	{
		return ReferenceEquals(this, obj) || obj is PushPart other && Equals(other);
	}

	/// <summary>Serves as the default hash function.</summary>
	/// <returns>A hash code for the current object.</returns>
	public override int GetHashCode()
	{
		// ReSharper disable NonReadonlyMemberInGetHashCode
		return HashCode.Combine(base.GetHashCode(), _pushedForeground, _pushedBackground);
		// ReSharper restore NonReadonlyMemberInGetHashCode
	}

	/// <summary>Returns a value that indicates whether the values of two <see cref="T:logPrintCore.Ansi.PushPart" /> objects are equal.</summary>
	/// <param name="left">The first value to compare.</param>
	/// <param name="right">The second value to compare.</param>
	/// <returns>true if the <paramref name="left" /> and <paramref name="right" /> parameters have the same value; otherwise, false.</returns>
	public static bool operator ==(PushPart? left, PushPart? right)
	{
		return Equals(left, right);
	}

	/// <summary>Returns a value that indicates whether two <see cref="T:logPrintCore.Ansi.PushPart" /> objects have different values.</summary>
	/// <param name="left">The first value to compare.</param>
	/// <param name="right">The second value to compare.</param>
	/// <returns>true if <paramref name="left" /> and <paramref name="right" /> are not equal; otherwise, false.</returns>
	public static bool operator !=(PushPart? left, PushPart? right)
	{
		return !Equals(left, right);
	}
}
