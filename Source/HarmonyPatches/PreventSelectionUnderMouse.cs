// Copyright (c) 2023 bradson
// This Source Code Form is subject to the terms of the MIT license.
// If a copy of the license was not distributed with this file,
// You can obtain one at https://opensource.org/licenses/MIT/.

using HarmonyLib;

namespace AdaptiveStorage.HarmonyPatches;

[HarmonyPatch(typeof(Selector), nameof(Selector.SelectableObjectsUnderMouse))]
[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public static class PreventSelectionUnderMouse
{
	[HarmonyPostfix]
	public static IEnumerable<object> Postfix(IEnumerable<object> __result)
	{
		foreach (var obj in __result)
		{
			if (obj is not Thing thing || HideStoredThingsFromSectionLayerAndOverlayDrawer.Prefix(thing))
				yield return obj;
		}
	}
}