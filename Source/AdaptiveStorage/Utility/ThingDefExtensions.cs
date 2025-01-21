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
		var baseMaxItemsInCell = def.building?.maxItemsInCell ?? 0;

		if (LWM.Active && LWM.GetCompProperties(def) is { } lwmProps)
			baseMaxItemsInCell = Math.Max(LWM.GetMaxStacksPerCell(lwmProps), baseMaxItemsInCell);

		return def.GetModExtension<Extension>()?.maxItemsByCell is { } maxItemsByCell
			? maxItemsByCell.Max(value => value.Max() ?? baseMaxItemsInCell)
			: baseMaxItemsInCell;
	}
	
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