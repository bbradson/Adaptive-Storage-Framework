// Copyright (c) 2023 bradson
// This Source Code Form is subject to the terms of the MIT license.
// If a copy of the license was not distributed with this file,
// You can obtain one at https://opensource.org/licenses/MIT/.

namespace AdaptiveStorage;

[PublicAPI]
#pragma warning disable CS9113
public class StorageGraphicWorker(StorageGraphic graphic, GraphicsDef def) : ITransformable<StorageRenderer>
{
	public virtual void PrintAt(SectionLayer layer, StorageRenderer renderer, in TransformData transformData)
	{
		var currentTransform = transformData;
		var parent = renderer.Parent;
		var parentAltitude = parent.def.Altitude;
		ref var yPosition = ref currentTransform.Position.y;
		var yOffset = yPosition - parentAltitude;
		var graphicDatas = graphic.graphicDatas;

		for (var i = 0; i < graphicDatas.Count; i++)
		{
			var graphicData = graphicDatas[i];
			
			yPosition = Mathf.Max(graphicData.AltitudeFor(parentAltitude) + yOffset, 0f);
			
			graphicData.GraphicColoredFor(renderer).PrintAt(layer, parent, currentTransform);
		}
	}
	
	public virtual void DrawAt(StorageRenderer renderer, in TransformData transformData)
	{
		var drawLoc = transformData.Position;
		var drawScale = transformData.Scale;
		var extraRotation = transformData.ExtraRotation.AsFloat;
		var parent = renderer.Parent;
		var rotation = parent.Rotation.Rotated(transformData.RotationDirection);
		var parentAltitude = parent.def.Altitude;
		ref var yPosition = ref drawLoc.y;
		var yOffset = yPosition - parentAltitude;
		var graphicDatas = graphic.graphicDatas;
		
		for (var i = 0; i < graphicDatas.Count; i++)
		{
			var graphicData = graphicDatas[i];

			yPosition = Mathf.Max(graphicData.AltitudeFor(parentAltitude) + yOffset, 0f);
			
			var buildingGraphic = graphicData.GraphicColoredFor(renderer);
			using (buildingGraphic.Scaled(drawScale))
				buildingGraphic.Draw(drawLoc, rotation, parent, extraRotation);
		}
	}
}
#pragma warning restore CS9113