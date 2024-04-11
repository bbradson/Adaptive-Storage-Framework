// Copyright (c) 2023 bradson
// This Source Code Form is subject to the terms of the MIT license.
// If a copy of the license was not distributed with this file,
// You can obtain one at https://opensource.org/licenses/MIT/.

namespace AdaptiveStorage;

public static class PrintUtility
{
	public static void PrintAt(this MinifiedThing minifiedThing, SectionLayer layer, in Vector3 drawLoc)
	{
		var crateFrontMaterial = minifiedThing.CrateFrontGraphic.MatSingle;

		Graphic.TryGetTextureAtlasReplacementInfo(crateFrontMaterial, TextureAtlasGroup.Item, false, false,
			out crateFrontMaterial, out var uvs, out _);
		Printer_Plane.PrintPlane(layer, drawLoc + (Altitudes.AltIncVect * 0.1f), minifiedThing.CrateFrontGraphic.drawSize,
			crateFrontMaterial, uvs: uvs);

		var rot = Rot4.South;
		var innerThingGraphic = minifiedThing.Graphic;
		if (innerThingGraphic is Graphic_Single)
			rot = Rot4.North;

		var innerThing = minifiedThing.InnerThing;
		var innerThingDef = innerThing.def;

#if !V1_4
		if (innerThingDef.overrideMinifiedRot != Rot4.Invalid)
			rot = innerThingDef.overrideMinifiedRot;
#endif

		var innerThingDrawSize = innerThingGraphic.drawSize;
		if (innerThing is ThingClass adaptive)
		{
			adaptive.PrintAt(layer, drawLoc
#if !V1_4
				+ innerThingDef.minifiedDrawOffset
#endif
				, innerThingDrawSize);
			return;
		}

		var innerThingMaterial = innerThingGraphic.MatAt(rot, minifiedThing);
		Graphic.TryGetTextureAtlasReplacementInfo(innerThingMaterial, innerThingDef.category.ToAtlasGroup(),
			false, false, out innerThingMaterial, out uvs, out _);
		Printer_Plane.PrintPlane(layer, drawLoc
#if !V1_4
			+ innerThingDef.minifiedDrawOffset
#endif
			, innerThingDrawSize, innerThingMaterial, uvs: uvs);
	}
	
	public static void PrintAt(this Graphic graphic, SectionLayer layer, Thing thing, in Vector3 drawLoc,
		float extraRotation)
	{
		var thingRotation = thing.Rotation;
		PrintThingAt(graphic, graphic.MatAt(thingRotation, thing), thing.def.category.ToAtlasGroup(),
			drawLoc + graphic.DrawOffset(thingRotation), thingRotation, layer, graphic.drawSize, extraRotation, true);
	}
	
	public static void PrintAt(this Graphic graphic, SectionLayer layer, Thing thing, in Vector3 drawLoc,
		in Vector2 drawSize, float extraRotation)
	{
		var thingRotation = thing.Rotation;
		PrintThingAt(graphic, graphic.MatAt(thingRotation, thing), thing.def.category.ToAtlasGroup(),
			drawLoc + graphic.DrawOffset(thingRotation), thingRotation, layer, drawSize, extraRotation, true);
	}
	
	public static void PrintThingAt(Thing thing, in Vector3 drawLoc, Rot4 thingRotation, SectionLayer layer,
		float drawScale, float extraRotation, bool drawShadow, in Vector2 maxDrawSize)
	{
		var graphic = thing.Graphic;
		PrintThingAt(graphic, graphic.MatAt(thingRotation, thing), thing.def.category.ToAtlasGroup(), drawLoc,
			thingRotation, layer, AdjustDrawSize(graphic.drawSize, drawScale, maxDrawSize), extraRotation,
			drawShadow);
	}

	public static void PrintThingAt(Graphic graphic, Material material, TextureAtlasGroup atlasGroup,
		in Vector3 drawLoc, Rot4 thingRotation, SectionLayer layer, Vector2 drawSize, float extraRotation,
		bool drawShadow)
	{
		var rotation = extraRotation + graphic.AngleFromRot(thingRotation);
		var flipUv = !graphic.ShouldDrawRotated;

		if (flipUv)
		{
			if (thingRotation.IsHorizontal)
				drawSize = drawSize.Rotated();
			
			flipUv = thingRotation.AsInt switch
			{
				Rot4.WestInt => graphic.WestFlipped,
				Rot4.EastInt => graphic.EastFlipped,
				_ => false
			};
		
			if (flipUv && graphic.data != null)
				rotation += graphic.data.flipExtraRotation;
		}

		PrintThingAt(layer, atlasGroup, drawLoc, drawSize, material, rotation, flipUv);

		if (drawShadow && graphic.ShadowGraphic is { } shadowGraphic)
			PrintShadowAt(shadowGraphic, drawLoc, thingRotation, layer);
	}

	public static void PrintThingAt(SectionLayer layer, TextureAtlasGroup atlasGroup, in Vector3 drawLoc,
		Vector2 drawSize, Material material, float rotation, bool flipUv)
	{
		Graphic.TryGetTextureAtlasReplacementInfo(material, atlasGroup, flipUv, true,
			out material, out var uvs, out var vertexColor);

		var colors = _colorsArray;
		Array.Fill(colors, vertexColor);
		Printer_Plane.PrintPlane(layer, drawLoc, drawSize, material, rotation, flipUv, uvs, colors);
	}

	private static Vector2 AdjustDrawSize(Vector2 drawSize, float drawScale, Vector2 maxDrawSize)
	{
		drawSize *= drawScale;

		if (drawSize.x > maxDrawSize.x)
			drawSize *= maxDrawSize.x / drawSize.x;

		if (drawSize.y > maxDrawSize.y)
			drawSize *= maxDrawSize.y / drawSize.y;

		return drawSize;
	}

	private static Color32[] _colorsArray = new Color32[4];
	
	public static void PrintShadowAt(Graphic_Shadow graphic, in Vector3 drawLoc, Rot4 thingRotation, SectionLayer layer)
		=> PrintShadowAt(graphic.shadowInfo, drawLoc, thingRotation, layer);
	
	public static void PrintShadowAt(ShadowData shadowData, in Vector3 drawLoc, Rot4 thingRotation, SectionLayer layer)
		=> Printer_Shadow.PrintShadow(layer, drawLoc
			+ (shadowData.offset + GlobalShadowPosOffset).RotatedBy(thingRotation), shadowData, thingRotation);

	public static Vector3 GlobalShadowPosOffset
		=> new(Graphic_Shadow.GlobalShadowPosOffsetX, AltitudeLayer.Shadows.AltitudeFor(),
			Graphic_Shadow.GlobalShadowPosOffsetZ);
}