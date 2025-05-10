// Copyright (c) 2023 bradson
// This Source Code Form is subject to the terms of the MIT license.
// If a copy of the license was not distributed with this file,
// You can obtain one at https://opensource.org/licenses/MIT/.

namespace AdaptiveStorage.Utility;

public static class PrintUtility
{
	public static void PrintAt(this MinifiedThing minifiedThing, SectionLayer layer, in TransformData transformData)
	{
		if (minifiedThing is ITransformable.ITransformable minifiedTransformable)
		{
			minifiedTransformable.PrintAt(layer, transformData);
			return;
		}

		PrintMinifiedThing(minifiedThing, layer, transformData);
	}

	private static void PrintMinifiedThing(MinifiedThing minifiedThing, SectionLayer layer,
		in TransformData transformData)
	{
		var drawScale = transformData.Scale;
		var drawLoc = transformData.Position;
		var minifiedThingRotation = minifiedThing.Rotation;
		minifiedThingRotation.Rotate(transformData.RotationDirection);
		
		var crateFrontGraphic = minifiedThing.CrateFrontGraphic;
		var extraRotation = transformData.ExtraRotation.AsFloat;
		var crateFrontMaterial = crateFrontGraphic.MatAt(minifiedThingRotation);

		Graphic.TryGetTextureAtlasReplacementInfo(crateFrontMaterial, TextureAtlasGroup.Item, false, false,
			out crateFrontMaterial, out var uvs, out _);
		Printer_Plane.PrintPlane(layer, drawLoc + (Altitudes.AltIncVect * 0.1f),
			drawScale * crateFrontGraphic.drawSize, crateFrontMaterial, extraRotation
			+ crateFrontGraphic.AngleFromRot(minifiedThingRotation), uvs: uvs);

		var innerThingGraphic = minifiedThing.Graphic;
		var innerThing = minifiedThing.InnerThing;
		var innerThingDef = innerThing.def;

		var rot =
#if !V1_4
			innerThingDef.overrideMinifiedRot is var overrideMinifiedRot && overrideMinifiedRot != Rot4.Invalid
				? overrideMinifiedRot
				:
#endif
				innerThingGraphic is Graphic_Single
					? Rot4.North
					: Rot4.South;

		var innerThingDrawSize = drawScale * innerThingGraphic.drawSize;
		if (innerThing is ITransformable.ITransformable transformable)
		{
			var innerThingTransform = transformData;
#if !V1_4
			innerThingTransform.Position += innerThingDef.minifiedDrawOffset;
#endif
			innerThingTransform.Scale = innerThingDrawSize / innerThing.Graphic.drawSize;
			
			innerThingTransform.Rot4 = rot;
			
			transformable.PrintAt(layer, innerThingTransform);
		}
		else
		{
			PrintThingAt(layer, innerThingDef.category.ToAtlasGroup(), drawLoc
#if !V1_4
				+ innerThingDef.minifiedDrawOffset
#endif
				, innerThingDrawSize, innerThingGraphic.MatAt(rot, innerThing),
				extraRotation + innerThingGraphic.AngleFromRot(minifiedThingRotation), false);
		}
	}

	public static Vector2 GetVisualSize(this Thing thing) => thing.def.size.ToVector2();

	public static void PrintAt(this Graphic graphic, SectionLayer layer, Thing thing, in TransformData transform)
	{
		var thingRotation = thing.Rotation;
		PrintThingAt(graphic, graphic.MatAt(thingRotation.Rotated(transform.RotationDirection), thing), layer,
			thing.def.category.ToAtlasGroup(), thingRotation, transform, true);
	}
	
	public static void PrintThingAt(Thing thing, SectionLayer layer, in TransformData transform, bool drawShadow,
		in Vector2 maxDrawSize)
	{
		var thingRotation = thing.Rotation;
		var graphic = thing.Graphic;
		var graphicDrawSize = graphic.drawSize;
		
		PrintThingAt(graphic, graphic.MatAt(thingRotation.Rotated(transform.RotationDirection), thing), layer,
			thing.def.category.ToAtlasGroup(), thingRotation,
			transform with { Scale = (transform.Scale * graphicDrawSize).Bounded(maxDrawSize) / graphicDrawSize },
			drawShadow);
	}

	public static void PrintThingAt(Graphic graphic, Material material, SectionLayer layer,
		TextureAtlasGroup atlasGroup, Rot4 thingRotation, in TransformData transform,
		bool drawShadow)
	{
		thingRotation.Rotate(transform.RotationDirection);
		var rotation = transform.ExtraRotation.AsFloat + graphic.AngleFromRot(thingRotation);
		var drawSize = graphic.drawSize * transform.Scale;
		var drawLoc = transform.Position + graphic.DrawOffset(thingRotation);
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

	private static readonly Color32[] _colorsArray = new Color32[4];
	
	public static void PrintShadowAt(Graphic_Shadow graphic, in Vector3 drawLoc, Rot4 thingRotation, SectionLayer layer)
		=> PrintShadowAt(graphic.shadowInfo, drawLoc, thingRotation, layer);
	
	public static void PrintShadowAt(ShadowData shadowData, in Vector3 drawLoc, Rot4 thingRotation, SectionLayer layer)
		=> Printer_Shadow.PrintShadow(layer, drawLoc
			+ (shadowData.offset + GlobalShadowPosOffset).RotatedBy(thingRotation), shadowData, thingRotation);

	public static Vector3 GlobalShadowPosOffset
		=> new(Graphic_Shadow.GlobalShadowPosOffsetX, AltitudeLayer.Shadows.AltitudeFor(),
			Graphic_Shadow.GlobalShadowPosOffsetZ);
}
