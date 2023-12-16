using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Runtime.Serialization;

namespace logPrint.Utils;

internal sealed class OrderedDictionary<TKey, TValue> : OrderedDictionary, IDictionary<TKey, TValue>
{
	// ReSharper disable UnusedMember.Global
	public OrderedDictionary() { }
	public OrderedDictionary(int capacity) : base(capacity) { }
	public OrderedDictionary(IEqualityComparer<TKey> comparer) : base(new UngenericComparer(comparer))
	{
		Comparer = comparer;
	}
	public OrderedDictionary(int capacity, IEqualityComparer<TKey> comparer) : base(capacity, new UngenericComparer(comparer))
	{
		Comparer = comparer;
	}

	// ReSharper disable once UnusedMember.Local
	OrderedDictionary(SerializationInfo info, StreamingContext context) : base(info, context) { }
	// ReSharper restore UnusedMember.Global


	public TValue this[TKey key] {
		get => (TValue)base[key];
		set => base[key] = value;
	}


	public IEqualityComparer<TKey> Comparer { get; } = EqualityComparer<TKey>.Default;

	public new ICollection<TKey> Keys => base.Keys.Cast<TKey>().ToList();
	public new ICollection<TValue> Values => base.Values.Cast<TValue>().ToList();


	public new IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
	{
		var enumerator = base.GetEnumerator();
		while (enumerator.MoveNext()) {
			yield return new KeyValuePair<TKey, TValue>((TKey)enumerator.Key, (TValue)enumerator.Value);
		}
	}

	public void Add(KeyValuePair<TKey, TValue> item)
	{
		base.Add(item.Key, item.Value);
	}

	public bool Contains(KeyValuePair<TKey, TValue> item)
	{
		return TryGetValue(item.Key, out TValue value) && Equals(item.Value, value);
	}

	public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
	{
		throw new NotSupportedException();
	}

	public bool Remove(KeyValuePair<TKey, TValue> item)
	{
		if (!Contains(item)) {
			return false;
		}


		base.Remove(item.Key);
		return true;
	}

	public bool ContainsKey(TKey key)
	{
		return Contains(key);
	}

	public bool ContainsValue(TValue value)
	{
		return Values.Contains(value);
	}

	public void Add(TKey key, TValue value)
	{
		base.Add(key, value);
	}

	public bool Remove(TKey key)
	{
		if (!ContainsKey(key)) {
			return false;
		}


		base.Remove(key);
		return true;
	}

	public bool TryGetValue(TKey key, out TValue value)
	{
		if (ContainsKey(key)) {
			value = this[key];
			return true;
		}


		value = default;
		return false;
	}


	sealed class UngenericComparer : IEqualityComparer
	{
		readonly IEqualityComparer<TKey> _comparer;


		public UngenericComparer(IEqualityComparer<TKey> comparer)
		{
			_comparer = comparer;
		}


		bool IEqualityComparer.Equals(object x, object y)
		{
			return _comparer.Equals((TKey)x, (TKey)y);
		}

		public int GetHashCode(object obj)
		{
			return obj.GetHashCode();
		}
	}
}
