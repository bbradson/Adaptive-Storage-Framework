// Copyright (c) 2023 bradson
// This Source Code Form is subject to the terms of the MIT license.
// If a copy of the license was not distributed with this file,
// You can obtain one at https://opensource.org/licenses/MIT/.

namespace AdaptiveStorage;

public static class ExtensionMethods
{
	public static bool Overlaps<TCollection, TElement>(this TCollection a, List<TElement> b)
		where TCollection : IList<TElement> where TElement : Def
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
	
	public static bool Overlaps<T>(this T[] a, int aCount, List<T> b) where T : Def
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
}