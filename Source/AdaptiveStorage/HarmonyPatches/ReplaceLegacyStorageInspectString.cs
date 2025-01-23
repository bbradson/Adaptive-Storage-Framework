// Copyright (c) 2023 bradson
// This Source Code Form is subject to the terms of the MIT license.
// If a copy of the license was not distributed with this file,
// You can obtain one at https://opensource.org/licenses/MIT/.

using HarmonyLib;

namespace AdaptiveStorage.HarmonyPatches;

[HarmonyPatch(typeof(Building_Storage), nameof(Building_Storage.GetInspectString))]
public static class ReplaceLegacyStorageInspectString
{
	[HarmonyPrefix]
	[HarmonyPriority(Priority.High)]
	public static bool Prefix(Building_Storage __instance, out string __result)
	{
		__result = InspectStringUtility.GetString(__instance);
		return false;
	}
}