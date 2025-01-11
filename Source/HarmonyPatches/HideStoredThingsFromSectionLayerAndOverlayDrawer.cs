// Copyright (c) 2023 bradson
// This Source Code Form is subject to the terms of the MIT license.
// If a copy of the license was not distributed with this file,
// You can obtain one at https://opensource.org/licenses/MIT/.

using System.Reflection;
using HarmonyLib;

namespace AdaptiveStorage.HarmonyPatches;

[HarmonyPatch]
[HarmonyPriority(Priority.High)]
public static class HideStoredThingsFromSectionLayerAndOverlayDrawer
{
	public static IEnumerable<MethodBase> TargetMethods { [HarmonyTargetMethods] get; }
		= new[]
		{
			AccessTools.Method(typeof(SectionLayer_ThingsGeneral), nameof(SectionLayer_ThingsGeneral.TakePrintFrom)),
			AccessTools.Method(typeof(OverlayDrawer), nameof(OverlayDrawer.RenderForbiddenOverlay))
		};

	[HarmonyPrefix]
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool Prefix(Thing t) => t.StoringAdaptiveStorage() is null;
}