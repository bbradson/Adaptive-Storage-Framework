// Copyright (c) 2023 bradson
// This Source Code Form is subject to the terms of the MIT license.
// If a copy of the license was not distributed with this file,
// You can obtain one at https://opensource.org/licenses/MIT/.

using HarmonyLib;

namespace AdaptiveStorage.HarmonyPatches;

[HarmonyPatch(typeof(Thing), nameof(Thing.Notify_ThingSelected))]
[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public static class AutomaticallyOpenTab
{
	[HarmonyPostfix]
	public static void Postfix(Thing __instance)
	{
		if (__instance is Building_Storage storage and not ThingClass)
			InspectTabUtility.TryOpen(storage);
	}
}