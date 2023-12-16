using System.Configuration;

namespace logPrint.Config;

internal abstract class TypedElement : NamedElement
{
	[ConfigurationProperty("type", IsRequired = false)]
	public string Type => this["type"] as string;


	public override string ToString()
	{
		return $"{{{GetType().Name}: {Name} is of type '{Type}'}}";
	}
}
