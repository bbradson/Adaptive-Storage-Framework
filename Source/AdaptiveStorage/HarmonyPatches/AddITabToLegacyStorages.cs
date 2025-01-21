// Copyright (c) 2023 bradson
// This Source Code Form is subject to the terms of the MIT license.
// If a copy of the license was not distributed with this file,
// You can obtain one at https://opensource.org/licenses/MIT/.

using HarmonyLib;

namespace AdaptiveStorage.HarmonyPatches;

[HarmonyPatch(typeof(Thing), nameof(Thing.GetInspectTabs))]
public static class AddITabToLegacyStorages
{
	[HarmonyPostfix]
	public static IEnumerable<InspectTabBase> Postfix(IEnumerable<InspectTabBase> __result, Thing __instance)
		=> __instance is ISlotGroupParent and not ThingClass
			? InspectTabUtility.Modify(__result)
			: __result;
}