using System.Text.RegularExpressions;

using JetBrains.Annotations;

namespace logPrint.Config.Flags.Evaluator;

internal interface IEvaluator
{
	[UsedImplicitly]
	bool Else { [UsedImplicitly] get; }

	bool Eval();

	void SetValues(Match match);
	void CallMethods();
	void Reset();
}
