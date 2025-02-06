// Copyright (c) 2025 bradson
// This Source Code Form is subject to the terms of the MIT license.
// If a copy of the license was not distributed with this file,
// You can obtain one at https://opensource.org/licenses/MIT/.

using System.Reflection;
using AdaptiveStorage.Fishery;
using HarmonyLib;

namespace AdaptiveStorage.ModCompatibility;

[HarmonyPatch]
public static class SaveOurShip2
{
	/// <summary>
	/// Save Our Ship 2's DisableForShelf patch sets the result of MaxItemsInCell to 1 during ship movement. This causes
	/// all storage buildings to get stuck with a MaxItemsInCell result of 1 after ship movement, as one might expect.
	/// Here we remove this feature to make storage buildings work the way they should again.
	/// </summary>
	public static MethodInfo? TargetMethod { [HarmonyTargetMethod] get; }
		= GenTypes.GetTypeInAnyAssembly("SaveOurShip2.DisableForMoveShelf") is { } sos2patchType
			? AccessTools.DeclaredMethod(sos2patchType, "Postfix")
			: null;

	[HarmonyPrepare]
	public static bool HarmonyPrepare() => TargetMethod != null;

	[HarmonyTranspiler]
	public static CodeInstructions Transpiler(CodeInstructions instructions, MethodBase method)
	{
		yield return FishTranspiler.Argument(method, "__result");
		yield return FishTranspiler.Return;
		// unpatching would be better, but it's applied late from StaticConstructorOnStartup and that'd mean requiring
		// a load order rule between the mods, or some other unreliable workaround
	}
}