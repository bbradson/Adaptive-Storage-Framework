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
using AdaptiveStorage.Fishery.FunctionPointers;
using AdaptiveStorage.Fishery.Pools;
using AdaptiveStorage.Fishery.Utility.Diagnostics;

namespace AdaptiveStorage.Fishery.Collections;

#pragma warning disable CS8766, CS8767
[PublicAPI]
[StructLayout(LayoutKind.Sequential)]
public partial class IntFishTable<TValue> : IDictionary<int, TValue>, IDictionary, IReadOnlyDictionary<int, TValue>
{
	private const int FIBONACCI_HASH = -1640531527; // unchecked((int)(uint)Math.Round(uint.MaxValue / FishMath.PHI));

	internal Entry[] _buckets;

	private int
		_bucketBitShift,
		_count;

	internal Tails _tails;

	private int
		_wrapAroundMask,
		_version;

	private float _maxLoadFactor = 0.5f;

	private Entry _defaultEntry = Entry.Default;

	public KeyCollection Keys => new(this);

	public ValueCollection Values => new(this);

	public int Version
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => _version;
		set => _version = value;
	}

	public int Count
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => _count;
	}

	public bool IsReadOnly => false;

	/// <summary>
	/// Empty FishTables have their entries set to a default value, causing fishTable.ContainsKey(DefaultKey) to return
	/// true. This property can be used for a check against this default, similar to a null. Unless set to a different
	/// default through the FishTable constructor, for most reference types, the returned value is a static invalid
	/// reference. Reference types implementing IEquatable or overriding Equals have null as default instead. Primitives
	/// have all their bytes set to sbyte.MaxValue. Strings have 8 characters set to sbyte.MaxValue on both their bytes.
	/// Structs have their contents set according to previous rules.
	/// </summary>
	public int DefaultKey => _defaultEntry.Key;

	public uint CollisionCount => (uint)GetTailingEntriesCount();

	public event Action<KeyValuePair<int, TValue>>?
		EntryAdded,
		EntryRemoved;

	public Func<int, TValue> ValueInitializer { get; set; } = static _ => Reflection.New<TValue>();

	public delegate void EntryEventHandler(int key, ref TValue value);

	public event Action<int>? KeyAdded
	{
		add => AddEvent<int, KeyEventHandler>(ref EntryAdded, value);
		remove => RemoveEvent(ref EntryAdded, value);
	}

	public event Action<int>? KeyRemoved
	{
		add => AddEvent<int, KeyEventHandler>(ref EntryRemoved, value);
		remove => RemoveEvent(ref EntryRemoved, value);
	}

	public event Action<TValue>? ValueAdded
	{
		add => AddEvent<TValue, ValueEventHandler>(ref EntryAdded, value);
		remove => RemoveEvent(ref EntryAdded, value);
	}

	public event Action<TValue>? ValueRemoved
	{
		add => AddEvent<TValue, ValueEventHandler>(ref EntryRemoved, value);
		remove => RemoveEvent(ref EntryRemoved, value);
	}

	private bool _hasLoggedForExcessiveHashCollisions;

	private static void AddEvent<T, THandler>(ref Action<KeyValuePair<int, TValue>>? @event, Action<T>? value)
		where THandler : IEventHandler<T>
	{
		if (value != null)
			@event += Reflection.New<THandler, Action<T>>(value).Invoke;
	}

	private static void RemoveEvent<T>(ref Action<KeyValuePair<int, TValue>>? @event, Action<T>? value)
	{
		if (@event is null)
			return;

		var delegates = @event.GetInvocationList();
		for (var i = delegates.Length; i-- > 0;)
		{
			if (delegates[i].Target is not IEventHandler<T> handler || handler.Action != value)
				continue;

			@event -= (Action<KeyValuePair<int, TValue>>)delegates[i];
			return;
		}
	}

	private interface IEventHandler<in T>
	{
		public Action<T> Action { get; }
		public void Invoke(KeyValuePair<int, TValue> pair);
	}

	private sealed record KeyEventHandler(Action<int> Action) : IEventHandler<int>
	{
		public void Invoke(KeyValuePair<int, TValue> pair) => Action(pair.Key);
	}

	private sealed record ValueEventHandler(Action<TValue> Action) : IEventHandler<TValue>
	{
		public void Invoke(KeyValuePair<int, TValue> pair) => Action(pair.Value);
	}

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

	ICollection<int> IDictionary<int, TValue>.Keys => Keys;

	ICollection<TValue> IDictionary<int, TValue>.Values => Values;

	ICollection IDictionary.Keys => Keys;

	ICollection IDictionary.Values => Values;

	bool IDictionary.IsFixedSize => false;

	object ICollection.SyncRoot => this; // matching System.Collections.Generic.Dictionary

	bool ICollection.IsSynchronized => false;

	IEnumerable<int> IReadOnlyDictionary<int, TValue>.Keys => Keys;

	IEnumerable<TValue> IReadOnlyDictionary<int, TValue>.Values => Values;

	object IDictionary.this[object key]
	{
		get => this[AssertKeyType(key)]!;
		set => this[AssertKeyType(key)] = AssertValueType(value);
	}

	private static int AssertKeyType(object key)
	{
		if (key is int tKey)
			return tKey;
		else if (key != null!)
			CollectionThrowHelper.ThrowWrongKeyTypeArgumentException<int>(key);

		return default!;
	}

	private static TValue AssertValueType(object? value)
	{
		if (value is TValue tValue)
			return tValue;
		else if (value != null!)
			CollectionThrowHelper.ThrowWrongValueTypeArgumentException<TValue>(value);

		return default!;
	}

	public TValue this[int key]
	{
		[CollectionAccess(CollectionAccessType.Read)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get
		{
			var bucketIndex = GetBucketIndex(key);

			while (true)
			{
				ref var bucket = ref _buckets[bucketIndex];

				if (key == bucket.Key)
					return bucket.Value!;

				ContinueWithTailOrThrow(ref bucketIndex, key);
			}
		}

		[CollectionAccess(CollectionAccessType.UpdatedContent)]
		set => InsertEntry(key, value, ReplaceBehaviour.Replace);
	}

	public IntFishTable() : this(0)
	{
	}

	public IntFishTable(int minimumCapacity) => Initialize(minimumCapacity);

	/// <summary>
	/// Initialize a new FishTable
	/// </summary>
	/// <param name="minimumCapacity">The minimum capacity of the backing array</param>
	/// <param name="defaultKey">
	/// The default key for empty entries. ContainsKey() returns true for this when empty
	/// </param>
	public IntFishTable(int minimumCapacity, int defaultKey)
	{
		_defaultEntry.Key = defaultKey;
		Initialize(minimumCapacity);
	}

	[MemberNotNull(nameof(_buckets))]
	[MemberNotNull(nameof(_tails))]
	private void Initialize(int minimumCapacity = 0)
	{
		minimumCapacity = minimumCapacity <= 4 ? 4 : Mathf.NextPowerOfTwo(minimumCapacity);

		_buckets = new Entry[minimumCapacity];
		_buckets.Fill(_defaultEntry);
		_tails = new((uint)minimumCapacity);
		_bucketBitShift = 32 - FishMath.TrailingZeroCount((uint)_buckets.Length);
		_wrapAroundMask = _buckets.Length - 1;
	}

	public IntFishTable(IEnumerable<KeyValuePair<int, TValue>> entries) : this(0) => AddRange(entries);

	private int TryGetTailIndex(int entryIndex)
	{
		var tail = _tails[entryIndex];
		return Tails.IsSoloOrEmpty(tail) ? -1 : Tails.GetTailIndex(entryIndex, tail, _wrapAroundMask);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private bool IsTail(int bucketIndex) => bucketIndex != GetBucketIndexForKeyAt(bucketIndex);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private bool IsTail(ref Entry entry, int bucketIndex) => bucketIndex != GetBucketIndex(entry.Key);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private bool HasTail(int entryIndex) => !_tails.IsSoloOrEmpty(entryIndex);

	private void SetEntryWithoutTail(int bucketIndex, in Entry entry)
	{
		_buckets[bucketIndex] = entry;
		_tails.SetSolo(bucketIndex);
	}

	private void SetBucketEmpty(int index)
	{
		var buckets = _buckets;
		if ((uint)buckets.Length <= (uint)index)
			CollectionThrowHelper.ThrowInvalidOperationException_ConcurrentOperationsNotSupported();

		buckets.UnsafeStore(index, _defaultEntry);
		_tails.SetEmpty(index);
	}

	private void SetEntryAsTail(int bucketIndex, in Entry entry, int parentIndex, uint offset)
	{
		SetEntryWithoutTail(bucketIndex, entry);
		_tails[parentIndex] = offset;
	}

	private void SetParentTail(int entryIndex, uint newTail) => _tails[GetParentBucketIndex(entryIndex)] = newTail;

	/// <summary>
	/// Returns true when adding a new entry, false for replacing an existing entry or failing to do so. Does not
	/// adjust Count, Version or invoke EntryAdded.
	/// </summary>
	private bool InsertEntry(int key, TValue? value, ReplaceBehaviour replaceBehaviour,
		bool shifting = false)
		=> InsertEntry(new(key, value), replaceBehaviour, shifting);

	/// <summary>
	/// Returns true when adding a new entry, false for replacing an existing entry or failing to do so. Does not
	/// adjust Count, Version or invoke EntryAdded.
	/// </summary>
	private bool InsertEntry(in Entry entry, ReplaceBehaviour replaceBehaviour, bool shifting = false)
	{
		var addedNewEntry = InsertEntryInternal(entry, replaceBehaviour);

		if (shifting)
			return addedNewEntry;

		_version++;

		if (!addedNewEntry)
			return false;

		OnEntryAdded(entry.AsKeyValuePair());
		return true;
	}

	private void OnEntryAdded(in KeyValuePair<int, TValue> entry)
	{
		// _version++; handled separately
		_count++;
		EntryAdded?.Invoke(entry);
	}

	/// <summary>
	/// Returns true when adding a new entry, false for replacing an existing entry or failing to do so. Does not
	/// adjust Count, Version or invoke EntryAdded.
	/// </summary>
	private bool InsertEntryInternal(in Entry entry, ReplaceBehaviour replaceBehaviour)
	{
	StartOfMethod:
		var bucketIndex = GetBucketIndex(entry.Key);

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

		if (IsTail(ref bucket, bucketIndex))
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

	private void InsertEntries(List<Entry> tailingEntries, ReplaceBehaviour replaceBehaviour,
		bool shifting = false)
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

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private bool IsBucketEmpty(int bucketIndex) => IsBucketEmpty(bucketIndex, _tails, _buckets);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private bool IsBucketEmpty(int bucketIndex, Tails tails, Entry[] buckets)
		=> tails.IsEmpty(bucketIndex) && VerifyAgainstNullBucket(bucketIndex, tails, buckets);

	private bool VerifyAgainstNullBucket(int bucketIndex, Tails tails, Entry[] buckets)
	{
		ref var bucket = ref buckets[bucketIndex];

		if (bucket.Key != DefaultKey)
			ThrowHelper.ThrowEmptyBucketKeyInvalidOperationException(bucketIndex, bucket);

		if (bucket.Value.Equals<TValue>(default))
			return true;

		// GetReference methods can get here when using a default key and accessing the returned ref
		tails.SetSolo(bucketIndex);
		OnEntryAdded(bucket.AsKeyValuePair()); // technically late, but it's an edge case and there's
		return false;                          // no good workaround without performance hit
	}

	private bool TryFindTailIndexAndReplace(in Entry entry, ref int bucketIndex,
		ReplaceBehaviour replaceBehaviour)
	{
		while (true)
		{
			if (!TryContinueWithTail(ref bucketIndex))
				return false;

			if (TryReplaceValueOfMatchingKey(entry, replaceBehaviour, bucketIndex))
				return true;
		}
	}

	private bool TryReplaceValueOfMatchingKey(in Entry entry, ReplaceBehaviour replaceBehaviour,
		int bucketIndex)
	{
		ref var bucket = ref _buckets[bucketIndex];
		if (bucket.Key != entry.Key)
			return false;
		
		if (IsBucketEmpty(bucketIndex))
			ThrowHelper.ThrowEmptyBucketIsTailInvalidOperationException(bucketIndex, bucket);

		switch (replaceBehaviour)
		{
			case ReplaceBehaviour.ThrowDuplicate:
				return CollectionThrowHelper.ThrowAddingDuplicateWithKeyArgumentException(entry.Key);
			case ReplaceBehaviour.ThrowConcurrent:
				return CollectionThrowHelper.ThrowInvalidOperationException_ConcurrentOperationsNotSupported<bool>();
			case ReplaceBehaviour.Replace:
				bucket.Value = entry.Value;
				goto default;
			case ReplaceBehaviour.Return:
			default:
				return true;
		}
	}

	private bool InsertAsTail(in Entry entry, int bucketIndex)
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
		InsertEntry(previousEntry, ReplaceBehaviour.ThrowConcurrent, true);

		if (tailingEntries != default)
			InsertEntries(tailingEntries, ReplaceBehaviour.ThrowConcurrent, true);

		return true;
	}

	private PooledList<Entry> TryGetAndClearTailingEntries(int bucketIndex)
	{
		if (!HasTail(bucketIndex))
			return default;

		var tailingEntries = new PooledList<Entry>();
		var tailIndex = _tails.GetTailIndex(bucketIndex, _wrapAroundMask);
		var buckets = _buckets;

		while (true)
		{
			if ((uint)tailIndex >= (uint)buckets.Length)
				CollectionThrowHelper.ThrowInvalidOperationException_ConcurrentOperationsNotSupported();

			tailingEntries.Add(buckets.UnsafeLoad(tailIndex));
			buckets.UnsafeStore(tailIndex, _defaultEntry);

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

	// probably fails due to varying jump distances
	/*private void EmplaceWithTailsForward(in Entry insertedEntry, int entryIndex)
	{
		var replacementEntry = insertedEntry;

		while (true)
		{
			if (entryIndex < 0)
			{
				InsertEntry(replacementEntry, false, true);
				return;
			}

			var tailIndex = HasTail(entryIndex) ? GetTailIndex(entryIndex) : -1;
			var previousEntry = _buckets[entryIndex];

			SetBucketEmpty(entryIndex);
			InsertEntry(replacementEntry, false, true);

			replacementEntry = previousEntry;
			entryIndex = tailIndex;
		}
	}*/

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

	public void EnsureCapacity(int minimumSize)
	{
		if (_buckets.Length >= minimumSize)
			return;

		Resize(Mathf.NextPowerOfTwo(minimumSize));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private bool CheckResize()
	{
		if (_count <= (int)(_buckets.Length * _maxLoadFactor))
			return false;

		Resize();
		return true;
	}

	private void Resize() => Resize(_buckets.Length << 1);

	private void Resize(int newSize)
	{
		var oldBuckets = _buckets;
		var oldTails = _tails;

		_tails = new((uint)newSize);
		_buckets = new Entry[newSize];
		_buckets.Fill(_defaultEntry);

		_bucketBitShift = 32 - FishMath.TrailingZeroCount((uint)newSize);
		_wrapAroundMask = _buckets.Length - 1;

		for (var i = 0; i < oldBuckets.Length; i++)
		{
			if (!IsBucketEmpty(i, oldTails, oldBuckets))
				InsertEntry(oldBuckets[i], ReplaceBehaviour.ThrowConcurrent, true);
		}
	}

#region GetterMethods
	[CollectionAccess(CollectionAccessType.Read)]
	public bool TryGetValue(int key, [MaybeNullWhen(false)] out TValue value)
	{
		var bucketIndex = GetBucketIndex(key);

		while (true)
		{
			ref var bucket = ref _buckets[bucketIndex];

			if (key == bucket.Key)
			{
				value = bucket.Value!;
				return true;
			}

			if (TryContinueWithTail(ref bucketIndex))
				continue;

			value = default;
			return false;
		}
	}

	[CollectionAccess(CollectionAccessType.Read)]
	public TValue? TryGetValue(int key)
	{
		var bucketIndex = GetBucketIndex(key);

		while (true)
		{
			ref var bucket = ref _buckets[bucketIndex];

			if (key == bucket.Key)
				return bucket.Value;

			if (!TryContinueWithTail(ref bucketIndex))
				return default;
		}
	}

	[CollectionAccess(CollectionAccessType.Read | CollectionAccessType.UpdatedContent)]
	public bool TryGetOrAddValue(int key, out TValue value)
	{
		var result = true;

	StartOfLookup:
		var bucketIndex = GetBucketIndex(key);

		while (true)
		{
			ref var bucket = ref _buckets[bucketIndex];

			if (key == bucket.Key)
			{
				value = bucket.Value!;
				return result;
			}

			if (ContinueWithTailOrAddNew(ref bucketIndex, key))
				continue;

			result = false;
			goto StartOfLookup;
		}
	}

	[CollectionAccess(CollectionAccessType.Read | CollectionAccessType.UpdatedContent)]
	public TValue GetOrAdd(int key)
	{
	StartOfMethod:
		var bucketIndex = GetBucketIndex(key);

		while (true)
		{
			ref var bucket = ref _buckets[bucketIndex];

			if (key == bucket.Key)
				return bucket.Value!;

			if (!ContinueWithTailOrAddNew(ref bucketIndex, key))
				goto StartOfMethod;
		}
	}

	/// <summary>
	/// Returns a reference to a value field of an existing entry if one exists, otherwise to a new entry. This
	/// reference is only valid until the next time the collection gets modified. An invalid reference gives undefined
	/// behaviour and does not throw.
	/// </summary>
	[CollectionAccess(CollectionAccessType.Read | CollectionAccessType.UpdatedContent)]
	public ref TValue GetOrAddReference(int key)
	{
	StartOfMethod:
		var bucketIndex = GetBucketIndex(key);

		while (true)
		{
			ref var bucket = ref _buckets[bucketIndex];

			if (key == bucket.Key)
				return ref bucket.Value!;

			if (!ContinueWithTailOrAddNew(ref bucketIndex, key))
				goto StartOfMethod;
		}
	}

	/// <summary>
	/// Returns a reference to a value field of an existing entry if one exists, otherwise throws a
	/// KeyNotFoundException. This reference is only valid until the next time the collection gets modified. An invalid
	/// reference gives undefined behaviour and does not throw.
	/// </summary>
	[CollectionAccess(CollectionAccessType.Read | CollectionAccessType.ModifyExistingContent)]
	public ref TValue GetReference(int key)
	{
		var bucketIndex = GetBucketIndex(key);

		while (true)
		{
			ref var bucket = ref _buckets[bucketIndex];

			if (key == bucket.Key)
				return ref bucket.Value!;

			ContinueWithTailOrThrow(ref bucketIndex, key);
		}
	}
#endregion

	[CollectionAccess(CollectionAccessType.Read)]
	public bool ContainsKey(int key)
	{
		var bucketIndex = GetBucketIndex(key);

		while (true)
		{
			if (key == _buckets[bucketIndex].Key)
				return true;

			if (!TryContinueWithTail(ref bucketIndex))
				return false;
		}
	}

	[CollectionAccess(CollectionAccessType.Read)]
	public unsafe bool ContainsValue(TValue value)
	{
		var equalityComparer = Equals<TValue>.Default;

		var length = _buckets.Length;
		for (var i = 0; i < length; i++)
		{
			if (!IsBucketEmpty(i) && equalityComparer(_buckets[i].Value!, value))
				return true;
		}

		return false;
	}

#region GetBucketIndexMethods
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private int GetBucketIndex(int key) => (key * FIBONACCI_HASH) >>> _bucketBitShift;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private int GetBucketIndexForKeyAt(int index) => GetBucketIndex(_buckets[index].Key);

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

#region ContinueWithTailMethods
	/// <summary>
	/// Tries setting the index to the tail's index and returns true on success. If none exists, inserts a new entry
	/// and returns false
	/// </summary>
	[MethodImpl(MethodImplOptions.NoInlining)]
	private bool ContinueWithTailOrAddNew(ref int entryIndex, int key)
	{
		if (TryContinueWithTail(ref entryIndex))
			return true;

		AddInitialized(key);
		return false;
	}

	/// <summary>
	/// Tries setting the index to the tail's index and returns true on success. If none exists, inserts a new entry
	/// and returns false
	/// </summary>
	[MethodImpl(MethodImplOptions.NoInlining)]
	private bool ContinueWithTailOrAddNew(ref int entryIndex, ref readonly int key)
	{
		if (TryContinueWithTail(ref entryIndex))
			return true;

		AddInitialized(key);
		return false;
	}

	/// <summary>
	/// Tries setting the index to the tail's index. If none exists, throws instead
	/// </summary>
	private void ContinueWithTailOrThrow(ref int entryIndex, int key)
	{
		if (!TryContinueWithTail(ref entryIndex))
			CollectionThrowHelper.ThrowKeyNotFoundException(key);
	}

	/// <summary>
	/// Tries setting the index to the tail's index. If none exists, throws instead
	/// </summary>
	private void ContinueWithTailOrThrow(ref int entryIndex, ref int key)
	{
		if (!TryContinueWithTail(ref entryIndex))
			CollectionThrowHelper.ThrowKeyNotFoundException(key);
	}

	/// <summary>
	/// Returns false for solo or empty, otherwise sets the index to the tail index and returns true
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private bool TryContinueWithTail(ref int entryIndex) => _tails.TryContinueWithTail(ref entryIndex, _wrapAroundMask);
#endregion

	[CollectionAccess(CollectionAccessType.UpdatedContent)]
	[MethodImpl(MethodImplOptions.NoInlining)]
	public void AddInitialized(int key) => Add(key, ValueInitializer(key));

	[MethodImpl(MethodImplOptions.NoInlining)]
	private void AddInitializedInternal(int key)
		=> InsertEntry(key, ValueInitializer(key), ReplaceBehaviour.ThrowConcurrent);

	[CollectionAccess(CollectionAccessType.UpdatedContent)]
	public void Add(int key, TValue value) => InsertEntry(key, value, ReplaceBehaviour.ThrowDuplicate);

	[CollectionAccess(CollectionAccessType.Read | CollectionAccessType.UpdatedContent)]
	public bool TryAdd(int key, TValue value) => InsertEntry(key, value, ReplaceBehaviour.Return);

	[CollectionAccess(CollectionAccessType.UpdatedContent)]
	public void EnsureInitialized(int key)
	{
		var bucketIndex = GetBucketIndex(key);
		while (true)
		{
			if (key == _buckets[bucketIndex].Key || !ContinueWithTailOrAddNew(ref bucketIndex, key))
				return;
		}
	}

	[CollectionAccess(CollectionAccessType.ModifyExistingContent)]
	public bool Remove(int key)
	{
		if (!RemoveInternal(key, out var removedEntry))
			return false;

		OnEntryRemoved(removedEntry.AsKeyValuePair());
		return true;
	}

	[CollectionAccess(CollectionAccessType.ModifyExistingContent)]
	public bool Remove(int key, [MaybeNullWhen(false)] out TValue value)
	{
		if (!RemoveInternal(key, out var removedEntry))
		{
			value = default;
			return false;
		}

		value = removedEntry.Value!;
		OnEntryRemoved(removedEntry.AsKeyValuePair());
		return true;
	}

	[CollectionAccess(CollectionAccessType.ModifyExistingContent)]
	public int RemoveWhere(Predicate<KeyValuePair<int, TValue>> predicate)
	{
		Guard.IsNotNull(predicate);

		var removedCount = 0;

		var length = _buckets.Length;
		for (var i = 0; i < length; i++)
		{
			if (IsBucketEmpty(i))
				continue;

			var entry = _buckets[i];

			if (!predicate(entry.AsKeyValuePair()))
				continue;

			if (!Remove(entry.Key))
				continue;

			removedCount++;
			i--; // extra check in case of emplacement after removal
		}

		return removedCount;
	}

	[CollectionAccess(CollectionAccessType.ModifyExistingContent)]
	public void RemoveRange(in ReadOnlySpan<int> keys)
	{
		if (keys.IsEmpty)
			return;

		ref var spanReference = ref keys.DangerousGetPinnableReference();
		for (nint i = keys.Length * sizeof(int); (i -= sizeof(int)) >= 0;)
			Remove(Unsafe.AddByteOffset(ref spanReference, i));
	}

	[CollectionAccess(CollectionAccessType.ModifyExistingContent)]
	public void RemoveRange(IEnumerable<int> keys)
	{
		Guard.IsNotNull(keys);
		
		foreach (var key in keys)
			Remove(key);
	}

	private void EmplaceWithTailsBackward(int entryIndex)
	{
		if (IsBucketEmpty(entryIndex))
			AdaptiveStorage.Fishery.Utility.Diagnostics.ThrowHelper.ThrowInvalidOperationException("Tried to emplace empty bucket");

		while (true)
		{
			if (!IsTail(entryIndex))
				return;
			
			var tailIndex = TryGetTailIndex(entryIndex);
			var entry = _buckets[entryIndex];

			SetBucketEmpty(entryIndex);
			InsertEntry(entry, ReplaceBehaviour.ThrowConcurrent, true);

			if (tailIndex < 0)
				return;

			entryIndex = tailIndex;
		}
	}

	private bool RemoveInternal(int key, out Entry removedEntry, bool checkValue = false,
		TValue? value = default)
	{
		var bucketIndex = GetBucketIndex(key);
		var isTail = false;

		while (true)
		{
			if (IsBucketEmpty(bucketIndex))
				goto OnFailure;
			
			ref var bucket = ref _buckets[bucketIndex];
			
			if (key == bucket.Key)
			{
				if (checkValue && !value.Equals<TValue>(bucket.Value))
					goto OnFailure;

				removedEntry = bucket;

				var tailIndex = TryGetTailIndex(bucketIndex);

				if (isTail)
					SetParentTail(bucketIndex, Tails.SOLO);

				SetBucketEmpty(bucketIndex);

				if (tailIndex >= 0)
					EmplaceWithTailsBackward(tailIndex);

				return true;
			}

			if (TryContinueWithTail(ref bucketIndex))
			{
				isTail = true;
				continue;
			}

		OnFailure:
			removedEntry = default;
			return false;
		}
	}

	[CollectionAccess(CollectionAccessType.UpdatedContent)]
	public void Add(KeyValuePair<int, TValue> item) => Add(item.Key, item.Value);

	[CollectionAccess(CollectionAccessType.UpdatedContent)]
	public void AddRange(IList<KeyValuePair<int, TValue>> range)
	{
		Guard.IsNotNull(range);

		EnsureCapacity((int)(range.Count * 1.5f));
		for (var i = range.Count; i-- > 0;)
			Add(range[i]);
	}

	[CollectionAccess(CollectionAccessType.UpdatedContent)]
	public unsafe void AddRange(in ReadOnlySpan<KeyValuePair<int, TValue>> range)
	{
		if (range.IsEmpty)
			return;

		nint count = range.Length;
		EnsureCapacity((int)(count * 1.5f));
		
		count *= sizeof(KeyValuePair<int, TValue>);
		ref var spanReference = ref range.DangerousGetPinnableReference();
		
		for (nint i = 0; i < count; i += sizeof(KeyValuePair<int, TValue>))
			Add(Unsafe.AddByteOffset(ref spanReference, i));
	}

	[CollectionAccess(CollectionAccessType.UpdatedContent)]
	public void AddRange(IEnumerable<KeyValuePair<int, TValue>> range)
	{
		Guard.IsNotNull(range);

		if (range is IList<KeyValuePair<int, TValue>> iList)
			AddRange(iList);
		else
			AddRangeFromEnumerable(range);
	}

	private void AddRangeFromEnumerable(IEnumerable<KeyValuePair<int, TValue>> range)
	{
		Guard.IsNotNull(range);

		foreach (var item in range)
			Add(item);
	}

	[CollectionAccess(CollectionAccessType.UpdatedContent)]
	public void AddRange<TSource>(IList<TSource> range, Func<TSource, int> keySelector,
		Func<TSource, TValue> valueSelector)
	{
		Guard.IsNotNull(range);
		Guard.IsNotNull(keySelector);
		Guard.IsNotNull(valueSelector);

		EnsureCapacity((int)(range.Count * 1.5f));
		for (var i = range.Count; i-- > 0;)
		{
			var item = range[i];
			Add(keySelector(item), valueSelector(item));
		}
	}

	[CollectionAccess(CollectionAccessType.UpdatedContent)]
	public void AddRange<TSource>(IEnumerable<TSource> range, Func<TSource, int> keySelector,
		Func<TSource, TValue> valueSelector)
	{
		Guard.IsNotNull(range);
		Guard.IsNotNull(keySelector);
		Guard.IsNotNull(valueSelector);

		if (range is IList<TSource> iList)
			AddRange(iList, keySelector, valueSelector);
		else
			AddRangeFromEnumerable(range, keySelector, valueSelector);
	}

	private void AddRangeFromEnumerable<TSource>(IEnumerable<TSource> range, Func<TSource, int> keySelector,
		Func<TSource, TValue> valueSelector)
	{
		Guard.IsNotNull(range);
		Guard.IsNotNull(keySelector);
		Guard.IsNotNull(valueSelector);

		foreach (var item in range)
			Add(keySelector(item), valueSelector(item));
	}

	[CollectionAccess(CollectionAccessType.UpdatedContent)]
	public void AddRange(IList<int> keys, IList<TValue> values)
	{
		Guard.IsNotNull(keys);
		Guard.IsNotNull(values);

		var count = keys.Count;
		if (count != values.Count)
			CollectionThrowHelper.ThrowInvalidKeysValuesSizeArgumentException();

		EnsureCapacity((int)(count * 1.5f));
		for (var i = count; i-- > 0;)
			Add(keys[i], values[i]);
	}

	[CollectionAccess(CollectionAccessType.UpdatedContent)]
	public void AddRange(IEnumerable<int> keys, IEnumerable<TValue> values)
	{
		Guard.IsNotNull(keys);
		Guard.IsNotNull(values);

		if (keys is IList<int> keysList && values is IList<TValue> valuesList)
			AddRange(keysList, valuesList);
		else
			AddRangeFromEnumerable(keys, values);
	}

	private void AddRangeFromEnumerable(IEnumerable<int> keys, IEnumerable<TValue> values)
	{
		Guard.IsNotNull(keys);
		Guard.IsNotNull(values);

		using var keyEnumerator = keys.GetEnumerator();
		using var valueEnumerator = values.GetEnumerator();

		while (keyEnumerator.MoveNext())
		{
			if (!valueEnumerator.MoveNext())
				CollectionThrowHelper.ThrowInvalidKeysValuesSizeArgumentException();

			Add(keyEnumerator.Current, valueEnumerator.Current);
		}
	}

	[CollectionAccess(CollectionAccessType.ModifyExistingContent)]
	public void Clear()
	{
		// ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
		if (_buckets is null)
			Initialize();

		_buckets.Fill(_defaultEntry);
		_tails.Reset();

		_count = 0;
		_version++;
	}

	[CollectionAccess(CollectionAccessType.Read)]
	public bool Contains(KeyValuePair<int, TValue> item)
		=> TryGetValue(item.Key, out var value)
			&& value.Equals<TValue>(item.Value);

	[CollectionAccess(CollectionAccessType.Read)]
	public void CopyTo(KeyValuePair<int, TValue>[] array, int arrayIndex)
	{
		var length = _buckets.Length;
		for (var i = 0; i < length; i++)
		{
			if (IsBucketEmpty(i))
				continue;

			array[arrayIndex] = _buckets[i].AsKeyValuePair()!;
			arrayIndex++;
		}
	}

	[CollectionAccess(CollectionAccessType.Read)]
	public PooledList<KeyValuePair<int, TValue>> ToPooledList()
	{
		var result = new PooledList<KeyValuePair<int, TValue>>(Count);
		var length = _buckets.Length;
		for (var i = 0; i < length; i++)
		{
			if (!IsBucketEmpty(i))
				result.Add(_buckets[i].AsKeyValuePair());
		}

		return result;
	}

	[CollectionAccess(CollectionAccessType.ModifyExistingContent)]
	public bool Remove(KeyValuePair<int, TValue> item)
	{
		if (!RemoveInternal(item.Key, out _, true, item.Value))
			return false;

		OnEntryRemoved(item);
		return true;
	}

	private void OnEntryRemoved(in KeyValuePair<int, TValue> entry)
	{
		_version++;
		_count--;
		EntryRemoved?.Invoke(entry);
	}

#region InterfaceMethods
	[CollectionAccess(CollectionAccessType.Read)]
	public Enumerator GetEnumerator() => new(this, Enumerator.KEY_VALUE_PAIR);

	IEnumerator<KeyValuePair<int, TValue>> IEnumerable<KeyValuePair<int, TValue>>.GetEnumerator()
		=> new Enumerator(this, Enumerator.KEY_VALUE_PAIR);

	IEnumerator IEnumerable.GetEnumerator() => new Enumerator(this, Enumerator.KEY_VALUE_PAIR);

	bool IDictionary.Contains(object key)
	{
		Guard.IsNotNull(key);

		return key is int tKey && ContainsKey(tKey);
	}

	void IDictionary.Add(object key, object value) => Add(AssertKeyType(key), AssertValueType(value));

	IDictionaryEnumerator IDictionary.GetEnumerator() => new Enumerator(this, Enumerator.DICT_ENTRY);

	void IDictionary.Remove(object key)
	{
		Guard.IsNotNull(key);

		if (key is int tKey)
			Remove(tKey);
	}

	void ICollection.CopyTo(Array array, int index)
	{
		if (array is KeyValuePair<int, TValue>[] typedArray)
		{
			CopyTo(typedArray, index);
			return;
		}

		var length = _buckets.Length;
		for (var i = 0; i < length; i++)
		{
			if (IsBucketEmpty(i))
				continue;

			((IList)array)[index] = _buckets[i].AsKeyValuePair();
			index++;
		}
	}
#endregion
}
#pragma warning restore CS8766, CS8767