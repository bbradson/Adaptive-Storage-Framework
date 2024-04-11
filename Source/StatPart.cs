// Copyright (c) 2023 bradson
// This Source Code Form is subject to the terms of the MIT license.
// If a copy of the license was not distributed with this file,
// You can obtain one at https://opensource.org/licenses/MIT/.

using System.Diagnostics.CodeAnalysis;

namespace AdaptiveStorage;

[PublicAPI]
public class StatPart : RimWorld.StatPart
{
	public override void TransformValue(StatRequest req, ref float val)
	{
		if (!TryGetStoringStorage(req.Thing, out var storage))
			return;

		val *= GetStatFactor(storage);
		val += GetStatOffset(storage);
	}

	private static bool TryGetStoringStorage(Thing? thing, [NotNullWhen(true)] out ThingClass? storage)
		=> (storage = thing is null || !thing.IsItem() ? null : thing.StoringThing() as ThingClass) != null;

	public override string? ExplanationPart(StatRequest req)
	{
		if (!TryGetStoringStorage(req.Thing, out var storage))
		{
			return null;
		}
		else
		{
			var statWorker = parentStat.Worker;
			return $"{storage.LabelNoCount}: {
				statWorker.ValueToString(GetStatFactor(storage), false, ToStringNumberSense.Factor)} {
					statWorker.ValueToString(GetStatOffset(storage), false, ToStringNumberSense.Offset)}";
		}
	}

	private float GetStatOffset(ThingClass storage)
	{
		var extension = storage.Extension;
		return extension is null
			? 0f
			: extension.itemStatOffsetsByQuality is { } itemStatOffsetsByQuality
			&& storage.CompQuality?.Quality is { } quality
			&& itemStatOffsetsByQuality.GetStatOffsetFromList(parentStat, quality) is not 0f and var result
				? result
				: extension.itemStatOffsets?.GetStatOffsetFromList(parentStat) ?? 0f;
	}

	private float GetStatFactor(ThingClass storage)
	{
		var extension = storage.Extension;
		return extension is null
			? 1f
			: extension.itemStatFactorsByQuality is { } itemStatFactorsByQuality
			&& storage.CompQuality?.Quality is { } quality
			&& itemStatFactorsByQuality.GetStatFactorFromList(parentStat, quality) is not 1f and var result
				? result
				: extension.itemStatFactors?.GetStatFactorFromList(parentStat) ?? 1f;
	}
}