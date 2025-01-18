// Copyright (c) 2024 bradson
// This Source Code Form is subject to the terms of the MIT license.
// If a copy of the license was not distributed with this file,
// You can obtain one at https://opensource.org/licenses/MIT/.

namespace AdaptiveStorage.PrintDatas;

public class CorpsePrintData(Corpse corpse) : UnsupportedThingPrintData
{
	public Corpse Corpse { get; } = corpse;
	
	public override void DrawAt(in TransformData transformData)
		=> DrawAt(DrawPhase.Draw, transformData, static (corpse, _, drawLoc, flip) => corpse.DrawNowAt(drawLoc, flip));

#if !V1_4
	public override void DynamicDrawPhaseAt(DrawPhase phase, in TransformData transformData)
		=> DrawAt(phase, transformData,
			static (corpse, phase, drawLoc, flip) => corpse.DynamicDrawPhaseAt(phase, drawLoc, flip));
#endif

	protected void DrawAt(DrawPhase phase, in TransformData transformData,
		Action<Corpse, DrawPhase, Vector3, bool> action)
	{
#if doesntWork // only human pawns use cached results and only when zoomed out
		var transform = transformData;
		var thing = Corpse;
		var flip = transform.IsFlipped;
		if (flip)
			transform.Flip();

		transform.CombinedRotation += ExtraRotation;

		if (thing.MultipleItemsPerCellDrawn())
			transform.Scale *= 1f / 0.8f;

		var pawn = Corpse.InnerPawn;
		var renderer = pawn.Drawer.renderer;
		var drawLoc = transform.Position + DrawOffset;

		if (phase != DrawPhase.Draw)
		{
			renderer.DynamicDrawPhaseAt(phase, drawLoc, ThingRotation);
			return;
		}

		pawn.DrawAt(drawLoc, flip); // comps draw here

		ref var preRenderResults = ref renderer.results;

		if (!preRenderResults.valid)
		{
			renderer.EnsureGraphicsInitialized();
			renderer.DynamicDrawPhaseAt(DrawPhase.ParallelPreDraw, drawLoc, ThingRotation);
		}

		if (!preRenderResults.draw)
			return;

		if (preRenderResults.useCached
			&& GlobalTextureAtlasManager.TryGetPawnFrameSet(pawn, out var frameSet, out _))
		{
			ref var drawParameters = ref preRenderResults.parms;
			var facing = drawParameters.facing;
			var bodyPos = preRenderResults.bodyPos;

			Graphics.DrawMesh(renderer.GetBlitMeshUpdatedFrame(frameSet, facing,
					preRenderResults.showBody ? PawnDrawMode.BodyAndHead : PawnDrawMode.HeadOnly),
				Matrix4x4.TRS(bodyPos,
					Quaternion.AngleAxis(preRenderResults.bodyAngle + transform.CombinedRotation.AsFloat, Vector3.up),
					(transform.Scale * RotatedDrawScale).ToVector3()),
				renderer.OverrideMaterialIfNeeded(MaterialPool.MatFrom(new MaterialRequest(frameSet.atlas,
					ShaderDatabase.Cutout)), PawnRenderFlags.None), 0);

			PawnRenderUtility.DrawEquipmentAndApparelExtras(pawn,
				bodyPos.WithYOffset(PawnRenderUtility.AltitudeForLayer(facing == Rot4.North ? -10f : 90f)),
				facing, drawParameters.flags);

			preRenderResults = default;
		}
		else
		{
			preRenderResults.parms.matrix *= Matrix4x4.TRS(default,
				Quaternion.AngleAxis(transform.CombinedRotation.AsFloat, Vector3.up),
				transform.Scale * RotatedDrawScale);
			
			renderer.RenderPawnAt(drawLoc, ThingRotation);
		}
#else
		var transform = transformData;
		var graphic = Graphic;
		var thing = Corpse;
		var previousRotation = thing.Rotation;
		var flip = transform.IsFlipped;
		if (flip)
			transform.Flip();

		transform.CombinedRotation += RotationAngle;

		if (thing.MultipleItemsPerCellDrawn())
			transform.Scale *= 1f / 0.8f;

#if !V1_4
		transform.Position.y -= thing.InnerPawn.Drawer.SeededYOffset;
#endif

		try
		{
			thing.Rotation = ThingRotation.Rotated(transform.RotationDirection);
			using (graphic?.Scaled(transform.Scale * RotatedDrawScale))
				action(thing, phase, transform.Position + DrawOffset, flip);
		}
		finally
		{
			thing.Rotation = previousRotation;
		}
#endif
	}
	
	public new class Factory : PrintData.Factory
	{
		public override bool IsCompatibleWith(Thing thing, Graphic? graphic, bool ignoreThingType)
			=> !ignoreThingType && thing is Corpse;

		public override PrintData CreateFor(Thing thing, Graphic? graphic) => new CorpsePrintData((Corpse)thing);
	}
}