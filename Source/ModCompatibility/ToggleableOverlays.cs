// Copyright (c) 2023 bradson
// This Source Code Form is subject to the terms of the MIT license.
// If a copy of the license was not distributed with this file,
// You can obtain one at https://opensource.org/licenses/MIT/.

using System.Reflection;
using HarmonyLib;

namespace AdaptiveStorage.ModCompatibility;

public static class ToggleableOverlays
{
	public static bool Active => ActiveMods.ToggleableOverlays;

	private static readonly MethodInfo? _checkMouseOverMethod
		= Active
			? AccessTools.DeclaredMethod("ToggleableOverlays.ToggleableOverlaysUtility:CheckMouseOver",
				[typeof(Thing), typeof(bool), typeof(bool)])
			?? throw new MissingMethodException(
				"ToggleableOverlays.ToggleableOverlaysUtility:CheckMouseOver(Thing,bool,bool)")
			: null;

	private static readonly FieldInfo? _hideStorageBuildingField
		= Active
			? AccessTools.DeclaredField("ToggleableOverlays.ModSettings_ToggleableOverlays:hideStorageBuilding")
			?? throw new MissingFieldException("ToggleableOverlays.ModSettings_ToggleableOverlays:hideStorageBuilding")
			: null;

	private static readonly Func<Thing, bool, bool, bool>? _checkMouseOverFunc
		= Active
			? (Func<Thing, bool, bool, bool>)_checkMouseOverMethod!.CreateDelegate(typeof(Func<Thing, bool, bool, bool>))
			: null;

	private static readonly AccessTools.FieldRef<bool>? _hideStorageBuildingsFunc
		= Active
			? AccessTools.StaticFieldRefAccess<bool>(_hideStorageBuildingField)
			: null;

	// https://github.com/Owlchemist/toggleable-overlays/blob/master/Source/Patch_Labels.cs#L109
	public static bool CheckMouseOver(Thing thing, bool sizeOne = false)
		=> _checkMouseOverFunc is null
			|| _checkMouseOverFunc(thing, _hideStorageBuildingsFunc?.Invoke() ?? true, sizeOne);
}