namespace logPrint.Ansi;

internal sealed class ResetPart : ColourPart
{
	public ResetPart(bool? isForeground = null) : base(isForeground, colour: 0xFF)
	{
		if (HasForeground) {
			currentForeground = DefaultForeground;
		}

		if (HasBackground) {
			currentBackground = DefaultBackground;
		}
	}
}
