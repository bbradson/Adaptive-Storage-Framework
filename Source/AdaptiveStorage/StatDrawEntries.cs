// Copyright (c) 2023 bradson
// This Source Code Form is subject to the terms of the MIT license.
// If a copy of the license was not distributed with this file,
// You can obtain one at https://opensource.org/licenses/MIT/.

namespace AdaptiveStorage;

public class StatDrawEntries(ThingClass parent)
{
	public readonly StatDrawEntry
		ItemsPerCell = new(StatCategoryDefOf.Building, Strings.TranslatedWithBackup.MaxNumStacks,
			parent.MaxItemsInCell.ToStringCached(), Strings.TranslatedWithBackup.MaxNumStacksDesc, 11),
		StorageCapacity = new(StatCategoryDefOf.Building, Strings.Translated.ASF_TotalStorageCapacity,
			parent.TotalSlots.ToStringCached(), GetStorageCapacityDescription(parent), 11);

	private static string GetStorageCapacityDescription(ThingClass parent)
	{
		var size = parent.Size;
		var sizeX = size.x.ToStringCached().Named("sizeX");
		var sizeZ = size.z.ToStringCached().Named("sizeZ");

		if (parent.Extension?.maxItemsByCell is { } maxItemsByCell)
		{
			var parentQuality = parent.QualityCategory;
			return string.Concat(Strings.Translated.ASF_StorageCapacityDescriptionCustomized.Formatted(sizeX, sizeZ),
				"\n\n",
				maxItemsByCell.GetContentString(value => (value.GetFor(parentQuality) ?? 0).ToStringCached()));
		}
		else
		{
			return Strings.Translated.ASF_StorageCapacityDescriptionStacksPerCell
				.Formatted(sizeX, sizeZ, parent.MaxItemsInCell.ToStringCached().Named("maxItemsInCell"));
		}
	}
}