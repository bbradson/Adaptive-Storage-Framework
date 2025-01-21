// Copyright (c) 2023 bradson
// This Source Code Form is subject to the terms of the MIT license.
// If a copy of the license was not distributed with this file,
// You can obtain one at https://opensource.org/licenses/MIT/.

using HarmonyLib;

namespace AdaptiveStorage.HarmonyPatches;

[HarmonyPatch(typeof(ListerMergeables), nameof(ListerMergeables.Notify_ThingStackChanged))]
public static class NotifyItemStackChanged
{
	[HarmonyPostfix]
	public static void Postfix(ListerMergeables __instance, Thing t)
		=> t.StoringAdaptiveStorage()?.Notify_ItemStackChanged(t);
}