// Copyright (c) 2024 bradson
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.
//
// Loosely based on https://github.com/matthewcrews/FastDictionaryTest/tree/main
// which itself is based on
// https://probablydance.com/2018/05/28/a-new-fast-hash-table-in-response-to-googles-new-fast-hash-table/

using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using AdaptiveStorage.Fishery.Collections.Internal;
using AdaptiveStorage.Fishery.Pools;
using AdaptiveStorage.Fishery.Utility.Diagnostics;

namespace AdaptiveStorage.Fishery.Collections;

#pragma warning disable CS8766, CS8767
[PublicAPI]
[StructLayout(LayoutKind.Sequential)]
public partial class IntFishSet : ISet<int>, IReadOnlyCollection<int>, ICollection
{
	private const int FIBONACCI_HASH = -1640531527; // unchecked((int)(uint)Math.Round(uint.MaxValue / FishMath.PHI));

	internal int[] _buckets;

	private int
		_bucketBitShift,
		_count;

	internal Tails _tails;

	private int
		_wrapAroundMask,
		_version;

	private float _maxLoadFactor = 0.5f;

	public int Version
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => _version;
		set
		{
			Guard.IsGreaterThan(value, _version);
			_version = value;
		}
	}

	public int Count
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => _count;
	}

	object ICollection.SyncRoot => this;

	bool ICollection.IsSynchronized => false;

	public bool IsReadOnly => false;

	/// <summary>
	/// Empty FishSets have their entries set to a default value, causing fishSet.Contains(DefaultValue) to return
	/// true. This property can be used for a check against this default, similar to a null. Unless set to a different
	/// default through the FishSet constructor, for most reference types, the returned value is a static invalid
	/// reference. Reference types implementing IEquatable or overriding Equals have null as default instead. Primitives
	/// have all their bytes set to sbyte.MaxValue. Strings have 8 characters set to sbyte.MaxValue on both their bytes.
	/// Structs have their contents set according to previous rules.
	/// </summary>
	// ReSharper disable once InconsistentNaming
	public const int DefaultValue = 0x7F7F7F7F;

	public uint CollisionCount => (uint)GetTailingEntriesCount();

	public event Action<int>?
		EntryAdded,
		EntryRemoved;

	public float MaxLoadFactor
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => _maxLoadFactor;
		set
		{
			Guard.IsInRange(value, 0f, 1f);
			_maxLoadFactor = value;
		}
	}

	private bool _hasLoggedForExcessiveHashCollisions;

	private static int AssertKeyType(object key)
	{
		if (key is int tKey)
			return tKey;
		else if (key != null!)
			CollectionThrowHelper.ThrowWrongKeyTypeArgumentException<int>(key);

		return default!;
	}

	public bool this[int key]
	{
		[CollectionAccess(CollectionAccessType.Read)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Contains(key);

		[CollectionAccess(CollectionAccessType.UpdatedContent)]
		set
		{
			if (value)
				InsertEntry(key, ReplaceBehaviour.Return);
			else
				Remove(key);
		}
	}

	public IntFishSet() : this(0)
	{
	}

	public IntFishSet(int minimumCapacity) => Initialize(minimumCapacity);

	[MemberNotNull(nameof(_buckets))]
	[MemberNotNull(nameof(_tails))]
	private void Initialize(int minimumCapacity = 0)
	{
		minimumCapacity = minimumCapacity <= 4 ? 4 : Mathf.NextPowerOfTwo(minimumCapacity);
		// Mathf.NextPowerOfTwo(minimumCapacity);

		_buckets = new int[minimumCapacity];
		_buckets.Fill(DefaultValue);
		_tails = new((uint)minimumCapacity);
		_bucketBitShift = 32 - FishMath.TrailingZeroCount((uint)_buckets.Length);
		_wrapAroundMask = _buckets.Length - 1;
	}

	public IntFishSet(IEnumerable<int> entries) : this(0)
	{
		foreach (var entry in entries)
			InsertEntry(entry, ReplaceBehaviour.Return);
	}

	private int TryGetTailIndex(int entryIndex)
	{
		var tail = _tails[entryIndex];
		return Tails.IsSoloOrEmpty(tail) ? -1 : Tails.GetTailIndex(entryIndex, tail, _wrapAroundMask);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private bool IsTail(int bucketIndex) => bucketIndex != GetBucketIndexForKeyAt(bucketIndex);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private bool IsTail(int entry, int bucketIndex) => bucketIndex != GetBucketIndex(entry);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private bool HasTail(int entryIndex) => !_tails.IsSoloOrEmpty(entryIndex);

	private void SetEntryWithoutTail(int bucketIndex, int entry)
	{
		_buckets[bucketIndex] = entry;
		_tails.SetSolo(bucketIndex);
	}

	private void SetBucketEmpty(int index)
	{
		var buckets = _buckets;
		if ((uint)buckets.Length <= (uint)index)
			CollectionThrowHelper.ThrowInvalidOperationException_ConcurrentOperationsNotSupported();

		buckets.UnsafeStore(index, DefaultValue);
		_tails.SetEmpty(index);
	}

	private void SetEntryAsTail(int bucketIndex, int entry, int parentIndex, uint offset)
	{
		SetEntryWithoutTail(bucketIndex, entry);
		_tails[parentIndex] = offset;
	}

	private void SetParentTail(int entryIndex, uint newTail) => _tails[GetParentBucketIndex(entryIndex)] = newTail;

	private bool InsertEntry(int entry, ReplaceBehaviour replaceBehaviour, bool shifting = false)
	{
		var addedNewEntry = InsertEntryInternal(entry, replaceBehaviour);

		if (shifting)
			return addedNewEntry;

		_version++;

		if (!addedNewEntry)
			return false;

		OnEntryAdded(entry);
		return true;
	}

	private void OnEntryAdded(int entry)
	{
		// _version++; handled separately
		_count++;
		EntryAdded?.Invoke(entry);
	}

	/// <summary>
	/// Returns true when adding a new entry, false for replacing an existing entry. AllowReplace: false causes throwing
	/// instead. Does not adjust Count, Version or invoke EntryAdded.
	/// </summary>
	private bool InsertEntryInternal(int entry, ReplaceBehaviour replaceBehaviour)
	{
	StartOfMethod:
		var bucketIndex = GetBucketIndex(entry);

		ref var bucket = ref _buckets[bucketIndex];
		if (IsBucketEmpty(bucketIndex))
		{
			SetEntryWithoutTail(bucketIndex, entry);
			return true;
		}

		if (TryReplaceValueOfMatchingKey(entry, replaceBehaviour, bucketIndex))
			return false;

		if (CheckResize())
			goto StartOfMethod;

		if (IsTail(bucket, bucketIndex))
		{
			var previousEntry = bucket;
			using var tailingEntries = TryGetAndClearTailingEntries(bucketIndex);

			SetParentTail(bucketIndex, Tails.SOLO);
			SetEntryWithoutTail(bucketIndex, entry);
			InsertEntry(previousEntry, ReplaceBehaviour.ThrowConcurrent, true);

			if (tailingEntries != default)
				InsertEntries(tailingEntries, ReplaceBehaviour.ThrowConcurrent, true);

			return true;
		}
		else
		{
			return !TryFindTailIndexAndReplace(entry, ref bucketIndex, replaceBehaviour)
				&& InsertAsTail(entry, bucketIndex);
		}
	}

	private void InsertEntries(List<int> tailingEntries, ReplaceBehaviour replaceBehaviour, bool shifting = false)
	{
		for (var i = 0; i < tailingEntries.Count; i++)
			InsertEntry(tailingEntries[i], replaceBehaviour, shifting);
	}

	private int GetTailCount(int index)
	{
		var tailCount = 0;

		while (TryContinueWithTail(ref index))
			tailCount++;

		return tailCount;
	}

	private bool IsBucketEmpty(int bucketIndex) => IsBucketEmpty(bucketIndex, _tails);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool IsBucketEmpty(int bucketIndex, Tails tails) => tails.IsEmpty(bucketIndex);

	private bool TryFindTailIndexAndReplace(int entry, ref int bucketIndex, ReplaceBehaviour replaceBehaviour)
	{
		while (true)
		{
			if (!TryContinueWithTail(ref bucketIndex))
				return false;

			if (TryReplaceValueOfMatchingKey(entry, replaceBehaviour, bucketIndex))
				return true;
		}
	}

	private bool TryReplaceValueOfMatchingKey(int entry, ReplaceBehaviour replaceBehaviour, int bucketIndex)
	{
		ref var bucket = ref _buckets[bucketIndex];
		if (bucket != entry)
			return false;
		
		if (IsBucketEmpty(bucketIndex))
			ThrowHelper.ThrowEmptyBucketIsTailInvalidOperationException(bucketIndex, bucket);

		switch (replaceBehaviour)
		{
			case ReplaceBehaviour.ThrowDuplicate:
				return CollectionThrowHelper.ThrowAddingDuplicateWithKeyArgumentException(entry);
			case ReplaceBehaviour.ThrowConcurrent:
				return CollectionThrowHelper.ThrowInvalidOperationException_ConcurrentOperationsNotSupported<bool>();
			case ReplaceBehaviour.Replace:
				bucket = entry;
				goto default;
			case ReplaceBehaviour.Return:
			default:
				return true;
		}
	}

	private bool InsertAsTail(int entry, int bucketIndex)
	{
		var parentIndex = bucketIndex;
		var offset = 2u;

		while (true)
		{
			bucketIndex = Tails.GetTailIndex(parentIndex, offset, _wrapAroundMask);

			if (IsBucketEmpty(bucketIndex))
			{
				SetEntryAsTail(bucketIndex, entry, parentIndex, offset);
				return true;
			}

			if (IsTail(bucketIndex) && offset < _tails[GetParentBucketIndex(bucketIndex)])
				break;

			if (++offset <= 15u)
				continue;

			if (_buckets.Length > Count * 5)
				Debug.WarnForPossiblyExcessiveResizing(this, entry);

			Resize();
			InsertEntry(entry, ReplaceBehaviour.ThrowConcurrent, true);
			return true;
		}

		var previousEntry = _buckets[bucketIndex];
		using var tailingEntries = TryGetAndClearTailingEntries(bucketIndex);

		SetParentTail(bucketIndex, Tails.SOLO);
		SetEntryAsTail(bucketIndex, entry, parentIndex, offset);

		InsertEntry(previousEntry!, ReplaceBehaviour.ThrowConcurrent, true);

		if (tailingEntries != default)
			InsertEntries(tailingEntries, ReplaceBehaviour.ThrowConcurrent, true);

		return true;
	}

	private PooledList<int> TryGetAndClearTailingEntries(int bucketIndex)
	{
		if (!HasTail(bucketIndex))
			return default;

		var tailingEntries = new PooledList<int>();
		var tailIndex = _tails.GetTailIndex(bucketIndex, _wrapAroundMask);
		var buckets = _buckets;

		while (true)
		{
			if ((uint)tailIndex >= (uint)buckets.Length)
				CollectionThrowHelper.ThrowInvalidOperationException_ConcurrentOperationsNotSupported();

			tailingEntries.Add(buckets.UnsafeLoad(tailIndex)!);
			buckets.UnsafeStore(tailIndex, DefaultValue);

			if (HasTail(tailIndex))
			{
				var nextTailIndex = _tails.GetTailIndex(tailIndex, _wrapAroundMask);
				_tails.SetEmpty(tailIndex);
				tailIndex = nextTailIndex;
			}
			else
			{
				_tails.SetEmpty(tailIndex);
				break;
			}
		}

		return tailingEntries;
	}

	private int GetTailingEntriesCount()
	{
		var count = 0;
		for (var i = _buckets.Length; i-- > 0;)
		{
			if (!IsBucketEmpty(i) && IsTail(i))
				count++;
		}

		return count;
	}

	private int[] GetLongestTail()
	{
		var buckets = _buckets;
		var longestTailIndex = -1;
		var longestTailLength = -1;
		for (var i = 0; i < buckets.Length; i++)
		{
			if (IsBucketEmpty(i) || !HasTail(i) || IsTail(i))
				continue;

			var currentTailLength = GetTailCount(i);
			if (currentTailLength <= longestTailLength)
				continue;

			longestTailLength = currentTailLength;
			longestTailIndex = i;
		}

		if (longestTailIndex < 0)
			return Array.Empty<int>();

		var resultArray = new int[longestTailLength];
		for (var i = 0;; i++)
		{
			resultArray[i] = longestTailIndex;

			if (!TryContinueWithTail(ref longestTailIndex))
				break;
		}

		return resultArray;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private bool CheckResize()
	{
		if (_count <= (int)(_buckets.Length * _maxLoadFactor))
			return false;

		Resize();
		return true;
	}

	private void Resize()
	{
		var oldBuckets = _buckets;
		var oldTails = _tails;

		_tails = new((uint)_buckets.Length << 1);
		_buckets = new int[_buckets.Length << 1];
		_buckets.Fill(DefaultValue);

		_bucketBitShift = 32 - FishMath.TrailingZeroCount((uint)_buckets.Length);
		_wrapAroundMask = _buckets.Length - 1;

		for (var i = 0; i < oldBuckets.Length; i++)
		{
			if (!IsBucketEmpty(i, oldTails))
				InsertEntry(oldBuckets[i]!, ReplaceBehaviour.ThrowConcurrent, true);
		}
	}

	[CollectionAccess(CollectionAccessType.Read)]
	public bool Contains(int key)
	{
		var bucketIndex = GetBucketIndex(key);

		while (true)
		{
			if (key == _buckets[bucketIndex])
				return true;

			if (!TryContinueWithTail(ref bucketIndex))
				return false;
		}
	}

#region GetBucketIndexMethods

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private int GetBucketIndex(int entry) => (entry * FIBONACCI_HASH) >>> _bucketBitShift;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private int GetBucketIndexForKeyAt(int index) => GetBucketIndex(_buckets[index]);

	private int GetParentBucketIndex(int childBucketIndex)
	{
		var ancestorIndex = GetBucketIndexForKeyAt(childBucketIndex);
		var nextIndex = ancestorIndex;
		var i = 0;

		while (true)
		{
			if (i++ > 32 && !_hasLoggedForExcessiveHashCollisions)
				Debug.WarnForPossiblyExcessiveHashCollisions(this);

			if (!TryContinueWithTail(ref nextIndex))
				ThrowHelper.ThrowFailedToFindParentInvalidOperationException(this, childBucketIndex);

			if (nextIndex == childBucketIndex)
				break;

			ancestorIndex = nextIndex;
		}

		return ancestorIndex;
	}
#endregion

	/// <summary>
	/// Returns false for solo or empty, otherwise sets the index to the tail index and returns true
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private bool TryContinueWithTail(ref int entryIndex) => _tails.TryContinueWithTail(ref entryIndex, _wrapAroundMask);

	[CollectionAccess(CollectionAccessType.UpdatedContent)]
	public bool Add(int item)
	{
		Guard.IsNotNull(item);
		return InsertEntry(item, ReplaceBehaviour.Return);
	}

	[CollectionAccess(CollectionAccessType.UpdatedContent)]
	public void AddRange(in ReadOnlySpan<int> items)
	{
		if (items.IsEmpty)
			return;

		nint count = items.Length;
		
		count *= sizeof(int);
		ref var spanReference = ref items.DangerousGetPinnableReference();
		
		for (nint i = 0; i < count; i += sizeof(int))
			Add(Unsafe.AddByteOffset(ref spanReference, i));
	}

	public void AddRange(IEnumerable<int> items)
	{
		Guard.IsNotNull(items);
		
		foreach (var item in items)
			Add(item);
	}

	void ISet<int>.UnionWith(IEnumerable<int> other) => AddRange(other);

	public void IntersectWith(IEnumerable<int> other)
	{
		if (other is ICollection<int> set)
		{
			foreach (var item in this)
			{
				if (!set.Contains(item))
					Remove(item);
			}
		}
		else
		{
			foreach (var item in this)
			{
				// ReSharper disable once PossibleMultipleEnumeration
				if (!System.Linq.Enumerable.Contains(other, item))
					Remove(item);
			}
		}
	}

	void ISet<int>.ExceptWith(IEnumerable<int> other) => RemoveRange(other);

	public void SymmetricExceptWith(IEnumerable<int> other)
	{
		foreach (var item in other)
		{
			if (!Add(item))
				Remove(item);
		}
	}

	public bool IsSubsetOf(IEnumerable<int> other) => Count <= ContainedItemCountOf(other);

	public bool IsProperSubsetOf(IEnumerable<int> other) => Count < ContainedItemCountOf(other);

	public bool IsSupersetOf(IEnumerable<int> other) => IsSupersetOf(other, out _);

	public bool IsProperSupersetOf(IEnumerable<int> other)
		=> IsSupersetOf(other, out var otherCount) && Count > otherCount;

	private bool IsSupersetOf(IEnumerable<int> other, out int otherCount)
	{
		otherCount = 0;
		foreach (var item in other)
		{
			if (!Contains(item))
				return false;

			otherCount++;
		}

		return true;
	}

	private int ContainedItemCountOf(IEnumerable<int> other)
	{
		var count = 0;
		foreach (var item in other)
		{
			if (Contains(item))
				count++;
		}

		return count;
	}

	public bool Overlaps(IEnumerable<int> other)
	{
		foreach (var item in other)
		{
			if (Contains(item))
				return true;
		}

		return false;
	}

	public bool SetEquals(IEnumerable<int> other) => IsSupersetOf(other, out var otherCount) && Count == otherCount;

	[CollectionAccess(CollectionAccessType.ModifyExistingContent)]
	public bool Remove(int item)
	{
		if (!RemoveInternal(item))
			return false;

		OnEntryRemoved(item);
		return true;
	}

	[CollectionAccess(CollectionAccessType.ModifyExistingContent)]
	public void RemoveRange(in ReadOnlySpan<int> items)
	{
		if (items.IsEmpty)
			return;

		ref var spanReference = ref items.DangerousGetPinnableReference();
		for (nint i = items.Length * sizeof(int); (i -= sizeof(int)) >= 0;)
			Remove(Unsafe.AddByteOffset(ref spanReference, i));
	}

	[CollectionAccess(CollectionAccessType.ModifyExistingContent)]
	public void RemoveRange(IEnumerable<int> items)
	{
		Guard.IsNotNull(items);
		
		foreach (var key in items)
			Remove(key);
	}

	private void EmplaceWithTailsBackward(int entryIndex)
	{
		if (IsBucketEmpty(entryIndex))
			AdaptiveStorage.Fishery.Utility.Diagnostics.ThrowHelper.ThrowInvalidOperationException("Tried to emplace entry bucket");

		while (true)
		{
			if (!IsTail(entryIndex))
				return;
			
			var tailIndex = TryGetTailIndex(entryIndex);
			var entry = _buckets[entryIndex];

			SetBucketEmpty(entryIndex);
			InsertEntry(entry!, ReplaceBehaviour.ThrowConcurrent, true);

			if (tailIndex < 0)
				return;

			entryIndex = tailIndex;
		}
	}

	private bool RemoveInternal(int key)
	{
		var bucketIndex = GetBucketIndex(key);
		var isTail = false;

		while (true)
		{
			if (IsBucketEmpty(bucketIndex))
				return false;

			if (key == _buckets[bucketIndex])
			{
				var tailIndex = TryGetTailIndex(bucketIndex);

				if (isTail)
					SetParentTail(bucketIndex, Tails.SOLO);

				SetBucketEmpty(bucketIndex);

				if (tailIndex >= 0)
					EmplaceWithTailsBackward(tailIndex);

				return true;
			}

			if (!TryContinueWithTail(ref bucketIndex))
				return false;

			isTail = true;
		}
	}

	void ICollection<int>.Add(int item) => Add(item);

	[CollectionAccess(CollectionAccessType.ModifyExistingContent)]
	public void Clear()
	{
		// ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
		if (_buckets is null)
			Initialize();

		_buckets.Fill(DefaultValue);
		_tails.Reset();

		_count = 0;
		_version++;
	}

	[CollectionAccess(CollectionAccessType.Read)]
	public void CopyTo(int[] array, int arrayIndex)
	{
		var length = _buckets.Length;
		for (var i = 0; i < length; i++)
		{
			if (IsBucketEmpty(i))
				continue;

			array[arrayIndex] = _buckets[i]!;
			arrayIndex++;
		}
	}

	[CollectionAccess(CollectionAccessType.Read)]
	public PooledList<int> ToPooledList()
	{
		var result = new PooledList<int>(Count);
		var length = _buckets.Length;
		for (var i = 0; i < length; i++)
		{
			if (!IsBucketEmpty(i))
				result.Add(_buckets[i]!);
		}

		return result;
	}

	private void OnEntryRemoved(int entry)
	{
		_version++;
		_count--;
		EntryRemoved?.Invoke(entry);
	}

#region InterfaceMethods
	[CollectionAccess(CollectionAccessType.Read)]
	public Enumerator GetEnumerator() => new(this, Enumerator.KEY_VALUE_PAIR);

	IEnumerator<int> IEnumerable<int>.GetEnumerator() => new Enumerator(this, Enumerator.KEY_VALUE_PAIR);

	IEnumerator IEnumerable.GetEnumerator() => new Enumerator(this, Enumerator.KEY_VALUE_PAIR);

	void ICollection.CopyTo(Array array, int index)
	{
		if (array is int[] typedArray)
		{
			CopyTo(typedArray, index);
			return;
		}

		var length = _buckets.Length;
		for (var i = 0; i < length; i++)
		{
			if (IsBucketEmpty(i))
				continue;

			((IList)array)[index] = _buckets[i];
			index++;
		}
	}
#endregion
}
#pragma warning restore CS8766, CS8767