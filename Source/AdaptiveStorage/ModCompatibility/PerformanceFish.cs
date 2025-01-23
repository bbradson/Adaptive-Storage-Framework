// Copyright (c) 2023 bradson
// This Source Code Form is subject to the terms of the MIT license.
// If a copy of the license was not distributed with this file,
// You can obtain one at https://opensource.org/licenses/MIT/.

namespace AdaptiveStorage.ModCompatibility;

public static class PerformanceFish
{
	public static bool Active => ActiveMods.PerformanceFish;

	private static readonly Action<ListerThings, Thing, ThingRequestGroup>?
		_addToGroupListFunc
			= Active
				? ReflectionUtility.CreateDelegate<Action<ListerThings, Thing, ThingRequestGroup>>(
					"PerformanceFish.Listers.ThingsPrepatches:AddToGroupList")
				: null,
		_removeFromGroupListFunc
			= Active
				? ReflectionUtility.CreateDelegate<Action<ListerThings, Thing, ThingRequestGroup>>(
					"PerformanceFish.Listers.ThingsPrepatches:RemoveFromGroupList")
				: null;

	// private static readonly Func<Thing, object>? _events
	// 	= Active ? ReflectionUtility.TypeByName("PerformanceFish.PrepatcherFields")
	// 		.GetMethods(BindingFlags.Public | BindingFlags.Static)
	// 		.First(static method => method.Name == "Events"
	// 			&& method.GetParameters() is [var parameter]
	// 			&& parameter.ParameterType == typeof(Thing))
	// 		.CreateDelegate<Func<Thing, object>>() : null;
	//
	// private static readonly Type? _instancedThingEventsType
	// 	= Active ? ReflectionUtility.TypeByName("PerformanceFish.Events.ThingEvents+Instanced") : null;
	//
	// public static EventInfo? RegisteredAtThingGrid { get; }
	// 	= Active ? ReflectionUtility.DeclaredEvent(_instancedThingEventsType!, "RegisteredAtThingGrid") : null;
	//
	// public static EventInfo? DeregisteredAtThingGrid { get; }
	// 	= Active ? ReflectionUtility.DeclaredEvent(_instancedThingEventsType!, "DeregisteredAtThingGrid") : null;
	//
	// public static object? Events(Thing thing) => _events?.Invoke(thing);

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