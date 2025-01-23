// Copyright (c) 2023 bradson
// This Source Code Form is subject to the terms of the MIT license.
// If a copy of the license was not distributed with this file,
// You can obtain one at https://opensource.org/licenses/MIT/.

using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
// ReSharper disable PossibleMultipleEnumeration

namespace AdaptiveStorage.HarmonyPatches;

[HarmonyPatch(typeof(GridsUtility), nameof(GridsUtility.GetMaxItemsAllowedInCell))]
public static class StorageLimit
{
	[HarmonyTranspiler]
	public static CodeInstructions Transpiler(CodeInstructions instructions, ILGenerator generator,
		MethodBase targetMethod)
	{
		var codes = instructions.ToList();

		var maxItemsInCellGetter
			= AccessTools.DeclaredPropertyGetter(typeof(Building), nameof(Building.MaxItemsInCell));

		if (!Assert(maxItemsInCellGetter != null, "Failed to find Building.MaxItemsInCell property"))
			return instructions;

		var cellArgument = targetMethod.GetParameters()
			.First(static parameter => parameter.ParameterType == typeof(IntVec3)).Position;

		var maxItemsInCellIndex = codes.FindIndex(code => code.Calls(maxItemsInCellGetter));

		if (!Assert(maxItemsInCellIndex >= 0, "Failed to find call to Building.MaxItemsInCell"))
			return instructions;

		var jumpLabel = generator.DefineLabel();
		
		// if (building is not ThingClass adaptive)
		codes.Insert(maxItemsInCellIndex++, Code.Dup);
		codes.Insert(maxItemsInCellIndex++, Code.Isinst[typeof(ThingClass)]);
		codes.Insert(maxItemsInCellIndex++, Code.Brtrue_S[jumpLabel]);
		
		// return building.MaxItemsInCell;

		var nextReturnIndex = maxItemsInCellIndex;
		while (++nextReturnIndex < codes.Count)
		{
			if (codes[nextReturnIndex].opcode == OpCodes.Ret)
				break;
		}
		
		if (!Assert(nextReturnIndex < codes.Count, "Failed to find ret opcode"))
			return instructions;
		
		// else { return adaptive.MaxItemsForCell(in c) }
		codes.Insert(++nextReturnIndex,
#if V1_4
			CodeInstructionEx.LoadArgument(
#else
			CodeInstruction.LoadArgument(
#endif
				cellArgument, true).WithLabels(jumpLabel));

		codes.Insert(++nextReturnIndex,
			CodeInstruction.Call(typeof(ThingClass), nameof(ThingClass.GetMaxItemsForCell)));
		
		codes.Insert(++nextReturnIndex, Code.Ret);
		
		return codes;
	}

	private static bool Assert(bool predicate, string errorText)
	{
		if (predicate)
			return true;

		Log.Error($"{errorText}\n{new StackTrace(true)}");
		return false;
	}
}