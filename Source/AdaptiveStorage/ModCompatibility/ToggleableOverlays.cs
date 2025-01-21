// Copyright (c) 2023 bradson
// This Source Code Form is subject to the terms of the MIT license.
// If a copy of the license was not distributed with this file,
// You can obtain one at https://opensource.org/licenses/MIT/.

using HarmonyLib;

namespace AdaptiveStorage.ModCompatibility;

public static class ToggleableOverlays
{
	public static bool Active => ActiveMods.ToggleableOverlays;
	
	private static readonly Func<Thing, bool, bool, bool>? _checkMouseOverFunc
		= Active
			? ReflectionUtility.CreateDelegate<Func<Thing, bool, bool, bool>>(
				"ToggleableOverlays.ToggleableOverlaysUtility:CheckMouseOver")
			: null;

	private static readonly AccessTools.FieldRef<bool>? _hideStorageBuildingsFunc
		= Active
			? ReflectionUtility.StaticFieldRefAccess<bool>(
				"ToggleableOverlays.ModSettings_ToggleableOverlays:hideStorageBuilding")
			: null;

	// https://github.com/Owlchemist/toggleable-overlays/blob/master/Source/Patch_Labels.cs#L109
	public static bool CheckMouseOver(ThingClass thing)
		=> _checkMouseOverFunc?.Invoke(thing, _hideStorageBuildingsFunc?.Invoke() ?? true, false)
			?? (!AdaptiveStorageFrameworkSettings.HideLabelsUntilMouseOver || thing.HasMouseOver);
}