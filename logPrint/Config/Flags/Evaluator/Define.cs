using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;

namespace logPrint.Config.Flags.Evaluator;

internal sealed class Define : TypedElement
{
	List<Value> _values;


	[ConfigurationProperty("", IsDefaultCollection = true)]
	[ConfigurationCollection(typeof(GenericCollection<Value>), AddItemName = "value")]
	GenericCollection<Value> Values => this[""] as GenericCollection<Value>;

	IEnumerable<Value> ValuesList => _values ??= Values.ToList();


	public override string ToString()
	{
		return $"{{{GetType().Name}: {Type} {Name}=[{string.Join(", ", Values)}]";
	}

	public string Value(List<string> selectedDefines)
	{
		return (
			ValuesList.FirstOrDefault(value => selectedDefines.Any(define => value.Name.StartsWith(define, StringComparison.OrdinalIgnoreCase)))
			?? ValuesList.First()
		).Value;
	}
}
