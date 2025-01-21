// Copyright (c) 2023 bradson
// This Source Code Form is subject to the terms of the MIT license.
// If a copy of the license was not distributed with this file,
// You can obtain one at https://opensource.org/licenses/MIT/.

namespace AdaptiveStorage;

public static class ContentColorExtensions
{
	public static ContentColorSource FirstDeclaredColor(this ContentColorSource colorSource,
		StorageGraphic storageGraphic, GraphicsDef graphicsDef)
		=> colorSource != ContentColorSource.Null ? colorSource
			: storageGraphic.useDominantContentColor is var graphicColor and not ContentColorSource.Null ? graphicColor
			: graphicsDef.useDominantContentColor;
}