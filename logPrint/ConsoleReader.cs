using System;
using System.Collections.Concurrent;
using System.Text;
using System.Threading;

namespace logPrint;

internal sealed class ConsoleReader : ILineReader
{
	readonly BlockingCollection<int> _buffer = new(boundedCapacity: 1);

	Thread _thread;
	bool _inputClosed;


	public ConsoleReader()
	{
		_thread = new Thread(
			() => {
				if (Console.IsInputRedirected) {
					int i;
					do {
						i = Console.Read();		//FIXME: Why under this current .NetCore, when piping directly from `dotnet run` do we stall here?  It works fine if I `>out; logPrint <out` and while debugging...
						_buffer.Add(i);
					} while (i != -1);
				} else {
					Console.TreatControlCAsInput = true;

					while (true) {
						var consoleKeyInfo = Console.ReadKey(intercept: true);
						if (consoleKeyInfo.KeyChar == 0) {
							// ignore dead keys:
							continue;
						}


						if (consoleKeyInfo.Modifiers == ConsoleModifiers.Control && (consoleKeyInfo.Key == ConsoleKey.C || consoleKeyInfo.Key == ConsoleKey.D)) {
							break;
						}


						_buffer.Add(
							consoleKeyInfo.Key == ConsoleKey.Enter
								? '\n'
								: consoleKeyInfo.KeyChar
						);
					}
				}

				_inputClosed = true;
			}
		);

		_thread.Start();
	}


	char? NextChar
		=> _buffer.TryTake(out var result, millisecondsTimeout: 0)
			? (char?)result
			: null;


	public string GetNextLine(TimeSpan timeout, int sleep = 100)
	{
		var line = new StringBuilder();
		var end = DateTime.Now + timeout;
		while (DateTime.Now < end) {
			char? c;
			while ((c = NextChar).HasValue) {
				line.Append(c.Value);
				end = DateTime.Now + timeout;

				if (c.Value == '\n') {
					return line.ToString();
				}
			}


			if (_inputClosed) {
				return null;
			}


			Thread.Sleep(sleep);
		}


		return line.ToString();
	}


	public void Dispose()
	{
		_thread?.Abort();
		_thread = null;
	}
}
