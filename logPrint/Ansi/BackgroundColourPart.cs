namespace logPrint.Ansi;

internal sealed class BackgroundColourPart : ColourPart
{
	public BackgroundColourPart(byte colour) : base(isForeground: false, colour) { }
}
