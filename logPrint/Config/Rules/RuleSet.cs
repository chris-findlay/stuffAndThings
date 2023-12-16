#if DEBUG
//#define DEBUG_MATCHING
#endif

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text.RegularExpressions;

#if DEBUG_MATCHING
using logPrint.Ansi;
using logPrint.Utils;

#endif
namespace logPrint.Config.Rules
{
	internal sealed class RuleSet : NamedElement
	{
		List<Var> _vars;
		List<Rule> _rules;
		Regex _recordStart;
		Regex _resetRE;


		[ConfigurationProperty("recordStart", IsRequired = false)]
		string RecordStartStr => this["recordStart"] as string;

		public Regex RecordStart
			=> string.IsNullOrEmpty(RecordStartStr)
				? null
				: (_recordStart ??= new Regex(RecordStartStr));

		[ConfigurationProperty("reset", IsRequired = false)]
		string ResetStr => this["reset"] as string;

		public Regex ResetRE
			=> _resetRE ??= string.IsNullOrEmpty(ResetStr)
				? null
				: new Regex(ResetStr);

		[ConfigurationProperty("vars", IsDefaultCollection = false)]
		[ConfigurationCollection(typeof(GenericCollection<Var>), AddItemName = "var")]
		GenericCollection<Var> Vars => this["vars"] as GenericCollection<Var>;


		public List<Var> VarList
			=> _vars ??= Vars
				.Cast<Var>()
				.ToList();


		[ConfigurationProperty("", IsDefaultCollection = true)]
		[ConfigurationCollection(typeof(GenericCollection<Rule>), AddItemName = "rule")]
		GenericCollection<Rule> Rules => this[""] as GenericCollection<Rule>;


		public List<Rule> RulesList
			=> _rules ??= Rules
				.Cast<Rule>()
				.Select(rule => rule.ProcessVars(VarList))
				.ToList();


		public void SetHilight(Regex grepRE)
		{
			_rules = RulesList;
			var hilightRule = new Rule();
			hilightRule.SetHilight(grepRE);
			_rules.Add(hilightRule);
		}

		public string Process(string line)
		{
#if DEBUG_MATCHING
			Console.Error.WriteLine(new string('=', AnsiConsoleColourExtensions.OutputWidth - 1));
			line.Dump("<<<<");
#endif
			RulesList.ForEach(rule => line = rule.Process(line));
#if DEBUG_MATCHING
			line.Dump(">>>>");
#endif
			return line;
		}

		public (LogLevel, string) Summarise(string line)
		{
			var result = (line, marker: ((char)LogLevel.None).ToString(), level: LogLevel.None);

			RulesList
				.Where(rule => Enum.IsDefined(typeof(LogLevel), rule.Name))
				.ToList()
				.ForEach(rule => result = rule.Summarise(result));

			return (result.level, result.marker);
		}
	}
}
