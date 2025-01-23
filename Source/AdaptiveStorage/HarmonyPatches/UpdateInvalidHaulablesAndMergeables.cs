// Copyright (c) 2023 bradson
// This Source Code Form is subject to the terms of the MIT license.
// If a copy of the license was not distributed with this file,
// You can obtain one at https://opensource.org/licenses/MIT/.

using HarmonyLib;

namespace AdaptiveStorage.HarmonyPatches;

/// <summary>
/// See <see cref="DeregisteredAtThingGridEvent.TryNotifyStorage"/>
/// </summary>
[HarmonyPatch(typeof(FilthMonitor), nameof(FilthMonitor.FilthMonitorTick))]
public static class UpdateInvalidHaulablesAndMergeables
{
	public static Queue<Thing> ThingsNeedingChecking { get; } = [];

	[HarmonyPostfix]
	public static void Postfix()
	{
		while (ThingsNeedingChecking.TryDequeue(out var thing))
		{
			if (thing.Destroyed)
				continue;
			
			var map = thing.TryGetMap();
			if (map is null)
				continue;

			map.listerHaulables.Check(thing);
			map.listerMergeables.Check(thing);
		}
	}
}