// Copyright (c) 2023 bradson
// This Source Code Form is subject to the terms of the MIT license.
// If a copy of the license was not distributed with this file,
// You can obtain one at https://opensource.org/licenses/MIT/.

using System.Diagnostics.CodeAnalysis;
#if !V1_4
using AdaptiveStorage.BackCompatibility;
#endif

// ReSharper disable InconsistentNaming

namespace AdaptiveStorage;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class StorageGraphicData : GraphicData
{
	public ContentColorSource
		colorOneSource = ContentColorSource.Null,
		colorTwoSource = ContentColorSource.Null;

	protected AltitudeLayer altitudeLayer = (AltitudeLayer)byte.MaxValue;

	public virtual Graphic GraphicColoredFor(StorageRenderer storageRenderer)
		=> this.GraphicColoredFor(ColorOneFor(storageRenderer), ColorTwoFor(storageRenderer));

	public bool TryGetAltitude(out float altitude)
	{
		if (altitudeLayer != (AltitudeLayer)byte.MaxValue)
		{
			altitude = altitudeLayer.AltitudeFor();
			return true;
		}

		altitude = default;
		return false;
	}

	public Color ColorOneFor(StorageRenderer storageRenderer)
		=> storageRenderer.GetColorFromSource(colorOneSource) * color;

	public Color ColorTwoFor(StorageRenderer storageRenderer)
		=> storageRenderer.GetColorFromSource(colorTwoSource) * colorTwo;

	private static Dictionary<GraphicData, StorageGraphicData> _database = [];

	[return: NotNullIfNotNull(nameof(graphicData))]
	public static StorageGraphicData? GetOrMakeFor(GraphicData? graphicData)
	{
		if (graphicData is null)
			return null;
		
		if (graphicData is not StorageGraphicData storageGraphicData
			&& !_database.TryGetValue(graphicData, out storageGraphicData))
		{
			_database.Add(graphicData, storageGraphicData = new());
			GenEx.MemberwiseShallowCopy(graphicData, storageGraphicData);
		}
		
		return storageGraphicData;
	}

	public static StorageGraphicData? TryGetFor(GraphicData? graphicData)
	{
		if (graphicData is null)
			return null;

		if (graphicData is not StorageGraphicData storageGraphicData)
			_database.TryGetValue(graphicData, out storageGraphicData);

		return storageGraphicData;
	}

	public virtual void Resolve(StorageGraphic storageGraphic, GraphicsDef graphicsDef)
	{
		if (!InColorSourceRange(colorOneSource))
		{
			colorOneSource = ContentColorSource.Null;
			WarnForInvalidColorSource(graphicsDef, nameof(colorOneSource));
		}
		
		if (!InColorSourceRange(colorTwoSource))
		{
			colorTwoSource = ContentColorSource.Null;
			WarnForInvalidColorSource(graphicsDef, nameof(colorTwoSource));
		}

		if (colorOneSource == ContentColorSource.Null)
			colorOneSource = storageGraphic.FirstDeclaredColorOne(graphicsDef);

		SetWhiteIfNullOrFalse(ref colorOneSource);
		
		if (colorTwoSource == ContentColorSource.Null)
			colorTwoSource = storageGraphic.FirstDeclaredColorTwo(graphicsDef);

		SetWhiteIfNullOrFalse(ref colorTwoSource);
	}

	private static void SetWhiteIfNullOrFalse(ref ContentColorSource colorSource)
	{
		if (colorSource is ContentColorSource.Null or ContentColorSource.False)
			colorSource = ContentColorSource.White;
	}

	private static bool InColorSourceRange(ContentColorSource colorSource)
		=> ((int)colorSource <= 255) & ((int)colorSource >= -3);

	private static void WarnForInvalidColorSource(Def parent, string fieldName)
		=> Log.Warning($"Invalid {fieldName} in {parent.GetType().FullName} named '{parent.defName}' from mod '{
			parent.modContentPack?.Name}'. Setting to default.");
}