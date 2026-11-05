// Written in 2024 by bradson
// To the extent possible under law, the author(s) have dedicated all
// copyright and related and neighboring rights to this software to the
// public domain worldwide. This software is distributed without any
// warranty. You should have received a copy of the CC0 Public Domain
// Dedication along with this software. If not, see
// http://creativecommons.org/publicdomain/zero/1.0/.

using System;
using System.Xml;
using HarmonyLib;
using JetBrains.Annotations;
using Verse;

namespace SaveGameCompatibility;

[HarmonyPatch(typeof(BackCompatibility), nameof(BackCompatibility.GetBackCompatibleType))]
public static class BackCompatibilityPatch
{
	[HarmonyPrefix]
	[UsedImplicitly]
	public static void GetBackCompatibleType(Type baseType, ref string providedClassName, XmlNode? node)
	{
		if (node is null
			|| providedClassName is not { Length: > 0 }
			|| !typeof(Thing).IsAssignableFrom(baseType)
			|| PatchDatabase.PatchesForType(providedClassName is { Length: > 0 } ? providedClassName : baseType.Name)
				is not { Count: > 0 } patches)
		{
			return;
		}

		var defName = node["def"]?.InnerText;
		var stuffName = node["stuff"]?.InnerText;
			
		for (var i = patches.Count; --i >= 0;)
		{
			var patch = patches[i];
			if (patch.newClassName is not { Length: > 0 } newClassName
				|| (patch.previousDefName is { Length: > 0 } previousDefName && previousDefName != defName)
				|| (patch.previousStuff is { Length: > 0 } previousStuff && previousStuff != stuffName))
			{
				continue;
			}

			providedClassName = newClassName;
			return;
		}
	}
}