using System.Collections.Generic;

namespace logPrintCore.Utils;

public interface IRentable<T>
{
	public LinkedListNode<T>? Node { get; set; }
}
