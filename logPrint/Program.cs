#if DEBUG
//#define DEBUG_MATCHING
//#define DEBUG_INPUT
//#define DEBUG_END
#endif

using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

using logPrint.Ansi;
using logPrint.Config;
using logPrint.Config.Flags;
using logPrint.Config.Rules;
using logPrint.Utils;

namespace logPrint;

internal static class Program
{
	const string DATE_TIME_FORMAT = "yyyy-MM-dd HH:mm:ss.ffff: ";


	internal static readonly List<FlagSet> FlagSets = new();

	static readonly TimeSpan _progressThrottle = TimeSpan.FromSeconds(0.5);
	static readonly char[] _lineBreakChars = Environment.NewLine.ToCharArray();

	static readonly Dictionary<char, TimeSpan> _timeScale = new() {
		{ 'S', TimeSpan.FromSeconds(1) },
		{ 'M', TimeSpan.FromMinutes(1) },
		{ 'H', TimeSpan.FromHours(1) }
	};

	static readonly List<(LogLevel level, string marker)> _bucket = new();


	#region Args-related fields

	static string ruleSetName;
	static RuleSet ruleSet;
	static RuleSet timeOutputRuleSet;

	static string fileName;
	static bool follow;
	static bool followDir;
	static bool breakForNextFile;
	static FileSystemWatcher watcher;

	static TimeSpan? startTime;
	static TimeSpan? endTime;

	static DateTime? start;
	static DateTime? end;

	static StepMode stepMode;
	static uint? stepCount;
	static uint stepProgress;
	static TimeSpan? stepTime;
	static DateTime? stepStartTime;

	static bool summariseMode;
	static StringBuilder summarised;

	static bool firstLine;
	static bool argsError;
	static bool verifyArgs;
	static bool shownSkip;

	static DateTime? lastTime;

	static bool flagQuery;
	static bool queriedFlagStateChanged;

	static bool exceptionQuery;

	static TimeSpan? timeQuery;
	static TimeDeltaMode timingMode;

	static string grep;
	static bool grepGrouped;
	static Regex grepRE;
	static string invGrep;
	static bool invGrepGrouped;
	static Regex invGrepRE;
	static string exclGrep;
	static bool exclGrepGrouped;
	static Regex exclGrepRE;

	#endregion

	static DateTime? progressDrawn;


	internal static DateTime? _lastPrintedTime;


	public static void Main(string[] args)
	{
		try {
			Run(args);
		} catch (Exception exception) {
			Console.ForegroundColor = ConsoleColor.White;
			Console.BackgroundColor = ConsoleColor.Red;
			Console.WriteLine(exception);
#if DEBUG
			if (Console.IsOutputRedirected) {
				Console.Error.WriteLine(new string('-', 128));
				Console.Error.WriteLine(exception);
			}
#endif
			Console.ResetColor();
		}

#if DEBUG_END
		if (System.Diagnostics.Debugger.IsAttached) {
			Console.WriteLine("---ProcessEND---");
			System.Diagnostics.Debugger.Break();
		}
#endif
	}


	static void Run(string[] args)
	{
		if (args.Contains("-D")) {
			verifyArgs = true;
			args = args.Where(arg => arg != "-D").ToArray();
#if DEBUG
			if (System.Diagnostics.Debugger.IsAttached) {
				System.Diagnostics.Debugger.Break();
			} else {
				System.Diagnostics.Debugger.Launch();
			}
#endif
		}

		var configSection = (LogPrintConfigSection)ConfigurationManager.GetSection("logConfig");

		if (!ProcessArgs(args, configSection)) {
			return;
		}


		ruleSet = GetRuleSet(configSection);

		if (summariseMode) {
			var tmp = ruleSetName;
			ruleSetName = null;
			timeOutputRuleSet = GetRuleSet(configSection);
			ruleSetName = tmp;
		}

		if (grepRE != null) {
			ruleSet.SetHilight(grepRE);
		}

		if (followDir) {
			// ReSharper disable once AssignNullToNotNullAttribute - too bad if this returns null.
			watcher = new FileSystemWatcher(Path.GetDirectoryName(Path.GetFullPath(fileName)), "*" + Path.GetExtension(fileName)) { EnableRaisingEvents = true };
			watcher.Created += HandleNewFile;
			watcher.Error += (sender, eventArgs) => {
				Console.Error.WriteLineColours("#R#~W~FileSystemWatcher Error: " + sender + "; " + eventArgs);
				eventArgs.Dump(multiLine: true);
			};
		}

		if (summariseMode) {
			summarised = new StringBuilder();
		}

		do {
			breakForNextFile = false;

			using (var reader = GetLineSource()) {
				var timeout = TimeSpan.FromSeconds(1);
				firstLine = true;

				if (ruleSet.RecordStart == null) {
					ProcessLines(reader, timeout);
				} else {
					ProcessRecords(reader, timeout);
				}
			}

			if (summarised?.Length > 0) {
				AppendAndClearBucket();

				// ReSharper disable once PossibleInvalidOperationException
				Console.Out.WriteLineColours(timeOutputRuleSet.Process(stepStartTime.Value.ToString(DATE_TIME_FORMAT)) + summarised);
				summarised.Clear();
			}

			if (!shownSkip) {
				continue;
			}


			Console.Out.WriteLineColours($"~K~------------------------------------~c~[~C~{lastTime:yyyy-MM-dd HH:mm:ss.ffff}~c~]~K~----------------------------------------------");
			shownSkip = false;
			progressDrawn = null;
		} while (followDir);

		if (progressDrawn.HasValue) {
			Console.Out.ClearLine();
		}
	}


