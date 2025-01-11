// Copyright (c) 2024 bradson
// This Source Code Form is subject to the terms of the MIT license.
// If a copy of the license was not distributed with this file,
// You can obtain one at https://opensource.org/licenses/MIT/.

namespace AdaptiveStorage.PrintDatas;

public class CorpsePrintData(Corpse corpse) : UnsupportedThingPrintData
{
	public Corpse Corpse { get; } = corpse;
	
	public override void DrawAt(in TransformData transformData)
		=> DrawAt(0, transformData, static (thing, _, drawLoc, flip) => thing.DrawNowAt(drawLoc, flip));

#if !V1_4
	public override void DynamicDrawPhaseAt(DrawPhase phase, in TransformData transformData)
		=> DrawAt(phase, transformData,
			static (thing, phase, drawLoc, flip) => thing.DynamicDrawPhaseAt(phase, drawLoc, flip));
#endif

	protected new void DrawAt<T>(T context, in TransformData transformData, Action<Thing, T, Vector3, bool> action)
	{
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
			using (graphic.Scaled(transform.Scale * DrawScale))
				action(thing, context, transform.Position + DrawOffset, flip);
		}
		finally
		{
			thing.Rotation = previousRotation;
		}
	}
	
	public new class Factory : PrintData.Factory
	{
		public override bool IsCompatibleWith(Thing thing, Graphic graphic) => thing is Corpse;

		public override PrintData CreateFor(Thing thing, Graphic graphic) => new CorpsePrintData((Corpse)thing);
	}
}