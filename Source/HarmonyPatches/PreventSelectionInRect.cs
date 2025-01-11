// Copyright (c) 2023 bradson
// This Source Code Form is subject to the terms of the MIT license.
// If a copy of the license was not distributed with this file,
// You can obtain one at https://opensource.org/licenses/MIT/.

using System.Linq;
using HarmonyLib;

namespace AdaptiveStorage.HarmonyPatches;

[HarmonyPatch(typeof(ThingSelectionUtility), nameof(ThingSelectionUtility.MultiSelectableThingsInScreenRectDistinct))]
public static class PreventSelectionInRect
{
	[HarmonyPostfix]
	public static void Postfix(ref IEnumerable<Thing> __result)
	{
		var currentEvent = Event.current;
		if (currentEvent.rawType == EventType.MouseUp && currentEvent.button == 0)
			__result = __result.Where(HideStoredThingsFromSectionLayerAndOverlayDrawer.Prefix);
	}
}