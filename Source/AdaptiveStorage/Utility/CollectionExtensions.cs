// Copyright (c) 2023 bradson
// This Source Code Form is subject to the terms of the MIT license.
// If a copy of the license was not distributed with this file,
// You can obtain one at https://opensource.org/licenses/MIT/.

using AdaptiveStorage.Fishery;
using AdaptiveStorage.Fishery.Utility.Diagnostics;
using AdaptiveStorage.PrintDatas;

namespace AdaptiveStorage.Utility;

public static class CollectionExtensions
{
	public static bool Overlaps<TCollection, TDef>(this List<TDef> a, TCollection b)
		where TCollection : IList<TDef> where TDef : Def
	{
		var bCount = b.Count;
		for (var i = a.Count; i-- > 0;)
		{
			var aItem = a[i];
			for (var j = bCount; j-- > 0;)
			{
				if (b[j] == aItem)
					return true;
			}
		}

		return false;
	}

	public static bool Overlaps<TCollection, TDef>(this TDef[] a, int aCount, TCollection b)
		where TCollection : IList<TDef> where TDef : Def
	{
		var bCount = b.Count;
		for (var i = aCount; i-- > 0;)
		{
			var aItem = a[i];
			for (var j = bCount; j-- > 0;)
			{
				if (b[j] == aItem)
					return true;
			}
		}

		return false;
	}

	public static bool Overlaps<TDef, TThing>(this List<TThing> a, List<TDef> b) where TDef : Def where TThing : Thing
	{
		var bCount = b.Count;
		for (var i = a.Count; i-- > 0;)
		{
			var aDef = a[i].def;
			for (var j = bCount; j-- > 0;)
			{
				if (b[j] == aDef)
					return true;
			}
		}

		return false;
	}

	public static bool Remove(this List<PrintData> printDatas, Thing thing)
	{
		for (var i = printDatas.Count; i-- > 0;)
		{
			if (printDatas[i].Thing != thing)
				continue;

			printDatas.RemoveAt(i);
			return true;
		}

		return false;
	}

	public static PrintData? TryGet(this List<PrintData> printDatas, Thing thing)
	{
		for (var i = printDatas.Count; i-- > 0;)
		{
			if (printDatas[i].Thing == thing)
				return printDatas[i];
		}

		return null;
	}

	public static int IndexOf<T>(this in ReadOnlySpan<T> span, T element) where T : class
	{
		ref var dataReference = ref span.DangerousGetPinnableReference();
		for (var i = span.Length; --i >= 0;)
		{
			if (Unsafe.Add(ref dataReference, i) == element)
				return i;
		}

		return -1;
	}

	public static int TryGetSeededIndex<T>(this List<T> list, int seed, Func<T, uint> weightSelector)
	{
		// const ulong FIBONACCI = 11400714819323198485UL;

		var weightSum = 0UL;
		list.UnwrapReadOnlyArray(out var array, out var count);
		
		for (var i = count; --i >= 0;)
			weightSum += weightSelector(array.UnsafeLoad(i));

		if (weightSum == 0UL)
			return -1;
		else if (count == 1)
			return 0;

		var target = RandomizeUsingPcgRxsMXs((ulong)(uint)seed) /* FIBONACCI*/ % weightSum;
		for (var i = 0; i < count; i++)
		{
			var weight = weightSelector(array.UnsafeLoad(i));
			if (weight == 0)
				continue;
			else if (target < weight)
				return i;

			target -= weight;
		}

		throw new InvalidOperationException("weightSelector returned invalid values");
	}

	public static ulong RandomizeUsingPcgRxsMXs(ulong value)
	{
		value = (value ^ (value >> (int)(5u + ((uint)(value >> 59) & 31u)))) * 12605985483714917081UL;
		
		return value ^ (value >> 43);
	}

	public static uint RandomizeUsingPcgRxsMXs(uint value)
	{
		value = (value ^ (value >> (int)(4u + ((value >> 28) & 15u)))) * 277803737u;

		return value ^ (value >> 22);
	}

	public static ulong ReversePcgRxsMXs(ulong value)
	{
		value = UnXorShift(value, 64u, 43u) * 15009553638781119849UL;

		return UnXorShift(value, 64u, 5u + ((uint)(value >> 59) & 31u));
	}

	public static uint ReversePcgRxsMXs(uint value)
	{
		value = UnXorShift(value, 32u, 22u) * 2897767785u;

		return UnXorShift(value, 32u, 4u + ((value >> 28) & 15u));
	}

