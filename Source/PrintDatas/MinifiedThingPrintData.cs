// Copyright (c) 2024 bradson
// This Source Code Form is subject to the terms of the MIT license.
// If a copy of the license was not distributed with this file,
// You can obtain one at https://opensource.org/licenses/MIT/.

namespace AdaptiveStorage.PrintDatas;

public class MinifiedThingPrintData(MinifiedThing thing) : PrintData
{
	public MinifiedThing MinifiedThing { get; } = thing;

	public override void DrawAt(in TransformData transformData)
	{
		var drawLoc = transformData.Position + DrawOffset;
		var drawScale = transformData.Scale * RotatedDrawScale;
		var extraRotation = (transformData.CombinedRotation + ExtraRotation).AsFloat;
		var thing = MinifiedThing;
		var crateFrontGraphic = thing.CrateFrontGraphic;

		using (crateFrontGraphic.Scaled(drawScale))
			crateFrontGraphic.DrawFromDef(drawLoc + (Altitudes.AltIncVect * 0.1f), Rot4.North, null, extraRotation);

		var thingGraphic = thing.Graphic;
		var rot = thingGraphic is Graphic_Single ? Rot4.North : Rot4.South;

#if !V1_4
		var innerThingDef = thing.InnerThing.def;
		if (innerThingDef.overrideMinifiedRot != Rot4.Invalid)
			rot = innerThingDef.overrideMinifiedRot;
		
		drawLoc += innerThingDef.minifiedDrawOffset - thingGraphic.DrawOffset(rot);
#endif
		
		using (thingGraphic.Scaled(drawScale))
			thingGraphic.Draw(drawLoc, rot, thing, extraRotation);
	}

	public override void PrintAt(SectionLayer layer, in TransformData transformData)
	{
		var transform = transformData;
		transform.Position += DrawOffset;
		transform.Scale *= RotatedDrawScale;
		transform.CombinedRotation += RotationAngle;
		MinifiedThing.PrintAt(layer, transform);
	}

	public static bool IsCompatibleThing(Thing thing) => CompatibleThingTypes.Contains(thing.GetType());

	public static HashSet<Type> CompatibleThingTypes { get; }
		= [..typeof(MinifiedThing).WithThingSubclassesNotOverridingPrintOrDraw()];

	public new class Factory : PrintData.Factory
	{
		public override bool IsCompatibleWith(Thing thing, Graphic? graphic)
			=> thing is MinifiedThing && IsCompatibleThing(thing);

		public override PrintData CreateFor(Thing thing, Graphic? graphic)
			=> new MinifiedThingPrintData((MinifiedThing)thing);
	}
}