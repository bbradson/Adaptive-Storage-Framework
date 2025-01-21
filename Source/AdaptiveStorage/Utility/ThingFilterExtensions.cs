// Copyright (c) 2023 bradson
// This Source Code Form is subject to the terms of the MIT license.
// If a copy of the license was not distributed with this file,
// You can obtain one at https://opensource.org/licenses/MIT/.

namespace AdaptiveStorage.Utility;

public static class ThingFilterExtensions
{
	public static void SetAllowAll(this ThingFilter filter, IEnumerable<ThingDef> defs, bool allow)
	{
		foreach (var def in defs)
			filter.SetAllow(def, allow);
	}

	public static void SetAllowAll(this ThingFilter filter, IEnumerable<ThingCategoryDef> defs, bool allow)
	{
		foreach (var def in defs)
			filter.SetAllow(def, allow);
	}

	public static void SetAllowAll(this ThingFilter filter, IEnumerable<SpecialThingFilterDef> defs, bool allow)
	{
		foreach (var def in defs)
			filter.SetAllow(def, allow);
	}

	/// <summary>
	/// Strips <see cref="SpecialThingFilterDef"/>s that cannot be viewed through storage settings but still get tested
	/// in <see cref="ThingFilter.Allows(Thing)"/> when including any <see cref="ThingCategoryDef"/> they belong to.
	/// This testing can lead to errors when having items not supported by those filters also included in storage
	/// settings
	/// </summary>
	public static void StripHiddenSpecialThingFilters(this ThingFilter filter)
		=> filter.SetAllowAll(DefaultHiddenSpecialThingFilters, true);

	public static IEnumerable<SpecialThingFilterDef> DefaultHiddenSpecialThingFilters { get; }
		= ((ITab_Storage)InspectTabManager.GetSharedInstance(typeof(ITab_Storage))).HiddenSpecialThingFilters();
}