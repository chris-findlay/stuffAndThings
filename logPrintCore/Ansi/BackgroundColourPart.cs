using System.Collections.Generic;
using logPrintCore.Utils;

namespace logPrintCore.Ansi;

internal sealed class BackgroundColourPart : ColourPart, IRentable<BackgroundColourPart>
{
	BackgroundColourPart() { }


	public BackgroundColourPart Init(byte colour)
	{
		Init(isForeground: false, colour);
		return this;
	}


	public static BackgroundColourPart Create()
	{
		return new();
	}


	public LinkedListNode<BackgroundColourPart>? Node { get; set; }
}
