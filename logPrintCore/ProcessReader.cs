#if DEBUG
//#define DEBUG_EXIT_HANDLING
#endif

using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

using logPrintCore.Utils;

namespace logPrintCore;

internal sealed class ProcessReader : ILineReader
{
	readonly StreamReader _reader;


	public ProcessReader(Process process, bool redirectStandardError, bool redirectStandardOutput)
	{
		_reader = redirectStandardOutput
			? redirectStandardError
				? new CombinedReader(process)
				: new FollowReader(process, process.StandardOutput)
			: new FollowReader(process, process.StandardError);
	}


	public string? GetNextLine(TimeSpan timeout, int sleep = 100)
	{
		string? line = null;
		var readTask = _reader.ReadLineAsync()
			.ContinueWith(
				task => line = task.Result.NullIfEmpty().RCoalesce(Environment.NewLine) ?? task.Result
			);

		return (Task.WaitAny(new Task[] { readTask }, timeout) == -1)
			? ""
			: line;
	}


	public void Dispose()
	{
		_reader.Dispose();
	}


	sealed class FollowReader : StreamReader
	{
		readonly Process _process;
		readonly StreamReader _processOutput;
		Task<string?> _readLineTask;


		public FollowReader(Process process, StreamReader processOutput) : base(new MemoryStream())
		{
			_process = process;
			_processOutput = processOutput;
			_readLineTask = _processOutput.ReadLineAsync();
		}


		public override Task<string?> ReadLineAsync()
		{
			string? result;
			if (_readLineTask.IsCompleted) {
				result = _readLineTask.Result;
				if (result != null) {
					_readLineTask = _processOutput.ReadLineAsync();
				}
#if DEBUG_EXIT_HANDLING
				else if (_process.HasExited) {
					if (Debugger.IsAttached) {
						Debugger.Break();
					} else {
						Debugger.Launch();
					}
				}
#endif
			} else {
				result = "";
			}


			return Task.FromResult(result);
		}


		protected override void Dispose(bool disposing)
		{
#if DEBUG_EXIT_HANDLING
			if (Debugger.IsAttached) {
				Debugger.Break();
			} else {
				Debugger.Launch();
			}

#endif
			base.Dispose(disposing);
			if (!_process.HasExited) {
				_process.Kill();
			}
		}
	}


	sealed class CombinedReader : StreamReader
	{
		readonly Process _process;
		readonly StreamReader _processStandardError;
		readonly StreamReader _processStandardOutput;

		Task<string?>? _stdErrTask;
		Task<string?>? _stdOutTask;


		public CombinedReader(Process process) : base(new MemoryStream())
		{
			_process = process;
			_processStandardError = process.StandardError;
			_processStandardOutput = process.StandardOutput;

			_stdErrTask = _processStandardError.ReadLineAsync();
			_stdOutTask = _processStandardOutput.ReadLineAsync();
		}


		public override Task<string?> ReadLineAsync()
		{
			string? result = null;

#if DEBUG_EXIT_HANDLING
			if (_process.HasExited) {
				if (Debugger.IsAttached) {
					Debugger.Break();
				} else {
					Debugger.Launch();
				}
			}

#endif
			if (_stdErrTask?.IsCompleted == true) {
				result = _stdErrTask.Result;
				_stdErrTask = (result == null && _process.HasExited)
					? null
					: _processStandardError.ReadLineAsync();
			}

			if (result == null && _stdOutTask?.IsCompleted == true) {
				result = _stdOutTask.Result;
				_stdOutTask = (result == null && _process.HasExited)
					? null
					: _processStandardOutput.ReadLineAsync();
			}

			if (result == null && !(_stdErrTask == null && _stdOutTask == null)) {
				result = "";	// Timeout indicator.
			}

			return Task.FromResult(result);
		}


		protected override void Dispose(bool disposing)
		{
#if DEBUG_EXIT_HANDLING
			if (Debugger.IsAttached) {
				Debugger.Break();
			} else {
				Debugger.Launch();
			}

#endif
			base.Dispose(disposing);
			if (!_process.HasExited) {
				_process.Kill();
			}
		}
	}
}
