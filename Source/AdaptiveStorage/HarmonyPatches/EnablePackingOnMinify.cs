// Copyright (c) 2023 bradson
// This Source Code Form is subject to the terms of the MIT license.
// If a copy of the license was not distributed with this file,
// You can obtain one at https://opensource.org/licenses/MIT/.

using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using AdaptiveStorage.Fishery;
using HarmonyLib;
// ReSharper disable PossibleMultipleEnumeration

namespace AdaptiveStorage.HarmonyPatches;

[HarmonyPatch(typeof(MinifyUtility),
#if V1_4
	nameof(MinifyUtility.MakeMinified)
#else
	nameof(MinifyUtility.MakeMinified_NewTemp)
#endif
)]
public static class EnablePackingOnMinify
{
	[HarmonyTranspiler]
	public static CodeInstructions Transpiler(CodeInstructions instructions, ILGenerator generator,
		MethodBase targetMethod)
	{
		try
		{
			var codes = instructions.ToList();

			var deSpawnOrDeselectMethod
				= AccessTools.DeclaredMethod(typeof(Thing), nameof(Thing.DeSpawnOrDeselect));

			if (!Assert(deSpawnOrDeselectMethod != null, "Failed to find Thing.DeSpawnOrDeselect method"))
				return instructions;

			var deSpawnOrDeselectIndex = codes.FindIndex(code => code.Calls(deSpawnOrDeselectMethod));

			if (!Assert(deSpawnOrDeselectIndex >= 0, "Failed to find call to Thing.DeSpawnOrDeselect"))
				return instructions;

			var minifyOrDeselectLabel = generator.DefineLabel();
			var nextInstructionLabel = generator.DefineLabel();

			// if (thing is not ThingClass adaptive)
			codes.Insert(deSpawnOrDeselectIndex++, FishTranspiler.FirstArgument(targetMethod, typeof(Thing)));
			codes.Insert(deSpawnOrDeselectIndex++, FishTranspiler.IsInstance<ThingClass>());
			codes.Insert(deSpawnOrDeselectIndex++, FishTranspiler.IfTrue_Short(minifyOrDeselectLabel));

			// thing.DeSpawnOrDeselect(mode);

			var newInstructionIndex = deSpawnOrDeselectIndex + 1;

			// else { MinifyOrDeselect(adaptive, mode) }
			codes.Insert(newInstructionIndex++, FishTranspiler.GoTo_Short(nextInstructionLabel));
			codes.Insert(newInstructionIndex++,
				FishTranspiler.Call(MinifyOrDeselect).WithLabels(minifyOrDeselectLabel));
		
			codes[newInstructionIndex].labels.Add(nextInstructionLabel);

			return codes;
		}
		catch (Exception ex)
		{
			Log.Error($"{ex}");
			return instructions;
		}
	}

	private static bool Assert(bool predicate, string errorText)
	{
		if (predicate)
			return true;

		Log.Error($"{errorText}\n{new StackTrace(true)}");
		return false;
	}
	
	public static bool MinifyOrDeselect(ThingClass building, DestroyMode mode = DestroyMode.Vanish)
	{
		var selector = Find.Selector;
		var isSelected = Current.ProgramState == ProgramState.Playing && selector.IsSelected(building);
		
		if (building.Spawned)
		{
			building.Minify(mode);
		}
		else if (isSelected)
		{
			selector.Deselect(building);
			Find.MainButtonsRoot.tabs.Notify_SelectedObjectDespawned();
		}

		return isSelected;
	}
}