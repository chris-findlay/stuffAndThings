using System.Configuration;

namespace logPrint.Config.Flags.Evaluator;

internal sealed class Field : TypedElement
{
	[ConfigurationProperty("value", IsRequired = true)]
	public string Value => this["value"] as string;


	public override string ToString()
	{
		return $"{{{GetType().Name}: {Type} {Name}='{Value}'";
	}
}
