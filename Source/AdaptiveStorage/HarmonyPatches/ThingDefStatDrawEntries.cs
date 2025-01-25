// Copyright (c) 2025 bradson
// This Source Code Form is subject to the terms of the MIT license.
// If a copy of the license was not distributed with this file,
// You can obtain one at https://opensource.org/licenses/MIT/.

using AdaptiveStorage.Fishery;
using AdaptiveStorage.ModCompatibility;
using HarmonyLib;

namespace AdaptiveStorage.HarmonyPatches;

[HarmonyPatch(typeof(ThingDef), nameof(ThingDef.SpecialDisplayStats))]
public static class ThingDefStatDrawEntries
{
	[HarmonyPostfix]
	public static IEnumerable<StatDrawEntry> Postfix(IEnumerable<StatDrawEntry> __result, ThingDef __instance,
		StatRequest req)
		=> req.HasThing || !__instance.thingClass.IsAssignableTo(typeof(Building_Storage))
			? __result
			: AppendStorageStats(__result, __instance, req);

	public static IEnumerable<StatDrawEntry> AppendStorageStats(IEnumerable<StatDrawEntry> statDrawEntries,
		ThingDef thingDef, StatRequest statRequest)
	{
		foreach (var baseStat in statDrawEntries)
			yield return baseStat;

		var quality = statRequest.QualityCategory;
		var maxItemsInAnyCell = thingDef.MaxItemsInAnyCell(quality);

		if (!LWM.Active)
		{
			yield return new(StatCategoryDefOf.Building, Strings.TranslatedWithBackup.MaxNumStacks,
				maxItemsInAnyCell.ToStringCached(), Strings.TranslatedWithBackup.MaxNumStacksDesc, 11);
		}

		yield return new(StatCategoryDefOf.Building, Strings.Translated.ASF_TotalStorageCapacity,
			GetTotalSlotCount(thingDef, quality, maxItemsInAnyCell).ToStringCached(),
			GetStorageCapacityDescription(thingDef, quality, maxItemsInAnyCell), 11);
	}

	private static int GetTotalSlotCount(ThingDef def, QualityCategory quality, int maxItemsInAnyCell)
	{
		var size = def.size;

		if (def.TryGetMaxItemsByCell() is { } maxItemsByCell)
		{
			var qualityCopy = quality;
			var defaultMaxItemsInCell = def.DefaultMaxItemsInCell(qualityCopy);
			return maxItemsByCell.Sum(value => value.GetFor(qualityCopy) ?? defaultMaxItemsInCell);
		}
		else
		{
			return size.Area * maxItemsInAnyCell;
		}
	}

	private static string GetStorageCapacityDescription(ThingDef def, QualityCategory quality, int maxItemsInAnyCell)
	{
		var size = def.Size;
		var sizeX = size.x.ToStringCached().Named("sizeX");
		var sizeZ = size.z.ToStringCached().Named("sizeZ");

		if (def.TryGetMaxItemsByCell() is { } maxItemsByCell)
		{
			var parentQuality = quality;
			return string.Concat(Strings.Translated.ASF_StorageCapacityDescriptionCustomized.Formatted(sizeX, sizeZ),
				"\n\n",
				maxItemsByCell.GetContentString(value => (value.GetFor(parentQuality) ?? 0).ToStringCached()));
		}
		else
		{
			return Strings.Translated.ASF_StorageCapacityDescriptionStacksPerCell
				.Formatted(sizeX, sizeZ, def.MaxItemsInAnyCell().ToStringCached().Named("maxItemsInCell"));
		}
	}
}