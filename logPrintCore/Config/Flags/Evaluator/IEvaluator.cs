using System;

using JetBrains.Annotations;

namespace logPrintCore.Config.Flags.Evaluator;

public interface IEvaluator
{
	[UsedImplicitly]
	bool Else { [UsedImplicitly] get; }

	bool Eval();

	Func<string>? GetOutput()
	{
		return null;
	}

	void CallMethods();
	void Reset();
}
