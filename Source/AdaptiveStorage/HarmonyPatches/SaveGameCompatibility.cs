// Copyright (c) 2023 bradson
// This Source Code Form is subject to the terms of the MIT license.
// If a copy of the license was not distributed with this file,
// You can obtain one at https://opensource.org/licenses/MIT/.

using System.Xml;
using HarmonyLib;

namespace AdaptiveStorage.HarmonyPatches;

[HarmonyPatch(typeof(Verse.BackCompatibility), nameof(Verse.BackCompatibility.GetBackCompatibleType))]
public static class SaveGameCompatibility
{
	[HarmonyPrefix]
	public static bool GetBackCompatibleType(Type baseType, string providedClassName, XmlNode? node, ref Type? __result)
		=> node is null
			|| providedClassName is not (nameof(Building) or nameof(Building_Storage)
				or "Verse.Building" or "RimWorld.Building_Storage" or "AdaptiveStorage.ThingClass")
			|| node["def"]?.InnerText is not { } defName
			|| DefDatabase<ThingDef>.GetNamedSilentFail(defName)?.thingClass is not { } thingClass
			|| (!typeof(ThingClass).IsAssignableFrom(thingClass) && providedClassName != "AdaptiveStorage.ThingClass")
			|| (__result = thingClass) is null;
}