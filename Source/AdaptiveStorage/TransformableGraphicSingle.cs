// Copyright (c) 2024 bradson
// This Source Code Form is subject to the terms of the MIT license.
// If a copy of the license was not distributed with this file,
// You can obtain one at https://opensource.org/licenses/MIT/.

namespace AdaptiveStorage;

public class TransformableGraphicSingle : Graphic_Single, ITransformable<Thing>
{
	public sealed override Mesh MeshAt(Rot4 rot) => MeshAt(rot, drawSize);

	public virtual Mesh MeshAt(Rot4 rot, Vector2 size)
	{
		if (rot.IsHorizontal && !ShouldDrawRotated)
			size = size.Rotated();

		var flip = size.IsFlipped();
		if (flip)
			size = size.Flip();

		if ((rot == Rot4.West && WestFlipped) || (rot == Rot4.East && EastFlipped))
			flip = !flip;
		
		return flip ? MeshPool.GridPlaneFlip(size) : MeshPool.GridPlane(size);
	}
	
	public sealed override void DrawWorker(Vector3 loc, Rot4 rot, ThingDef thingDef, Thing? thing, float extraRotation)
	{
		if (thing is null)
			base.DrawWorker(loc, rot, thingDef, thing, extraRotation);
		else
			DrawAt(thing, new(loc, Vector2.one, RotationAngle.FromRot4(rot) + extraRotation));
	}

	public virtual void DrawAt(Thing thing, in TransformData transformData)
	{
		var rot = transformData.Rot4;
		var extraRotation = transformData.ExtraRotation.AsFloat;
		var loc = transformData.Position;

		var mesh = MeshAt(rot, drawSize * transformData.Scale); // <-- new
		var quat = QuatFromRot(rot);
		
		if (extraRotation != 0f)
			quat *= Quaternion.Euler(Vector3.up * extraRotation);
		
		loc += DrawOffset(rot);
		DrawMeshInt(mesh, loc, quat, MatAt(rot, thing));

		ShadowGraphic?.DrawWorker(loc, rot, thing.def, thing, extraRotation);
	}

	public sealed override void Print(SectionLayer layer, Thing thing, float extraRotation)
		=> PrintAt(layer, thing, new(RotationAngle.FromRot4(thing.Rotation) + extraRotation));

	public virtual void PrintAt(SectionLayer layer, Thing thing, in TransformData transformData)
	{
		var thingRotation = transformData.Rot4; // <-- new
		var extraRotation = transformData.ExtraRotation.AsFloat;
		var center = transformData.Position; // <-- new

		var flipUv = false;
		var size = drawSize * transformData.Scale; // <-- new
		if (size.IsFlipped())
		{
			size = size.Flip();
			flipUv = !flipUv;
		}
		
		if (!ShouldDrawRotated)
		{
			if (thingRotation.IsHorizontal)
				size = drawSize.Rotated();

			if ((thingRotation == Rot4.West && WestFlipped) || (thingRotation == Rot4.East && EastFlipped))
				flipUv = !flipUv;
		}

		if (thing.MultipleItemsPerCellDrawn())
			size *= 0.8f;
		
		var rot = AngleFromRot(thingRotation) + extraRotation;
		if (flipUv && data != null)
			rot += data.flipExtraRotation;
		
		var material = MatAt(thingRotation, thing);

		TryGetTextureAtlasReplacementInfo(material, thing.def.category.ToAtlasGroup(), flipUv, true,
			out material, out var uvs, out var vertexColor);
		
		Printer_Plane.PrintPlane(layer, center, size, material, rot, flipUv, uvs,
			[vertexColor, vertexColor, vertexColor, vertexColor]);

		ShadowGraphic?.Print(layer, thing, 0.0f);
	}

	public override Graphic GetColoredVersion(Shader newShader, Color newColor, Color newColorTwo)
		=> GraphicDatabase.Get<TransformableGraphicSingle>(path, newShader, drawSize, newColor, newColorTwo, data);
}