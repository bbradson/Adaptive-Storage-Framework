// Copyright (c) 2025 bradson
// This Source Code Form is subject to the terms of the MIT license.
// If a copy of the license was not distributed with this file,
// You can obtain one at https://opensource.org/licenses/MIT/.

using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using AdaptiveStorage.Fishery;
using HarmonyLib;

namespace AdaptiveStorage.HarmonyPatches;

[HarmonyPatch]
public static class StoreUtilityPatches
{
#if !V1_4
	[HarmonyPatch(typeof(StoreUtility), "TryFindBestBetterStoreCellForWorker")]
	public static class PreventStorageLookupFaster
	{
		/// <summary>
		/// <see cref="StoreUtility.TryFindBestBetterStoreCellForWorker"/> checks
		/// <see cref="ISlotGroup"/>.<see cref="ISlotGroup.Settings"/>.<see cref
		/// ="StorageSettings.AllowedToAccept(Thing)"/> instead of <see cref="SlotGroup"/>.<see cref="SlotGroup.parent"
		/// />.<see cref="ISlotGroupParent.Accepts"/> since 1.5. Adaptive Storage has <see cref
		/// ="ISlotGroupParent.Accepts"/> overridden and needs to do additional checks there.
		/// </summary>
		[HarmonyPrefix]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool Prefix(Thing t, ISlotGroup? slotGroup)
			=> slotGroup != null
				&& (slotGroup.Settings.owner is not ThingClass adaptive || adaptive.HasCapacityForThing(t));

		// [HarmonyTranspiler]
		// public static CodeInstructions Transpiler(CodeInstructions instructions, MethodBase method,
		// 	ILGenerator generator)
		// {
		// 	var codes = instructions.ToList();
		//
		// 	var allowedToAcceptIndex = codes.FindIndex(static code
		// 		=> code.operand is MethodInfo m
		// 		&& m.DeclaringType == typeof(StorageSettings)
		// 		&& m.Name == nameof(StorageSettings.AllowedToAccept));
		//
		// 	if (allowedToAcceptIndex < 0)
		// 		return ErrorAndReturn(codes, "failed to find a call to 'StorageSettings.AllowedToAccept'");
		//
		// 	var storageSettingsIndex = codes.FindLastIndex(0, allowedToAcceptIndex, static code
		// 		=> code.operand is MethodInfo m
		// 		&& m.DeclaringType == typeof(ISlotGroup)
		// 		&& m.Name == "get_Settings");
		//
		// 	if (storageSettingsIndex < 0)
		// 		return ErrorAndReturn(codes, "failed to find a call to 'StorageSettings.AllowedToAccept'");
		//
		// 	try
		// 	{
		// 		var ownerAllowsLabel = generator.DefineLabel();
		//
		// 		codes.InsertRange(allowedToAcceptIndex,
		// 		[
		// 			FishTranspiler.Call(OwnerAllows), FishTranspiler.IfTrue_Short(ownerAllowsLabel), FishTranspiler.Return,
		// 			FishTranspiler.Argument(method, "slotGroup").WithLabels(ownerAllowsLabel),
		// 			codes[storageSettingsIndex].Clone(), FishTranspiler.Argument(method, "t")
		// 		]);
		// 	}
		// 	catch (Exception ex)
		// 	{
		// 		Log.Error($"{ex}");
		// 	}
		// 	
		// 	// !slotGroup.Settings.AllowedToAccept(t)
		// 	// => !slotGroup.Settings.OwnerAllows(settings, t) || !slotGroup.Settings.AllowedToAccept(t)
		//
		// 	return codes;
		// }
		//
		// private static List<CodeInstruction> ErrorAndReturn(List<CodeInstruction> codes, string message)
		// {
		// 	Log.Error($"'AdaptiveStorage.HarmonyPatches.StoreUtilityPatches.TryFindBestBetterStoreCellForWorker"
		// 		+ $".Transpiler' {message}. Current instructions:\n{
		// 			string.Join("\n", codes.Select(static code => $"{code.opcode} {code.operand} {
		// 				code.labels?.ToStringSafeEnumerable()} {code.blocks?.ToStringSafeEnumerable()}"))}");
		//
		// 	return codes;
		// }
		//
		// public static bool OwnerAllows(StorageSettings settings, Thing t)
		// 	=> settings.owner is not ThingClass adaptive || adaptive.HasCapacityForThing(t);
		//
		// this was useless
	}
#endif

	[HarmonyPatch(typeof(StoreUtility), "NoStorageBlockersIn")]
	public static class FixMissingValidStackDestinationCheck
	{
		/// <summary>
		/// <see cref="StoreUtility.NoStorageBlockersIn"/> doesn't verify that items picked as stack destinations using
		/// <see cref="Thing.CanStackWith"/> are allowed by the storage building. This patch fixes that, for adaptive
		/// storage buildings.
		/// </summary>
		[HarmonyTranspiler]
		public static CodeInstructions Transpiler(CodeInstructions instructions, MethodBase method)
		{
			var codes = instructions.ToList();

			var canStackWithIndex = codes.FindIndex(static code
				=> code.operand is MethodInfo m
				&& m.DeclaringType == typeof(Thing)
				&& m.Name == nameof(Thing.CanStackWith));

			if (canStackWithIndex < 0)
				return ErrorAndReturn(codes, "failed to find a call to 'Thing.CanStackWith'");

			var stackLimitIndex = codes.FindIndex(canStackWithIndex, static code
				=> code.operand is FieldInfo f
				&& f.DeclaringType == typeof(ThingDef)
				&& f.Name == nameof(ThingDef.stackLimit));

			// Performance Fish reorders the checks to have stackLimit tested before CanStackWith.
			// IsValidStackDestination should go after whatever runs last
			if (stackLimitIndex > canStackWithIndex)
				canStackWithIndex = stackLimitIndex;

			var branchIndex = codes.FindIndex(canStackWithIndex, static code => code.Branches(out _));

			if (branchIndex < 0)
				return ErrorAndReturn(codes, "failed to find its branch target instruction");

			try
			{
				codes.InsertRange(branchIndex + 1,
				[
					FishTranspiler.FirstLocalVariable(method, typeof(Thing)),
					FishTranspiler.Call(IsValidStackDestination),
					FishTranspiler.IfFalse_Short((Label)codes[branchIndex].operand)
				]);
			}
			catch (Exception ex)
			{
				Log.Error($"{ex}");
			}
			
			return codes;
		}

		private static List<CodeInstruction> ErrorAndReturn(List<CodeInstruction> codes, string message)
		{
			Log.Error($"'AdaptiveStorage.HarmonyPatches.StoreUtilityPatches.NoStorageBlockersIn.Transpiler' "
				+ $"{message}. Current instructions:\n{
					string.Join("\n", codes.Select(static code => $"{code.opcode} {code.operand} {
						code.labels?.ToStringSafeEnumerable()} {code.blocks?.ToStringSafeEnumerable()}"))}");
			
			return codes;
		}

		public static bool IsValidStackDestination(Thing thing)
			=> thing.StoringAdaptiveStorage() is { } building && building.ContainsAndAllows(thing);
	}
}