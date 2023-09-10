// Copyright (c) 2023 bradson
// This Source Code Form is subject to the terms of the MIT license.
// If a copy of the license was not distributed with this file,
// You can obtain one at https://opensource.org/licenses/MIT/.

using HarmonyLib;

namespace AdaptiveStorage.ModCompatibility;

public static class LWM
{
	public static bool Active => CompPropertiesType != null;
	
	public static readonly Type? CompPropertiesType = AccessTools.TypeByName("LWM.DeepStorage.Properties");
	
	public static readonly AccessTools.FieldRef<CompProperties, int>?
		MaxNumberStacks = TryGetField("maxNumberStacks"),
		MinNumberStacks = TryGetField("minNumberStacks");

	private static AccessTools.FieldRef<CompProperties, int>? TryGetField(string name)
		=> CompPropertiesType is { } propsType
			? AccessTools.FieldRefAccess<CompProperties, int>(AccessTools.Field(propsType, name))
			: null;

	public static CompProperties? GetCompProperties(ThingDef thingDef)
		=> thingDef.comps?.Find(static props => props != null && props.GetType() == CompPropertiesType);

	public static int GetMaxStacksPerCell(CompProperties compProperties)
		=> Math.Max(MinNumberStacks!(compProperties), MaxNumberStacks!(compProperties));
}