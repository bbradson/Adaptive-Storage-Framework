// Copyright (c) 2023 bradson
// This Source Code Form is subject to the terms of the MIT license.
// If a copy of the license was not distributed with this file,
// You can obtain one at https://opensource.org/licenses/MIT/.

using System.Reflection;
using HarmonyLib;

namespace AdaptiveStorage.ModCompatibility;

public static class PerformanceFish
{
	public static bool Active => ActiveMods.PerformanceFish;
	
	private static readonly MethodInfo?
		_addToGroupListMethod
			= Active
				? AccessTools.DeclaredMethod("PerformanceFish.Listers.ThingsPrepatches:AddToGroupList")
				?? throw new MissingMethodException("PerformanceFish.Listers.ThingsPrepatches:AddToGroupList")
				: null,
		_removeFromGroupListMethod
			= Active
				? AccessTools.DeclaredMethod("PerformanceFish.Listers.ThingsPrepatches:RemoveFromGroupList")
				?? throw new MissingMethodException("PerformanceFish.Listers.ThingsPrepatches:RemoveFromGroupList")
				: null;

	private static readonly Action<ListerThings, Thing, ThingRequestGroup>?
		_addToGroupListFunc
			= Active
				? (Action<ListerThings, Thing, ThingRequestGroup>)_addToGroupListMethod!.CreateDelegate(
					typeof(Action<ListerThings, Thing, ThingRequestGroup>))
				: null,
		_removeFromGroupListFunc
			= Active
				? (Action<ListerThings, Thing, ThingRequestGroup>)_removeFromGroupListMethod!.CreateDelegate(
					typeof(Action<ListerThings, Thing, ThingRequestGroup>))
				: null;

	public static bool AddToGroupList(ListerThings lister, Thing thing, ThingRequestGroup group)
	{
		if (_addToGroupListFunc is null)
			return false;

		_addToGroupListFunc(lister, thing, group);
		return true;
	}

	public static bool RemoveFromGroupList(ListerThings lister, Thing thing, ThingRequestGroup group)
	{
		if (_removeFromGroupListFunc is null)
			return false;

		_removeFromGroupListFunc(lister, thing, group);
		return true;
	}
}