	static bool ProcessArgs(string[] args, LogPrintConfigSection configSection)
	{
		AnsiConsoleColourExtensions.outputMode = ConsoleColourOutputMode.ConsoleColor;

		args = ProcessArgsRepeats();


		for (var i = 0; i < args.Length; i++) {
			var arg = args[i];
			var argValue = arg.Substring(1);
			switch (arg[0]) {
				case '-':
					if (!ProcessDashArg(arg, argValue, ref i)) {
						return false;
					}

					break;

				case '@':
					ProcessAtArg(argValue);

					break;

				case '~':
					if (endTime.HasValue) {
						Console.Error.WriteLineColours($"#R#~W~Multiple endTimes supplied!  Dropping `~Y~{endTime}~W~`!");
						argsError = true;
					}

					endTime = argValue.TryParseTimeSpan();
					if (endTime == null) {
						Console.Error.WriteLineColours($"#R#~W~Can't parse end time `~Y~{argValue}~W~`!");
						argsError = true;
					}

					break;

				case '+':
					if (startTime.HasValue) {
						var length = argValue.TryParseTimeSpan();
						if (length.HasValue) {
							if (endTime.HasValue) {
								Console.Error.WriteLineColours($"#R#~W~Multiple endTimes supplied!  Dropping `~Y~{endTime}~W~`!");
								argsError = true;
							}

							endTime = startTime + length;
						} else {
							Console.Error.WriteLineColours($"#R#~W~Can't parse length `~Y~{argValue}~W~`!");
							argsError = true;
						}
					} else {
						Console.Error.WriteLineColours($"#R#~Y~Length without start time `~W~{argValue}~Y~`!");
						argsError = true;
					}

					break;

				default:
					if (fileName != null) {
						Console.Error.WriteLineColours($"#R#~W~Can only have 1 file - dropping '~Y~{fileName}~W~'!");
						argsError = true;
					}

					fileName = arg;
					break;
			}
		}

		FinalArgsValidation();

		return !argsError || PrintArgsError(configSection);


		// ReSharper disable once ImplicitlyCapturedClosure - don't care.
		string[] ProcessArgsRepeats()
		{
			for (var i = 0; i < args.Length - 2; i++) {
				var match = Regex.Match(args[i], @"^(\d+)x$");
				if (!match.Success) {
					continue;
				}


				var repeat = new[] { args[i + 1], args[i + 2] };
				var argsList = args.Take(i - 1).ToList();

				var count = int.Parse(match.Groups[1].Value);
				for (int j = 0; j < count; j++) {
					argsList.AddRange(repeat);
				}

				args = argsList.Concat(args.Skip(i + 3)).ToArray();
			}

			return args;
		}

		bool ProcessDashArg(string arg, string argValue, ref int i)
		{
			if (arg.Length == 1) {
				if (fileName != null) {
					Console.Error.WriteLineColours($"#R#~W~Can only have 1 file - dropping '~Y~{fileName}~W~'");
					argsError = true;
				}

				fileName = arg;
				return true;
			}


			switch (arg.Substring(1, 1)) {
				case "-":
					switch (argValue) {
						case "-help":
							Console.Out.WriteLineColours(configSection.Usage);
							return false;

						case "-config":
							Console.Out.WriteLineColours(configSection.Config);
							return false;

						case "-list":
							ListConfig(configSection);
							return false;

						default:
							Console.Error.WriteLineColours($"#R#~W~Unknown option '~Y~{arg}~W~'");
							argsError = true;
							return true;
					}


				case "?":
				case "h":
					Console.Out.WriteLineColours(configSection.Usage);
					return false;


				case "A":
				case "a":
					AnsiConsoleColourExtensions.outputMode = ConsoleColourOutputMode.Ansi;
					break;

				case "e":
					exceptionQuery = true;
					break;

				case "F":
					follow = true;
					followDir = (argValue == "FD");
					break;

				case "f":
					if (i < args.Length - 1) {
						bool isFlagQuery = (argValue == "fq");
						flagQuery |= isFlagQuery;

						var flagSetName = args[++i];
						if (flagSetName == "*") {
							FlagSets.AddRange(GetAllFlagSets(isFlagQuery, configSection));
						} else {
							var flagSet = GetFlagSet(flagSetName, isFlagQuery, configSection);
							if (flagSet != null) {
								FlagSets.Add(flagSet);
							}
						}
					} else {
						Console.Error.WriteLineColours($"#R#~W~Option -{argValue} requires an argument!");
						argsError = true;
					}

					break;

				case "g":
					switch (argValue) {
						case "gv" when i < args.Length - 1: {
							var grepValue = args[++i];
							if (invGrep == null) {
								invGrep = grepValue;
							} else {
								if (invGrepGrouped) {
									invGrep += $"|(?:{grepValue})";
								} else {
									invGrep = $"(?:{invGrep})|(?:{grepValue})";
									invGrepGrouped = true;
								}
							}

							break;
						}

						case "gv":
							Console.Error.WriteLineColours("#R#~W~Option -gv requires an argument!");
							argsError = true;
							break;

						case "gV" when i < args.Length - 1: {
							var grepValue = args[++i];
							if (exclGrep == null) {
								exclGrep = grepValue;
							} else {
								if (exclGrepGrouped) {
									exclGrep += $"|(?:{grepValue})";
								} else {
									exclGrep = $"(?:{exclGrep})|(?:{grepValue})";
									exclGrepGrouped = true;
								}
							}

							break;
						}

						case "gV":
							Console.Error.WriteLineColours("#R#~W~Option -gv requires an argument!");
							argsError = true;
							break;

						default: {
							if (i < args.Length - 1) {
								var grepValue = args[++i];
								if (grep == null) {
									grep = grepValue;
								} else {
									if (grepGrouped) {
										grep += $"|(?:{grepValue})";
									} else {
										grep = $"(?:{grep})|(?:{grepValue})";
										grepGrouped = true;
									}
								}
							} else {
								Console.Error.WriteLineColours("#R#~W~Option -g requires an argument!");
								argsError = true;
							}

							break;
						}
					}

					break;

				case "l":
					ListConfig(configSection);
					return false;

				case "t":
					switch (argValue) {
						case "ta":
							timingMode = TimeDeltaMode.PerAll;
							break;

						case "tt":
							timingMode = TimeDeltaMode.PerThread;
							break;

						case "tv":
							timingMode = TimeDeltaMode.PerVisible;
							break;

						default:
							// ReSharper disable once PossibleNullReferenceException
							if (argValue.StartsWith("tq", StringComparison.Ordinal)) {
								var nextArg = argValue.Substring(2).NullIfEmpty()
									?? ((i < args.Length - 1)
										? args[++i]
										: null);

								if (nextArg == null) {
									Console.Error.WriteLineColours("#R#~W~Option -tq requires a value!");
									argsError = true;
									return true;
								}


								var time = (nextArg.Length == 1 && !char.IsDigit(nextArg[0]))
									? _timeScale[nextArg.ToUpperInvariant()[0]]
									: nextArg.TryParseTimeSpan();

								if (time.HasValue) {
									timeQuery = time;
								} else {
									Console.Error.WriteLineColours($"#R#~W~Unable to parse value of -tq option: `~Y~{nextArg}~W~`!");
									argsError = true;
								}

								if (!FlagSets.Any(fs => fs is TimeMarker)) {
									FlagSets.Add(GetFlagSet(arg, isFlagQuery: false, configSection));
								}
							} else {
								FlagSets.Add(GetFlagSet(arg, isFlagQuery: false, configSection));
							}


							break;
					}

					break;

				case "T":
					FlagSets.Add(GetFlagSet(arg, isFlagQuery: false, configSection));
					break;

				case "r":
					if (i < args.Length - 1) {
						if (ruleSetName != null) {
							Console.Error.WriteLineColours($"#R#~W~Can only have 1 RuleSet - dropping '~Y~{ruleSetName}~W~'");
							argsError = true;
						}

						ruleSetName = args[++i];
					} else {
						Console.Error.WriteLineColours("#R#~W~Option -r requires an argument!");
						argsError = true;
					}

					break;

				case "S":
					if (i < args.Length - 1) {
						summariseMode = true;
						var stepArgParts = args[++i].Split('/');
						if (stepArgParts.Length > 1) {
							stepCount = stepArgParts[1].TryParseUInt();
							if (!stepCount.HasValue) {
								Console.Error.WriteLineColours("#R#~W~Invalid scale-factor value for the -S option: ~Y~" + stepArgParts[1]);
								argsError = true;
								summariseMode = false;
							}
						}

						stepTime = stepArgParts[0].TryParseTimeSpan();
						if (!stepTime.HasValue) {
							Console.Error.WriteLineColours("#R#~W~Invalid time-span value for the -S option: ~Y~" + stepArgParts[1]);
							argsError = true;
							summariseMode = false;
						}
					} else {
						Console.Error.WriteLineColours("#R#~W~Option -S requires an argument!");
						argsError = true;
					}

					break;

				case "s":
					if (i < args.Length - 1) {
						var stepArg = args[++i];
						var normalisedStepArg = stepArg.ToUpperInvariant();
						if (normalisedStepArg.EndsWith("L", StringComparison.Ordinal)) {
							stepMode = StepMode.LineCount;
							stepCount = normalisedStepArg.TrimEnd('L').TryParseUInt();

							if (!stepCount.HasValue) {
								Console.Error.WriteLineColours("#R#~W~Invalid line-count value for the -s option: ~Y~" + stepArg);
								argsError = true;
							}
						} else if (stepArg.Contains(":")) {
							stepMode = StepMode.TimeSpan;
							stepTime = stepArg.TryParseTimeSpan();

							if (!stepTime.HasValue) {
								Console.Error.WriteLineColours("#R#~W~Invalid time-span value for the -s option: ~Y~" + stepArg);
								argsError = true;
							}
						} else {
							stepMode = StepMode.RecordCount;
							stepCount = stepArg.TryParseUInt();

							if (!stepCount.HasValue) {
								Console.Error.WriteLineColours("#R#~W~Invalid record-count value for the -s option: ~Y~" + stepArg);
								argsError = true;
							}
						}
					} else {
						Console.Error.WriteLineColours("#R#~W~Option -s requires an argument!");
						argsError = true;
					}

					break;

				default:
					Console.Error.WriteLineColours($"#R#~W~Unknown option '~Y~{arg}~W~'");
					argsError = true;
					break;
			}

			return true;
		}

		static void ProcessAtArg(string argValue)
		{
			var parts = argValue.Split('~');
			if (parts.Length > 1) {
				if (startTime.HasValue) {
					Console.Error.WriteLineColours($"#R#~W~Multiple startTimes supplied!  Dropping `~Y~{startTime}~W~`!");
					argsError = true;
				}

				startTime = parts[0].TryParseTimeSpan();
				if (startTime.HasValue) {
					if (endTime.HasValue) {
						Console.Error.WriteLineColours($"#R#~W~Multiple endTimes supplied!  Dropping `~Y~{endTime}~W~`!");
						argsError = true;
					}

					endTime = parts[1].TryParseTimeSpan();
					if (endTime != null) {
						return;
					}


					Console.Error.WriteLineColours($"#R#~W~Can't parse end time `~Y~{parts[1]}~W~`!");
					argsError = true;
				} else {
					Console.Error.WriteLineColours($"#R#~W~Can't parse start time `~Y~{parts[0]}~W~`!");
					argsError = true;
				}
			} else {
				parts = argValue.Split('+');
				if (parts.Length > 1) {
					if (startTime.HasValue) {
						Console.Error.WriteLineColours($"#R#~W~Multiple startTimes supplied!  Dropping `~Y~{startTime}~W~`!");
						argsError = true;
					}

					startTime = parts[0].TryParseTimeSpan();
					if (startTime.HasValue) {
						var length = parts[1].TryParseTimeSpan();
						if (length.HasValue) {
							if (endTime.HasValue) {
								Console.Error.WriteLineColours($"#R#~W~Multiple endTimes supplied!  Dropping `~Y~{endTime}~W~`!");
								argsError = true;
							}

							endTime = startTime + length;
						} else {
							Console.Error.WriteLineColours($"#R#~W~Can't parse length `~Y~{parts[1]}~W~`!");
							argsError = true;
						}
					} else {
						Console.Error.WriteLineColours($"#R#~W~Can't parse start time `~Y~{parts[0]}~W~`!");
						argsError = true;
					}
				} else {
					if (startTime.HasValue) {
						Console.Error.WriteLineColours($"#R#~W~Multiple startTimes supplied!  Dropping `~Y~{startTime}~W~`!");
						argsError = true;
					}

					startTime = argValue.TryParseTimeSpan();
					if (startTime != null) {
						return;
					}


					Console.Error.WriteLineColours($"#R#~W~Can't parse start time `~Y~{argValue}~W~`!");
					argsError = true;
				}
			}
		}
	}

