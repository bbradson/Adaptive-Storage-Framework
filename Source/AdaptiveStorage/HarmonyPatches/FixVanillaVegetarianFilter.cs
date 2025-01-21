// Copyright (c) 2024 bradson
// This Source Code Form is subject to the terms of the MIT license.
// If a copy of the license was not distributed with this file,
// You can obtain one at https://opensource.org/licenses/MIT/.

using HarmonyLib;

namespace AdaptiveStorage.HarmonyPatches;

[HarmonyPatch(typeof(SpecialThingFilterWorker_Vegetarian), nameof(SpecialThingFilterWorker_Vegetarian.Matches))]
public static class FixVanillaVegetarianFilter
{
	[HarmonyPrefix]
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool Prefix(Thing t) => t.def.IsIngestible;
}