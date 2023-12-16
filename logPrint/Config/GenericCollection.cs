using System.Collections.Generic;
using System.Configuration;
using System.Linq;

namespace logPrint.Config;

internal sealed class GenericCollection<T> : ConfigurationElementCollection
	where T : NamedElement, new()
{
	protected override ConfigurationElement CreateNewElement()
	{
		return new T();
	}

	protected override object GetElementKey(ConfigurationElement element)
	{
		return ((T)element).Name;
	}


	public List<T> ToList()
	{
		return this
			.Cast<T>()
			.ToList();
	}
}
