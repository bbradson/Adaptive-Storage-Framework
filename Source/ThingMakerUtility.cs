// Copyright (c) 2023 bradson
// This Source Code Form is subject to the terms of the MIT license.
// If a copy of the license was not distributed with this file,
// You can obtain one at https://opensource.org/licenses/MIT/.

namespace AdaptiveStorage;

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
}