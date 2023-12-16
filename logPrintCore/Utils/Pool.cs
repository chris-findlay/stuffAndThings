using System;
using System.Collections.Generic;

namespace logPrintCore.Utils;

public class Pool<T>
	where T : IRentable<T>
{
	readonly LinkedList<T> _free;
	readonly LinkedList<T> _used;
	readonly int _growthSize;
	readonly Func<T> _factory;

	int _capacity;
	int _allocated;


	public Pool(int initialCapacity, int growthSize, Func<T> factory)
	{
		_capacity = initialCapacity;
		_growthSize = growthSize;
		_factory = factory;
		_allocated = 0;
		_free = new();
		_used = new();

		for (int i = 0; i < initialCapacity; i++) {
			_free.AddFirst(_factory());
		}
	}


	public bool IsEmpty => _allocated == 0;


	public T Rent()
	{
		if (_allocated == _capacity) {
			_capacity += _growthSize;
			for (int i = 0; i < _growthSize; i++) {
				_free.AddFirst(_factory());
			}
		}

		_allocated++;
		var item = _free.First!.Value;
		_free.RemoveFirst();

		_used.AddFirst(item);

		item.Node = _used.Find(item);

		return item;
	}

	public void Return(T item)
	{
		_used.Remove(item.Node!);
		item.Node = null;
		_free.AddFirst(item);
		_allocated--;
	}
}
