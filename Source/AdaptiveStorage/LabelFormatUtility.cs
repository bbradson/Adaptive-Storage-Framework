// Copyright (c) 2025 bradson
// This Source Code Form is subject to the terms of the MIT license.
// If a copy of the license was not distributed with this file,
// You can obtain one at https://opensource.org/licenses/MIT/.

namespace AdaptiveStorage;

public static class LabelFormatUtility
{
	public static string? TryGenerateCustomLabel(this Extension? extension, Thing thing)
		=> (extension?.labelFormat ?? LabelFormat.Default).TryGenerateCustomLabel(thing);

	public static string? TryGenerateCustomLabel(this LabelFormat labelFormat, Thing thing)
	{
		var stuff = thing.GetStuffToUse();
		return stuff is null
			? null
			: labelFormat switch
			{
				LabelFormat.NoStuff => thing.def.label
					+ GenLabel.LabelExtras(thing,
#if V1_4
						1,
#endif
						true, true),
				LabelFormat.StuffAsNoun => (string)Strings.Translated.ThingMadeOfStuffLabel.Formatted(stuff.label,
						thing.def.label)
					+ GenLabel.LabelExtras(thing,
#if V1_4
						1,
#endif
						true, true),
				LabelFormat.Default => null,
				_ => throw new($"Invalid LabelFormat: {labelFormat}")
			};
	}
}
