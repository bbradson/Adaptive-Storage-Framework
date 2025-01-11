// Copyright (c) 2023 bradson
// This Source Code Form is subject to the terms of the MIT license.
// If a copy of the license was not distributed with this file,
// You can obtain one at https://opensource.org/licenses/MIT/.

namespace AdaptiveStorage;

[PublicAPI]
public class Minified : MinifiedThing, ITransformable
{
	public Vector2 VisualSize { get; private set; }
	
	public MinifiedExtension? Extension { get; private set; }

	public override Graphic Graphic => cachedGraphic ??= LoadInnerThingGraphic();

	protected virtual Graphic LoadInnerThingGraphic()
	{
		var innerThing = InnerThing;
		var innerThingDef = innerThing.def;
		var innerThingSize = innerThingDef.size;
		var graphic = innerThing.Graphic.ExtractInnerGraphicFor(innerThing);
		var graphicDrawSize = graphic.drawSize;
		
		if (innerThingSize.Max() > 1)
		{
			var minifiedDrawSize = innerThingSize.ToVector2().Bounded(VisualSize); // VisualSize instead of hardcoded 1
			
			minifiedDrawSize.x /= innerThingSize.x;
			minifiedDrawSize.x *= graphicDrawSize.x;
			
			minifiedDrawSize.y /= innerThingSize.z;
			minifiedDrawSize.y *= graphicDrawSize.y;
			
			graphic = graphic.GetCopy(minifiedDrawSize, null);
		}

		var minifiedDrawScale = innerThingDef.minifiedDrawScale;
		if (Math.Abs(minifiedDrawScale - 1f) > 1.0001f)
			graphic = graphic.GetCopy(graphicDrawSize * minifiedDrawScale, null);

		return graphic;
	}

	protected override Graphic LoadCrateFrontGraphic()
		=> StyleDef is { Graphic: not null } styleDef
			? styleDef.graphicData?.GraphicColoredFor(this) ?? styleDef.Graphic
			: def.graphicData?.GraphicColoredFor(this) ?? base.LoadCrateFrontGraphic();

#if V1_4
	public
#else
	protected
#endif
		sealed override void DrawAt(Vector3 drawLoc, bool flip = false)
		=> DrawAt(new(drawLoc, flip ? Vector2.one.Flip() : Vector2.one));

	public virtual void DrawAt(in TransformData transformData)
	{
		var onlyRealTimeDrawing = !Spawned || def.drawerType == DrawerType.RealtimeOnly;
		var innerThing = InnerThing;
		var innerThingRealTimeDrawing = onlyRealTimeDrawing || innerThing.def.ShouldRealTimeDraw();

		if (!innerThingRealTimeDrawing)
			return;

		if (onlyRealTimeDrawing)
			DrawFrontGraphic(transformData);

		DrawInnerThing(innerThing, transformData);
	}

	protected virtual void DrawFrontGraphic(in TransformData transformData)
	{
		var frontGraphic = CrateFrontGraphic;
		using (frontGraphic.Scaled(transformData.Scale))
		{
			frontGraphic.DrawFromDef(transformData.Position + (Altitudes.AltIncVect * 0.1f), Rotation, null,
				transformData.CombinedRotation.AsFloat);
		}
	}

	protected virtual void DrawInnerThing(Thing innerThing, in TransformData transformData)
	{
		var innerThingRotation = GetRotationForInnerThing(innerThing);
		innerThing.Rotation = innerThingRotation;
		innerThing.Position = Position;

		if (innerThing is ITransformable scalable)
		{
			DrawScalableInnerThing(innerThing, transformData, scalable);
		}
		else
		{
			DrawRegularInnerThing(innerThing, transformData.Position, transformData.Scale,
				transformData.CombinedRotation.AsFloat, innerThingRotation);
		}
	}

	private void DrawScalableInnerThing(Thing innerThing, TransformData transformData,
		ITransformable transformable)
	{
#if !V1_4
		transformData.Position += innerThing.def.minifiedDrawOffset;
#endif
		transformData.Scale *= innerThing.GetVisualSize().Bounded(VisualSize);
		
		transformable.DrawAt(transformData);
	}

	private void DrawRegularInnerThing(Thing innerThing, in Vector3 drawLoc, Vector2 drawScale, float extraRotation,
		Rot4 innerThingRotation)
	{
		var thingGraphic = Graphic;
		using (thingGraphic.Scaled(drawScale))
		{
			thingGraphic.Draw(drawLoc
#if !V1_4
				+ innerThing.def.minifiedDrawOffset
#endif
				+ thingGraphic.DrawOffset(innerThingRotation), innerThingRotation, innerThing);
		}
	}

	public sealed override void Print(SectionLayer layer) => PrintAt(layer, new(DrawPos));

	public virtual void PrintAt(SectionLayer layer, in TransformData transformData)
	{
		PrintFrontGraphic(layer, transformData);

		var innerThing = InnerThing;
		var innerThingDef = innerThing.def;
		if (def.drawerType != DrawerType.MapMeshOnly
			&& innerThingDef.drawerType is not (DrawerType.MapMeshOnly or DrawerType.MapMeshAndRealTime))
		{
			return;
		}

		PrintInnerThing(layer, innerThing, transformData, innerThingDef);
	}

	protected void PrintFrontGraphic(SectionLayer layer, in TransformData transformData)
	{
		var frontGraphic = CrateFrontGraphic;
		var rotation = Rotation.Rotated(transformData.RotationDirection);

		PrintUtility.PrintThingAt(layer, TextureAtlasGroup.Item, transformData.Position + (Altitudes.AltIncVect * 0.1f),
			frontGraphic.drawSize * transformData.Scale, frontGraphic.MatAt(rotation),
			transformData.ExtraRotation.AsFloat + frontGraphic.AngleFromRot(rotation), false);
	}

	protected void PrintInnerThing(SectionLayer layer, Thing innerThing, TransformData transformData,
		ThingDef innerThingDef)
	{
		var innerThingRotation = GetRotationForInnerThing(innerThing);
		innerThing.Rotation = innerThingRotation;
		innerThing.Position = Position;
		
#if !V1_4
		transformData.Position += innerThingDef.minifiedDrawOffset;
#endif

		var innerThingSize = innerThing.GetVisualSize();
		transformData.Scale *= innerThingSize.Bounded(VisualSize) / innerThingSize;

		if (innerThing is ITransformable scalable)
		{
			scalable.PrintAt(layer, transformData);
		}
		else
		{
			var innerThingGraphic = Graphic;
			PrintUtility.PrintThingAt(layer, innerThingDef.category.ToAtlasGroup(), transformData.Position
				, innerThingGraphic.drawSize * transformData.Scale,
				innerThingGraphic.MatAt(innerThingRotation, innerThing),
				transformData.CombinedRotation.AsFloat, false);
		}
	}

	protected Rot4 GetRotationForInnerThing(Thing innerThing)
		=> 
#if !V1_4
			innerThing.def.overrideMinifiedRot is var overrideMinifiedRot && overrideMinifiedRot != Rot4.Invalid
			? overrideMinifiedRot
			:
#endif
			def.size is var size && size.x == size.z && Graphic is Graphic_Single
				? Rot4.North
				: Rotation;

	protected virtual void PostInitialize()
	{
		Extension = def.GetModExtension<MinifiedExtension>();
		VisualSize = this.GetVisualSize();
	}

	public override void PostMake()
	{
		base.PostMake();
		PostInitialize();
	}

	public override void ExposeData()
	{
		base.ExposeData();
		PostInitialize();
	}
}