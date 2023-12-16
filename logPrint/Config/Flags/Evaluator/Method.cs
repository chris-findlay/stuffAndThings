using System.Configuration;

using JetBrains.Annotations;

namespace logPrint.Config.Flags.Evaluator;

internal sealed class Method : NamedElement
{
	[ConfigurationProperty("code", IsRequired = true)]
	[CanBeNull]
	public string Code => this["code"] as string;
}
