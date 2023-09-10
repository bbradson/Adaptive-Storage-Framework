// Copyright (c) 2023 bradson
// This Source Code Form is subject to the terms of the MIT license.
// If a copy of the license was not distributed with this file,
// You can obtain one at https://opensource.org/licenses/MIT/.

using HarmonyLib;

namespace AdaptiveStorage.HarmonyPatches;

[HarmonyPatch(typeof(ThingSelectionUtility), nameof(ThingSelectionUtility.MultiSelectableThingsInScreenRectDistinct))]
[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public static class PreventSelectionInRect
{
	[HarmonyPostfix]
	public static IEnumerable<Thing> Postfix(IEnumerable<Thing> __result)
	{
		foreach (var thing in __result)
		{
			if (HideStoredThingsFromSectionLayerAndOverlayDrawer.Prefix(thing))
				yield return thing;
		}
	}
}