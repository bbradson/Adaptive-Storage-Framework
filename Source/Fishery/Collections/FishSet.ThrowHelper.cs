// Copyright (c) 2022 bradson
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v.2.0.If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using System.Diagnostics.CodeAnalysis;
using AdaptiveStorage.Fishery.Pools;

namespace AdaptiveStorage.Fishery.Collections;

public partial class IntFishSet
{
	private static class ThrowHelper
	{
		[DoesNotReturn]
		internal static void ThrowEmptyBucketIsTailInvalidOperationException(int bucketIndex, int bucket)
			=> throw new InvalidOperationException(
				$"Bucket marked as empty was attached as tail to a key. This should never happen. Index: '{
					bucketIndex}', value: '{bucket}'");
		
		[DoesNotReturn]
		internal static void ThrowFailedToFindParentInvalidOperationException(IntFishSet fishSet, int childBucketIndex)
		{
			using var pooledStringBuilder = new PooledStringBuilder(227);
			var stringBuilder = pooledStringBuilder.Builder;

			pooledStringBuilder
				.Append("Failed to find parent index in IntFishSet for entry: ");
			
			Debug.AppendEntryDescription(fishSet, stringBuilder, childBucketIndex);
			
			stringBuilder
				.Append(", count: '")
				.Append(fishSet._count)
				.Append("', bucket array length: '")
				.Append(fishSet._buckets.Length)
				.Append("', total tailing entries count: '")
				.Append(fishSet.GetTailingEntriesCount())
				.Append("', known chain of tails:");

			var ancestorIndex = fishSet.GetBucketIndexForKeyAt(childBucketIndex);
			while (true)
			{
				pooledStringBuilder.Append('\n');
				Debug.AppendEntryDescription(fishSet, stringBuilder, ancestorIndex);

				if (!fishSet.TryContinueWithTail(ref ancestorIndex))
					break;
			}

			pooledStringBuilder.Append("\n\nFull dump:");
			Debug.AppendFullDump(fishSet, stringBuilder);

			throw new InvalidOperationException(pooledStringBuilder.ToString());
		}
	}
}