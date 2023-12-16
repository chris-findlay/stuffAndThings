//#define DEBUG_AUTO_ID

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

using logPrintCore.Ansi;
using logPrintCore.Utils;

namespace logPrintCore.Config.Flags;

internal class FlagSet
{
	protected const string SEPARATOR = "#W#~w~|#!#~!~";


	event StateChangeCallback? OnReset;


	static readonly ReferenceEqualityComparer<FlagSet> _comparer = new();
	static readonly OrderedDictionary<FlagSet, string> _trackedIDs = new(_comparer);


	bool _wasReset;


	public bool _autoTrackID;
	public Regex? _trackIDValueRE;


	[Required]
	public virtual string Name { get; protected init; } = null!;

	public Regex? TrackID { get; private init; }

	[Required]
	public virtual Flag[] Flags { get; protected set; } = null!;

	public bool IsQuerying => (OnReset != null);


	List<FlagSet>? _others;

	List<FlagSet> Others
		=> _others ??= Program.FlagSets
			.Where(fs => fs.Name == Name && fs._autoTrackID)
			.ToList();


	public virtual string Process(string line)
	{
		if (_wasReset) {
			_wasReset = false;
			OnReset?.Invoke(flag: null, FlagState.TransitioningOn);
		}

		var idMatch = TrackID?.Match(line);
		if (!(idMatch?.Success ?? false)) {
			return ProcessFlags(line);
		}


		var gotID = idMatch.Groups["id"].Success;
		var matchedID = idMatch.Groups["id"].Value;

		if (_autoTrackID) {
			if (_trackedIDs.ContainsKey(this)) {
				var handleBlankID = !gotID
					&& _trackedIDs.Keys
						.Skip(_trackedIDs.IndexOfKey(this) + 1)
						.SelectMany(fs => fs.Flags)
#if DEBUG_AUTO_ID
						.DumpList(evalItem: f => f.State)
#endif
						.All(f => f.State == f.InitialState);

#if DEBUG_AUTO_ID
				Console.Error.WriteLine($"Track#{TrackedIDs.IndexOfKey(this)}/{TrackedIDs.Count - 1}:{FlagsList.Max(f => f.State)} '{TrackedIDs[this]}' :: '{matchedID}' = {matchedID == TrackedIDs[this]} || {handleBlankID}");
#endif
				if (matchedID == _trackedIDs[this] || handleBlankID) {
#if DEBUG_AUTO_ID
						Console.Error.WriteLine(line);

#endif
					return ProcessFlags(line);
				}


#if DEBUG_AUTO_ID
				Console.Error.WriteLine("Not us.");
#endif
				line = "";

				// ReSharper disable once InvertIf
				if (gotID && _trackedIDs.IndexOfKey(this) == _trackedIDs.Count - 1 && Others.IndexOf(this, _comparer) == Others.Count - 1) {
					Console.Out.WriteLineColours(
						$"#Y#~M~>>> ~R~Warning:#y# ~r~New ID (~Y~{matchedID}~r~) found but no #b#~c~-f{(OnReset == null ? "" : "q")} ~C~{Name}~Y~=#y#~r~ left to process it!"
					);

					OnReset?.Invoke(flag: null, FlagState.TransitioningOff);
				}
			} else if (_trackedIDs.ContainsValue(matchedID)) {
#if DEBUG_AUTO_ID
				Console.Error.WriteLine("OtherTrack matched");
#endif
				line = "";
			} else if (gotID) {
				_trackedIDs[this] = matchedID;
#if DEBUG_AUTO_ID
				Console.Error.WriteLine($"Track#{TrackedIDs.IndexOfKey(this)}/{TrackedIDs.Count - 1}:{FlagsList.Max(f => f.State)} <-- '{matchedID}'");
				Console.Error.WriteLine(line);
#endif
			} else {
#if DEBUG_AUTO_ID
				Console.Error.WriteLine("No ID.");
#endif
				line = "";
			}
		} else if (!_trackIDValueRE?.IsMatch(matchedID) ?? false) {
			line = "";
		}


		return ProcessFlags(line);
	}

	string ProcessFlags(string line)
	{
		return Flags.Aggregate(
			new StringBuilder(Flags.Length * 7 + SEPARATOR.Length),
			(sb, flag) => sb.Append(flag.Process(line)),
			sb => sb
				.Append(SEPARATOR)
				.ToString()
		);
	}

	public virtual string Reset(string line)
	{
		var result = Flags.Aggregate(
			new StringBuilder(Flags.Length * 7 + SEPARATOR.Length),
			(sb, flag) => sb.Append(flag.Reset()),
			sb => sb
				.Append(SEPARATOR)
				.ToString()
		);

		OnReset?.Invoke(flag: null, FlagState.Off);
		_wasReset = true;

		if (_trackedIDs.ContainsKey(this)) {
			_trackedIDs.Remove(this);
		}

		return result;
	}


	public FlagSet Copy(string? trackID)
	{
		return new() {
			Name = Name,
			TrackID = TrackID,
			_autoTrackID = (trackID == ""),
			_trackIDValueRE = string.IsNullOrEmpty(trackID)
				? null
				: new Regex(trackID),
			Flags = Flags
				.Select(flag => flag.Copy())
				.ToArray(),
		};
	}

	public void SetSubMatch(string flagName, List<string> selectedDefines, bool flagQuery, StateChangeCallback changeHandler)
	{
		var matchingFlags = Flags
			.Where(flag => flag.Name.StartsWith(flagName, StringComparison.OrdinalIgnoreCase))
			.ToList();

		if (flagQuery) {
			OnReset += changeHandler;

			matchingFlags.ForEach(flag => flag.OnStateChange += changeHandler);
		} else {
			Flags = matchingFlags.ToArray();
		}

		Flags.ToList().ForEach(flag => flag.SelectedDefines = selectedDefines);
	}
}