	static void FinalArgsValidation()
	{
		if (grep != null) {
			grepRE = new Regex(grep);
		}

		if (invGrep != null) {
			invGrepRE = new Regex(invGrep);
		}

		if (exclGrep != null) {
			exclGrepRE = new Regex(exclGrep);
		}

		if (follow && (fileName ?? "-") == "-") {
			Console.Error.WriteLineColours($"#R#~W~Follow{(followDir ? "Directory" : "")} without filename doesn't make sense!");
			argsError = true;
		}

		if (fileName?.StartsWith("/cygdrive") ?? false) {
			fileName = Regex.Replace(fileName, "^/cygdrive/(.)", "$1:");
		}
	}

	static bool PrintArgsError(LogPrintConfigSection configSection)
	{
		// ReSharper disable once StringLiteralTypo
		Console.Error.WriteColours("~M~Errors parsing args.  Continue anyway? [~Y~y~y~es~M~/~Y~N~g~o~M~/~Y~l~y~ist~M~/~Y~u~y~sage~M~/~Y~c~y~onfig~M~]: ");
		Console.Error.Flush();

		var ch = ((char)Console.Read()).ToString().ToUpperInvariant()[0];
		Console.Error.ClearLine();
		switch (ch) {
			case 'U':
				Console.Out.WriteLineColours(configSection.Usage);
				break;

			case 'C':
				Console.Out.WriteLineColours(configSection.Config);
				break;

			case 'L':
				ListConfig(configSection);
				break;
		}

		return (ch == 'Y');
	}

