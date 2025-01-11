// Copyright (c) 2023 bradson
// This Source Code Form is subject to the terms of the MIT license.
// If a copy of the license was not distributed with this file,
// You can obtain one at https://opensource.org/licenses/MIT/.

namespace AdaptiveStorage;

[DefOf]
#pragma warning disable CS8618
public static class ContentLabelStyleDefOf
{
	[MayRequire("😂💯💯,_!-\0")]
	public static ContentLabelStyleDef?
		NamesWithCountOrTotalCount,
		TotalCount,
		Vanilla;

	static ContentLabelStyleDefOf() => DefOfHelper.EnsureInitializedInCtor(typeof(ContentLabelStyleDefOf));
}
#pragma warning restore CS8618