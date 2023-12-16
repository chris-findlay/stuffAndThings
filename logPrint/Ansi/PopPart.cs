#if DEBUG
//#define DEBUG_ASSEMBLY
#endif

using System;

namespace logPrint.Ansi;

internal sealed class PopPart : ColourPart
{
#if DEBUG_ASSEMBLY
	private PushPart _link;


#endif
	public PopPart(bool? isForeground) : base(isForeground, colour: 0xF0) { }


	public void Link(PushPart pushPart)
	{
		if ((pushPart.IsForeground ?? IsForeground) != IsForeground) {
			throw new InvalidOperationException($"Mismatched Push({pushPart.IsForeground}) vs Pop({IsForeground})!");
		}


#if DEBUG_ASSEMBLY
			_link = pushPart;
#endif
		if (HasForeground) {
			currentForeground = pushPart.pushedForeground;
		}

		if (HasBackground) {
			currentBackground = pushPart.pushedBackground;
		}
	}


	public override bool MergeWith(Part previous, out Part merged)
	{
		if (!(previous is PopPart pop) || pop.IsForeground == IsForeground) {
			merged = null;
			return false;
		}


		merged = this;
		if (HasForeground) {
			currentBackground = previous.currentBackground;
		}

		if (HasBackground) {
			currentForeground = previous.currentForeground;
		}

		IsForeground = null;
		return true;
	}

#if DEBUG_ASSEMBLY
	protected override string DebugOutput()
	{
		return base.DebugOutput() + $" %%% {_link}";
	}
#endif
}
