namespace logPrint.Ansi;

internal sealed class PushPart : ColourPart
{
	public PushPart(bool? isForeground) : base(isForeground, colour: 0xF0) { }


	public byte pushedForeground = 0xCC;
	public byte pushedBackground = 0xCC;


	public override bool MergeWith(Part previous, out Part merged)
	{
		var push = previous as PushPart;
		if (push?.IsForeground == null || push.IsForeground == IsForeground) {
			merged = null;
			return false;
		}


		merged = this;

		if (HasForeground) {
			pushedBackground = push.pushedBackground;
		} else {
			pushedForeground = push.pushedForeground;
		}

		IsForeground = null;
		currentBackground = currentForeground = 0xE0;

		return true;
	}


	public override string ToAnsi()
	{
		return "";
	}

	public override void ApplyConsoleColor() { }


	protected override string DebugOutput()
	{
		return $"{base.DebugOutput()} PF={(HasForeground ? pushedForeground.ToString("X2") : "  ")}, PB={(HasBackground ? pushedBackground.ToString("X2") : "  ")}";
	}
}
