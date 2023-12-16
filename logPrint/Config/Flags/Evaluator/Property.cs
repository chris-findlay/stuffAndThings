using System.Configuration;

namespace logPrint.Config.Flags.Evaluator;

internal sealed class Property : TypedElement
{
	[ConfigurationProperty("code", IsRequired = true)]
	public string Code => this["code"] as string;
}
