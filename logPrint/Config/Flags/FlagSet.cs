//#define DEBUG_AUTO_ID

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

using logPrint.Ansi;
using logPrint.Utils;

namespace logPrint.Config.Flags;

internal class FlagSet : NamedElement
{
	static uint nextID;
// ReSharper disable once UnusedMember.Local
uint _id = nextID++;

	protected const string SEPARATOR = "#W#~w~|#!#~!~";


	event StateChangeCallback OnReset;


	static readonly ReferenceEqualityComparer<FlagSet> Comparer = new();
	static readonly OrderedDictionary<FlagSet, string> TrackedIDs = new(Comparer);


	List<Flag> _flags;
	Regex _trackIdRE;
	bool _wasReset;


	public bool autoTrackID;
	public Regex trackIDValueRE;


	[ConfigurationProperty("trackID", IsRequired = false)]
	string TrackIDStr => this["trackID"] as string;

	public Regex TrackIdRE => _trackIdRE ??= string.IsNullOrEmpty(TrackIDStr)
		? null
		: new Regex(TrackIDStr);


	[ConfigurationProperty("", IsDefaultCollection = true)]
	[ConfigurationCollection(typeof(GenericCollection<Flag>), AddItemName = "flag")]
	GenericCollection<Flag> Flags => this[""] as GenericCollection<Flag>;


	public virtual List<Flag> FlagsList => _flags ??= Flags?
		.Cast<Flag>()
		.ToList();

	public bool IsQuerying => OnReset != null;


	List<FlagSet> _others;

	List<FlagSet> Others
		=> _others ??= Program.FlagSets
			.Where(fs => fs.Name == Name && fs.autoTrackID)
			.ToList();


	public virtual string Process(string line)
	{
		if (_wasReset) {
			_wasReset = false;
			OnReset?.Invoke(flag: null, FlagState.TransitioningOn);
		}

		var idMatch = TrackIdRE?.Match(line);
		if (!(idMatch?.Success ?? false)) {
			return ProcessFlags(line);
		}


		var gotID = idMatch.Groups["id"].Success;
		var matchedID = idMatch.Groups["id"].Value;

		if (autoTrackID) {
			if (TrackedIDs.ContainsKey(this)) {
				var handleBlankID = !gotID
					&& TrackedIDs.Keys
						.Skip(TrackedIDs.IndexOfKey(this) + 1)
						.SelectMany(fs => fs.FlagsList)
#if DEBUG_AUTO_ID
						.DumpList(evalItem: f => f.State)
#endif
						.All(f => f.State == f.InitialState);

#if DEBUG_AUTO_ID
				Console.Error.WriteLine($"Track#{TrackedIDs.IndexOfKey(this)}/{TrackedIDs.Count - 1}:{FlagsList.Max(f => f.State)} '{TrackedIDs[this]}' :: '{matchedID}' = {matchedID == TrackedIDs[this]} || {handleBlankID}");
#endif
				if (matchedID == TrackedIDs[this] || handleBlankID) {
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
				if (gotID && TrackedIDs.IndexOfKey(this) == TrackedIDs.Count - 1 && Others.IndexOf(this, Comparer) == Others.Count - 1) {
					Console.Out.WriteLineColours(
						$"#Y#~M~>>> ~R~Warning:#y# ~r~New ID (~Y~{matchedID}~r~) found but no #b#~c~-f{(OnReset == null ? "" : "q")} ~C~{Name}~Y~=#y#~r~ left to process it!"
					);

					OnReset?.Invoke(flag: null, FlagState.TransitioningOff);
				}
			} else if (TrackedIDs.ContainsValue(matchedID)) {
#if DEBUG_AUTO_ID
				Console.Error.WriteLine("OtherTrack matched");
#endif
				line = "";
			} else if (gotID) {
				TrackedIDs[this] = matchedID;
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
		} else if (!trackIDValueRE?.IsMatch(matchedID) ?? false) {
			line = "";
		}


		return ProcessFlags(line);
	}

	string ProcessFlags(string line)
	{
		return FlagsList.Aggregate(
			new StringBuilder(FlagsList.Count * 7 + SEPARATOR.Length),
			(sb, flag) => sb.Append(flag.Process(line)),
			sb => sb
				.Append(SEPARATOR)
				.ToString()
		);
	}

	public virtual string Reset(string line)
	{
		var result = FlagsList.Aggregate(
			new StringBuilder(FlagsList.Count * 7 + SEPARATOR.Length),
			(sb, flag) => sb.Append(flag.Reset()),
			sb => sb
				.Append(SEPARATOR)
				.ToString()
		);

		OnReset?.Invoke(flag: null, FlagState.Off);
		_wasReset = true;

		if (TrackedIDs.ContainsKey(this)) {
			TrackedIDs.Remove(this);
		}

		return result;
	}


	public FlagSet Copy(string trackID)
	{
		return new() {
			["name"] = Name,
			_trackIdRE = TrackIdRE,
			autoTrackID = (trackID == ""),
			trackIDValueRE = string.IsNullOrEmpty(trackID)
				? null
				: new Regex(trackID),
			_flags = FlagsList
				.Select(flag => flag.Copy())
				.ToList()
		};
	}

	public void SetSubMatch(string flagName, List<string> selectedDefines, bool flagQuery, StateChangeCallback changeHandler)
	{
		var matchingFlags = FlagsList
			.Where(flag => flag.Name.StartsWith(flagName, StringComparison.OrdinalIgnoreCase))
			.ToList();

		if (flagQuery) {
			OnReset += changeHandler;

			matchingFlags.ForEach(flag => flag.OnStateChange += changeHandler);
		} else {
			_flags = matchingFlags;
		}

		_flags.ForEach(flag => flag.SelectedDefines = selectedDefines);
	}
}
