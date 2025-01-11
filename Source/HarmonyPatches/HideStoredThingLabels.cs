// Copyright (c) 2023 bradson
// This Source Code Form is subject to the terms of the MIT license.
// If a copy of the license was not distributed with this file,
// You can obtain one at https://opensource.org/licenses/MIT/.
/*
using System.Runtime.CompilerServices;
using HarmonyLib;

namespace AdaptiveStorage.HarmonyPatches;

[HarmonyPatch(typeof(ThingWithComps), nameof(ThingWithComps.DrawGUIOverlay))]
[HarmonyPriority(Priority.High)]
public static class HideStoredThingLabels
{
	[HarmonyPrefix]
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool Prefix(ThingWithComps __instance)
		=> !__instance.IsItem()
			|| __instance.StoringThing() is not ThingClass storingThing
			|| __instance == storingThing;
}*/