// Copyright (c) 2023 bradson
// This Source Code Form is subject to the terms of the MIT license.
// If a copy of the license was not distributed with this file,
// You can obtain one at https://opensource.org/licenses/MIT/.

using HarmonyLib;

namespace AdaptiveStorage.HarmonyPatches;

[HarmonyPatch(typeof(ThingFilter), nameof(ThingFilter.ExposeData))]
public static class RemoveInvalidDefsOnStorageSettingsLoading
{
	[HarmonyPostfix]
	public static void Postfix(ThingFilter __instance)
	{
		(__instance.allowedDefs ??= []).RemoveWhere(static def => def is null);
		(__instance.disallowedSpecialFilters ??= []).RemoveAll(static def => def is null);
	}
}