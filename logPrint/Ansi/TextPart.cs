namespace logPrint.Ansi;

internal sealed class TextPart : Part
{
	public readonly string _text;


	public TextPart(string text)
	{
		_text = text;
	}


	public override bool MergeWith(Part previous, out Part merged)
	{
		merged = null;
		return false;
	}

	public override string ToAnsi()
	{
		return _text;
	}


	protected override string DebugOutput()
	{
		return _text;
	}
}
