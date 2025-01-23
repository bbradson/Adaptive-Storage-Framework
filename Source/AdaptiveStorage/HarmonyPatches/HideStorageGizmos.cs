// Copyright (c) 2023 bradson
// This Source Code Form is subject to the terms of the MIT license.
// If a copy of the license was not distributed with this file,
// You can obtain one at https://opensource.org/licenses/MIT/.

using HarmonyLib;

namespace AdaptiveStorage.HarmonyPatches;

[HarmonyPatch(typeof(Building_Storage), nameof(Building_Storage.GetGizmos))]
public static class HideStorageGizmos
{
	[HarmonyPostfix]
	public static IEnumerable<Gizmo> Postfix(IEnumerable<Gizmo> __result, Building_Storage __instance)
		=> __instance is ThingClass
			? __result
			: GizmoUtility.Filter(__result);
}