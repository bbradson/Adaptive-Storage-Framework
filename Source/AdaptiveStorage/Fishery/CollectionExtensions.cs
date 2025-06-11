// Copyright (c) 2024 bradson
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using System.Diagnostics.CodeAnalysis;
using System.Text;
using AdaptiveStorage.Fishery.Pools;
using AdaptiveStorage.Fishery.Utility.Diagnostics;

namespace AdaptiveStorage.Fishery;

[PublicAPI]
public static class CollectionExtensions
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool TryGetItem<T>(this List<T> list, int index, [NotNullWhen(true)] out T? item)
	{
		if ((uint)list._size > (uint)index)
		{
			item = Array.UnsafeLoad(list._items, index)!;
			return true;
		}

		item = default;
		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static T? TryGetItem<T>(this List<T> list, int index)
		=> (uint)list._size > (uint)index ? Array.UnsafeLoad(list._items, index)! : default;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static T UnsafeLoad<T>(this List<T> list, int index) => Array.UnsafeLoad(list._items, index);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void UnsafeStore<T>(this List<T> list, int index, T value)
		=> Array.UnsafeStore(list._items, index, value);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool TryGetItem<T>(this T[] array, int index, out T? item)
	{
		if ((uint)array.Length > (uint)index)
		{
			item = Array.UnsafeLoad(array, index);
			return true;
		}

		item = default;
		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static T? TryGetItem<T>(this T[] array, int index)
		=> (uint)array.Length > (uint)index ? Array.UnsafeLoad(array, index) : default;

	public static T[] ToArray<T>(this IEnumerable<T> enumerable, int length)
	{
		var array = new T[length];

		if (enumerable is IList<T> iList)
			array.Fill(iList);
		else
			array.FillUsingEnumerable(enumerable);

		return array;
	}

	public static TElement[] ToArray<TEnumerable, TElement>(this TEnumerable enumerable, TElement[] destination)
		where TEnumerable : IList<TElement>
	{
		Guard.IsNotNull(destination);

		destination.Fill(enumerable);
		return destination;
	}

	public static T[] ToArray<T>(this IEnumerable<T> enumerable, T[] destination)
	{
		Guard.IsNotNull(destination);

		if (enumerable is IList<T> iList)
			destination.Fill(iList);
		else
			destination.FillUsingEnumerable(enumerable);

		return destination;
	}

	private static void FillUsingEnumerable<T>(this T[] array, IEnumerable<T> enumerable, int startIndex = 0,
		int count = -1)
	{
		var i = 0u;
		foreach (var item in enumerable)
		{
			if (i >= (uint)count || startIndex >= array.Length)
				break;

			array[startIndex++] = item;
			i++;
		}
	}

	public static void Fill<TElement, TCollection>(this TElement[] array, TCollection collection, int startIndex = 0,
		int count = -1)
		where TCollection : IList<TElement>
	{
		Guard.IsNotNull(collection);

		if (count <= 0)
			count = collection.Count;

		if (count <= array.Length - startIndex)
		{
			collection.CopyTo(array, startIndex);
			return;
		}

		for (var listIndex = 0; listIndex < count && startIndex < array.Length; startIndex++, listIndex++)
			array[startIndex] = collection[listIndex];
	}

	public static void Fill<T>(this T[] array, IEnumerable<T> enumerable, int startIndex = 0, int count = -1)
	{
		Guard.IsNotNull(enumerable);

		if (enumerable is IList<T> iList)
			array.Fill(iList, startIndex, count);
		else
			array.FillUsingEnumerable(enumerable, startIndex, count);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void AddRangeFast<T>(this List<T> list, List<T> range)
	{
		if (range._size < 1)
			return;

		list.EnsureCapacity(list._size + range._size);

		range.UnsafeCopyTo(list, list._size);

		list._size += range._size;
		list._version++;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void UnsafeCopyTo<T>(this List<T> source, List<T> destination, int destinationStartIndex)
		=> UnsafeBlockCopy(ref source._items[0], ref destination._items[destinationStartIndex], source._size);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void UnsafeCopyTo<T>(this T[] source, T[] destination, int destinationStartIndex)
		=> UnsafeBlockCopy(ref source[0], ref destination[destinationStartIndex], source.Length);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static unsafe void UnsafeBlockCopy<T>(ref T source, ref T destination, int count)
		=> Unsafe.CopyBlock(ref Unsafe.As<T, byte>(ref destination),
			ref Unsafe.As<T, byte>(ref source),
			(uint)((nint)sizeof(T) * count));

	/// <summary>
	/// Returns a list's internal _items array. Keep in mind list.Version() needs to change every time items are
	/// modified.
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static T[] ItemsUnchecked<T>(this List<T> list) => list._items;

	/// <summary>
	/// Returns a span pointing to a list's internal _items array. Keep in mind list.Version() needs to change every
	/// time items are modified.
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Span<T> AsSpanUnchecked<T>(this List<T> list) => new(list._items, 0, list._size);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ReadOnlySpan<T> AsReadOnlySpan<T>(this List<T> list) => new(list._items, 0, list._size);

	public static void ReplaceContentsWith<T>(this List<T> list, [AllowNull] List<T> collection)
	{
		if (ReferenceEquals(list, collection))
			return;

		if (collection is null
			|| (collection._size is var collectionCount
				&& collectionCount == 0))
		{
			list.Clear();
			return;
		}

		if (collectionCount <= list._size)
		{
			if (collectionCount < list._size)
				list.RemoveRange(collectionCount, list._size - collectionCount);

			collection.UnsafeCopyTo(list, 0);
		}
		else
		{
			list._items = new T[collectionCount];
			collection.UnsafeCopyTo(list, 0);
			list._size = collectionCount;
		}

		list._version++;
	}

	public static void ReplaceContentsWith<TThis, TOther>(this List<TThis> list, [AllowNull] TOther collection)
		where TOther : ICollection<TThis>
	{
		if (typeof(TOther) == typeof(List<TThis>))
			list.ReplaceContentsWith(Unsafe.As<List<TThis>>(collection));

		if (ReferenceEquals(list, collection))
			return;

		if (collection is null
			|| (collection.Count is var collectionCount
				&& collectionCount == 0))
		{
			list.Clear();
			return;
		}

		if (collectionCount <= list._size)
		{
			if (collectionCount < list._size)
				list.RemoveRange(collectionCount, list._size - collectionCount);
			collection.CopyTo(list._items, 0);
		}
		else
		{
			list._items = new TThis[collectionCount];
			collection.CopyTo(list._items, 0);
			list._size = collectionCount;
		}

		list._version++;
	}

	public static PooledList<T> ToPooledList<T>(this T[] source) => new(source);

	public static PooledList<T> ToPooledList<T>(this List<T> source) => new(source);

	public static PooledList<T> ToPooledList<T>(this in ReadOnlySpan<T> source) => new(source);

	public static PooledList<T> ToPooledList<T>(this in Span<T> source) => new(source);
	
	public static PooledList<T> ToPooledList<T>(this IEnumerable<T> source) => new(source);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void Clear<T>(this T[] array) => Array.Clear(array, 0, array.Length);

	// public static List<T> Copy<T>(this List<T> source)
	// {
	// 	Guard.IsNotNull(source);
	// 	
	// 	var destination = new List<T>(source._size);
	//
	// 	if (source._size == 0)
	// 		return destination;
	// 	
	// 	// Unsafe.CopyBlock(ref Unsafe.As<T, byte>(ref destination._items[0]),
	// 	// 	ref Unsafe.As<T, byte>(ref source._items[0]), (uint)sizeof(T) * (uint)source._size);
	// 	
	// 	AdaptiveStorage.Fishery.CollectionExtensions.UnsafeBlockCopy(ref source._items[0],	// does a type check that causes issues
	// 		ref destination._items[0], source._size);							// when called from transpiled methods
	// 	
	// 	destination._size = source._size;
	// 	
	// 	return destination;
	// }

	public static unsafe List<T> Copy<T>(this List<T> source)
	{
		Guard.IsNotNull(source);

		var destination = new List<T>(source._size);

		if (source._size == 0)
			return destination;

		Unsafe.CopyBlock(ref Unsafe.As<T, byte>(ref destination._items[0]),
			ref Unsafe.As<T, byte>(ref source._items[0]), (uint)(source._size * sizeof(T)));

		destination._size = source._size;

		return destination;
	}

	public static List<T> AsOrToList<T>(this IEnumerable<T> enumerable)
		=> enumerable as List<T> ?? System.Linq.Enumerable.ToList(enumerable);

	public static void UnwrapReadOnlyArray<T>(this List<T> list, out T[] array, out int count)
	{
		array = list._items;
		count = list._size;
		Guard.IsLessThanOrEqualTo((uint)count, (uint)array.Length);
	}

	public static ref T GetArrayDataReference<T>(this List<T> list, out nint count)
	{
		count = list._size;
		var items = list._items;
		Guard.IsLessThanOrEqualTo((uint)count, (uint)items.Length);
		return ref items[0];
		// return Unsafe.AddByteOffset(ref Unsafe.As<System.Pinnable<T>>(list._items).Data,
		// 	SpanHelpers.PerTypeValues<T>.ArrayAdjustment);
	}
	
	public static ref T GetArrayDataReference<T>(this List<T> list) => ref list._items[0];

	public static void Fill<T>(this List<T> list, T value, int count)
	{
		Guard.IsNotNull(list);
		Guard.IsGreaterThanOrEqualTo(count, 0);

		ref var listSize = ref list._size;
		if (listSize + count >= list._items.Length)
			list.EnsureCapacity(listSize + count);

		var listItems = list._items;
		
		for (var i = count; --i >= 0;)
			listItems[listSize++] = value;
		
		list._version++;
	}

	public static void Fill<T>(this T[] array, T value)
	{
		Guard.IsNotNull(array);

		var i = array.Length - 1;

		if (i < 0)
			return;

		do
		{
			array.UnsafeStore(i, value);
		}
		while (--i >= 0);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static T UnsafeLoad<T>(this T[] array, int index) => Array.UnsafeLoad(array, index);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void UnsafeStore<T>(this T[] array, int index, T value) => Array.UnsafeStore(array, index, value);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static TTo UnsafeCast<TFrom, TTo>(this TFrom obj)
	{
		var copy = obj;
		return Unsafe.As<TFrom, TTo>(ref copy);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void AddRangeFast<T>(this List<T> list, T[] range)
	{
		Guard.IsNotNull(list);
		Guard.IsNotNull(range);

		if (range.Length < 1)
			return;

		list.EnsureCapacity(list._size + range.Length);
		range.UnsafeCopyTo(list._items, list._size);

		list._size += range.Length;
		list._version++;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void AddRange<T>(this List<T> list, in ReadOnlySpan<T> collection)
	{
		Guard.IsNotNull(list);

		var count = collection.Length;
		if (count < 1)
			return;

		list.AddRangeInternal(ref collection.DangerousGetPinnableReference(), count);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void AddRange<T>(this List<T> list, in Span<T> collection)
	{
		Guard.IsNotNull(list);

		var count = collection.Length;
		if (count < 1)
			return;

		list.AddRangeInternal(ref collection.DangerousGetPinnableReference(), count);
	}

	private static void AddRangeInternal<T>(this List<T> list, ref T collectionReference, int count)
	{
		list.EnsureCapacity(list._size + count);
		UnsafeBlockCopy(ref collectionReference, ref list._items[list._size], count);

		list._size += count;
		list._version++;
	}

	public static void AddRange<T, TInput>(this List<T> list, List<TInput> collection, Func<TInput, T> selector)
	{
		Guard.IsNotNull(list);
		Guard.IsNotNull(collection);
		
		nint count = collection.Count;
		if (count < 1)
			return;
		
		list.AddRangeInternal(ref collection._items[0], count, selector);
	}
	
	public static void AddRange<T, TInput>(this List<T> list, TInput[] collection, Func<TInput, T> selector)
	{
		Guard.IsNotNull(list);
		Guard.IsNotNull(collection);
		
		nint count = collection.Length;
		if (count < 1)
			return;
		
		list.AddRangeInternal(ref collection[0], count, selector);
	}

	public static void AddRange<T, TInput>(this List<T> list, in ReadOnlySpan<TInput> collection,
		Func<TInput, T> selector)
	{
		Guard.IsNotNull(list);

		nint count = collection.Length;
		if (count < 1)
			return;

		list.AddRangeInternal(ref collection.DangerousGetPinnableReference(), count, selector);
	}

	private static unsafe void AddRangeInternal<T, TInput>(this List<T> list, ref TInput collectionReference,
		nint count, Func<TInput, T> selector)
	{
		ref var listSize = ref list._size;
		list.EnsureCapacity(listSize + (int)count);
		var listItems = list._items;

		count *= sizeof(TInput);
		for (nint i = 0; i < count; i += sizeof(TInput))
			listItems.UnsafeStore(listSize++, selector(Unsafe.AddByteOffset(ref collectionReference, i)));

		list._version++;
	}
	
	public static void AddRange<T, TInput>(this List<T> list, IEnumerable<TInput> collection, Func<TInput, T> selector)
	{
		Guard.IsNotNull(list);
		Guard.IsNotNull(collection);
		
		switch (collection)
		{
			case List<TInput> collectionList:
			{
				list.AddRange(collectionList, selector);
				return;
			}
			case TInput[] collectionArray:
			{
				list.AddRange(collectionArray, selector);
				return;
			}
			case ICollection<TInput> iCollection:
			{
				AddRangeInternal(list, selector, iCollection);
				break;
			}
			default:
			{
				foreach (var element in collection)
					list.Add(selector(element));
				break;
			}
		}
		
		list._version++;
	}

	private static void AddRangeInternal<T, TInput>(List<T> list, Func<TInput, T> selector, ICollection<TInput> iCollection)
	{
		var count = iCollection.Count;
		if (count < 1)
			return;

		ref var listSize = ref list._size;
		list.EnsureCapacity(listSize + count);
		var listItems = list._items;
				
		foreach (var element in iCollection)
			listItems.UnsafeStore(listSize++, selector(element));
	}

	public static int RemoveAll<TContext, TElement>(this List<TElement> list, ref TContext context,
		[InstantHandle] PredicateWithRefContext<TContext, TElement> match)
	{
		Guard.IsNotNull(list);
		Guard.IsNotNull(match);
		
		list.UnwrapReadOnlyArray(out var array, out var size);

		var freeIndex = 0; // the first free slot in items array

		// Find the first item which needs to be removed.
		while (freeIndex < size && !match(ref context, array.UnsafeLoad(freeIndex)))
			freeIndex++;
		
		if (freeIndex >= size)
			return 0;

		var current = freeIndex + 1;
		while (current < size)
		{
			// Find the first item which needs to be kept.
			while (current < size && match(ref context, array.UnsafeLoad(current)))
				current++;

			if (current < size)
			{
				// copy item to the free slot.
				array.UnsafeStore(freeIndex++, array.UnsafeLoad(current++));
			}
		}

		// if (RuntimeHelpers.IsReferenceOrContainsReferences<TElement>()) // TODO: benchmark
		{
			Array.Clear(array, freeIndex,
				size - freeIndex); // Clear the elements so that the gc can reclaim the references.
		}

		var result = size - freeIndex;
		list._size = freeIndex;
		list._version++;
		return result;
	}

	public static int RemoveAll<TContext, TElement>(this List<TElement> list, TContext context,
		[InstantHandle] Predicate<TContext, TElement> match)
	{
		Guard.IsNotNull(list);
		Guard.IsNotNull(match);

		list.UnwrapReadOnlyArray(out var array, out var size);

		var freeIndex = 0; // the first free slot in items array

		// Find the first item which needs to be removed.
		while (freeIndex < size && !match(context, array.UnsafeLoad(freeIndex)))
			freeIndex++;

		if (freeIndex >= size)
			return 0;

		var current = freeIndex + 1;
		while (current < size)
		{
			// Find the first item which needs to be kept.
			while (current < size && match(context, array.UnsafeLoad(current)))
				current++;

			if (current < size)
			{
				// copy item to the free slot.
				array.UnsafeStore(freeIndex++, array.UnsafeLoad(current++));
			}
		}

		// if (RuntimeHelpers.IsReferenceOrContainsReferences<TElement>()) // TODO: benchmark
		{
			Array.Clear(array, freeIndex,
				size - freeIndex); // Clear the elements so that the gc can reclaim the references.
		}

		var result = size - freeIndex;
		list._size = freeIndex;
		list._version++;
		return result;
	}

	public static TElement? Find<TContext, TElement>(this List<TElement> list, TContext context,
		Predicate<TContext, TElement> match)
	{
		Guard.IsNotNull(list);
		Guard.IsNotNull(match);
		list.UnwrapReadOnlyArray(out var array, out var size);
		
		for (var i = 0; i < size; i++)
		{
			if (match(context, array.UnsafeLoad(i)))
				return array.UnsafeLoad(i);
		}

		return default;
	}

	public static TElement? Find<TContext, TElement>(this List<TElement> list, ref TContext context,
		PredicateWithRefContext<TContext, TElement> match)
	{
		Guard.IsNotNull(list);
		Guard.IsNotNull(match);
		list.UnwrapReadOnlyArray(out var array, out var size);

		for (var i = 0; i < size; i++)
		{
			if (match(ref context, array.UnsafeLoad(i)))
				return array.UnsafeLoad(i);
		}

		return default;
	}

	public static TElement? FindLast<TContext, TElement>(this List<TElement> list, TContext context,
		Predicate<TContext, TElement> match)
	{
		Guard.IsNotNull(list);
		Guard.IsNotNull(match);
		list.UnwrapReadOnlyArray(out var array, out var size);

		for (var i = size; --i >= 0;)
		{
			if (match(context, array.UnsafeLoad(i)))
				return array.UnsafeLoad(i);
		}

		return default;
	}

	public static TElement? FindLast<TContext, TElement>(this List<TElement> list, ref TContext context,
		PredicateWithRefContext<TContext, TElement> match)
	{
		Guard.IsNotNull(list);
		Guard.IsNotNull(match);
		list.UnwrapReadOnlyArray(out var array, out var size);

		for (var i = size; --i >= 0;)
		{
			if (match(ref context, array.UnsafeLoad(i)))
				return array.UnsafeLoad(i);
		}

		return default;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void Add(this PooledList<char> list, string value) => list.List.Add(value);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void Add(this List<char> list, string value)
	{
		Guard.IsNotNull(list);
		Guard.IsNotNull(value);

		var count = value.Length;
		if (count < 1)
			return;
		
		list.AddRangeInternal(
#if V1_4 || V1_5
			ref value.m_firstChar,
#else
			ref value._firstChar,
#endif
			count);
	}

	public static unsafe StringBuilder Append(this StringBuilder builder, ReadOnlySpan<char> span)
	{
		Guard.IsNotNull(builder);
		if (span.IsEmpty)
			return builder;
		
		fixed (char* firstChar = &span.DangerousGetPinnableReference())
			return builder.Append(firstChar, span.Length);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static StringBuilder Append(this StringBuilder builder, List<char> value)
		=> builder.Append(value._items, 0, value._size);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static StringBuilder Append<T>(this StringBuilder builder, T? value)
		// ReSharper disable once RedundantToStringCallForValueType
		// ReSharper disable once CompareNonConstrainedGenericWithNull
		// ReSharper disable once HeapView.PossibleBoxingAllocation
		=> value == null ? builder : builder.Append(value.ToString());

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ref T GetReference<T>(this List<T> list, int index)
	{
		Guard.IsInRange(index, 0, list.Count);

		list._version++;
		return ref list._items[index];
	}

	[Obsolete("Renamed to GetReferenceUnsafe")]
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ref T GetReferenceUnverifiable<T>(this List<T> list, int index) => ref list.GetReferenceUnsafe(index);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ref T GetReferenceUnsafe<T>(this List<T> list, int index) => ref list._items[index];

	public static void RemoveAtFastUnordered<T>(this List<T> list, int index)
	{
		ref var lastBucket = ref list._items[list._size - 1];
		list[index] = lastBucket;
		lastBucket = default!;
		list._size--;
	}

	public static bool RemoveFastUnordered<T>(this List<T> list, T item)
	{
		var index = list.IndexOf(item);
		if (index < 0)
			return false;

		list.RemoveAtFastUnordered(index);
		return true;
	}

	public static void InsertFastUnordered<T>(this List<T> list, int index, T item)
	{
		if ((uint)index > (uint)list._size)
			ThrowHelper.ThrowArgumentOutOfRangeException();

		if (list._size == list._items.Length)
			list.EnsureCapacity(list._size + 1);

		ref var targetBucket = ref list._items[index];

		list._items[list._size] = targetBucket;
		targetBucket = item;
		list._size++;
		list._version++;
	}

#if !V1_4 && !V1_5
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static ref T DangerousGetPinnableReference<T>(this Span<T> span)
		=> ref Unsafe.AsRef(span.GetPinnableReference());

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static ref T DangerousGetPinnableReference<T>(this ReadOnlySpan<T> span)
		=> ref Unsafe.AsRef(span.GetPinnableReference());
#endif
}

public delegate bool PredicateWithRefContext<TContext, TElement>(ref TContext context, TElement element);
