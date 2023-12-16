namespace logPrint.Ansi;

internal sealed class ForegroundColourPart : ColourPart
{
	public ForegroundColourPart(byte colour) : base(isForeground: true, colour) { }
}
