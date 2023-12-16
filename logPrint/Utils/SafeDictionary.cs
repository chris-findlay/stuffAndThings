using System;
using System.Collections;
using System.Collections.Generic;

namespace logPrint.Utils;

internal sealed class SafeDictionary<TKey, TValue> : IDictionary<TKey, TValue>
{
	readonly MissingKeyOperation _missingKeyOperation;
	readonly Func<TKey, SafeDictionary<TKey, TValue>, TValue> _missingValueFunc;
	readonly Dictionary<TKey, TValue> _dict = new();


	// ReSharper disable once MemberCanBePrivate.Global
	public SafeDictionary(MissingKeyOperation missingKeyOperation)
	{
		_missingKeyOperation = missingKeyOperation;

		if (missingKeyOperation == MissingKeyOperation.ReturnKey && typeof(TKey) != typeof(TValue)) {
			throw new ArgumentOutOfRangeException(nameof(missingKeyOperation), "Cannot use MissingKeyOperation.ReturnKey unless TKey == TValue!");
		}
	}
	public SafeDictionary(Func<TKey, SafeDictionary<TKey, TValue>, TValue> missingValueFunc) : this(MissingKeyOperation.EvaluateFunc)
	{
		_missingValueFunc = missingValueFunc;
	}


	public int Count => _dict.Count;
	public bool IsReadOnly => false;

	public ICollection<TKey> Keys => _dict.Keys;
	public ICollection<TValue> Values => _dict.Values;


	public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
	{
		return _dict.GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}

	public void Add(KeyValuePair<TKey, TValue> item)
	{
		_dict.Add(item.Key, item.Value);
	}

	public void Clear()
	{
		_dict.Clear();
	}

	public bool Contains(KeyValuePair<TKey, TValue> item)
	{
		return (_dict.ContainsKey(item.Key) && Equals(_dict[item.Key], item.Value));
	}

	public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
	{
		throw new NotSupportedException();
	}

	public bool Remove(KeyValuePair<TKey, TValue> item)
	{
		return _dict.Remove(item.Key);
	}

	public bool ContainsKey(TKey key)
	{
		return _dict.ContainsKey(key);
	}

	public void Add(TKey key, TValue value)
	{
		_dict.Add(key, value);
	}

	public bool Remove(TKey key)
	{
		return _dict.Remove(key);
	}

	public bool TryGetValue(TKey key, out TValue value)
	{
		return _dict.TryGetValue(key, out value);
	}

	public TValue this[TKey key] {
		get
			=> ContainsKey(key)
				? _dict[key]
				: _missingKeyOperation switch {
					MissingKeyOperation.ReturnDefault => default,
					MissingKeyOperation.ReturnKey => (TValue)Convert.ChangeType(key, typeof(TValue)),
					MissingKeyOperation.EvaluateFunc => _missingValueFunc(key, this),
					_ => throw new ArgumentOutOfRangeException(nameof(_missingKeyOperation), _missingKeyOperation, $"Unhandled MissingKeyOperation value: '{_missingKeyOperation}'")
				};
		set => _dict[key] = value;
	}


	public enum MissingKeyOperation
	{
		ReturnDefault,
		ReturnKey,
		EvaluateFunc
	}
}
