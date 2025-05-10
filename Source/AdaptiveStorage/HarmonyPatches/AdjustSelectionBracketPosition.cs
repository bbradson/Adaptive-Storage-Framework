// Copyright (c) 2025 bradson
// This Source Code Form is subject to the terms of the MIT license.
// If a copy of the license was not distributed with this file,
// You can obtain one at https://opensource.org/licenses/MIT/.

using System.Reflection;
using HarmonyLib;

namespace AdaptiveStorage.HarmonyPatches;

[HarmonyPatch]
public static class AdjustSelectionBracketPosition
{
	// Harmony normally doesn't support generics, but this method here doesn't use its generic argument either.
	// Replacing it with object changes nothing. The only type check in there is done with obj is Thing, not something
	// like typeof(T) == typeof(Thing)
	public static MethodBase TargetMethod { [HarmonyTargetMethod] get; }
		= AccessTools.DeclaredMethod(typeof(SelectionDrawerUtility),
			nameof(SelectionDrawerUtility.CalculateSelectionBracketPositionsWorld), generics: [typeof(object)]);
	
	[HarmonyPrefix]
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void Prefix(object obj, ref Vector3 worldPos)
	{
		if (obj is Thing thing
			&& thing.StoringAdaptiveStorage() is { } storageBuilding
			&& storageBuilding.Renderer?.TryGetPrintDataOf(thing) is { } printData)
		{
			worldPos += storageBuilding.DrawPos + printData.DrawOffset - thing.DrawPos;
		}
	}
}
