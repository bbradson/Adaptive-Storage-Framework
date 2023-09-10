// Copyright (c) 2023 bradson
// This Source Code Form is subject to the terms of the MIT license.
// If a copy of the license was not distributed with this file,
// You can obtain one at https://opensource.org/licenses/MIT/.

using System.Reflection;

namespace AdaptiveStorage.ModCompatibility;

public static class ActiveMods
{
	public static readonly bool
		Fishery = Contains(PackageIDs.FISHERY),
		Harmony = Contains(PackageIDs.HARMONY),
		Multiplayer = Contains(PackageIDs.MULTIPLAYER),
		PerformanceFish = Contains(PackageIDs.PERFORMANCE_FISH),
		Prepatcher = Contains(PackageIDs.PREPATCHER),
		ToggleableOverlays = Contains(PackageIDs.TOGGLEABLE_OVERLAYS),
		VanillaExpandedFramework = Contains(PackageIDs.VANILLA_EXPANDED_FRAMEWORK);
	
	// ModsConfig.IsActive does not ignore _steam postfixes, causing it to fail when local copies were made
	public static bool Contains(string packageID) => TryGetModMetaData(packageID) != null;
	
	public static ModMetaData? TryGetModMetaData(string packageID)
		=> ModLister.GetActiveModWithIdentifier(packageID, true);

	public static ModContentPack? TryGetModContentPack(string packageID)
	{
		var allMods = LoadedModManager.RunningModsListForReading;
		var count = allMods.Count;
		for (var i = 0; i < count; i++)
		{
			if (allMods[i].PackageIdPlayerFacing.Equals(packageID, StringComparison.OrdinalIgnoreCase))
				return allMods[i];
		}

		return null;
	}
	
	public static Assembly? TryGetAssembly(this ModContentPack modContentPack, string name)
	{
		var assemblies = modContentPack.assemblies.loadedAssemblies;
		
		for (var i = 0; i < assemblies.Count; i++)
		{
			if (assemblies[i].GetName().Name.Equals(name, StringComparison.OrdinalIgnoreCase))
				return assemblies[i];
		}

		return null;
	}
}