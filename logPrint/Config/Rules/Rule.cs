#if DEBUG
//#define DEBUG_MATCHING
#endif

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
#if DEBUG_MATCHING
using System.Reflection;
#endif
using System.Text;
using System.Text.RegularExpressions;

using logPrint.Ansi;
using logPrint.Utils;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace logPrint.Config.Rules
{
	internal sealed class Rule : NamedElement
	{
		static readonly JsonSerializerSettings _jsonSerializerSettings = new() {
			DateParseHandling = DateParseHandling.None,
			FloatParseHandling = FloatParseHandling.Double
		};


#if DEBUG_MATCHING
		private readonly Func<Type, PropertyInfo, bool> _propFilter = (_, p) => new[] {
			nameof(Name),
			nameof(TestRE),
			nameof(MatchRE),
			nameof(SubRules)
		}.Contains(p.Name);

		private readonly Func<Type, PropertyInfo, bool> _recurseFilter = (_, p) => p.Name == nameof(SubRules);

#endif
		Regex _testRE;
		Regex _matchRE;
		string _replaceStr;

		ParseType? _parseType;
		JObject _json;
		JToken _originalJson;
		List<Var> _vars;
		SafeDictionary<string, string> _jsonLookup;


		[ConfigurationProperty("test", IsRequired = false)]
		string TestStr => (this["test"] as string).NullIfEmpty();

		[ConfigurationProperty("match", IsRequired = true)]
		string MatchStr => this["match"] as string;

		[ConfigurationProperty("parse", IsRequired = false)]
		ParseType? Parse => _parseType ??= this["parse"] as ParseType? ?? ParseType.None;

		ParseType ParseType => Parse ?? ParseType.None;

		[ConfigurationProperty("replace", IsRequired = true)]
		string Replace => this["replace"] as string;

		string ReplaceStr => _replaceStr ??= Replace.Replace("\\r", "\r").Replace("\\n", "\n").Replace("\\t", "\t");		//BUG: this .Replace code is not working, and runs before the -D debugger is attached!!!

		[ConfigurationProperty("repeat", IsRequired = false)]
		bool Repeat => this["repeat"] as bool? ?? false;

		[ConfigurationProperty("", IsDefaultCollection = true)]
		[ConfigurationCollection(typeof(GenericCollection<Rule>), AddItemName = "group")]
		GenericCollection<Rule> SubRules => this[""] as GenericCollection<Rule>;

		Rule Parent { get; set; }

		List<Rule> SubRulesList => SubRules
			.Cast<Rule>()
			.Select(
				rule => {
					rule.Parent = this;
					return rule;
				}
			)
			.ToList();

		Regex TestRE => _testRE ??= (TestStr == null)
			? null
			: new Regex(TestStr);

		Regex MatchRE => _matchRE ??= new Regex(MatchStr);


		List<ReplacePart> _processedReplaceParts;

		List<ReplacePart> ProcessedReplace {
			get {
				if (_processedReplaceParts != null) {
					return _processedReplaceParts;
				}


				_processedReplaceParts = new List<ReplacePart>();
				int i = 0;
				int j = ReplaceStr.IndexOf('$');
				while (j != -1) {
					if (i < j) {
						_processedReplaceParts.Add(new ReplacePart(ReplaceStr.Substring(i, j - i)));
					}

					if (ReplaceStr[j + 1] == '{') {
						i = j + 2;
						j = ReplaceStr.IndexOf('}', i);
						var substring = ReplaceStr.Substring(i, j - i);
						_processedReplaceParts.Add(
							(ParseType != ParseType.None && substring.StartsWith("JSON.", StringComparison.Ordinal))
								? new JsonReplacePart(substring)
								: new ReplacePart(substring, isGroup: true)
						);

						i = j + 1;
					} else {
						j = i = j + 1;
						while (++j < ReplaceStr.Length && ReplaceStr[j] >= '0' && ReplaceStr[j] <= '9') { }

						_processedReplaceParts.Add(new ReplacePart(int.Parse(ReplaceStr.Substring(i, j - i))));
						i = j;
					}

					j = ReplaceStr.IndexOf('$', i);
				}

				if (i < ReplaceStr.Length) {
					_processedReplaceParts.Add(new ReplacePart(ReplaceStr.Substring(i)));
				}

#if DEBUG_MATCHING
				_processedReplaceParts.Dump(multiLine: true);

#endif
				return _processedReplaceParts;
			}
		}


		public void SetHilight(Regex grepRE)
		{
			_testRE = null;
			_matchRE = grepRE;
			_replaceStr = "~k~#W#$0";
		}


		public Rule ProcessVars(List<Var> vars)
		{
			_vars = vars;
			_jsonLookup = new SafeDictionary<string, string>(SafeDictionary<string, string>.MissingKeyOperation.ReturnDefault);
			_vars
				.Where(var => var.Name.StartsWith("JSON.", StringComparison.Ordinal))
				.ToList()
				.ForEach(var => _jsonLookup.Add(var.Name.Substring(5), var.Value));

			_replaceStr = Regex.Replace(
				Replace,
				"%([^%]+)%",
				match => vars.FirstOrDefault(v => v.Name.Equals(match.Groups[1].Value, StringComparison.OrdinalIgnoreCase))?.Value
					?? $"%MISSING: {match.Groups[1].Value}%"
			);

			SubRulesList.ForEach(rule => rule.ProcessVars(vars));

			return this;
		}

		public string Process(string line)
		{
			if (ParseType != ParseType.None) {
				// ReSharper disable once InvertIf
				if (Repeat) {
					while ((TestRE?.IsMatch(line) ?? true) && MatchRE.IsMatch(line)) {
						line = MatchRE.Replace(line, ProcessJson);
					}

					return line;
				}


				return (TestRE?.IsMatch(line) ?? true)
					? MatchRE.Replace(line, ProcessJson)
					: line;
			}


			if (SubRules.Count > 0) {
				return ProcessWithSubRules(line);
			}


#if DEBUG_MATCHING
			bool isMatch = TestRE?.IsMatch(line) ?? true;
			TestRE.Dump(Name + "|TestRE ");
			isMatch.Dump(Name + "|Test");
			if (isMatch) {
				this.Dump(Name + "=", true, _propFilter, _recurseFilter);
				MatchRE.IsMatch(line).Dump(Name + "|Match");
				MatchRE.Replace(line, $"~<~#<#{Replace}#>#~>~").Dump(Name + "|=result");
			}
#endif
			// ReSharper disable once InvertIf
			if (Repeat) {
				while ((TestRE?.IsMatch(line) ?? true) && MatchRE.IsMatch(line)) {
					line = MatchRE.Replace(
						line,
						AnsiConsoleColourExtensions.PUSH_COLOURS
							.RCoalesce(ReplaceStr.Replace("\\r", "\r").Replace("\\n", "\n").Replace("\\t", "\t").NullIfEmpty(), AnsiConsoleColourExtensions.POP_COLOURS)
							?? ""
					);
				}

				return line;
			}


			return (TestRE?.IsMatch(line) ?? true)
				? MatchRE.Replace(
					line,
					AnsiConsoleColourExtensions.PUSH_COLOURS
						.RCoalesce(ReplaceStr.Replace("\\r", "\r").Replace("\\n", "\n").Replace("\\t", "\t").NullIfEmpty(), AnsiConsoleColourExtensions.POP_COLOURS)
						?? ""
				)
				: line;
		}

		string ProcessJson(Match match)
		{
			if (ParseType == ParseType.Parent) {
				_json = Parent._json;
				_originalJson = Parent._originalJson;
			} else {
				_json = (JObject)JsonConvert.DeserializeObject(
					match.Groups["JSON"]?.Value ?? match.Value,
					_jsonSerializerSettings
				);

				_originalJson = _json?.DeepClone();
			}

			return ProcessMatch(match);
		}


		string ProcessWithSubRules(string line)
		{
#if DEBUG_MATCHING
			bool isMatch = TestRE?.IsMatch(line) ?? true;
			TestRE.Dump(Name + "|TestRE ");
			isMatch.Dump(Name + "|Test");
			if (isMatch) {
				this.Dump(Name + "=", true, _propFilter, _recurseFilter);
				MatchRE.IsMatch(line).Dump(Name + "|Match");
				MatchRE.Replace(line, $"~<~#<#{Replace}#>#~>~").Dump(Name + "|=result");
			}
#endif
			// ReSharper disable once InvertIf
			if (Repeat) {
				while ((TestRE?.IsMatch(line) ?? true) && MatchRE.IsMatch(line)) {
					line = MatchRE.Replace(line, ProcessMatch);
				}

				return line;
			}


			return (TestRE?.IsMatch(line) ?? true)
				? MatchRE.Replace(line, ProcessMatch)
				: line;
		}

		string ProcessMatch(Match match)
		{
			var result = new StringBuilder();
#if DEBUG_MATCHING
			match.Dump("match", true, (_, p) => new[] { "Success", "Groups", "Name", "Value" }.Contains(p.Name), (_, p) => p.Name == "Groups");
#endif
			foreach (var part in ProcessedReplace) {
#if DEBUG_MATCHING
				part.Dump("part ");
#endif
				if (part is JsonReplacePart jsonReplacePart) {
					result.Append(ProcessGroup(FormatJson(jsonReplacePart.Evaluate(_json, match)), part.GroupName));
				} else if (part.GroupName != null) {
					result.Append(ProcessGroup(match.Groups[part.GroupName], part.GroupName));
				} else if (part.GroupNumber != null) {
					result.Append(ProcessGroup(match.Groups[part.GroupNumber.Value], name: part.GroupNumber.ToString()));
				} else {
					result.Append(part.Text);
				}
			}

#if DEBUG_MATCHING
			result.Dump("result");
#endif
			return AnsiConsoleColourExtensions.PUSH_COLOURS
				.RCoalesce(result.ToString().NullIfEmpty(), AnsiConsoleColourExtensions.POP_COLOURS);
		}

		string ProcessGroup(Group matchGroup, string name)
		{
			return matchGroup.Success
				? ProcessGroup(matchGroup.Value, name)
				: "";
		}

		string ProcessGroup(string str, string name)
		{
#if DEBUG_MATCHING
			matchGroup.Dump("group", true);
#endif
			return SubRulesList
				.Where(rule => rule.Name.Split('.').First() == name)
				.Aggregate(
					str,
					(value, subRule) => subRule
						.Process(value)
#if DEBUG_MATCHING
							.Dump("groupResult")
#endif
				);
		}


		public (string line, string marker, LogLevel level) Summarise((string line, string marker, LogLevel level) result)
		{
			var level = (LogLevel)Enum.Parse(typeof(LogLevel), Name);
			return (TestRE?.IsMatch(result.line) ?? true)
				? MatchRE.IsMatch(result.line)
					? (
						result.line,
						MatchRE.Replace(
							((char)level).ToString(),
							AnsiConsoleColourExtensions.PUSH_COLOURS
								.RCoalesce(ReplaceStr.Replace("\\r", "\r").Replace("\\n", "\n").Replace("\\t", "\t").NullIfEmpty(), AnsiConsoleColourExtensions.POP_COLOURS)
								?? ""
						),
						level
					)
					: result
				: result;
		}


		string FormatJson(JToken jToken, bool coloursSimpleValues = false, string indent = "")
		{
			var newIndent = indent + (_jsonLookup["indent"] ?? "\t");
			var sb = new StringBuilder();

			// ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault - see default case trivia
			switch (jToken.Type) {
				case JTokenType.Object:
					sb.Append(AnsiConsoleColourExtensions.PUSH_COLOURS).Append(_jsonLookup["{}"]).Append("{");

					List<JProperty> props = ((JObject)jToken).Properties().ToList();
					if (props.Any()) {
						sb.AppendLine();
						for (var i = 0; i < props.Count; i++) {
							JProperty jProperty = props[i];
							sb.Append(newIndent)
								.Append(_jsonLookup["prop"])
								.Append(jProperty.Name)
								.Append(_jsonLookup[":"])
								.Append(": ")
								.Append(FormatJson(jProperty.Value, coloursSimpleValues: true, newIndent));

							if (i < props.Count - 1) {
								sb.Append(_jsonLookup[","]).Append(',');
							}

							sb.AppendLine();
						}

						sb.Append(indent).Append(_jsonLookup["{}"]);
					} else if (!coloursSimpleValues) {
						return "{}";
					}


					sb.Append("}").Append(AnsiConsoleColourExtensions.POP_COLOURS);
					break;

				case JTokenType.Array:
					sb.Append(AnsiConsoleColourExtensions.PUSH_COLOURS).Append(_jsonLookup["[]"]).Append("[");

					List<JToken> arr = ((JArray)jToken).ToList();
					if (arr.Any()) {
						sb.AppendLine();
						for (var i = 0; i < arr.Count; i++) {
							sb.Append(newIndent)
								.Append(FormatJson(arr[i], coloursSimpleValues: true, newIndent));

							if (i == arr.Count) {
								sb.Append(_jsonLookup[","]).Append(',');
							}

							sb.AppendLine();
						}

						sb.Append(indent).Append(_jsonLookup["[]"]);
					} else if (!coloursSimpleValues) {
						return "[]";
					}


					sb.Append("]").Append(AnsiConsoleColourExtensions.POP_COLOURS);
					break;

				case JTokenType.Integer:
					ConditionalFormat(_jsonLookup["0"] ?? _jsonLookup["0.0"]);
					break;

				case JTokenType.Float:
					ConditionalFormat(_jsonLookup["0.0"] ?? _jsonLookup["0"]);
					break;

				case JTokenType.String:
					ConditionalFormat(_jsonLookup["\""]);
					break;

				case JTokenType.Boolean:
					ConditionalFormat(_jsonLookup["!"]);
					break;

				case JTokenType.Null:
					AlwaysFormat(_jsonLookup["null"]);
					break;

				case JTokenType.Undefined:
					AlwaysFormat(_jsonLookup["undefined"]);
					break;

				/*
				case JTokenType.None:
				case JTokenType.Constructor:
				case JTokenType.Property:
				case JTokenType.Comment:
				case JTokenType.Raw:
				case JTokenType.Bytes:
				case JTokenType.Date:
				case JTokenType.Guid:
				case JTokenType.Uri:
				case JTokenType.TimeSpan:
				*/
				default:
					throw new ArgumentOutOfRangeException(nameof(jToken), jToken.Type, $"Unhandled JToken.Type: `{jToken.Type}`!");
			}


			return sb.ToString();


			void ConditionalFormat(string format)
			{
				if (coloursSimpleValues) {
					sb.Append(AnsiConsoleColourExtensions.PUSH_COLOURS).Append(format);
				}

				sb.Append(jToken);

				if (coloursSimpleValues) {
					sb.Append(AnsiConsoleColourExtensions.POP_COLOURS);
				}
			}

			void AlwaysFormat(string format)
			{
				sb.Append(AnsiConsoleColourExtensions.PUSH_COLOURS).Append(format).Append(jToken).Append(AnsiConsoleColourExtensions.POP_COLOURS);
			}
		}


		public override string ToString()
		{
			return $"{GetType().Name}: {Name}, {(ParseType == ParseType.None ? "" : $"{nameof(Parse)}: {Parse}, ")}{(Repeat ? $"{nameof(Repeat)}, " : "")}{nameof(SubRules)}: [{string.Join(", ", SubRules.Cast<Rule>())}]";
		}


		class ReplacePart
		{
			public ReplacePart(string text, bool isGroup = false)
			{
				if (isGroup) {
					GroupName = text;
				} else {
					Text = text.Replace("\\r", "\r").Replace("\\n", "\n").Replace("\\t", "\t");
				}
			}
			public ReplacePart(int groupNumber)
			{
				GroupNumber = groupNumber;
			}


			public string Text { get; }
			public int? GroupNumber { get; }
			public string GroupName { get; protected set; }


			public override string ToString()
			{
				return $"{{{GetType().Name}: {"Text=".RCoalesce(Text)}{"Group#=".RCoalesce(GroupNumber.ToString().NullIfEmpty())}{"GroupName=".RCoalesce(GroupName)}}}";
			}
		}


		sealed class JsonReplacePart : ReplacePart
		{
			public Func<JObject, Match, JToken> Evaluate { get; }


			public JsonReplacePart(string jsonFunc) : base(jsonFunc, isGroup: true)
			{
				var i = jsonFunc.IndexOf('(');
				var func = jsonFunc.Substring(5, i - 5);
				GroupName = jsonFunc.Substring(i + 1, jsonFunc.Length - 7 - func.Length);

				bool allowMissing = func.EndsWith("?", StringComparison.Ordinal);
				switch (func) {
					case "read":
					case "read?":
						Evaluate = (jObject, match) => ReadAndRemove(jObject, GroupName, match, allowMissing);
						break;

					case "value":
						Evaluate = (jObject, match) => Read(jObject, GroupName, match, allowMissing);
						break;

					case "unread":
						GroupName = "*";
						// ReSharper disable once ImplicitlyCapturedClosure - don't care.
						Evaluate = (jObject, _) => jObject.Children().Any() ? jObject : (JToken)"";
						break;

					default:
						throw new ArgumentOutOfRangeException(nameof(func), func, $"Unhandled JSON func: `{func}`!");
				}
			}


			static JToken ReadAndRemove(JToken json, string jsonPath, Match match, bool allowMissing)
			{
				return JsonPath(json, ResolvePath(jsonPath, match), allowMissing, remove: true);
			}

			static JToken Read(JToken json, string jsonPath, Match match, bool allowMissing)
			{
				return JsonPath(json, ResolvePath(jsonPath, match), allowMissing);
			}

			static List<string> ResolvePath(string jsonPath, Match match)
			{
				if (jsonPath[0] == '$') {
					jsonPath = match.Groups[jsonPath.Substring(1)].Value;
				}

				return jsonPath
					.Split('.')
					.Select(p => p.Split('[').Select(q => q.EndsWith("]", StringComparison.Ordinal) ? '[' + q : q))
					.SelectMany(x => x)
					.ToList();
			}

			static JToken JsonPath(JToken json, List<string> path, bool allowMissing, bool remove = false)
			{
				JToken result = json;
				for (var i = 0; i < path.Count; i++) {
					string s = path[i];
					result = result?.SelectToken(s)
						?? result?.SelectToken(Regex.Replace(s, @"^\[([^'""].*)]$", "['$1']"))
						?? result?.SelectToken(Regex.Replace(s, "^@?([^:]+)(?::.+)?$", "$1"));		// Finally, try minus a @ prefix or :format suffix.

					if (result == null) {
						return allowMissing
							? ""
							: $"MISSING:{s}";
					}


					if (remove && i == path.Count - 1) {
						result.Parent?.Remove();
					}
				}

				return result;
			}
		}
	}
}
