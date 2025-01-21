// Copyright (c) 2023 bradson
// This Source Code Form is subject to the terms of the MIT license.
// If a copy of the license was not distributed with this file,
// You can obtain one at https://opensource.org/licenses/MIT/.

using AdaptiveStorage.ModCompatibility;

namespace AdaptiveStorage.Utility;

public static class ItemRenderControls
{
	public static void DisableItemDrawing(this Thing item, Map map) => map.dynamicDrawManager.DeRegisterDrawable(item);
	
	public static void RestoreItemDrawing(this Thing item, Map map) => map.dynamicDrawManager.RegisterDrawable(item);
	
	public static void DisableItemGUIOverlay(this Thing item, Map map)
	{
		var lister = map.listerThings;
		
		if (!PerformanceFish.RemoveFromGroupList(lister, item, ThingRequestGroup.HasGUIOverlay))
			lister.ThingsInGroup(ThingRequestGroup.HasGUIOverlay).Remove(item);
	}

	public static void RestoreItemGUIOverlay(this Thing item, Map map)
	{
		var lister = map.listerThings;
		var guiOverlayGroup = lister.ThingsInGroup(ThingRequestGroup.HasGUIOverlay);

		if (guiOverlayGroup.Contains(item))
			return;

		if (!PerformanceFish.AddToGroupList(lister, item, ThingRequestGroup.HasGUIOverlay))
			guiOverlayGroup.Add(item);
	}

	public static void DisableItemRendering(this Thing item, Map? map = null)
	{
		map ??= item.TryGetMap();
		if (map is null || !map.spawnedThings.Contains(item))
			return;
		
		var itemDef = item.def;
		
		if (itemDef.ShouldRealTimeDraw())
			item.DisableItemDrawing(map);
		
		if (itemDef.HasGUIOverlay())
			item.DisableItemGUIOverlay(map);
	}

	public static void RestoreItemRendering(this Thing item, Map? map = null)
	{
		map ??= item.TryGetMap();
		if (map is null || !map.spawnedThings.Contains(item))
			return;
		
		var itemDef = item.def;
		
		if (itemDef.ShouldRealTimeDraw())
			item.RestoreItemDrawing(map);
		
		if (itemDef.HasGUIOverlay())
			item.RestoreItemGUIOverlay(map);
	}
}