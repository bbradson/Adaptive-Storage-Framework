// Copyright (c) 2024 bradson
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using System.Diagnostics.CodeAnalysis;
using AdaptiveStorage.Fishery.Pools;

namespace AdaptiveStorage.Fishery.Collections;

public partial class IntFishTable<TValue>
{
	internal static class ThrowHelper
	{
		[DoesNotReturn]
		internal static void ThrowEmptyBucketKeyInvalidOperationException(int bucketIndex, Entry bucket)
			=> throw new InvalidOperationException(
				$"Bucket marked as empty had non-default key. This should never happen. Index: '{
					bucketIndex}', key: '{bucket.Key}', value: '{bucket.Value}'");
		
		[DoesNotReturn]
		internal static void ThrowEmptyBucketIsTailInvalidOperationException(int bucketIndex, Entry bucket)
			=> throw new InvalidOperationException(
				$"Bucket marked as empty was attached as tail to a key. This should never happen. Index: '{
					bucketIndex}', key: '{bucket.Key}', value: '{bucket.Value}'");

		[DoesNotReturn]
		internal static void ThrowFailedToFindParentInvalidOperationException(IntFishTable<TValue> fishTable,
			int childBucketIndex)
		{
			using var pooledStringBuilder = new PooledStringBuilder(227);
			var stringBuilder = pooledStringBuilder.Builder;

			pooledStringBuilder
				.Append("Failed to find parent index in IntFishTable<")
				.Append(typeof(TValue))
				.Append("> for entry: ");
			
			Debug.AppendEntryDescription(fishTable, stringBuilder, childBucketIndex);
			
			stringBuilder
				.Append(", count: '")
				.Append(fishTable._count)
				.Append("', bucket array length: '")
				.Append(fishTable._buckets.Length)
				.Append("', total tailing entries count: '")
				.Append(fishTable.GetTailingEntriesCount())
				.Append("', known chain of tails:");

			var ancestorIndex = fishTable.GetBucketIndexForKeyAt(childBucketIndex);
			while (true)
			{
				pooledStringBuilder.Append('\n');
				Debug.AppendEntryDescription(fishTable, stringBuilder, ancestorIndex);

				if (!fishTable.TryContinueWithTail(ref ancestorIndex))
					break;
			}

			pooledStringBuilder.Append("\n\nFull dump:");
			Debug.AppendFullDump(fishTable, stringBuilder);

			throw new InvalidOperationException(pooledStringBuilder.ToString());
		}
	}
}