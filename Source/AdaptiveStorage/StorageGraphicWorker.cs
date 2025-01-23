// Copyright (c) 2023 bradson
// This Source Code Form is subject to the terms of the MIT license.
// If a copy of the license was not distributed with this file,
// You can obtain one at https://opensource.org/licenses/MIT/.

using AdaptiveStorage.PrintDatas;

namespace AdaptiveStorage;

[PublicAPI]
#pragma warning disable CS9113
public class StorageGraphicWorker(StorageGraphic graphic, GraphicsDef def) : ITransformable<PrintData>
{
	public virtual Graphic GetGraphicFor(StorageGraphicData graphicData, StorageRenderer renderer)
		=> graphicData.GraphicColoredFor(renderer);

	public virtual void UpdatePrintData(PrintData printData, StorageGraphicData? graphicData)
	{
		var parent = printData.Thing;
		printData.ThingRotation = parent.Rotation;
		
		if (graphicData != null && graphicData.TryGetAltitude(out var altitude))
			printData.DrawOffset = new(0f, altitude - parent.def.Altitude, 0f);
	}

	public virtual void PrintAt(SectionLayer layer, PrintData printData, in TransformData transformData)
		=> printData.PrintAt(layer, transformData);

	public virtual void DrawAt(PrintData printData, in TransformData transformData) => printData.DrawAt(transformData);
}
#pragma warning restore CS9113