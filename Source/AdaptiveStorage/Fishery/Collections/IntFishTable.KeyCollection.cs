// Copyright (c) 2024 bradson
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using System.Collections;
using AdaptiveStorage.Fishery.Pools;
using AdaptiveStorage.Fishery.Utility.Diagnostics;

namespace AdaptiveStorage.Fishery.Collections;

public partial class IntFishTable<TValue>
{
	[PublicAPI]
	public readonly record struct KeyCollection(IntFishTable<TValue> Parent) : ICollection<int>, ICollection
	{
		public IEnumerator<int> GetEnumerator()
		{
			foreach (var entry in Parent)
				yield return entry.Key;
		}

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

		public bool Contains(int item) => Parent.ContainsKey(item);

		public void CopyTo(int[] array, int arrayIndex)
		{
			foreach (var entry in Parent)
				array[arrayIndex++] = entry.Key;
		}

		public void CopyTo(Array array, int index)
		{
			if (array is int[] typedArray)
			{
				CopyTo(typedArray, index);
			}
			else
			{
				foreach (var entry in Parent)
					array.SetValue(entry.Key, index++);
			}
		}

		public PooledList<int> ToPooledList()
		{
			var result = new PooledList<int>(Count);
			var length = Parent._buckets.Length;
			for (var i = 0; i < length; i++)
			{
				if (!Parent.IsBucketEmpty(i))
					result.Add(Parent._buckets[i].Key!);
			}

			return result;
		}

		public bool Remove(int item) => Parent.Remove(item);

		public int RemoveWhere(Predicate<int> predicate)
		{
			Guard.IsNotNull(predicate);
			
			var removedCount = 0;
			var buckets = Parent._buckets;
			var tails = Parent._tails;
			var length = buckets.Length;
			
			for (var i = 0; i < length; i++)
			{
				if (Parent.IsBucketEmpty(i, tails, buckets))
					continue;

				var entry = buckets[i];

				if (!predicate(entry.Key))
					continue;

				if (!Parent.Remove(entry.Key))
					continue;
				
				removedCount++;
				i--; // extra check in case of emplacement after removal
			}
		
			return removedCount;
		}

		public int Count => Parent.Count;

		public bool IsReadOnly => true;

		void ICollection<int>.Add(int item) => throw new NotSupportedException();

		void ICollection<int>.Clear() => throw new NotSupportedException();

		bool ICollection.IsSynchronized => false;

		object ICollection.SyncRoot => throw new NotSupportedException();
	}
}