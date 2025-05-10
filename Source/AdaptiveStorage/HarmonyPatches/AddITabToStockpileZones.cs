// Copyright (c) 2023 bradson
// This Source Code Form is subject to the terms of the MIT license.
// If a copy of the license was not distributed with this file,
// You can obtain one at https://opensource.org/licenses/MIT/.

using HarmonyLib;

namespace AdaptiveStorage.HarmonyPatches;

[HarmonyPatch(typeof(Zone_Stockpile), nameof(Zone_Stockpile.GetInspectTabs))]
public static class AddITabToStockpileZones
{
	[HarmonyPostfix]
	public static IEnumerable<InspectTabBase>? Postfix(IEnumerable<InspectTabBase>? __result, Zone_Stockpile __instance)
		=> AdaptiveStorageFrameworkSettings.ContentsTab is ContentsITab
			? InspectTabUtility.Modify(__result)
			: __result;
}
