// Copyright (c) 2023 bradson
// This Source Code Form is subject to the terms of the MIT license.
// If a copy of the license was not distributed with this file,
// You can obtain one at https://opensource.org/licenses/MIT/.

using AdaptiveStorage.ModCompatibility;

namespace AdaptiveStorage;

public static class LegacyStorageUtility
{
	public static Thing[] StoredThings(this object? obj)
	{
		if (obj is ThingClass adaptive)
			return adaptive.StoredThings.ToArray();
		
		var list = SimplePool<List<Thing>>.Get();
		list.Clear();

		switch (obj)
		{
			case IThingHolder thingHolder:
			{
				AddThingsFromThingHolder(thingHolder, list);
				break;
			}
			case ISlotGroupParent slotGroupParent and not Thing { Spawned: not true }:
			{
				AddThingsFromSlotGroup(slotGroupParent, list);
				break;
			}
		}

		var array = list.Count > 0 ? list.ToArray() : Array.Empty<Thing>();
		list.Clear();
		SimplePool<List<Thing>>.Return(list);

		return array;
	}

	private static void AddThingsFromSlotGroup(ISlotGroupParent slotGroupParent, List<Thing> list)
	{
		var cellsList = slotGroupParent.AllSlotCellsList();
		var map = slotGroupParent.Map;
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
			list.Add(directlyHeldThings[i]);
	}

	public static int CurrentSlotLimit(this object? obj)
		=> obj is ThingClass adaptive ? adaptive.CurrentSlotLimit : obj.TotalSlots();

	public static int TotalSlots(this object? obj)
	{
		var thing = obj as Thing;

		if (thing != null)
		{
			if (thing is ThingClass adaptive)
				return adaptive.TotalSlots;
			else if (!thing.Spawned)
				return 0;
		}

		if (obj is not ISlotGroupParent slotGroup)
			return 0;

		return slotGroup.AllSlotCellsList().Count
			* (thing != null
				? LWM.Active && LWM.GetCompProperties(thing.def) is { } lwmProps
					? LWM.GetMaxStacksPerCell(lwmProps)
					: Math.Max(thing is Building building ? building.MaxItemsInCell : 1,
						thing.def.building?.maxItemsInCell ?? 1)
				: 1);
	}
}