	static void ListConfig(LogPrintConfigSection configSection)
	{
		flagQuery = exceptionQuery = false;
		grepRE = invGrepRE = exclGrepRE = null;
		start = end = null;
		timeQuery = null;
		FlagSets.Clear();
		PrintHeader(recordKind: null, configSection);
	}


	static IEnumerable<FlagSet> GetAllFlagSets(bool isFlagQuery, LogPrintConfigSection configSection)
	{
		return configSection.FlagSetList
			.Select(
				flagSet => {
					flagSet.SetSubMatch("", new List<string>(), isFlagQuery, OnQueryFlagStateChange);
					return flagSet;
				}
			);
	}

	static FlagSet GetFlagSet(string flagSetName, bool isFlagQuery, LogPrintConfigSection configSection)
	{
		if (flagSetName.ToUpperInvariant().StartsWith("-T", StringComparison.Ordinal)) {
			return new TimeMarker(flagSetName.Substring(2).TryParseInt(), flagSetName[1] == 'T', timingMode);
		}


		var parts = flagSetName.Split('=');
		flagSetName = parts[0];
		var flagSetTrackID = (parts.Length > 1)
			? parts[1]
			: null;

		parts = flagSetName.Split(':');
		flagSetName = parts[0];
		var selectedDefines = (parts.Length > 1)
			? parts[1].Split(',').ToList()
			: new List<string>();

		parts = flagSetName.Split('/');
		flagSetName = parts[0];
		var flagName = (parts.Length > 1)
			? parts[1]
			: "";

		var flagSets = configSection.FlagSetList
			.Where(set => set.Name.StartsWith(flagSetName, StringComparison.OrdinalIgnoreCase))
			.ToList();


#if DEBUG_MATCHING
		flagSets.Dump("flagSets", true, p => p.DeclaringType == typeof(FlagSet), p => p.PropertyType.IsGenericType);
#endif

		FlagSet flagSet;
		switch (flagSets.Count) {
			case 0:
				Console.Error.WriteLineColours($"#R#~W~Unknown FlagSet name: '~Y~{flagSetName}~W~'!");
				argsError = true;
				return null;

			case 1:
				flagSet = flagSets.First().Copy(flagSetTrackID);
				flagSet.SetSubMatch(flagName, selectedDefines, isFlagQuery, OnQueryFlagStateChange);
				return flagSet;
		}


		Console.Error.WriteLineColours($"#R#~W~Ambiguous FlagSet name '~Y~{flagSetName}~W~' matches '{string.Join("', '", flagSets.Select(fs => fs.Name))}'; using the first match!");
		argsError = true;

		flagSet = flagSets.First().Copy(flagSetTrackID);
		flagSet.SetSubMatch(flagName, selectedDefines, isFlagQuery, OnQueryFlagStateChange);
		return flagSet;
	}

	static void OnQueryFlagStateChange(Flag flag, FlagState newFlagState)
	{
		queriedFlagStateChanged = true;
	}

	static RuleSet GetRuleSet(LogPrintConfigSection configSection)
	{
		var result = (ruleSetName.TrimToNull() == null)
			? configSection.RuleSetList.First()
			: configSection.RuleSetList.FirstOrDefault(set => set.Name.StartsWith(ruleSetName, StringComparison.OrdinalIgnoreCase)) ?? configSection.RuleSetList.First();

#if DEBUG_MATCHING
		ruleSet.Dump("ruleSet", true, p => p.DeclaringType == typeof(RuleSet), p => p.PropertyType.IsGenericType);
#endif

		if (ruleSetName.TrimToNull() == null || result.Name.StartsWith(ruleSetName, StringComparison.OrdinalIgnoreCase)) {
			return result;
		}


		Console.Error.WriteLineColours($"#R#~W~Unknown RuleSet name: '~Y~{ruleSetName}~W~'!");
		argsError = true;

		return result;
	}


	static void HandleNewFile(object sender, FileSystemEventArgs fileSystemEventArgs)
	{
		Console.Error.WriteLineColours($"#g#~Y~fileName ~W~:= ~C~{fileSystemEventArgs.FullPath}");
		fileName = fileSystemEventArgs.FullPath;
		breakForNextFile = true;
	}


	static ILineReader GetLineSource()
	{
		return (fileName == null || fileName == "-")
			? new ConsoleReader()
			: new FileReader(fileName, follow);
	}


	#region Funky

	readonly struct Colour : IEquatable<Colour>
	{
		public static readonly Dictionary<char, Colour> Palette = new() {
			{ 'k', new Colour(0, 0, 0) },
			{ 'r', new Colour(128, 0, 0) },
			{ 'g', new Colour(0, 128, 0) },
			{ 'y', new Colour(128, 128, 0) },
			{ 'b', new Colour(0, 0, 128) },
			{ 'm', new Colour(128, 0, 128) },
			{ 'c', new Colour(0, 128, 128) },
			{ 'w', new Colour(170, 170, 170) },
			{ 'K', new Colour(85, 85, 85) },
			{ 'R', new Colour(255, 0, 0) },
			{ 'G', new Colour(0, 255, 0) },
			{ 'Y', new Colour(255, 255, 0) },
			{ 'B', new Colour(0, 0, 255) },
			{ 'M', new Colour(255, 0, 255) },
			{ 'C', new Colour(0, 255, 255) },
			{ 'W', new Colour(255, 255, 255) }
		};


		Colour(byte r, byte g, byte b)
		{
			_r = r;
			_g = g;
			_b = b;
		}


		readonly byte _r;
		readonly byte _g;
		readonly byte _b;


		public override string ToString()
		{
			return $"<{_r:X2}/{_g:X2}/{_b:X2}>";
		}


		public override int GetHashCode()
		{
			unchecked {
				int hashCode = _r.GetHashCode();
				hashCode = hashCode * 397 ^ _g.GetHashCode();
				hashCode = hashCode * 397 ^ _b.GetHashCode();
				return hashCode;
			}
		}

		public bool Equals(Colour other)
		{
			return (
				_r == other._r
				&& _g == other._g
				&& _b == other._b
			);
		}

		public override bool Equals(object obj)
		{
			return (obj is Colour colour && Equals(colour));
		}

		public static bool operator ==(Colour a, Colour b)
		{
			return a.Equals(b);
		}

		public static bool operator !=(Colour a, Colour b)
		{
			return !a.Equals(b);
		}


		static char BestMatch(Colour c)
		{
			return Palette.Select(
					pair => new {
						pair,
						distance = Math.Sqrt(
							Math.Pow(c._r - pair.Value._r, 2)
							+ Math.Pow(c._g - pair.Value._g, 2)
							+ Math.Pow(c._b - pair.Value._b, 2)
						)
					}
				)
				.OrderBy(entry => entry.distance)
				.First()
				.pair.Key;
		}

		static Colour Lerp(Colour start, double t, Colour end)
		{
			t = Math.Max(0.0, Math.Min(1.0, t));
			var tt = 1.0 - t;

			return new Colour(
				(byte)(start._r * tt + end._r * t),
				(byte)(start._g * tt + end._g * t),
				(byte)(start._b * tt + end._b * t)
			);
		}

		public static char LerpChar(Colour start, double t, Colour end)
		{
			return BestMatch(Lerp(start, t, end));
		}
	}


