// Copyright (c) 2024 bradson
// This Source Code Form is subject to the terms of the MIT license.
// If a copy of the license was not distributed with this file,
// You can obtain one at https://opensource.org/licenses/MIT/.

namespace AdaptiveStorage.PrintDatas;

public class UnsupportedThingPrintData : PrintData
{
	// public override bool SupportsPrinting => false;

	public override void PrintAt(SectionLayer layer, in TransformData transformData)
	{
		var transform = transformData;
		var graphic = Graphic;
		var thing = Thing;
		var previousRotation = thing.Rotation;

		transform.Position += DrawOffset;
		transform.Scale *= RotatedDrawScale;
		transform.CombinedRotation += RotationAngle;

		if (thing.MultipleItemsPerCellDrawn())
			transform.Scale *= 1f / 0.8f;

		try
		{
			thing.Rotation = ThingRotation.Rotated(transform.RotationDirection);
			if (graphic?.data != null)
				transform.Position -= thing.DrawPos;

			using (graphic?.Transformed(transform))
				thing.Print(layer);
		}
		finally
		{
			thing.Rotation = previousRotation;
		}
	}

	public override void DrawAt(in TransformData transformData)
		=> DrawAt(DrawPhase.Draw, transformData, static (thing, _, drawLoc, flip) => thing.DrawNowAt(drawLoc, flip));

#if !V1_4
	public override void DynamicDrawPhaseAt(DrawPhase phase, in TransformData transformData)
		=> DrawAt(phase, transformData,
			static (thing, phase, drawLoc, flip) => thing.DynamicDrawPhaseAt(phase, drawLoc, flip));
#endif

	protected void DrawAt(DrawPhase phase, in TransformData transformData, Action<Thing, DrawPhase, Vector3, bool> action)
	{
		var transform = transformData;
		var graphic = Graphic;
		var thing = Thing;
		var previousRotation = thing.Rotation;
		var flip = transform.IsFlipped;
		if (flip)
			transform.Flip();

		transform.CombinedRotation += RotationAngle;

		if (thing.MultipleItemsPerCellDrawn())
			transform.Scale *= 1f / 0.8f;

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
	}

	public new class Factory : PrintData.Factory
	{
		public override bool IsCompatibleWith(Thing thing, Graphic? graphic, bool ignoreThingType) => !ignoreThingType;

		public override PrintData CreateFor(Thing thing, Graphic? graphic) => new UnsupportedThingPrintData();
	}
}