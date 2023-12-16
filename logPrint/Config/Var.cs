using System.Configuration;

namespace logPrint.Config;

internal class Var : NamedElement
{
	[ConfigurationProperty("value", IsRequired = true)]
	public string Value => this["value"] as string;


	public override string ToString()
	{
		return $"{{{GetType().Name}: {Name}='{Value}'";
	}
}