	static readonly SafeDictionary<int, Colour> _floaters = new(SafeDictionary<int, Colour>.MissingKeyOperation.ReturnDefault);
	static readonly double[] _floaterPos = new double[3];

	static readonly double[] _floaterSpeed = new double[3];
	//private static readonly double[,] FloaterColourSpeed = new double[3, 3];
	static readonly Random _random = new();

	static int funkyTime = -1;

	static string RenderFunky(int length)
	{
		if (++funkyTime == 0) {
			var palettes = new[] {
				"kKwW",
				"bBcC",
				"rRmM",
				"gGyY",
				string.Join("", Colour.Palette.Keys)
			};

			var palette = palettes[_random.Next(0, palettes.Length)];
			do {
				_floaters[0] = Colour.Palette[palette[_random.Next(0, palette.Length)]];
				_floaters[1] = Colour.Palette[palette[_random.Next(0, palette.Length)]];
				_floaters[2] = Colour.Palette[palette[_random.Next(0, palette.Length)]];
			} while (_floaters[0] == _floaters[1] || _floaters[0] == _floaters[2] || _floaters[1] == _floaters[2]);

			_floaterPos[0] = 0;
			_floaterPos[1] = _random.NextDouble();
			_floaterPos[2] = _random.NextDouble();

			_floaterSpeed[0] = 0;
			_floaterSpeed[1] = _random.NextDouble() / 2;
			_floaterSpeed[2] = _random.NextDouble() / 2;

			//for (int i = 0; i < 3; i++) {
			//	for (int j = 0; j < 3; j++) {
			//		FloaterColourSpeed[i, j] = Random.NextDouble() / 200;
			//	}
			//}

			return null;
		}


		for (int i = 0; i < 3; i++) {
			_floaterPos[i] = i * Math.Sin(funkyTime * _floaterSpeed[i]) / 3.0 + i / 3.0;
			//Floaters[i].R = (byte)Math.Max(0.0, Math.Min(255.0, Floaters[i].R + Math.Sin(funkyTime * FloaterColourSpeed[i, 0]) * 128));
			//Floaters[i].G = (byte)Math.Max(0.0, Math.Min(255.0, Floaters[i].G + Math.Sin(funkyTime * FloaterColourSpeed[i, 1]) * 128));
			//Floaters[i].B = (byte)Math.Max(0.0, Math.Min(255.0, Floaters[i].B + Math.Sin(funkyTime * FloaterColourSpeed[i, 2]) * 128));
		}


		var ordered = _floaterPos
			.Select(
				(p, i) => new {
					p,
					i
				}
			)
			.OrderBy(x => x.p)
			.ToList();

		int size = Math.Min(length, funkyTime);
		var sb = new StringBuilder();
		for (int i = 0; i < size; i++) {
			var t = (size == 1)
				? 0.5
				: i / (double)(size - 1);

			var floaterBefore = ordered.LastOrDefault(x => x.p <= t);
			var floaterAfter = ordered.FirstOrDefault(x => x.p > t);

			Colour colourBefore;
			double p0;
			if (floaterBefore == null) {
				colourBefore = Colour.Palette['k'];
				p0 = 0.0;
			} else {
				colourBefore = _floaters[floaterBefore.i];
				p0 = floaterBefore.p;
			}

			Colour colourAfter;
			double p1;
			if (floaterAfter == null) {
				colourAfter = Colour.Palette['k'];
				p1 = 1.0;
			} else {
				colourAfter = _floaters[floaterAfter.i];
				p1 = floaterAfter.p;
			}

			sb.Append($"#{Colour.LerpChar(colourBefore, (t - p0) / (p1 - p0), colourAfter)}# ");
		}

		return sb.ToString();
	}

	static string FunkyIndent(int length)
	{
		return $"{RenderFunky(length)}#!#{((length - funkyTime > -1) ? new string(' ', length - funkyTime) : null)}";
	}

	#endregion

