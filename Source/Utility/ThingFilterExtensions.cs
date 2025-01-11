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
}