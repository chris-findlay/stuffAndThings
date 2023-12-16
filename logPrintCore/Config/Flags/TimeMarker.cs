using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using logPrintCore.Utils;

namespace logPrintCore.Config.Flags;

internal sealed class TimeMarker : FlagSet
{
	static readonly Regex _timeRE = new(@"^(?:\d{4}(?:-\d\d){2} |(?:\d\d/){2}\d{4}\|)?(?:\d\d:){2}\d\d.\d{3,4}|\d\d-...-\d{4} (?:\d\d:){2}\d\d.\d{3}|\d{4}(?:-\d\d){2}T(?:\d\d:){2}\d\d\.\d+Z");
	static readonly Regex _threadIdRE = new(@"\[ ?(?<tID>\d+)\](?= \[.\])|TID: 0*(?<tID>\d+)");
	static readonly Dictionary<int, DateTime> _lastPerThreadTimes = new();


	public static DateTime? GetTime(string line)
	{
		return _timeRE.Match(line).Value.TryParseDateTimeExact(new[] { "yyyy-MM-ddTHH:mm:ss.fffffffZ", "yyyy-MM-dd HH:mm:ss.ffff", "MM/dd/yyyy|HH:mm:ss.fff", "HH:mm:ss.fff", "dd-MMM-yyyy HH:mm:ss.fff" });
	}

	static DateTime GetTime(string line, DateTime defaultValue)
	{
		return GetTime(line) ?? defaultValue;
	}


	readonly int _size;
	readonly bool _outputTimeSpan;
	readonly TimeDeltaMode _timeDeltaMode;

	DateTime _last = DateTime.MinValue;


	public TimeMarker(int? size, bool outputTimeSpan, TimeDeltaMode timingPerThread)
	{
		_timeDeltaMode = timingPerThread;
		_outputTimeSpan = outputTimeSpan;
		_size = size ?? 6;
	}


	public TimeSpan? LastDelta { get; private set; }


	public override string Name
		=> $"#y#TimeMarker{(
			_outputTimeSpan
				? "~M~<TimeSpan>"
				: $"~m~(size #Y#{_size}#y#)#!#"
		)}{
		_timeDeltaMode switch {
			TimeDeltaMode.PerThread => " ~C~Per Thread",
			TimeDeltaMode.PerVisible => " ~Y~Per Visible",
			_ => "",
		}}";


	public override Flag[] Flags { get; protected set; } = Array.Empty<Flag>();


	public override string Process(string line)
	{
		var at = GetTime(line, DateTime.MinValue);

		Func<DateTime> getLast = () => _last;
		Action<DateTime> setLast = value => _last = value;
		switch (_timeDeltaMode) {
			case TimeDeltaMode.PerThread: {
				var threadIDMatch = _threadIdRE.Match(line);
				var threadID = int.Parse(threadIDMatch.Groups["tID"].Value.NullIfEmpty() ?? "-1");

				getLast = () => _lastPerThreadTimes.TryGetValue(threadID, out var time)
					? time
					: DateTime.MinValue;

				setLast = value => _lastPerThreadTimes[threadID] = value;
				break;
			}

			case TimeDeltaMode.PerVisible:
				getLast = () => Program._lastPrintedTime ?? DateTime.MinValue;
				setLast = _ => { };
				break;

			case TimeDeltaMode.PerAll:
				break;

			default:
#pragma warning disable CA2208	// Instantiate argument exceptions correctly
				throw new ArgumentOutOfRangeException(nameof(_timeDeltaMode), _timeDeltaMode, $"Unhandled {nameof(TimeDeltaMode)} value: {_timeDeltaMode}");
#pragma warning restore CA2208
		}


		var last = getLast();
		if (at == DateTime.MinValue || last == DateTime.MinValue) {
			setLast(at);
			return new string(' ', _size) + SEPARATOR;
		}


		string prefix;
		char markOn;
		var markOff = ' ';
		int onSize;

		TimeSpan delta;
		Func<string, int, char, string> pad;

		LastDelta = at - last;
		if (at >= last) {
			delta = LastDelta.Value;
			pad = (str, size, ch) => str.PadRight(size, ch);
		} else {
			delta = -LastDelta.Value;
			pad = (str, size, ch) => str.PadLeft(size, ch);
			markOff = '-';
		}

		setLast(at);

		if (delta.Ticks == 0) {
			return _outputTimeSpan
				? string.Concat("~g~#K# ", delta.ToString("G"), SEPARATOR)
				: string.Concat("~g~#K#", "0".PadLeft(_size / 2).PadRight(_size), SEPARATOR);
		}


		if (delta.TotalMilliseconds <= 1000.0) {
			prefix = "~w~#K#";
			markOn = 'm';
			onSize = (int)Math.Ceiling(_size * delta.TotalMilliseconds / 1000.0);
		} else if (delta.TotalSeconds < 60.0) {
			prefix = "~Y~#y#";
			markOn = 's';
			onSize = (int)Math.Ceiling(_size * delta.TotalSeconds / 60.0);
		} else if (delta.TotalMinutes < 60.0) {
			prefix = "~M~#m#";
			markOn = 'M';
			onSize = (int)Math.Ceiling(_size * delta.TotalMinutes / 60.0);
		} else if (delta.TotalHours < 24.0) {
			prefix = "~R~#r#";
			markOn = 'H';
			onSize = (int)Math.Ceiling(_size * delta.TotalHours / 24.0);
		} else {
			return _outputTimeSpan
				? string.Concat("#R#~Y~", markOff.ToString().Replace(" ", "+"), delta.ToString("G"), SEPARATOR)
				: string.Concat("#R#~Y~", delta.TotalDays, "days!").PadRight(_size, '!') + SEPARATOR;
		}


		return _outputTimeSpan
			? string.Concat(prefix, markOff.ToString().Replace(" ", "+"), delta.ToString("G"), SEPARATOR)
			: string.Concat(prefix, pad(new(markOn, onSize), _size, markOff), SEPARATOR);
	}

	public override string Reset(string line)
	{
		base.Reset(line);

		_lastPerThreadTimes.Clear();
		return Process(line);
	}


	public override string ToString()
	{
		return $"{{{GetType().Name} size={_size}}}";
	}
}
