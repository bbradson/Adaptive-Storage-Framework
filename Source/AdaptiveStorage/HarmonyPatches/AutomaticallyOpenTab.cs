// Copyright (c) 2023 bradson
// This Source Code Form is subject to the terms of the MIT license.
// If a copy of the license was not distributed with this file,
// You can obtain one at https://opensource.org/licenses/MIT/.

using HarmonyLib;

namespace AdaptiveStorage.HarmonyPatches;

[HarmonyPatch(typeof(SelectionDrawer), nameof(SelectionDrawer.Notify_Selected))]
public static class AutomaticallyOpenTab
{
	[HarmonyPostfix]
	public static void Postfix(object t)
	{
		if (t is IStoreSettingsParent and (not ThingClass) and ISelectable selectable)
			InspectTabUtility.TryOpen(selectable);
	}
}