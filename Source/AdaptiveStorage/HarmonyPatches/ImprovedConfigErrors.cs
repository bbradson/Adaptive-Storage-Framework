// Copyright (c) 2023 bradson
// This Source Code Form is subject to the terms of the MIT license.
// If a copy of the license was not distributed with this file,
// You can obtain one at https://opensource.org/licenses/MIT/.

using HarmonyLib;

namespace AdaptiveStorage.HarmonyPatches;

[HarmonyPatch(typeof(ThingDef), nameof(ThingDef.ConfigErrors))]
public static class ImprovedConfigErrors
{
	[HarmonyPostfix]
	public static void Postfix(ref IEnumerable<string> __result, ThingDef __instance)
	{
		if (__instance.category != ThingCategory.Building
			|| !typeof(Building_Storage).IsAssignableFrom(__instance.thingClass))
		{
			return;
		}

		if (__instance.passability == Traversability.Impassable && (__instance.size.x | __instance.size.z) > 1)
		{
			Log.Warning($"Storage building of def '{__instance}{(__instance.modContentPack?.Name is { } modName
				? $"' from mod '{modName}" : "")}' is impassable, but has a size greater than 1. This causes issues "
				+ $"with pathfinding when having any blocked tile. Storage buildings should be either 1x1 or be "
				+ $"passable on all cells.");
		}
	}
}