// Copyright (c) 2023 bradson
// This Source Code Form is subject to the terms of the MIT license.
// If a copy of the license was not distributed with this file,
// You can obtain one at https://opensource.org/licenses/MIT/.

using AdaptiveStorage.Fishery.Utility.Diagnostics;

namespace AdaptiveStorage.Utility;

public readonly record struct GraphicTransformScope : IDisposable
{
	private readonly Graphic _graphic;

	private readonly Vector2 _previousDrawSize;

	private readonly Vector3?
		_previousDrawOffsetNorth,
		_previousDrawOffsetEast,
		_previousDrawOffsetSouth,
		_previousDrawOffsetWest;

	private readonly Vector3 _previousDrawOffset;

	public GraphicTransformScope(Graphic graphic, in TransformData transform)
	{
		Guard.IsNotNull(graphic);
		Guard.IsNotNull(transform);
		
		_graphic = graphic;
		ref var currentDrawSize = ref graphic.drawSize;
		_previousDrawSize = currentDrawSize;
		currentDrawSize *= transform.Scale;

		var graphicData = graphic.data;
		if (graphicData is null)
			return; // ThingStyleDefs can have Graphics with null GraphicData

		var transformDrawOffset = transform.Position;
		
		ref var currentDirectionalDrawOffset = ref graphicData.drawOffsetNorth;
		_previousDrawOffsetNorth = currentDirectionalDrawOffset;
		currentDirectionalDrawOffset += transformDrawOffset;

		currentDirectionalDrawOffset = ref graphicData.drawOffsetEast;
		_previousDrawOffsetEast = currentDirectionalDrawOffset;
		currentDirectionalDrawOffset += transformDrawOffset;

		currentDirectionalDrawOffset = ref graphicData.drawOffsetSouth;
		_previousDrawOffsetSouth = currentDirectionalDrawOffset;
		currentDirectionalDrawOffset += transformDrawOffset;

		currentDirectionalDrawOffset = ref graphicData.drawOffsetWest;
		_previousDrawOffsetWest = currentDirectionalDrawOffset;
		currentDirectionalDrawOffset += transformDrawOffset;

		ref var currentDrawOffset = ref graphicData.drawOffset;
		_previousDrawOffset = currentDrawOffset;
		currentDrawOffset += transformDrawOffset;
	}

	public void Dispose()
	{
		_graphic.drawSize = _previousDrawSize;

		var graphicData = _graphic.data;
		if (graphicData is null)
			return; // ThingStyleDefs can have Graphics with null GraphicData
		
		graphicData.drawOffsetNorth = _previousDrawOffsetNorth;
		graphicData.drawOffsetEast = _previousDrawOffsetEast;
		graphicData.drawOffsetSouth = _previousDrawOffsetSouth;
		graphicData.drawOffsetWest = _previousDrawOffsetWest;
		graphicData.drawOffset = _previousDrawOffset;
	}
}

public readonly record struct GraphicScaleScope : IDisposable
{
	private readonly Graphic _graphic;

	private readonly Vector2 _previousValue;

	public GraphicScaleScope(Graphic graphic, Vector2 drawScale)
	{
		_graphic = graphic;
		ref var currentDrawSize = ref graphic.drawSize;
		_previousValue = currentDrawSize;
		currentDrawSize *= drawScale;
	}

	public void Dispose() => _graphic.drawSize = _previousValue;
}