// Copyright (c) 2023 bradson
// This Source Code Form is subject to the terms of the MIT license.
// If a copy of the license was not distributed with this file,
// You can obtain one at https://opensource.org/licenses/MIT/.

namespace AdaptiveStorage.Utility;

public static class ThingMakerUtility
{
	public static Thing MakeCorpse(ThingDef thingDef)
	{
		var thing = PawnGenerator.GeneratePawn(
			(thingDef.ingestible?.sourceDef ?? ThingDef.Named(thingDef.defName["Corpse_".Length..]))
			.race.AnyPawnKind);
		
		thing.Kill(null);
		return thing.Corpse;
	}

	public static Thing MakeThings(ThingDef thingDef, int count = int.MaxValue, ThingDef? stuff = null)
	{
		var thing = ThingMaker.MakeThing(thingDef, stuff ?? GenStuff.DefaultStuffFor(thingDef));
		thing.stackCount = Math.Min(count, thingDef.stackLimit);
		return thing;
	}

	public static Thing Make(ThingDef thingDef, int count = int.MaxValue, ThingDef? stuff = null)
		=> thingDef.IsCorpse ? MakeCorpse(thingDef) : MakeThings(thingDef, count, stuff);

	// https://github.com/lilwhitemouse/RimWorld-LWM.DeepStorage/blob/master/DeepStorage/Deep_Storage_ITab.cs#L216-L235
	public static Thing? EjectFromStorage(Thing building, Thing item, int count = int.MaxValue, bool forbid = true,
		in IntVec3 dropOffset = default)
	{
		var map = building.TryGetMap();
		if (map is null)
			return null;
		
		var dropCell = building.Position + dropOffset;
		item = item.SplitOff(Math.Min(count, item.stackCount));

		if (!GenDrop.TryDropSpawn(item, dropCell, map, ThingPlaceMode.Near, out var resultingThing, null,
			cell => !map.ContainsStorageBuildingAt(cell)))
		{
			GenSpawn.Spawn(resultingThing = item, dropCell, map);
		}

		if (forbid && resultingThing.TryGetComp<CompForbiddable>() is { } compForbiddable)
			compForbiddable.Forbidden = true;

		if (resultingThing is null || !resultingThing.Spawned || resultingThing.Position == dropCell)
			Messages.Message(Strings.Translated.ASF_MapFilled, new(dropCell, map), MessageTypeDefOf.NegativeEvent, false);

		return resultingThing;
	}
}