using System.Configuration;

namespace logPrint.Config;

internal abstract class NamedElement : ConfigurationElement
{
	[ConfigurationProperty("name", IsRequired = true, IsKey = true)]
	public virtual string Name => this["name"] as string;


	public override string ToString()
	{
		return $"{{{GetType().Name}: {nameof(Name)}='{Name}'}}";
	}
}
