using System.Collections.Generic;
using logPrintCore.Utils;

namespace logPrintCore.Ansi;

internal sealed class ResetPart : ColourPart, IRentable<ResetPart>
{
	ResetPart() { }


	public ResetPart Init(bool? isForeground = null)
	{
		Init(isForeground, colour: 0xFF);

		if (HasForeground) {
			_currentForeground = DefaultForeground;
		}

		if (HasBackground) {
			_currentBackground = DefaultBackground;
		}

		return this;
	}


	public static ResetPart Create()
	{
		return new();
	}


	public LinkedListNode<ResetPart>? Node { get; set; }
}
