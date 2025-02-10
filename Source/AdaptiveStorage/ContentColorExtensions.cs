// Copyright (c) 2023 bradson
// This Source Code Form is subject to the terms of the MIT license.
// If a copy of the license was not distributed with this file,
// You can obtain one at https://opensource.org/licenses/MIT/.

namespace AdaptiveStorage;

public static class ContentColorExtensions
{
	public static ContentColorSource FirstDeclaredColorOne(this StorageGraphic storageGraphic, GraphicsDef graphicsDef)
		=> storageGraphic.colorOneSource != ContentColorSource.Null ? storageGraphic.colorOneSource
			: graphicsDef.colorOneSource != ContentColorSource.Null ? graphicsDef.colorOneSource
			: ContentColorSource.ColorOne.FromDominantContentColor(storageGraphic, graphicsDef);

	public static ContentColorSource FirstDeclaredColorTwo(this StorageGraphic storageGraphic, GraphicsDef graphicsDef)
		=> storageGraphic.colorTwoSource != ContentColorSource.Null ? storageGraphic.colorTwoSource
			: graphicsDef.colorTwoSource != ContentColorSource.Null ? graphicsDef.colorTwoSource
			: ContentColorSource.ColorTwo.FromDominantContentColor(storageGraphic, graphicsDef);

	private static ContentColorSource FromDominantContentColor(this ContentColorSource colorSource,
		StorageGraphic storageGraphic, GraphicsDef graphicsDef)
		=> colorSource.FromDominantContentColor(storageGraphic.useDominantContentColor != ContentColorSource.Null
				? storageGraphic.useDominantContentColor
				: graphicsDef.useDominantContentColor);

	private static ContentColorSource FromDominantContentColor(this ContentColorSource colorSource,
		ContentColorSource useDominantContentColor)
		=> useDominantContentColor switch
		{
			ContentColorSource.ColorOne or ContentColorSource.First => colorSource == ContentColorSource.ColorOne
				? ContentColorSource.First
				: colorSource,
			ContentColorSource.ColorTwo or ContentColorSource.Second => colorSource == ContentColorSource.ColorTwo
				? ContentColorSource.First
				: colorSource,
			ContentColorSource.Null or ContentColorSource.False => colorSource,
			_ => useDominantContentColor
		};

	public static int MaxColorSourceIndex(this GraphicsDef def)
		=> def.graphics is { Count: > 0 } graphics
			? graphics.Max(def, static (def, graphic)
				=> graphic.graphicDatas is { Count: > 0 } graphicDatas
					? graphicDatas.Max(static graphicData
						=> Max(graphicData.colorOneSource, graphicData.colorTwoSource))
					: Max(graphic.FirstDeclaredColorOne(def), graphic.FirstDeclaredColorTwo(def)))
			: Math.Max(Max(def.colorOneSource, def.colorTwoSource),
				Max(ContentColorSource.ColorOne.FromDominantContentColor(def.useDominantContentColor),
					ContentColorSource.ColorTwo.FromDominantContentColor(def.useDominantContentColor)));

	private static int Max(ContentColorSource x, ContentColorSource y) => Math.Max((int)x, (int)y);
}