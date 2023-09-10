// Copyright (c) 2023 bradson
// This Source Code Form is subject to the terms of the MIT license.
// If a copy of the license was not distributed with this file,
// You can obtain one at https://opensource.org/licenses/MIT/.

namespace AdaptiveStorage;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class StorageGraphic
{
	public GraphicData? graphicData;

	public List<GraphicData> graphicDatas = new();

	public bool?
		showContainedItems,
		useDominantContentColor;

	public int minimumStackCount;

	public void Initialize()
	{
		if (graphicData != null)
			graphicDatas.AddDistinct(graphicData);
	}
}