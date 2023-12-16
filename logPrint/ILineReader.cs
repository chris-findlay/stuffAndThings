using System;

namespace logPrint;

internal interface ILineReader : IDisposable
{
	string GetNextLine(TimeSpan timeout, int sleep = 100);
}
