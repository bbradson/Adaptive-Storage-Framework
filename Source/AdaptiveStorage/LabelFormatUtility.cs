// Copyright (c) 2025 bradson
// This Source Code Form is subject to the terms of the MIT license.
// If a copy of the license was not distributed with this file,
// You can obtain one at https://opensource.org/licenses/MIT/.

using AdaptiveStorage.Fishery;
using AdaptiveStorage.Fishery.Pools;

namespace AdaptiveStorage;

public static class LabelFormatUtility
{
	public static string? GenerateBaseConstructibleLabel(ThingDef buildDef, ThingDef? stuffToUse, Extension? extension,
		Precept_ThingStyle? styleSourcePrecept)
		=> buildDef is { label: { } defLabel }
			? extension.TryGenerateCustomLabel(
				styleSourcePrecept != null ? styleSourcePrecept.TransformThingLabel(defLabel) : defLabel, stuffToUse)
			: null;
	
	public static string? TryGenerateCustomLabel(this Extension? extension, Thing thing)
		=> (extension?.labelFormat ?? LabelFormat.Default).TryGenerateCustomLabel(thing);

	public static string? TryGenerateCustomLabel(this Extension? extension, string thingLabel, ThingDef? stuff,
		int hitPoints = -1, uint maxHitPoints = 0, QualityCategory? quality = null)
		=> (extension?.labelFormat ?? LabelFormat.Default).TryGenerateCustomLabel(thingLabel, stuff, hitPoints,
			maxHitPoints, quality);

	public static string? TryGenerateCustomLabel(this LabelFormat labelFormat, Thing thing)
	{
		if (labelFormat == LabelFormat.Default)
			return null;

		var useHitPoints = thing.def is { useHitPoints: true, stackLimit: 1 };
		
		var result = labelFormat.TryGenerateCustomLabel(
			thing.StyleDef is { overrideLabel: { Length: > 0 } styleOverrideLabel }
				? styleOverrideLabel
				: thing.def.label, thing.GetStuffToUse(), useHitPoints ? thing.HitPoints : -1,
			useHitPoints ? (uint)thing.MaxHitPoints : 0u, thing.TryGetQuality(out var quality) ? quality : null);

		if (thing is ThingWithComps { AllComps: { } comps })
		{
			comps.UnwrapReadOnlyArray(out var array, out var count);
			for (var i = 0; i < count; i++)
				result = array[i].TransformLabel(result);
		}
		
		return result;
	}

	public static string? TryGenerateCustomLabel(this LabelFormat labelFormat, string thingLabel, ThingDef? stuff,
		int hitPoints = -1, uint maxHitPoints = 0, QualityCategory? quality = null)
		=> stuff is null
			? null
			: labelFormat switch
			{
				LabelFormat.NoStuff => thingLabel + LabelExtras(hitPoints, maxHitPoints, quality),
				LabelFormat.StuffAsNoun => (string)Strings.Translated.ThingMadeOfStuffLabel.Formatted(stuff.label,
						thingLabel)
					+ LabelExtras(hitPoints, maxHitPoints, quality),
				LabelFormat.Default => null,
				_ => throw new($"Invalid LabelFormat: {labelFormat}")
			};

	public static string LabelExtras(int hitPoints = -1, uint maxHitPoints = 0, QualityCategory? quality = null)
	{
		using var stringBuilder = new PooledStringBuilder();
		
		var hasQuality = quality != null;
		var showHitPoints = (uint)hitPoints < maxHitPoints;
		
		if (hasQuality || showHitPoints)
		{
			stringBuilder.Append(" (");
			
			if (hasQuality)
				stringBuilder.Append(quality.GetValueOrDefault().GetLabel());
			
			if (showHitPoints)
			{
				if (hasQuality)
					stringBuilder.Append(" ");
				
				stringBuilder.Append(((float)hitPoints / maxHitPoints).ToStringPercent());
			}

			stringBuilder.Append(")");
		}

		return stringBuilder.ToString();
	}
}
