using System.Collections.Generic;
using logPrintCore.Utils;

namespace logPrintCore.Ansi;

internal sealed class ForegroundColourPart : ColourPart, IRentable<ForegroundColourPart>
{
	ForegroundColourPart() { }


	public ForegroundColourPart Init(byte colour)
	{
		Init(isForeground: true, colour);
		return this;
	}


	public static ForegroundColourPart Create()
	{
		return new();
	}


	public LinkedListNode<ForegroundColourPart>? Node { get; set; }
}
