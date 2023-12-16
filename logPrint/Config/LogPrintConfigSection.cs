using System.Collections.Generic;
using System.Configuration;
using System.Linq;

using JetBrains.Annotations;

using logPrint.Config.Flags;
using logPrint.Config.Rules;

namespace logPrint.Config;

[UsedImplicitly]
internal class LogPrintConfigSection : ConfigurationSection
{
	List<FlagSet> _flagSetList;
	List<RuleSet> _ruleSetList;


	[ConfigurationProperty("usage")]
	public UsageElement Usage => this["usage"] as UsageElement;

	[ConfigurationProperty("definition")]
	public UsageElement Config => this["definition"] as UsageElement;


	[ConfigurationProperty("flagSets")]
	[ConfigurationCollection(typeof(GenericCollection<FlagSet>), AddItemName = "flagSet")]
	GenericCollection<FlagSet> FlagSets => this["flagSets"] as GenericCollection<FlagSet>;

	[ConfigurationProperty("ruleSets")]
	[ConfigurationCollection(typeof(GenericCollection<RuleSet>), AddItemName = "ruleSet")]
	GenericCollection<RuleSet> RuleSets => this["ruleSets"] as GenericCollection<RuleSet>;


	public IEnumerable<FlagSet> FlagSetList => _flagSetList ??= FlagSets.Cast<FlagSet>().ToList();

	public IEnumerable<RuleSet> RuleSetList => _ruleSetList ??= RuleSets.Cast<RuleSet>().ToList();
}
