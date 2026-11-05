// Written in 2024 by bradson
// To the extent possible under law, the author(s) have dedicated all
// copyright and related and neighboring rights to this software to the
// public domain worldwide. This software is distributed without any
// warranty. You should have received a copy of the CC0 Public Domain
// Dedication along with this software. If not, see
// http://creativecommons.org/publicdomain/zero/1.0/.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml;
using HarmonyLib;
using JetBrains.Annotations;
using Verse;

namespace PostInheritanceOperation;

[HarmonyPatch(typeof(LoadedModManager), nameof(LoadedModManager.ParseAndProcessXML))]
public static class XmlInheritanceResolvePostfix
{
	[HarmonyTranspiler]
	[UsedImplicitly]
	public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions,
		MethodBase targetMethod)
	{
		var codes = instructions.ToList();

		var index = codes.FindIndex(static code => code.Calls(MethodOf(XmlInheritance.Resolve)));

		if (index < 0)
		{
			Log.Error("PostInheritanceOperation failed to apply its ParseAndProcessXml patch. All xpath patches of "
				+ "this type will not work.");
			return codes;
		}

		codes.InsertRange(++index,
		[
			CodeInstruction.LoadArgument(targetMethod.GetParameters()
				.First(static parameter => parameter.ParameterType == typeof(XmlDocument)).Position),
			CodeInstruction.CallClosure(PatchQueue.ApplyPatches)
		]);
		
		return codes;
	}

	private static MethodInfo MethodOf(Delegate method) => method.Method;
}