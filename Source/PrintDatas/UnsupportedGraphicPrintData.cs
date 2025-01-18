// Copyright (c) 2024 bradson
// This Source Code Form is subject to the terms of the MIT license.
// If a copy of the license was not distributed with this file,
// You can obtain one at https://opensource.org/licenses/MIT/.

namespace AdaptiveStorage.PrintDatas;

public class UnsupportedGraphicPrintData : PrintData
{
	public override void PrintAt(SectionLayer layer, in TransformData transformData)
	{
		var transform = transformData;
		var graphic = Graphic!;
		var thing = Thing;
		var previousRotation = thing.Rotation;

		transform.Position += DrawOffset;
		transform.Scale *= RotatedDrawScale;

		if (thing.MultipleItemsPerCellDrawn())
			transform.Scale *= 1f / 0.8f;

		try
		{
			thing.Rotation = ThingRotation;
			if (graphic.data != null)
				transform.Position -= thing.DrawPos;

			using (graphic.Transformed(transform))
				graphic.Print(layer, thing, transform.CombinedRotation.AsFloat + ExtraRotation);
		}
		finally
		{
			thing.Rotation = previousRotation;
		}
	}

	public override void DrawAt(in TransformData transformData)
	{
		var transform = transformData;
		var graphic = Graphic!;
		var thing = Thing;

		if (thing.MultipleItemsPerCellDrawn())
			transform.Scale *= 1f / 0.8f;
		
		using (graphic.Scaled(transform.Scale * RotatedDrawScale))
		{
			graphic.DrawWorker(transform.Position + DrawOffset, ThingRotation, thing.def, thing,
				transform.CombinedRotation.AsFloat + ExtraRotation);
		}
	}

	public new class Factory : PrintData.Factory
	{
		public override bool IsCompatibleWith(Thing thing, Graphic? graphic, bool ignoreThingType)
			=> (ignoreThingType || OptimizedPrintData.IsCompatibleThing(thing)) && graphic != null;

		public override PrintData CreateFor(Thing thing, Graphic? graphic) => new UnsupportedGraphicPrintData();
	}
}