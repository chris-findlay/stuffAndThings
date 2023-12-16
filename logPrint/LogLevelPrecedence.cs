using System.Collections.Generic;

namespace logPrint;

internal static class Precedence
{
	public static readonly List<LogLevel> LogLevels = new() {
		LogLevel.None,
		LogLevel.Trace,
		LogLevel.Debug,
		LogLevel.Info,
		LogLevel.Warn,
		LogLevel.Error,
		LogLevel.Fatal
	};
}
