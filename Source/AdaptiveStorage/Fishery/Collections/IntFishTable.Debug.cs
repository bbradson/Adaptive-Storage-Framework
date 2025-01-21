// Copyright (c) 2024 bradson
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using System.Linq;
using System.Text;
using AdaptiveStorage.Fishery.Pools;

namespace AdaptiveStorage.Fishery.Collections;

public partial class IntFishTable<TValue>
{
	private static class Debug
	{
		[MethodImpl(MethodImplOptions.NoInlining)]
		public static void WarnForPossiblyExcessiveResizing(IntFishTable<TValue> fishTable, Entry entry)
			=> Log.Warning($"FishTable is resizing from a large number of clashing hashCodes. Last inserted key: '{
				entry.Key}', value: '{entry.Value}', count: '{fishTable._count}', bucket array length: '{
					fishTable._buckets.Length}', tailing entries: '{fishTable.GetTailingEntriesCount()}'");
		
		[MethodImpl(MethodImplOptions.NoInlining)]
		public static void WarnForPossiblyExcessiveHashCollisions(IntFishTable<TValue> fishTable)
		{
			fishTable._hasLoggedForExcessiveHashCollisions = true;
			Log.Warning($"FishTable contains a large number of clashing hashCodes. FishTable.Count: '{
				fishTable._count}', internal bucket array length: '{
					fishTable._buckets.Length}', tailing entries count: '{
						fishTable.GetTailingEntriesCount()}', longest tail:\n{
							string.Join("\n", fishTable.GetLongestTail().Select(index
								=> GetEntryDescription(fishTable, index)))}");
		}

		public static string GetEntryDescription(IntFishTable<TValue> fishTable, int index)
		{
			using var sb = new PooledStringBuilder();
			AppendEntryDescription(fishTable, sb.Builder, index);
			return sb.ToString();
		}

		public static void AppendEntryDescription(IntFishTable<TValue> fishTable, StringBuilder stringBuilder,
			int index)
		{
			var entry = fishTable._buckets[index];
			stringBuilder.Append("{ index: '")
				.Append(index)
				.Append("' key: '")
				.Append<int>(entry.Key)
				.Append(entry.Key == fishTable.DefaultKey ? "' (default), value: '" : "', value: '")
				.Append<TValue>(entry.Value)
				.Append(fishTable._tails.IsEmpty(index) ? "' (empty) }" : "' }");
		}

		public static void AppendFullDump(IntFishTable<TValue> fishTable, StringBuilder stringBuilder)
		{
			var buckets = fishTable._buckets;
			for (var j = 0; j < buckets.Length; j++)
			{
				stringBuilder.Append('\n');
				AppendEntryDescription(fishTable, stringBuilder, j);
			}
		}
	}
}