// Copyright (c) 2023 bradson
// This Source Code Form is subject to the terms of the MIT license.
// If a copy of the license was not distributed with this file,
// You can obtain one at https://opensource.org/licenses/MIT/.

using System.Runtime.CompilerServices;

namespace AdaptiveStorage;

public static class StoreUtilityExtensions
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsItem(this Thing thing) => thing.def.category == ThingCategory.Item;

	public static bool ContainsStorageBuildingAt(this Map map, in IntVec3 cell)
		=> cell.GetSlotGroup(map) is { parent: Building_Storage };
	
	public static List<Thing> GetThingListUnchecked(this in IntVec3 cell, Map map)
		=> map.thingGrid.ThingsListAtFast(map.cellIndices.CellToIndex(cell.x, cell.z));

	public static List<Thing> GetThingListUnchecked(this IntVec2 cell, Map map)
		=> map.thingGrid.ThingsListAtFast(map.cellIndices.CellToIndex(cell.x, cell.z));
}