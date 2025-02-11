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
			: storageGraphic.useDominantContentColor ?? graphicsDef.useDominantContentColor ? ContentColorSource.First
			: ContentColorSource.ColorOne;

	public static ContentColorSource FirstDeclaredColorTwo(this StorageGraphic storageGraphic, GraphicsDef graphicsDef)
		=> storageGraphic.colorTwoSource != ContentColorSource.Null ? storageGraphic.colorTwoSource
			: graphicsDef.colorTwoSource != ContentColorSource.Null ? graphicsDef.colorTwoSource
			: ContentColorSource.ColorTwo;

	public static int MaxColorSourceIndex(this GraphicsDef def)
		=> def.graphics is { Count: > 0 } graphics
			? graphics.Max(def, static (def, graphic)
				=> graphic.graphicDatas is { Count: > 0 } graphicDatas
					? graphicDatas.Max(static graphicData
						=> Max(graphicData.colorOneSource, graphicData.colorTwoSource))
					: Max(graphic.FirstDeclaredColorOne(def), graphic.FirstDeclaredColorTwo(def)))
			: Math.Max(Max(def.colorOneSource, def.colorTwoSource),
				def.useDominantContentColor ? (int)ContentColorSource.First : (int)ContentColorSource.ColorOne);

	private static int Max(ContentColorSource x, ContentColorSource y) => Math.Max((int)x, (int)y);
}