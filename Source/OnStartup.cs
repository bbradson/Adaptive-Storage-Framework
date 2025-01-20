// Copyright (c) 2023 bradson
// This Source Code Form is subject to the terms of the MIT license.
// If a copy of the license was not distributed with this file,
// You can obtain one at https://opensource.org/licenses/MIT/.

global using System;
global using System.Collections.Generic;
global using System.Runtime.CompilerServices;
global using AdaptiveStorage.Utility;
global using JetBrains.Annotations;
global using RimWorld;
global using UnityEngine;
global using Verse;
global using CodeInstructions = System.Collections.Generic.IEnumerable<HarmonyLib.CodeInstruction>;

namespace AdaptiveStorage;

[StaticConstructorOnStartup]
public static class OnStartup
{
	static OnStartup()
	{
		// var sw = new Stopwatch();
		// sw.Start();
		
		// TODO: move parts of patching elsewhere. StaticConstructorOnStartup blocks the UI thread
		
		AdaptiveStorageFrameworkMod.Harmony.PatchAll(typeof(AdaptiveStorageFrameworkMod).Assembly);
		
		// sw.Stop();
		// Log.Message($"Initializing ASF took: {sw.ElapsedMilliseconds} ms"); // less than 100 ms though
	}
}