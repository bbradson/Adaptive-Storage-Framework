// Copyright (c) 2023 bradson
// This Source Code Form is subject to the terms of the MIT license.
// If a copy of the license was not distributed with this file,
// You can obtain one at https://opensource.org/licenses/MIT/.

using AdaptiveStorage.ModCompatibility;

namespace AdaptiveStorage;

public static class LegacyStorageUtility
{
	public static IList<Thing> StoredThings(this Thing? thing)
	{
		if (thing is ThingClass adaptive)
			return adaptive.StoredThings;
		
		var list = SimplePool<List<Thing>>.Get();
		list.Clear();

		switch (thing)
		{
			case IThingHolder thingHolder:
			{
				AddThingsFromThingHolder(thingHolder, list);
				break;
			}
			case ISlotGroupParent slotGroupParent when thing.Spawned:
			{
				AddThingsFromSlotGroup(thing, slotGroupParent, list);
				break;
			}
		}

		var array = list.Count > 0 ? list.ToArray() : Array.Empty<Thing>();
		list.Clear();
		SimplePool<List<Thing>>.Return(list);

		return array;
	}

	private static void AddThingsFromSlotGroup(Thing thing, ISlotGroupParent slotGroupParent, List<Thing> list)
	{
		var cellsList = slotGroupParent.AllSlotCellsList();
		var map = thing.Map;
		for (var i = 0; i < cellsList.Count; i++)
		{
			var thingsAtCell = cellsList[i].GetThingListUnchecked(map);
			for (var j = 0; j < thingsAtCell.Count; j++)
			{
				var thingAtCell = thingsAtCell[j];
				if (thingAtCell.Spawned && thingAtCell.def.EverStorable(false))
					list.Add(thingAtCell);
			}
		}
	}

	private static void AddThingsFromThingHolder(IThingHolder thingHolder, List<Thing> list)
	{
		var directlyHeldThings = thingHolder.GetDirectlyHeldThings();
		for (var i = 0; i < directlyHeldThings.Count; i++)
			list.Add(directlyHeldThings.GetAt(i));
	}

	public static int TotalSlots(this Thing? thing)
		=> thing switch
		{
			ThingClass adaptive => adaptive.TotalSlots,
			ISlotGroupParent slotGroup when thing.Spawned
				=> slotGroup.AllSlotCellsList().Count
				* (LWM.Active && LWM.GetCompProperties(thing.def) is { } lwmProps
					? LWM.GetMaxStacksPerCell(lwmProps)
					: thing.def.building?.maxItemsInCell ?? 1),
			_ => 0
		};
}