	public static ulong UnXorShift(ulong x, uint bits, uint shift)
	{
		if (shift << 1 >= bits)
			return x ^ (x >> (int)shift);

		var lowMask1 = (1UL << (int)(bits - (shift << 1))) - 1UL;
		var top1 = x;
		top1 ^= top1 >> (int)shift;
		top1 &= ~lowMask1;
		x = top1 | (x & lowMask1);
		var bottom2 = x & ((1UL << (int)(bits - shift)) - 1UL);
		bottom2 = UnXorShift(bottom2, bits - shift, shift);
		bottom2 &= lowMask1;
		return top1 | bottom2;
	}

	public static uint UnXorShift(uint x, uint bits, uint shift)
	{
		if (shift << 1 >= bits)
			return x ^ (x >> (int)shift);

		var lowMask1 = (1u << (int)(bits - (shift << 1))) - 1u;
		var top1 = x;
		top1 ^= top1 >> (int)shift;
		top1 &= ~lowMask1;
		x = top1 | (x & lowMask1);
		var bottom2 = x & ((1u << (int)(bits - shift)) - 1u);
		bottom2 = UnXorShift(bottom2, bits - shift, shift);
		bottom2 &= lowMask1;
		return top1 | bottom2;
	}

	public static void AddRangeDistinct<T>(this List<T> list, IEnumerable<T> range)
	{
		foreach (var item in range)
		{
			if (!list.Contains(item))
				list.Add(item);
		}
	}

	public static int IndexOf(this int[] array, int element) => array.IndexOfInternal(element, array.Length);

