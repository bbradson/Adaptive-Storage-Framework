// Copyright (c) 2024 bradson
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using System.Collections;
using AdaptiveStorage.Fishery.Collections.Internal;

namespace AdaptiveStorage.Fishery.Collections;

public partial class IntFishTable<TValue>
{
	public record struct Enumerator : IEnumerator<KeyValuePair<int, TValue>>, IDictionaryEnumerator
	{
		private readonly IntFishTable<TValue> _dictionary;
		private readonly int _version;
		private uint _index;
		private KeyValuePair<int, TValue> _current;
		private readonly int _getEnumeratorRetType; // What should Enumerator.Current return?

		internal const int DICT_ENTRY = 1;
		internal const int KEY_VALUE_PAIR = 2;

		internal Enumerator(IntFishTable<TValue> dictionary, int getEnumeratorRetType)
		{
			_dictionary = dictionary;
			_version = dictionary._version;
			_index = 0u;
			_getEnumeratorRetType = getEnumeratorRetType;
			_current = default;
		}

		public bool MoveNext()
		{
			if (_version != _dictionary._version)
				CollectionThrowHelper.ThrowInvalidOperationException_InvalidOperation_EnumFailedVersion();

			while (_index < (uint)_dictionary._buckets.Length)
			{
				if (_dictionary.IsBucketEmpty((int)_index++))
					continue;

				_current = _dictionary._buckets[_index - 1].AsKeyValuePair()!;
				return true;
			}

			_index = (uint)_dictionary._buckets.Length + 1u;
			_current = default;
			return false;
		}

		[CollectionAccess(CollectionAccessType.Read)]
		public KeyValuePair<int, TValue> Current => _current;

		public void Dispose()
		{
		}

		object IEnumerator.Current
		{
			get
			{
				if (_index == 0u || _index == (uint)_dictionary._buckets.Length + 1u)
					CollectionThrowHelper.ThrowInvalidOperationException_InvalidOperation_EnumOpCantHappen();

				return _getEnumeratorRetType == DICT_ENTRY
					? new DictionaryEntry(_current.Key!, _current.Value)
					: _current;
			}
		}

		void IEnumerator.Reset()
		{
			if (_version != _dictionary._version)
				CollectionThrowHelper.ThrowInvalidOperationException_InvalidOperation_EnumFailedVersion();

			_index = 0u;
			_current = default;
		}

		DictionaryEntry IDictionaryEnumerator.Entry
		{
			get
			{
				if (_index == 0u || _index == (uint)_dictionary._buckets.Length + 1u)
					CollectionThrowHelper.ThrowInvalidOperationException_InvalidOperation_EnumOpCantHappen();

				return new(_current.Key!, _current.Value);
			}
		}

		object IDictionaryEnumerator.Key
		{
			get
			{
				if (_index == 0u || _index == (uint)_dictionary._buckets.Length + 1u)
					CollectionThrowHelper.ThrowInvalidOperationException_InvalidOperation_EnumOpCantHappen();

				return _current.Key;
			}
		}

		object? IDictionaryEnumerator.Value
		{
			get
			{
				if (_index == 0u || _index == (uint)_dictionary._buckets.Length + 1u)
					CollectionThrowHelper.ThrowInvalidOperationException_InvalidOperation_EnumOpCantHappen();

				return _current.Value;
			}
		}
	}
}