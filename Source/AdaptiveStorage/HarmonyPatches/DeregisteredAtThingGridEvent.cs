// Copyright (c) 2023 bradson
// This Source Code Form is subject to the terms of the MIT license.
// If a copy of the license was not distributed with this file,
// You can obtain one at https://opensource.org/licenses/MIT/.

using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

namespace AdaptiveStorage.HarmonyPatches;

[HarmonyPatch(typeof(ThingGrid), nameof(ThingGrid.DeregisterInCell))]
public static class DeregisteredAtThingGridEvent
{
	[HarmonyTranspiler]
	public static CodeInstructions Transpiler(CodeInstructions instructions, MethodBase method)
	{
		var codes = instructions.ToList();

		codes.InsertRange(codes.FindIndex(static code
				=> code.opcode == OpCodes.Callvirt
				&& code.operand is MethodInfo { Name: "Remove" } method
				&& method.DeclaringType!.Name.Contains("List"))
			+ 1,
			[Code.Ldarg_1, Code.Ldarga_S[2], Code.Call[((Delegate)TryNotifyStorage).Method]]);

		return codes;
	}

	public static void TryNotifyStorage(Thing thing, in IntVec3 cell)
	{
		if (!thing.IsItem())
			return;

		thing.StoringAdaptiveStorage()?.Notify_ItemDeregisteredAtCell(thing, in cell);

		var map = thing.Map;
		
		// only ever possible when other mods directly change the position of still spawned things. Doing so without
		// notifying storage buildings, listerHaulables and listerMergeables causes various subtle vanilla issues, like
		// items not getting seen for merging and not getting hauled into higher priority storage. The checks here
		// should cover those, though it would be wonderful if mods were to just correctly despawn and spawn items.
		if (map.spawnedThings.Contains(thing))
		{
			// Spawned is always true here, spawnedThings returns false when within thing.DeSpawn, which handles
			// everything correctly
			UpdateInvalidHaulablesAndMergeables.ThingsNeedingChecking.Enqueue(thing);
		}
	}
}