	static void PrintHeader(string recordKind, LogPrintConfigSection configSection = null)
	{
		// ReSharper disable once StringLiteralTypo
		const string TEMPLATE = "cccBBBBBb";
		var length = "LogPrint:".Length;

		string Indent(int count)
		{
			return (count < length)
				? TEMPLATE.Substring(count)
					.Aggregate(
						new StringBuilder(),
						(sb, c) => sb.Append($"#{c}# "),
						sb => sb.Append("#k#").Append(new string(' ', count)).ToString()
					)
				: FunkyIndent(length);
		}


		var isList = (configSection != null);
		if (isList) {
			Console.Out.WriteLineColours("#c#~W~Log#B#Print~C~#b#:#k# Available config:");
		} else {
			Console.Out.WriteLineColours(
				$"#c#~W~Log#B#Print~C~#b#:#k# #y#{(
					follow
						? $"~M~Following{
							(
								followDir
									? "~R~ Directory"
									: ""
							)
						}~C~"
						: "Processing"
				)} ~Y~{recordKind}~W~#g# from ~Y~{(
				(fileName ?? "-") == "-"
					? "~M~STDIN"
					: fileName
			)}#!#"
			);
		}

		var i = 0;

		if (isList) {
			ListRuleSets();
		} else {
			Console.Out.WriteLineColours($"{Indent(++i)} with ~Y~RuleSet ~G~{ruleSet.Name}");
		}

		if (startTime.HasValue) {
			if (endTime.HasValue) {
				Console.Out.WriteLineColours(
					endTime == startTime
						? $"{Indent(++i)} showing logs ~Y~@ ~C~{startTime}"
						: $"{Indent(++i)} showing logs ~Y~@ ~C~{startTime} ~c~- ~C~{endTime}"
				);
			} else {
				Console.Out.WriteLineColours($"{Indent(++i)} showing logs ~Y~@ ~C~{startTime} ~c~onwards");
			}
		}

		if (flagQuery || timeQuery.HasValue || exceptionQuery || !(grep == null && invGrep == null)) {
			PrintQueries();
		}

		switch (stepMode) {
			case StepMode.None:
				break;

			case StepMode.RecordCount:
				Console.Out.WriteLineColours($"{Indent(++i)} ~c~Stepping by ~C~{stepCount} records ~c~at a time.");
				break;

			case StepMode.LineCount:
				Console.Out.WriteLineColours($"{Indent(++i)} ~c~Stepping by about ~C~{stepCount} lines ~c~at a time.");
				break;

			case StepMode.TimeSpan:
				Console.Out.WriteLineColours($"{Indent(++i)} ~c~Stepping by ~C~{stepTime} ~c~at a time.");
				break;

			default:
				throw new ArgumentOutOfRangeException(nameof(stepMode), stepMode, $"Unhandled StepMode value: `{stepMode}`!");
		}

		if (summariseMode) {
			Console.Out.WriteLineColours($"{Indent(++i)} #g#~W~Summarising log into ~C~{stepTime}~W~ buckets{", scaled down by ~M~".RCoalesce(stepCount.ToString().NullIfEmpty())}~W~.");
		}

		if (isList) {
			FlagSets.AddRange(configSection.FlagSetList);
			FlagSets.Add(new TimeMarker(size: null, outputTimeSpan: false, TimeDeltaMode.PerAll));
		}

		if (!flagQuery && FlagSets.Count > 0) {
			PrintFlags();
		}

		// ReSharper disable once InvertIf
		if (verifyArgs && recordKind != null) {
			Console.WriteLine("---Press-Enter---");
			Console.ReadLine();
		}

		return;


		void ListRuleSets()
		{
			foreach (var currentRuleSet in configSection.RuleSetList) {
				Console.Out.WriteLineColours($"{Indent(++i)} ~Y~RuleSet ~G~{currentRuleSet.Name}~y~:");
				var maxWidth = Console.IsOutputRedirected
					? int.MaxValue
					: Console.WindowWidth - 1;

				if (currentRuleSet.VarList.Any()) {
					const string FIRST = "~m~Vars~r~: ~c~";
					const string OTHERS = "   ~c~";

					string Prefix(int count)
					{
						return $"{Indent(count)}     ";
					}

					var output = new StringBuilder($"{Prefix(++i)}{FIRST}{currentRuleSet.VarList[0].Name}");

					var index = 1;
					while (index < currentRuleSet.VarList.Count) {
						while (index < currentRuleSet.VarList.Count
							&& output.ToString().StripColourCodes().Length + currentRuleSet.VarList[index].Name.Length + 2 < maxWidth
						) {
							output
								.Append("~B~, ~c~")
								.Append(currentRuleSet.VarList[index++].Name);
						}

						Console.Out.WriteLineColours(output.ToString());
						output.Clear();
						if (index < currentRuleSet.VarList.Count) {
							output.Append(Prefix(++i) + OTHERS + currentRuleSet.VarList[index++].Name);
						}
					}

					if (output.Length > 0) {
						Console.Out.WriteLineColours(output.ToString());
					}
				}

				foreach (var rule in currentRuleSet.RulesList) {
					Console.Out.WriteLineColours($"{Indent(++i)}     ~y~Rule ~g~{rule.Name}");
				}
			}

			Console.Out.WriteLineColours(Indent(++i));
		}

		// ReSharper disable once ImplicitlyCapturedClosure - don't care.
		void PrintQueries()
		{
			var joiner = "     ";
			Console.Out.WriteLineColours($"{Indent(++i)} ~G~Querying ~g~for:");
			if (flagQuery) {
				Console.Out.WriteLineColours($"{Indent(++i)}     ~G~Flag ~m~changes: ");
				FlagSets.ForEach(
					flagSet => {
						var suffix = (flagSet.autoTrackID || flagSet.trackIDValueRE != null)
							? "~Y~=~y~" + flagSet.trackIDValueRE
							: null;

						// ReSharper disable once AccessToModifiedClosure - evaluated immediately.
						Console.Out.WriteLineColours($"{Indent(++i)}         ~{(flagSet.IsQuerying ? 'G' : 'M')}~{flagSet.Name}{suffix}");
						flagSet.FlagsList.ForEach(flag => Console.Out.WriteLineColours($"{Indent(++i)}             ~{(flag.IsQuerying ? 'G' : 'M')}~{flag.Name}"));
					}
				);

				joiner = "  ~g~or:";
			}

			if (timeQuery.HasValue) {
				Console.Out.WriteLineColours($"{Indent(++i)} {joiner} ~R~Long-Duration: ~r~time-since-last-log ~R~>= ~M~{timeQuery}");
				joiner = "  ~g~or:";
			}

			if (exceptionQuery) {
				Console.Out.WriteLineColours($"{Indent(++i)} {joiner} ~M~Exceptions.");
				joiner = "  ~g~or:";
			}

			if (grep != null) {
				Console.Out.WriteLineColours($"{Indent(++i)} {joiner} ~M~Lines matching ~c~/~C~{grep}~c~/~M~.");
				joiner = "  ~g~or:";
			}

			if (invGrep != null) {
				Console.Out.WriteLineColours($"{Indent(++i)} {joiner} ~M~Lines ~Y~not~M~ matching ~c~/~C~{invGrep}~c~/~M~.");
			}

			if (exclGrep != null) {
				Console.Out.WriteLineColours($"{Indent(++i)} {joiner} ~Y~Suppress~M~ lines matching ~c~/~C~{exclGrep}~c~/~M~.");
			}
		}

		// ReSharper disable once ImplicitlyCapturedClosure - don't care.
		void PrintFlags()
		{
			Console.Out.WriteLineColours(
				isList
					? $"{Indent(++i)} Flags~m~:"
					: $"{Indent(++i)} With ~M~Flags~m~:"
			);

			FlagSets.ForEach(
				flagSet => {
					var suffix = (isList && !(flagSet is TimeMarker || flagSet.TrackIdRE == null) || flagSet.autoTrackID || flagSet.trackIDValueRE != null)
						? "~Y~=~y~"
						+ (flagSet.trackIDValueRE?.ToString()
							?? (
								isList
									? "~Y~[~c~/id/~Y~]"
									: ""
							)
						)
						: null;

					Console.Out.WriteLineColours($"{Indent(++i)}     ~M~{flagSet.Name}{suffix}");
					foreach (var flag in flagSet.FlagsList) {
						Console.Out.WriteLineColours($"{Indent(++i)}         ~m~{flag.Name}");
						if (isList) {
							flag.EvalsList.ForEach(eval => Console.Out.WriteLineColours($"{Indent(++i)}             ~y~{eval.When}"));
						}
					}
				}
			);
		}
	}


	static void ProcessLines(ILineReader reader, TimeSpan timeout)
	{
		PrintHeader("lines");

		string line;

		var accumulated = new StringBuilder();
		while (!((line = reader.GetNextLine(timeout)) == null || breakForNextFile)) {
#if DEBUG_INPUT
			Console.Error.WriteLine("Read got: " + ("'".RCoalesce(line, "'") ?? "null").Replace("\r", @"\r").Replace("\n", @"\n"));
#endif
			if (line == "") {
				// Timed out...
#if DEBUG_INPUT
				Console.Error.WriteLine("Read timed out");
#endif
				Thread.Sleep(timeout);
				continue;
			}


			accumulated.Append(line);
#if DEBUG_INPUT
			Console.Error.WriteLine("acc => " + accumulated.ToString().Replace("\r", @"\r").Replace("\n", @"\n"));
#endif

			var output = accumulated.ToString();
			if (!output.Contains("\n")) {
				continue;
			}


#if DEBUG_INPUT
			Console.Error.WriteLine("Found a line...");
#endif
			var parts = Regex.Match(output, @"^(.+[\n\r]+)*(.+)");
#if DEBUG_INPUT
			parts.Groups[0].Captures.Cast<Capture>().Select(c => c.Value).ToList().Dump("wholeLines", true);
			parts.Groups[1].Value.Dump("partialLine");
#endif

			accumulated.Clear();
#if DEBUG_INPUT
			Console.Error.WriteLine("clear acc");

#endif
			if (parts.Groups[0].Success) {
				accumulated.Append(parts.Groups[1].Value);
#if DEBUG_INPUT
				Console.Error.WriteLine("acc += partial: " + parts.Groups[1].Value.Replace("\r", @"\r").Replace("\n", @"\n"));
#endif
			}

			parts.Groups[0]
				.Captures
				.Cast<Capture>()
				.Select(c => c.Value)
				.ToList()
				.ForEach(ProcessAndOutputLine);
		}


		if (accumulated.Length > 0) {
			accumulated
				.ToString()
				.Split(_lineBreakChars, StringSplitOptions.RemoveEmptyEntries)
				.ToList()
				.ForEach(ProcessAndOutputLine);
		}
	}

	static void ProcessRecords(ILineReader reader, TimeSpan timeout)
	{
		PrintHeader($"Records~W~, delimited by /~C~{ruleSet.RecordStart}~W~/");

		string line;

		var record = new List<string>();

		while (!((line = reader.GetNextLine(timeout)) == null || breakForNextFile)) {
#if DEBUG_INPUT
			Console.Error.WriteLine("Read got: " + ("'".RCoalesce(line, "'") ?? "null").Replace("\r", @"\r").Replace("\n", @"\n"));
#endif
			if (line == "") {
				// Timed out...
#if DEBUG_INPUT
				Console.Error.WriteLine("Read timed out; record#=" + record.Count);
#endif
				if (record.Count > 0) {
					var lastLine = record[record.Count - 1];
					if (lastLine[lastLine.Length - 1] != '\n') {
						continue;
					}


					ProcessAndOutputLine(string.Join("", record));
					record.Clear();
				}


				Thread.Sleep(timeout);
				continue;
			}


			bool isStartOfNewRecord = ruleSet.RecordStart.IsMatch(line);
			if (isStartOfNewRecord && record.Count > 0) {
#if DEBUG_INPUT
				Console.Error.WriteLine("New record starts; output last record#=" + record.Count);
#endif
				ProcessAndOutputLine(string.Join("", record));
				record.Clear();
			}

#if DEBUG_INPUT
			if (!isStartOfNewRecord) {
				Console.Error.WriteLine($"broken record; record#={record.Count}; line = '{line.Replace("\r", @"\r").Replace("\n", @"\n")}'");
			}

#endif
			if (record.Count == 0) {
#if DEBUG_INPUT
				Console.Error.WriteLine($"Add (first) line = '{line.Replace("\r", @"\r").Replace("\n", @"\n")}'");
#endif
				record.Add(line);
			} else {
				var lastLine = record[record.Count - 1];
				if (lastLine[lastLine.Length - 1] == '\n') {
#if DEBUG_INPUT
					Console.Error.WriteLine($"Add line = '{line.Replace("\r", @"\r").Replace("\n", @"\n")}'");
#endif
					record.Add(line);
				} else {
#if DEBUG_INPUT
					Console.Error.WriteLine($"Append line = '{line.Replace("\r", @"\r").Replace("\n", @"\n")}'");
#endif
					record[record.Count - 1] += line;
				}
			}
#if DEBUG_INPUT
			Console.Error.WriteLine($"record# => {record.Count}; line = '{line.Replace("\r", @"\r").Replace("\n", @"\n")}'");
#endif
		}


		if (record.Count > 0) {
			ProcessAndOutputLine(string.Join("", record));
		}
	}

	static void ProcessAndOutputLine(string line)
	{
		var time = TimeMarker.GetTime(line);

		if (firstLine) {
			stepProgress = 0U;
			if (!summariseMode) {
				stepStartTime = time;
			}

			if (time.HasValue) {
				if (startTime.HasValue) {
					start = time.Value.Date + startTime;
				}

				if (endTime.HasValue) {
					end = time.Value.Date + endTime;
				}
			}

			firstLine = false;
		}

		lastTime = time;

		if (start.HasValue || end.HasValue) {
			if (time < start || time > end) {	// Nullable comparisons of null are false.
				if (!shownSkip) {
					Console.Out.WriteLineColours($"~K~---------------------------------~w~8~W~<~K~-~c~[~C~{time:yyyy-MM-dd HH:mm:ss.ffff}~c~]~K~----------------------------------------------");
					shownSkip = true;
					progressDrawn = null;
				}

				FlagSets.ForEach(flagSet => flagSet.Process(line));		// Still need to process these...

				return;
			}
		}

		shownSkip = false;

		var action = ruleSet.ResetRE?.IsMatch(line) ?? false
			? (Func<FlagSet, string, string>)(
				(flagSet, l) => flagSet.Reset(l)
			)
			: (flagSet, l) => flagSet.Process(l);

		var flagsOutput = FlagSets.Aggregate(
			new StringBuilder(),
			(sb, flagSet) => sb.Append(action(flagSet, line)),
			sb => sb.ToString()
		);

		if (ProcessQueryTests()) {
			return;
		}


		if (summariseMode && time.HasValue) {
			SummariseLine(time.Value, ruleSet.Summarise(line.TrimEnd(_lineBreakChars).EscapeColourCodeChars()));
			return;
		}


		var output = ruleSet.Process(line.TrimEnd(_lineBreakChars).EscapeColourCodeChars());

		_lastPrintedTime = time;
		progressDrawn = null;

		//*
		Console.Out.WriteColours(flagsOutput);
		Console.Out.WriteLineColours(output);
		/*/
		Console.WriteLine(output);
		/**/
		queriedFlagStateChanged = false;

		switch (stepMode) {
			case StepMode.None:
				break;

			case StepMode.RecordCount:
				stepProgress++;
				if (stepProgress >= stepCount) {
					stepProgress = 0U;
					Pause();
				}

				break;

			case StepMode.LineCount:
				stepProgress += (uint)output.Split('\n').Length;
				if (stepProgress >= stepCount) {
					stepProgress = 0U;
					Pause();
				}

				break;

			case StepMode.TimeSpan:
				if (time.HasValue && stepStartTime.HasValue && time.Value - stepStartTime.Value >= stepTime) {
					stepStartTime = time;
					Pause();
				}

				break;

			default:
				throw new ArgumentOutOfRangeException(nameof(stepMode), stepMode, $"Unhandled StepMode value: `{stepMode}`!");
		}


		return;


		// ReSharper disable once ImplicitlyCapturedClosure - don't care.
		bool ProcessQueryTests()
		{
			var delta = ((TimeMarker)FlagSets.FirstOrDefault(flagSet => flagSet is TimeMarker))?.LastDelta;

			// ReSharper disable once InvertIf
			if (
				(flagQuery || timeQuery.HasValue || exceptionQuery || !(grep == null && invGrep == null))	// There is a query of some sort...
				&& !(																						// And not:
						queriedFlagStateChanged																	// :This only ever happens if flagQuery is true.
						|| delta >= timeQuery																	// :Nullable comparison of null is false.
						|| exceptionQuery && line.Contains("Exception")											// :Explicitly checked.
						|| (grepRE?.IsMatch(line) ?? false)														// :Implicitly checked.
						|| !(invGrepRE?.IsMatch(line) ?? true)													// :Implicitly checked.
					)
				|| (exclGrepRE?.IsMatch(line) ?? false)
			) {
				// ReSharper disable once InvertIf
				if (time.HasValue && !(DateTime.Now - progressDrawn < _progressThrottle)) {		// Nullable compare.
					Console.Out.WriteColours($"#K#~k~ {time.Value:yyyy-MM-dd HH:mm:ss.ffff} \r");
					progressDrawn = DateTime.Now;
				}

				return true;
			}

			return false;
		}
	}

	static void SummariseLine(DateTime time, (LogLevel level, string marker) output)
	{
		if (time < stepStartTime + stepTime) {
			AppendSummaryItem(output);
		} else {
			// ReSharper disable once PossibleInvalidOperationException
			stepStartTime ??= time.TruncateTo(stepTime.Value);

			AppendAndClearBucket();
			if (summarised.Length > 0) {
				// ReSharper disable once PossibleInvalidOperationException
				Console.Out.WriteLineColours(timeOutputRuleSet.Process(stepStartTime.Value.ToString(DATE_TIME_FORMAT)) + summarised);
				summarised.Clear();
			}

			for (stepStartTime += stepTime; stepStartTime < time - stepTime; stepStartTime += stepTime) {
				Console.Out.WriteLineColours(timeOutputRuleSet.Process(stepStartTime.Value.ToString(DATE_TIME_FORMAT)));
			}

			AppendSummaryItem(output);
		}
	}

	static void AppendSummaryItem((LogLevel level, string marker) output)
	{
		if (stepCount > 1) {
			_bucket.Add(output);
			if (_bucket.Count == stepCount.Value) {
				AppendAndClearBucket();
			}
		} else {
			summarised.Append(output.marker);
		}
	}

	static void AppendAndClearBucket()
	{
		if (!_bucket.Any()) {
			return;
		}


		summarised.Append(
			_bucket
				.OrderByDescending(pair => Precedence.LogLevels.IndexOf(pair.level))
				.First()
				.marker
		);

		_bucket.Clear();
	}

	static void Pause()
	{
		while (true) {
			Console.Out.WriteColours("#w#~W~Paused> ");
			var key = Console.ReadKey(intercept: true);
			Console.Out.Write("\r       \r");
			if (key.Modifiers == 0 && (key.Key == ConsoleKey.Q || key.Key == ConsoleKey.Escape)) {
				breakForNextFile = true;
			}
#if DEBUG
			else if (key.Key == ConsoleKey.D) {
				if (System.Diagnostics.Debugger.IsAttached) {
					System.Diagnostics.Debugger.Break();
				} else {
					System.Diagnostics.Debugger.Launch();
				}
			}
#endif
			else if (key.KeyChar == '-') {
				Console.Out.WriteLineColours("~K~" + new string('-', Console.WindowWidth - 1));
			} else if (key.Key == ConsoleKey.F1 || key.Key == ConsoleKey.H || key.Key == ConsoleKey.Help || key.KeyChar == '?') {
#if DEBUG
				Console.Out.WriteLineColours("#K#~g~When ~W~Paused~g~, press ~Y~Q~g~ or ~Y~Esc~g~ to ~R~Exit~g~, ~Y~-~g~ to write an ~G~<hr/>~g~, ~Y~?~g~, ~Y~H~g~ or ~Y~F1~g~ for this ~G~help~g~, ~Y~D~g~ to ~R~Debug~g~, or anything else to ~C~step~g~.");
#else
				Console.Out.WriteLineColours("#K#~g~When ~W~Paused~g~, press ~Y~Q~g~ or ~Y~Esc~g~ to ~R~Exit~g~, ~Y~-~g~ to write an ~G~<hr/>~g~, ~Y~?~g~, ~Y~H~g~ or ~Y~F1~g~ for this ~G~help~g~, or anything else to ~C~step~g~.");
#endif
				continue;
			}


			break;
		}
	}
}
