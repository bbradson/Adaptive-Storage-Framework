// Copyright (c) 2023 bradson
// This Source Code Form is subject to the terms of the MIT license.
// If a copy of the license was not distributed with this file,
// You can obtain one at https://opensource.org/licenses/MIT/.

namespace AdaptiveStorage;

[DefOf]
public static class InternalDefOf
{
	public static ThingDef Shelf;
	
#pragma warning disable CS8618
	static InternalDefOf() => DefOfHelper.EnsureInitializedInCtor(typeof(InternalDefOf));
#pragma warning restore CS8618
}