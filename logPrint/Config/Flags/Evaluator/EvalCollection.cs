using System.Collections.Generic;
using System.Configuration;
using System.Linq;

namespace logPrint.Config.Flags.Evaluator;

internal sealed class EvalCollection : ConfigurationElementCollection
{
	protected override ConfigurationElement CreateNewElement()
	{
		return new Eval();
	}

	protected override object GetElementKey(ConfigurationElement element)
	{
		return ((Eval)element).When;
	}


	public List<Eval> ToList()
	{
		return this
			.Cast<Eval>()
			.ToList();
	}
}
