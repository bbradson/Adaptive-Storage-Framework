// Copyright (c) 2023 bradson
// This Source Code Form is subject to the terms of the MIT license.
// If a copy of the license was not distributed with this file,
// You can obtain one at https://opensource.org/licenses/MIT/.

#if V1_4
using System.Reflection.Emit;
using HarmonyLib;

namespace AdaptiveStorage.BackCompatibility;

public static class CodeInstructionEx
{
	/// <summary>Creates a CodeInstruction loading an argument with the given index, using the shorter forms when possible</summary>
	/// <param name="index">The index of the argument</param>
	/// <param name="useAddress">Use address of argument</param>
	/// <returns></returns>
	public static CodeInstruction LoadArgument(int index, bool useAddress = false)
		=> useAddress
			? index < 256 ? new(OpCodes.Ldarga_S, Convert.ToByte(index)) : new(OpCodes.Ldarga, index)
			: index switch
			{
				0 => new(OpCodes.Ldarg_0),
				1 => new(OpCodes.Ldarg_1),
				2 => new(OpCodes.Ldarg_2),
				3 => new(OpCodes.Ldarg_3),
				< 256 => new(OpCodes.Ldarg_S, Convert.ToByte(index)),
				_ => new(OpCodes.Ldarg, index)
			};
}
#endif