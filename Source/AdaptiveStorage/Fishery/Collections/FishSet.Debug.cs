// Copyright (c) 2024 bradson
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v.2.0.If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using System.Linq;
using System.Text;
using AdaptiveStorage.Fishery.Pools;

namespace AdaptiveStorage.Fishery.Collections;

public partial class IntFishSet
{
	private static class Debug
	{
		[MethodImpl(MethodImplOptions.NoInlining)]
		public static void WarnForPossiblyExcessiveResizing(IntFishSet fishSet, int entry)
			=> Log.Warning($"FishSet is resizing from a large number of clashing hashCodes. Last inserted key: '{
				entry}', count: '{fishSet._count}', bucket array length: '{
					fishSet._buckets.Length}', tailing entries: '{fishSet.GetTailingEntriesCount()}'");
		
		[MethodImpl(MethodImplOptions.NoInlining)]
		public static void WarnForPossiblyExcessiveHashCollisions(IntFishSet fishSet)
		{
			fishSet._hasLoggedForExcessiveHashCollisions = true;
			Log.Warning($"FishSet contains a large number of clashing hashCodes. FishSet.Count: '{
				fishSet._count}', internal bucket array length: '{
					fishSet._buckets.Length}', tailing entries count: '{
						fishSet.GetTailingEntriesCount()}', longest tail:\n{
							string.Join("\n", fishSet.GetLongestTail().Select(index
								=> GetEntryDescription(fishSet, index)))}");
		}

		public static string GetEntryDescription(IntFishSet fishSet, int index)
		{
			using var sb = new PooledStringBuilder();
			AppendEntryDescription(fishSet, sb.Builder, index);
			return sb.ToString();
		}

		public static void AppendEntryDescription(IntFishSet fishSet, StringBuilder stringBuilder,
			int index)
		{
			var entry = fishSet._buckets[index];
			stringBuilder.Append("{ index: '")
				.Append(index)
				.Append("' entry: '")
				.Append(entry)
				.Append(entry == DefaultValue ? "' (default), hashCode: '" : "', hashCode: '")
				.Append(entry)
				.Append(fishSet._tails.IsEmpty(index) ? "' (empty) }" : "' }");
		}

		public static void AppendFullDump(IntFishSet fishSet, StringBuilder stringBuilder)
		{
			var buckets = fishSet._buckets;
			for (var j = 0; j < buckets.Length; j++)
			{
				stringBuilder.Append('\n');
				AppendEntryDescription(fishSet, stringBuilder, j);
			}
		}
	}
}