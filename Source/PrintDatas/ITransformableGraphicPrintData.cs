// Copyright (c) 2024 bradson
// This Source Code Form is subject to the terms of the MIT license.
// If a copy of the license was not distributed with this file,
// You can obtain one at https://opensource.org/licenses/MIT/.

namespace AdaptiveStorage.PrintDatas;

public class ITransformableGraphicPrintData(ITransformable<Thing> graphic) : PrintData
{
	public ITransformable<Thing> Transformable { get; } = graphic;

	public override void DrawAt(in TransformData transformData)
	{
		var transform = transformData;
		transform.Position += DrawOffset;
		transform.Scale *= RotatedDrawScale;
		transform.CombinedRotation += RotationAngle;
		Transformable.DrawAt(Thing, transform);
	}

	public override void PrintAt(SectionLayer layer, in TransformData transformData)
	{
		var transform = transformData;
		transform.Position += DrawOffset;
		transform.Scale *= RotatedDrawScale;
		transform.CombinedRotation += RotationAngle;
		Transformable.PrintAt(layer, Thing, transform);
	}

	public new class Factory : PrintData.Factory
	{
		public override bool IsCompatibleWith(Thing thing, Graphic? graphic)
			=> graphic is ITransformable<Thing>
				&& OptimizedPrintData.IsCompatibleThing(thing);

		public override PrintData CreateFor(Thing thing, Graphic? graphic)
			=> new ITransformableGraphicPrintData((ITransformable<Thing>)graphic!);
	}
}