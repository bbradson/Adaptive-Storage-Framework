// Copyright (c) 2023 bradson
// This Source Code Form is subject to the terms of the MIT license.
// If a copy of the license was not distributed with this file,
// You can obtain one at https://opensource.org/licenses/MIT/.

using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

namespace AdaptiveStorage.HarmonyPatches;

[HarmonyPatch(typeof(ThingGrid), nameof(ThingGrid.RegisterInCell))]
public static class RegisteredAtThingGridEvent
{
	[HarmonyTranspiler]
	public static CodeInstructions Transpiler(CodeInstructions instructions, MethodBase method)
	{
		var codes = instructions.ToList();

		codes.InsertRange(codes.FindIndex(static code
				=> code.opcode == OpCodes.Callvirt
				&& code.operand is MethodInfo { Name: "Add" } method
				&& method.DeclaringType!.Name.Contains("List"))
			+ 1,
			[Code.Ldarg_1, Code.Ldarga_S[2], Code.Call[((Delegate)TryNotifyStorage).Method]]);

		return codes;
	}

	public static void TryNotifyStorage(Thing thing, in IntVec3 cell)
	{
		if (thing.IsItem())
			thing.StoringAdaptiveStorage()?.Notify_ItemRegisteredAtCell(thing, in cell);
	}
}