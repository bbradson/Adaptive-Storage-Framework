// Copyright (c) 2024 bradson
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using System.Collections;
using AdaptiveStorage.Fishery.FunctionPointers;
using AdaptiveStorage.Fishery.Pools;
using AdaptiveStorage.Fishery.Utility.Diagnostics;

namespace AdaptiveStorage.Fishery.Collections;

public partial class IntFishTable<TValue>
{
	[PublicAPI]
	public readonly record struct ValueCollection(IntFishTable<TValue> Parent) : ICollection<TValue>, ICollection
	{
		public IEnumerator<TValue> GetEnumerator()
		{
			foreach (var entry in Parent)
				yield return entry.Value;
		}

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

		public bool Contains(TValue item) => Parent.ContainsValue(item);

		public void CopyTo(TValue[] array, int arrayIndex)
		{
			foreach (var entry in Parent)
				array[arrayIndex++] = entry.Value;
		}

		public void CopyTo(Array array, int index)
		{
			if (array is TValue[] typedArray)
			{
				CopyTo(typedArray, index);
			}
			else
			{
				foreach (var entry in Parent)
					array.SetValue(entry.Value, index++);
			}
		}

		public PooledList<TValue> ToPooledList()
		{
			var result = new PooledList<TValue>(Count);
			var length = Parent._buckets.Length;
			for (var i = 0; i < length; i++)
			{
				if (!Parent.IsBucketEmpty(i))
					result.Add(Parent._buckets[i].Value!);
			}

			return result;
		}

		public unsafe bool Remove(TValue item)
		{
			var buckets = Parent._buckets;
			var tails = Parent._tails;
			var equalityComparer = Equals<TValue>.Default;
			var length = buckets.Length;
			
			for (var i = 0; i < length; i++)
			{
				if (Parent.IsBucketEmpty(i, tails, buckets))
					continue;

				var entry = buckets[i];

				if (!equalityComparer(entry.Value!, item))
					continue;

				if (Parent.Remove(entry.Key))
					return true;
			}
			
			return false;
		}

		public int RemoveWhere(Predicate<TValue> predicate)
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

				if (!predicate(entry.Value!))
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

		void ICollection<TValue>.Add(TValue item) => throw new NotSupportedException();

		void ICollection<TValue>.Clear() => throw new NotSupportedException();

		bool ICollection.IsSynchronized => false;

		object ICollection.SyncRoot => throw new NotSupportedException();
	}
}