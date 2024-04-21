// Copyright (c) 2023 bradson
// This Source Code Form is subject to the terms of the MIT license.
// If a copy of the license was not distributed with this file,
// You can obtain one at https://opensource.org/licenses/MIT/.

using AdaptiveStorage.Pools;
#if V1_4
using System.Runtime.CompilerServices;
#endif

namespace AdaptiveStorage;

public static class ExtensionMethods
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
			if (printDatas[i].Thing != thing)
				continue;

			return printDatas[i];
		}

		return null;
	}

	public static int TryGetSeededIndex<T>(this List<T> list, int seed, Func<T, uint> weightSelector)
	{
		const ulong FIBONACCI = 11400714819323198485UL;

		var weightSum = 0UL;
		var count = list.Count;
		
		for (var i = count; i-- > 0;)
			weightSum += weightSelector(list[i]);

		if (weightSum == 0UL)
			return -1;
		else if (count == 1)
			return 0;

		var target = (uint)seed * FIBONACCI % weightSum;
		for (var i = 0; i < count; i++)
		{
			var weight = weightSelector(list[i]);
			if (weight == 0)
				continue;
			else if (target < weight)
				return i;

			target -= weight;
		}

		throw new InvalidOperationException();
	}

	public static void AddRangeDistinct<T>(this List<T> list, IEnumerable<T> range)
	{
		foreach (var item in range)
		{
			if (!list.Contains(item))
				list.Add(item);
		}
	}

	public static PooledIList<List<TSource>> ToPooledList<TSource>(this IEnumerable<TSource> source)
	{
		var result = new PooledIList<List<TSource>>();
		
		try
		{
			result.List.AddRange(source);
		}
		catch
		{
			result.Dispose();
			throw;
		}
		
		return result;
	}

	public static void SetAllowAll(this ThingFilter filter, IEnumerable<ThingDef> defs, bool allow)
	{
		foreach (var def in defs)
			filter.SetAllow(def, allow);
	}

	public static void SetAllowAll(this ThingFilter filter, IEnumerable<ThingCategoryDef> defs, bool allow)
	{
		foreach (var def in defs)
			filter.SetAllow(def, allow);
	}

	public static void SetPosition(this ref Matrix4x4 matrix, in Vector3 position)
	{
		matrix.m03 = position.x;
		matrix.m13 = position.y;
		matrix.m23 = position.z;
	}

	public static Vector3 GetPosition(this in Matrix4x4 matrix)
		=> default(Vector3) with { x = matrix.m03, y = matrix.m13, z = matrix.m23 };

	public static TextureAtlasGroup GetAtlasGroup(this Thing thing) => thing.def.category.ToAtlasGroup();

#if V1_4
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void DrawNowAt(this Thing thing, Vector3 drawLoc, bool flip = false)
		=> thing.DrawAt(drawLoc, flip);
#endif
}