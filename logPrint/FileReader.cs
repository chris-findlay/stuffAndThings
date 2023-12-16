using System;
using System.IO;
using System.Threading;

using logPrint.Ansi;
using logPrint.Utils;

namespace logPrint;

internal sealed class FileReader : ILineReader
{
	readonly string _fileName;
	readonly bool _follow;
	readonly Timer _timer;

	FileStream _fileStream;
	StreamReader _streamReader;
	long _lastLength;


	public FileReader(string fileName, bool follow)
	{
		_fileName = fileName;
		_follow = follow;

		_timer = new Timer(CheckForDeletion, state: null, dueTime: Timeout.Infinite, period: Timeout.Infinite);

		OpenFile();
	}


	void OpenFile()
	{
		var printedMessage = false;
		for (;;) {
			try {
				lock (this) {
					_fileStream = File.Open(_fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);

					_lastLength = _fileStream.Length;

					_streamReader = new StreamReader(_fileStream);

					_timer.Change(dueTime: 200, Timeout.Infinite);
				}

				break;
			} catch (Exception exception) {
				if (!_follow) {
					throw;
				}


				if (!printedMessage) {
					Console.Error.WriteLineColours($"#M#~Y~  {exception.Message}  ~W~Waiting for it to exist...  ");
					printedMessage = true;
				}

				Thread.Sleep(200);
			}
		}
	}

	void CheckForDeletion(object state)
	{
		_timer.Change(Timeout.Infinite, Timeout.Infinite);

		try {
			var stream = File.Open(_fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
			stream.Close();
			stream.Dispose();

			_timer.Change(dueTime: 200, Timeout.Infinite);
		} catch (Exception exception) {
			if (exception is UnauthorizedAccessException) {
				lock (this) {
					Console.Error.WriteLineColours("#Y#~R~  File Deleted  ");

					_streamReader.Close();
					_streamReader.Dispose();
					_streamReader = null;

					_fileStream.Dispose();
					_fileStream = null;
				}

				OpenFile();
			} else {
				Console.Error.WriteLineColours($"#R#~Y~  {exception.Message}  ");
			}
		}
	}


	public string GetNextLine(TimeSpan timeout, int sleep = 100)
	{
		bool waitHere;
		do {
			lock (this) {
				waitHere = (_fileStream == null);
			}

			if (waitHere) {
				Thread.Sleep(200);
			}
		} while (waitHere);

		if (!_follow) {
			return _streamReader.ReadLine().RCoalesce(Environment.NewLine);
		}


		if (_streamReader.EndOfStream && _lastLength == _fileStream.Length) {
			return "";
		}


		if (_lastLength > _fileStream.Length) {
			Console.Error.WriteLineColours("#Y#~B~  File Truncated  ");
			_fileStream.Seek(0, SeekOrigin.Begin);
		}

		_lastLength = _fileStream.Length;

		var line = _streamReader.ReadLine();
		return line + Environment.NewLine;
	}


	public void Dispose()
	{
		_streamReader?.Dispose();
		_fileStream?.Dispose();
		_timer?.Dispose();
	}
}
