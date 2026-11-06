// Written in 2024 by bradson
// To the extent possible under law, the author(s) have dedicated all
// copyright and related and neighboring rights to this software to the
// public domain worldwide. This software is distributed without any
// warranty. You should have received a copy of the CC0 Public Domain
// Dedication along with this software. If not, see
// http://creativecommons.org/publicdomain/zero/1.0/.

using System.Linq;
using System.Xml;
using HarmonyLib;
using JetBrains.Annotations;
using Verse;

namespace DefNameLink;

[HarmonyPatch(typeof(ThingFilter), nameof(ThingFilter.ExposeData))]
public static class ThingFilterExposeDataStrip
{
	[HarmonyPrefix]
	[UsedImplicitly]
	public static void Prefix(ThingFilter __instance)
	{
		if (Scribe.mode != LoadSaveMode.LoadingVars
			|| Scribe.loader.curXmlParent is not { } xmlNode)
		{
			return;
		}
		
		if (DefNameLink.OfType<ThingDef>.Any)
			ApplyDefNameLinks<ThingDef>(xmlNode, "allowedDefs");

		if (DefNameLink.OfType<SpecialThingFilterDef>.Any)
			ApplyDefNameLinks<SpecialThingFilterDef>(xmlNode, "disallowedSpecialFilters");

		if (DefNameLink.OfType<ThingCategoryDef>.Any
			&& xmlNode["overrideRootDef"] is { InnerText: { } overrideRootDef } overrideRootNode
			&& DefNameLink.OfType<ThingCategoryDef>.TryGet(overrideRootDef)?.defName is { } replacementDefName)
		{
			overrideRootNode.InnerText = replacementDefName;
		}
	}

	private static void ApplyDefNameLinks<T>(XmlNode parentNode, string listNodeName) where T : Def
	{
		if (parentNode[listNodeName] is not { ChildNodes: { } defNodes } listNode)
			return;
		
		object[] defNodeArray = [..defNodes];
		var defNames = defNodeArray.Select(static node => ((XmlNode)node).InnerText).ToHashSet();
		foreach (XmlNode defNode in defNodeArray)
		{
			if (DefNameLink.OfType<T>.TryGet(defNode.InnerText)?.defName is not { } replacementDefName)
				continue;

			if (defNames.Add(replacementDefName))
				defNode.InnerText = replacementDefName;
			else
				listNode.RemoveChild(defNode);
		}
	}
}