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
// ReSharper disable PossibleMultipleEnumeration

namespace SaveGameCompatibility;

[HarmonyPatch(typeof(Thing), nameof(Thing.ExposeData))]
public static class ExposeDataPatch
{
	[HarmonyTranspiler]
	[UsedImplicitly]
	public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions,
		MethodBase targetMethod)
	{
		var codes = instructions.ToList();

		var beforeScribeDefIndex = codes.FindIndex(static code => code.operand is "def");
		if (beforeScribeDefIndex < 0)
		{
			LogFailure();
			return instructions;
		}

		codes.InsertRange(beforeScribeDefIndex,
			[CodeInstruction.LoadArgument(0), CodeInstruction.CallClosure(ScribeDefPrefix)]);

		var afterScribeDefIndex = codes.FindIndex(beforeScribeDefIndex, static code
			=> code.operand is MethodInfo { Name: nameof(Scribe_Defs.Look) }) + 1;
		
		if (afterScribeDefIndex < 0)
		{
			LogFailure();
			return instructions;
		}

		codes.InsertRange(afterScribeDefIndex,
			[CodeInstruction.LoadArgument(0), CodeInstruction.CallClosure(ScribeDefPostfix)]);
		
		return codes;
	}

	private static void LogFailure()
		=> Log.Error("SaveGameCompatibility.Operation failed to apply its Thing.ExposeData patch. All xpath patches "
			+ "of this type will not work.");

	public static void ScribeDefPrefix(Thing thing)
	{
		try
		{
			if (!LoadingVars || !PatchDatabase.Any)
				return;

			var xmlParent = CurXmlParent;
			var defNode = xmlParent["def"];
			var defName = defNode?.InnerText;
			var stuffNode = xmlParent["stuff"];
			var stuffName = stuffNode?.InnerText;
			var typeNode = xmlParent.Attributes?["Class"];
			var typeName = typeNode?.InnerText;
			
			if (!typeName.NullOrEmpty() && PatchDatabase.PatchesForType(typeName!) is { Count: > 0 } typePatches)
			{
				for (var i = 0; i < typePatches.Count; i++)
				{
					var patch = typePatches[i];
					if (patch.previousDefName.NullOrEmpty()
						&& patch.previousStuff.NullOrEmpty()
						&& !patch.newStuff.NullOrEmpty())
					{
						ApplyStuffChange(patch, xmlParent, stuffNode,
							DefDatabase<ThingDef>.GetNamedSilentFail(defName));
					}
				}
			}

			if (!defName.NullOrEmpty() && PatchDatabase.PatchesForDef(defName!) is { Count: > 0 } defPatches)
			{
				for (var i = 0; i < defPatches.Count; i++)
				{
					var patch = defPatches[i];
					if (patch.previousStuff.NullOrEmpty() || patch.previousStuff == stuffName)
						ApplyPatchToXmlNodePreDefAssignment(patch, xmlParent, defName, defNode, stuffNode);
					
					// before def assignment means having to modify the xmlNode
				}
			}

			if (!stuffName.NullOrEmpty() && PatchDatabase.PatchesForStuff(stuffName!) is { Count: > 0 } stuffPatches)
			{
				for (var i = 0; i < stuffPatches.Count; i++)
				{
					var patch = stuffPatches[i];
					if (patch.previousDefName.NullOrEmpty())
						ApplyPatchToXmlNodePreDefAssignment(patch, xmlParent, defName, defNode, stuffNode);
				}
			}
		}
		catch (Exception ex)
		{
			Log.Error(ex.ToString());
		}
	}

	public static void ScribeDefPostfix(Thing thing)
	{
		try
		{
			if (!LoadingVars || !PatchDatabase.Any)
				return;
			
			// BackCompatibilityConverters can change the def between the prefix and this

			var def = thing.def;
			var defName = def.defName;

			var xmlParent = CurXmlParent;
			var stuffNode = xmlParent["stuff"];
			var stuffName = stuffNode?.InnerText;

			if (!defName.NullOrEmpty() && PatchDatabase.PatchesForDef(defName!) is { Count: > 0 } defPatches)
			{
				for (var i = 0; i < defPatches.Count; i++)
				{
					var patch = defPatches[i];
					if (patch.previousStuff.NullOrEmpty() || patch.previousStuff == stuffName)
						ApplyPatchToXmlNodePostDefAssignment(patch, xmlParent, thing, stuffNode);
					
					// passing in a thing reference to directly assign to the def field
				}
			}

			if (!stuffName.NullOrEmpty() && PatchDatabase.PatchesForStuff(stuffName!) is { Count: > 0 } stuffPatches)
			{
				for (var i = 0; i < stuffPatches.Count; i++)
				{
					var patch = stuffPatches[i];
					if (patch.previousDefName.NullOrEmpty())
						ApplyPatchToXmlNodePostDefAssignment(patch, xmlParent, thing, stuffNode);
				}
			}
		}
		catch (Exception ex)
		{
			Log.Error(ex.ToString());
		}
	}

	private static void ApplyPatchToXmlNodePostDefAssignment(Operation patch, XmlNode xmlParent, Thing thing,
		XmlElement? stuffNode)
	{
		ref var thingDef = ref thing.def;
		if (patch.newDefName is { Length: > 0 } newDefName
			&& newDefName != thingDef.defName
			&& patch.TryGetNewDef() is { } newDef)
		{
			thingDef = newDef;
		}

		ApplyStuffChange(patch, xmlParent, stuffNode, thingDef);
	}

	private static void ApplyPatchToXmlNodePreDefAssignment(Operation patch, XmlNode xmlParent, string? defName,
		XmlElement? defNode, XmlElement? stuffNode)
	{
		var def = default(ThingDef);
		if (!patch.newDefName.NullOrEmpty() && (def = patch.TryGetNewDef()) != null)
			(defNode ?? xmlParent.CreateChild("def")).InnerText = defName = patch.newDefName!;

		ApplyStuffChange(patch, xmlParent, stuffNode, def ?? DefDatabase<ThingDef>.GetNamedSilentFail(defName));
	}

	private static void ApplyStuffChange(Operation patch, XmlNode xmlParent, XmlElement? stuffNode, ThingDef? thingDef)
	{
		if (thingDef?.MadeFromStuff ?? true)
		{
			if (!patch.newStuff.NullOrEmpty() && patch.TryGetNewStuff() != null)
				(stuffNode ?? xmlParent.CreateChild("stuff")).InnerText = patch.newStuff!;
		}
		else if (stuffNode != null)
		{
			xmlParent.RemoveChild(stuffNode);
		}
	}

	private static XmlNode CreateChild(this XmlNode xmlParent, string name)
		=> xmlParent.AppendChild(xmlParent.OwnerDocument!.CreateElement(name));

	private static XmlNode CurXmlParent => Scribe.loader.curXmlParent;

	private static bool LoadingVars => Scribe.mode == LoadSaveMode.LoadingVars;
}