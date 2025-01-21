// Copyright (c) 2022 bradson
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v.2.0.If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using System.Collections;
using AdaptiveStorage.Fishery.Collections.Internal;

namespace AdaptiveStorage.Fishery.Collections;

public partial class IntFishSet
{
#pragma warning disable CS8766, CS8767
	public record struct Enumerator : IEnumerator<int>
	{
		private readonly IntFishSet _fishSet;
		private readonly int _version;
		private uint _index;
		private int _current;

		internal const int KEY_VALUE_PAIR = 2;

		internal Enumerator(IntFishSet fishSet, int getEnumeratorRetType)
		{
			_fishSet = fishSet;
			_version = fishSet._version;
			_index = 0u;
			_current = default;
		}

		public bool MoveNext()
		{
			if (_version != _fishSet._version)
				CollectionThrowHelper.ThrowInvalidOperationException_InvalidOperation_EnumFailedVersion();

			while (_index < (uint)_fishSet._buckets.Length)
			{
				if (_fishSet.IsBucketEmpty((int)_index++))
					continue;

				_current = _fishSet._buckets[_index - 1]!;
				return true;
			}

			_index = (uint)_fishSet._buckets.Length + 1u;
			_current = default;
			return false;
		}

		[CollectionAccess(CollectionAccessType.Read)]
		public int Current => _current;

		public void Dispose()
		{
		}

		object IEnumerator.Current
		{
			get
			{
				if (_index == 0u || _index == (uint)_fishSet._buckets.Length + 1u)
					CollectionThrowHelper.ThrowInvalidOperationException_InvalidOperation_EnumOpCantHappen();

				return _current;
			}
		}

		void IEnumerator.Reset()
		{
			if (_version != _fishSet._version)
				CollectionThrowHelper.ThrowInvalidOperationException_InvalidOperation_EnumFailedVersion();

			_index = 0u;
			_current = default;
		}
	}
}