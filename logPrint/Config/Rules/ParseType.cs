using JetBrains.Annotations;

namespace logPrint.Config.Rules;

internal enum ParseType
{
	None,
	[UsedImplicitly] Json,
	Parent
}
