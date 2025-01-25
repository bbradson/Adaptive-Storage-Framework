// Copyright (c) 2023 bradson
// This Source Code Form is subject to the terms of the MIT license.
// If a copy of the license was not distributed with this file,
// You can obtain one at https://opensource.org/licenses/MIT/.

using AdaptiveStorage.ModCompatibility;

namespace AdaptiveStorage.Utility;

public static class ThingDefExtensions
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool SingleCell(this ThingDef def)
	{
		var size = def.size;
		return (size.x == 1) & (size.z == 1);
	}

	public static int MaxItemsInAnyCell(this ThingDef def)
	{
		var baseMaxItemsInCell = def.DefaultMaxItemsInCell();

		return TryGetMaxItemsByCell(def) is { } maxItemsByCell
			? maxItemsByCell.Max(value => value.Max() ?? baseMaxItemsInCell)
			: baseMaxItemsInCell;
	}

	public static int MaxItemsInAnyCell(this ThingDef def, QualityCategory quality)
		=> def.TryGetMaxItemsForQuality(quality, out var maxItemsPerCellForQuality)
			? maxItemsPerCellForQuality
			: def.MaxItemsInAnyCell();

	public static int DefaultMaxItemsInCell(this ThingDef def)
	{
		var baseMaxItemsInCell = def.building?.maxItemsInCell ?? 0;

		return LWM.Active && LWM.GetCompProperties(def) is { } lwmProps
			? Math.Max(LWM.GetMaxStacksPerCell(lwmProps), baseMaxItemsInCell)
			: baseMaxItemsInCell;
	}

	public static int DefaultMaxItemsInCell(this ThingDef def, QualityCategory quality)
		=> def.TryGetMaxItemsForQuality(quality, out var maxItemsPerCellForQuality)
			? maxItemsPerCellForQuality
			: def.DefaultMaxItemsInCell();

	private static bool TryGetMaxItemsForQuality(this ThingDef def, QualityCategory quality, out int itemsByCell)
	{
		if (def.TryGetMaxItemsPerCellByQuality() is { } maxItemsPerCellByQuality
			&& maxItemsPerCellByQuality.GetFor(quality) is { } maxItemsPerCellForQuality)
		{
			itemsByCell = maxItemsPerCellForQuality;
			return true;
		}

		itemsByCell = 0;
		return false;
	}

	public static CellTable<ValuesByQuality>? TryGetMaxItemsByCell(this ThingDef def)
		=> def.GetModExtension<Extension>()?.maxItemsByCell;

	public static ValuesByQuality? TryGetMaxItemsPerCellByQuality(this ThingDef def)
		=> def.GetModExtension<Extension>()?.maxItemsPerCellByQuality;

	public static bool HasGUIOverlay(this ThingDef def) => ThingRequestGroup.HasGUIOverlay.Includes(def);

	public static bool ShouldRealTimeDraw(this ThingDef def)
		=> def.drawerType is DrawerType.MapMeshAndRealTime or DrawerType.RealtimeOnly;

	public static Vector3 GetOffsetFromCenter(this ThingDef def, IntVec2 storageCell)
	{
		var size = def.size;
		var sizeVector = default(Vector3);
		sizeVector.x = ((size.x & 1) == 0 ? 0.5f : 0f) + (storageCell.x - (size.x >> 1));
		sizeVector.z = ((size.z & 1) == 0 ? 0.5f : 0f) + (storageCell.z - (size.z >> 1));
		sizeVector.y = 0f;

		return sizeVector;
	}
}