#if DEBUG
//#define DEBUG_ASSEMBLY
#endif

using System;
using System.Collections.Generic;
using logPrintCore.Utils;

namespace logPrintCore.Ansi;

internal sealed class PopPart : ColourPart, IRentable<PopPart>
{
	PopPart() { }


#if DEBUG_ASSEMBLY
	private PushPart _link;


#endif
	public PopPart Init(bool? isForeground)
	{
		Init(isForeground, colour: 0xF0);
		return this;
	}


	public static PopPart Create()
	{
		return new();
	}


	public LinkedListNode<PopPart>? Node { get; set; }


	public void Link(PushPart pushPart)
	{
		if ((pushPart._isForeground ?? _isForeground) != _isForeground) {
			throw new InvalidOperationException($"Mismatched Push({pushPart._isForeground}) vs Pop({_isForeground})!");
		}


#if DEBUG_ASSEMBLY
		_link = pushPart;
#endif
		if (HasForeground) {
			_currentForeground = pushPart._pushedForeground;
		}

		if (HasBackground) {
			_currentBackground = pushPart._pushedBackground;
		}
	}


	public override bool MergeWith(Part previous, out Part merged)
	{
		if (previous is not PopPart pop || pop._isForeground == _isForeground) {
			merged = null!;
			return false;
		}


		merged = this;
		if (HasForeground) {
			_currentBackground = previous._currentBackground;
		}

		if (HasBackground) {
			_currentForeground = previous._currentForeground;
		}

		_isForeground = null;
		return true;
	}

#if DEBUG_ASSEMBLY
	protected override string DebugOutput()
	{
		return base.DebugOutput() + $" %%% {_link}";
	}
#endif
}
