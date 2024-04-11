// Copyright (c) 2023 bradson
// This Source Code Form is subject to the terms of the MIT license.
// If a copy of the license was not distributed with this file,
// You can obtain one at https://opensource.org/licenses/MIT/.

global using System;
global using System.Collections.Generic;
global using JetBrains.Annotations;
global using RimWorld;
global using UnityEngine;
global using Verse;

namespace AdaptiveStorage;

[StaticConstructorOnStartup]
[UsedImplicitly]
public static class OnStartup
{
	static OnStartup()
	{
		AdaptiveStorageFrameworkMod.Harmony.PatchAll();

		var thingDefs = DefDatabase<ThingDef>.AllDefsListForReading;
		for (var i = thingDefs.Count; i-- > 0;)
		{
			var thingDef = thingDefs[i];
			if (thingDef is null)
			{
				LogDefDatabaseCorruption(thingDefs, i);
				continue;
			}
			
			if (thingDef.GetModExtension<Extension>() is { } extension)
				extension.Initialize(thingDef);
		}

		var graphicsDefs = DefDatabase<GraphicsDef>.AllDefsListForReading;
		foreach (var graphicsDef in graphicsDefs)
		{
			try
			{
				graphicsDef.Initialize();
			}
			catch (Exception e)
			{
				Log.Error($"Error while initializing '{graphicsDef}' from mod '{
					graphicsDef.modContentPack?.Name ?? "null"}':\n{e}");
			}
		}
	}

	private static void LogDefDatabaseCorruption(List<ThingDef> thingDefs, int i)
	{
		Log.Error("DefDatabase contains null defs. This indicates something having broken during loading "
			+ "of earlier mods");

		thingDefs.RemoveAt(i);
		DefDatabase<ThingDef>.SetIndices();
	}
}