// Copyright (c) 2023 bradson
// This Source Code Form is subject to the terms of the MIT license.
// If a copy of the license was not distributed with this file,
// You can obtain one at https://opensource.org/licenses/MIT/.

namespace AdaptiveStorage;

[PublicAPI]
public class Minified : MinifiedThing
{
	protected override Graphic LoadCrateFrontGraphic()
		=> StyleDef is { Graphic: not null } styleDef
			? styleDef.graphicData?.GraphicColoredFor(this) ?? styleDef.Graphic
			: def.graphicData?.GraphicColoredFor(this) ?? base.LoadCrateFrontGraphic();

#if V1_4
	public
#else
	protected
#endif
		override void DrawAt(Vector3 drawLoc, bool flip = false)
	{
		CrateFrontGraphic.DrawFromDef(drawLoc + (Altitudes.AltIncVect * 0.1f), Rot4.North, null);

		var innerThing = InnerThing;
		innerThing.Rotation = Rotation;
		innerThing.Position = Position;
		
		innerThing.DrawNowAt(drawLoc
#if !V1_4
			+ innerThing.def.minifiedDrawOffset
#endif
			, flip);

// 		var innerThingDef = innerThing.def;
// 		var rot = GetRot4ForInnerThing(innerThingDef);
// 		
// 		Graphic.Draw(drawLoc
// #if !V1_4
// 			+ innerThingDef.minifiedDrawOffset
// #endif
// 			+ Graphic.DrawOffset(rot), rot, this);
	}

	public override void Print(SectionLayer layer)
	{
		var drawPos = DrawPos;
		var frontGraphic = CrateFrontGraphic;
		
		PrintUtility.PrintThingAt(layer, TextureAtlasGroup.Item, drawPos + (Altitudes.AltIncVect * 0.1f),
			frontGraphic.drawSize, frontGraphic.MatSingle, 0f, false);

		var innerThing = InnerThing;
		innerThing.Rotation = Rotation;
		innerThing.Position = Position;
		
		if (innerThing is IPrintable adaptive)
		{
			adaptive.PrintAt(layer, drawPos
#if !V1_4
				+ innerThing.def.minifiedDrawOffset
#endif
			);
		}
		else
		{
			innerThing.Print(layer);
		}

		// 		var innerThingDef = innerThing.def;
// 		PrintUtility.PrintThingAt(layer, innerThingDef.category.ToAtlasGroup(), drawPos
// #if !V1_4
// 			+ innerThingDef.minifiedDrawOffset
// #endif
// 			, Graphic.drawSize, Graphic.MatAt(GetRot4ForInnerThing(innerThingDef), this), 0f, false);
	}

// 	private Rot4 GetRot4ForInnerThing(ThingDef innerThingDef)
// 		=>
// #if !V1_4
// 			innerThingDef.overrideMinifiedRot != Rot4.Invalid
// 			? innerThingDef.overrideMinifiedRot
// 			:
// #endif
// 			Graphic is Graphic_Single
// 				? Rot4.North
// 				: Rot4.South;

	// public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
	// {
	// 	var spawned = Spawned;
	// 	var map = Map;
	// 	base.Destroy(mode);
	// 	if (InnerThing == null)
	// 		return;
	//
	// 	InstallBlueprintUtility.CancelBlueprintsFor(this);
	// 	if (!spawned | ((mode != DestroyMode.Deconstruct) & (mode != DestroyMode.KillFinalize)))
	// 		return;
	//
	// 	if (mode == DestroyMode.Deconstruct)
	// 		SoundDefOf.Building_Deconstructed.PlayOneShot(new TargetInfo(Position, map));
	// 		
	// 	GenLeaving.DoLeavingsFor(InnerThing, map, mode, this.OccupiedRect());
	// }
}