	public static int IndexOf(this int[] array, int element, int count)
	{
		Guard.IsLessThanOrEqualTo(count, array.Length);
		return array.IndexOfInternal(element, count);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static int IndexOfInternal(this int[] array, int element, int count)
	{
		while (--count >= 0)
		{
			if (array.UnsafeLoad(count) == element)
				return count;
		}

		return -1;
	}
	
	public static int IndexOf(this in ReadOnlySpan<int> span, int element)
	{
		ref var anchor = ref span.DangerousGetPinnableReference();
		for (var i = span.Length; --i >= 0;)
		{
			if (Unsafe.Add(ref anchor, i) == element)
				return i;
		}

		return -1;
	}

	public static bool Contains(this int[] array, int element) => array.IndexOf(element) >= 0;

	public static bool Contains(this int[] array, int element, int count) => array.IndexOf(element, count) >= 0;

	public static bool Contains(this in ReadOnlySpan<int> span, int element) => span.IndexOf(element) >= 0;

	public static int Sum(this int[] array) => array.SumInternal(array.Length);

	public static int Sum(this int[] array, int count)
	{
		Guard.IsLessThanOrEqualTo((uint)count, (uint)array.Length);
		return array.SumInternal(count);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static int SumInternal(this int[] array, int count)
	{
		var result = 0;
		while (--count >= 0)
			result += array.UnsafeLoad(count);

		return result;
	}

	public static int Sum<T>(this List<T> list, Func<T, int> selector)
	{
		var result = 0;
		list.UnwrapReadOnlyArray(out var array, out var count);
		while (--count >= 0)
			result += selector(array.UnsafeLoad(count));

		return result;
	}

	public static int Sum<T>(this in ReadOnlySpan<T> span, Func<T, int> selector)
	{
		var result = 0;
		ref var anchor = ref span.DangerousGetPinnableReference();
		for (var i = span.Length; --i >= 0;)
			result += selector(Unsafe.Add(ref anchor, i));

		return result;
	}

	public static int Count(this in ReadOnlySpan<int> span, int element)
	{
		var result = 0;
		ref var anchor = ref span.DangerousGetPinnableReference();
		for (var i = span.Length; --i >= 0;)
		{
			var current = Unsafe.Add(ref anchor, i);
			if (current == element)
				result++;
		}

		return result;
	}

	public static T[] Copy<T>(this T[] array) => [..array];

	public static int Max(this int[] array)
	{
		var count = array.Length;
		if (count == 0)
			return 0;
		
		var result = int.MinValue;
		for (var i = count; --i >= 0;)
			result = Math.Max(result, array.UnsafeLoad(i));

		return result;
	}

	public static int Max(this List<int> list)
	{
		list.UnwrapReadOnlyArray(out var array, out var count);
		if (count == 0)
			return 0;
		
		var result = int.MinValue;
		for (var i = count; --i >= 0;)
			result = Math.Max(result, array.UnsafeLoad(i));

		return result;
	}

	public static int Max<T>(this T[] array, Func<T, int> selector)
	{
		Guard.IsNotNull(array);
		Guard.IsNotNull(selector);

		var count = array.Length;
		if (count == 0)
			return 0;
		
		var result = int.MinValue;
		for (var i = count; --i >= 0;)
			result = Math.Max(result, selector(array.UnsafeLoad(i)));

		return result;
	}

	public static int Max<T>(this List<T> list, Func<T, int> selector)
	{
		Guard.IsNotNull(list);
		Guard.IsNotNull(selector);

		list.UnwrapReadOnlyArray(out var array, out var count);
		if (count == 0)
			return 0;

		var result = int.MinValue;
		for (var i = count; --i >= 0;)
			result = Math.Max(result, selector(array.UnsafeLoad(i)));

		return result;
	}

	public static int Max<TElement, TContext>(this List<TElement> list, TContext context,
		Func<TContext, TElement, int> selector)
	{
		Guard.IsNotNull(list);
		Guard.IsNotNull(selector);

		list.UnwrapReadOnlyArray(out var array, out var count);
		if (count == 0)
			return 0;

		var result = int.MinValue;
		for (var i = count; --i >= 0;)
			result = Math.Max(result, selector(context, array.UnsafeLoad(i)));

		return result;
	}

	public static int Min(this int[] array)
	{
		var count = array.Length;
		if (count == 0)
			return 0;

		var result = int.MaxValue;
		for (var i = count; --i >= 0;)
			result = Math.Min(result, array.UnsafeLoad(i));

		return result;
	}

	public static int Min(this List<int> list)
	{
		list.UnwrapReadOnlyArray(out var array, out var count);
		if (count == 0)
			return 0;

		var result = int.MaxValue;
		for (var i = count; --i >= 0;)
			result = Math.Min(result, array.UnsafeLoad(i));

		return result;
	}

	public static int Min<T>(this T[] array, Func<T, int> selector)
	{
		Guard.IsNotNull(array);
		Guard.IsNotNull(selector);

		var count = array.Length;
		if (count == 0)
			return 0;

		var result = int.MaxValue;
		for (var i = count; --i >= 0;)
			result = Math.Min(result, selector(array.UnsafeLoad(i)));

		return result;
	}

	public static int Min<T>(this List<T> list, Func<T, int> selector)
	{
		Guard.IsNotNull(list);
		Guard.IsNotNull(selector);
		
		list.UnwrapReadOnlyArray(out var array, out var count);
		if (count == 0)
			return 0;

		var result = int.MaxValue;
		for (var i = count; --i >= 0;)
			result = Math.Min(result, selector(array.UnsafeLoad(i)));

		return result;
	}

	public static int Min<TElement, TContext>(this List<TElement> list, TContext context,
		Func<TContext, TElement, int> selector)
	{
		Guard.IsNotNull(list);
		Guard.IsNotNull(selector);

		list.UnwrapReadOnlyArray(out var array, out var count);
		if (count == 0)
			return 0;

		var result = int.MaxValue;
		for (var i = count; --i >= 0;)
			result = Math.Min(result, selector(context, array.UnsafeLoad(i)));

		return result;
	}

	public static bool TryAddDistinct<T>(this List<T> list, T item) where T : class
	{
		list.UnwrapReadOnlyArray(out var array, out var count);
		while (--count >= 0)
		{
			if (array.UnsafeLoad(count) == item)
				return false;
		}
		
		list.Add(item);
		return true;
	}

	public static IEnumerable<TElement> Where<TElement>(this List<TElement> list, Predicate<TElement> predicate)
	{
		list.UnwrapReadOnlyArray(out var array, out var count);
		for (var i = 0; i < count; i++)
		{
			var element = array[i];
			if (predicate(element))
				yield return element;
		}
	}

	public static IEnumerable<TElement> Where<TContext, TElement>(this List<TElement> list, TContext context,
		Predicate<TContext, TElement> predicate)
	{
		list.UnwrapReadOnlyArray(out var array, out var count);
		for (var i = 0; i < count; i++)
		{
			var element = array[i];
			if (predicate(context, element))
				yield return element;
		}
	}
}