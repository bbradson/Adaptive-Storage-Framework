// Copyright (c) 2023 bradson
// This Source Code Form is subject to the terms of the MIT license.
// If a copy of the license was not distributed with this file,
// You can obtain one at https://opensource.org/licenses/MIT/.

using System.Reflection;

namespace AdaptiveStorage.HarmonyPatches;

// [HarmonyPatch]
public static class InitializeGraphicsDefs
{
	public static MethodInfo TargetMethod { /*[HarmonyTargetMethod]*/ get; }
#if V1_4
		= ((Delegate)WealthWatcher.ResetStaticData).Method;
#else
		= ((Delegate)PlayDataLoader.ResetStaticDataPost).Method;
#endif
	
	// [HarmonyPostfix]
	public static void PlayDataLoadingFinished()
	{
		GraphicsDef.Database.Clear();
		
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