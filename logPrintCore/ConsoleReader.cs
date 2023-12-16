#if DEBUG
//#define DEBUG_INPUT
#endif

using System;

#if DEBUG_INPUT
using logPrintCore.Ansi;
#endif
using logPrintCore.Utils;

namespace logPrintCore;

internal sealed class ConsoleReader : ILineReader
{
	//NOTE: For some reason, under Cygwin, Console.In.ReadLineAsync returns null every time STDERR gets input until the first time STDIN gets input, incorrectly indicating EOS.
	bool _seenInput;
	bool _inputClosed;


	public string? GetNextLine(TimeSpan timeout, int sleep = 100)
	{
		if (_inputClosed) {
			return null;
		}


		var line = Console.ReadLine().RCoalesce(Environment.NewLine);
#if DEBUG_INPUT
		Console.Out.WriteLineColours($"~M~*********** ~Y~Got '{((line == null) ? "~R~null" : "~G~" + line)}~Y~' ~M~***********");
#endif
		if (line is null) {
			if (!_seenInput) {
				line = "";
			} else {
				_inputClosed = true;
			}
#if DEBUG_INPUT
			Console.Out.WriteLineColours($"~M~*********** ~R~CLOSE: {_inputClosed} ~M~***********");
#endif
		} else {
			_seenInput = true;
		}

#if DEBUG_INPUT
		Console.Out.WriteLineColours($"~c~*********** {((line == null) ? "~R~null" : line == "" ? "~Y~BLANK" : "~G~line" + line.Length)} ~c~***********");
#endif
		return line;
	}


	public void Dispose() { }
}
