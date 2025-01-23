// Copyright (c) 2023 bradson
// This Source Code Form is subject to the terms of the MIT license.
// If a copy of the license was not distributed with this file,
// You can obtain one at https://opensource.org/licenses/MIT/.

using System.Text;
using AdaptiveStorage.Fishery;
using AdaptiveStorage.Fishery.Pools;
using HarmonyLib;

namespace AdaptiveStorage;

public static class InspectStringUtility
{
	public static string GetString(Building_Storage building)
	{
		var text = _thingWithCompsGetInspectString(building);

		if (!building.Spawned)
			return text;

		var stringBuilder = SimplePool<StringBuilder>.Get();
		stringBuilder.Clear();

		if (building is IStorageGroupMember { Group: { } group })
			AppendLinkedStorageSettings(group, text, stringBuilder);

		if (!text.NullOrEmpty() || stringBuilder.Length > 0)
			stringBuilder.Append('\n');

		var storedThings = building.StoredThings();

		text += AppendCount(storedThings.Length > 0
				? AppendStoresThingsSection(stringBuilder, storedThings)
				: stringBuilder.Append(Strings.TranslatedWithBackup.Empty.CapitalizeFirst()), building, storedThings)
			.ToString();

		stringBuilder.Clear();
		SimplePool<StringBuilder>.Return(stringBuilder);
		return text;
	}

	private static StringBuilder AppendCount(StringBuilder stringBuilder, Building_Storage building,
		ICollection<Thing> storedThings)
		=> stringBuilder
			.Append(". (")
			.Append(Strings.Stacks(storedThings.Count, building.TotalSlots()))
			.Append(')');

	private static StringBuilder AppendStoresThingsSection(StringBuilder stringBuilder, IList<Thing> storedThings)
	{
		stringBuilder.Append(Strings.Translated.StoresThings)
			.Append(": ");

		AppendStoredThings(stringBuilder, storedThings, ", ", sortBy: static (x, y)
			=> string.CompareOrdinal(x.Key.label, y.Key.label) is var labelResult && labelResult != 0
				? labelResult
				: y.Value.CompareTo(x.Value));
		
		return stringBuilder;
	}

	private static void AppendStoredThings(StringBuilder stringBuilder, IList<Thing> storedThings, string separator,
		bool includeCount = true, int max = int.MaxValue, Comparison<KeyValuePair<ThingDef, int>>? sortBy = null,
		string? overflowText = null)
	{
		using var pooledStoredThingCountList = GetThingCountPairs(storedThings);
		
		var storedThingCountList = pooledStoredThingCountList.List;
		
		if (sortBy != null)
			storedThingCountList.Sort(sortBy);

		if (overflowText != null && storedThingCountList.Count > max)
		{
			storedThingCountList[max - 1] = new(null!,
				storedThingCountList.AsReadOnlySpan()[(max - 1)..].Sum(static pair => pair.Value));
		}

		var firstItem = true;
		for (var i = 0; i < storedThingCountList.Count && i < max; i++)
		{
			var storedThingCountPair = storedThingCountList[i];
			if (!firstItem)
				stringBuilder.Append(separator);

			stringBuilder.Append(storedThingCountPair.Key != null
				? GenLabel.ThingLabel(storedThingCountPair.Key, null, includeCount ? storedThingCountPair.Value : 1)
					.CapitalizeFirst()
				: string.Concat(overflowText, " x",
					(includeCount ? storedThingCountPair.Value : storedThingCountList.Count - max + 1)
					.ToStringCached()));

			firstItem = false;
		}
	}

	private static PooledList<KeyValuePair<ThingDef, int>> GetThingCountPairs(IList<Thing> things)
	{
		var thingCountPairs = SimplePool<Dictionary<ThingDef, int>>.Get();
		thingCountPairs.Clear();

		for (var i = things.Count; --i >= 0;)
		{
			var storedThingDef = things[i].def;
			thingCountPairs[storedThingDef]
				= thingCountPairs.TryGetValue(storedThingDef) + Math.Max(things[i].stackCount, 1);
		}

		var result = thingCountPairs.ToPooledList();

		thingCountPairs.Clear();
		SimplePool<Dictionary<ThingDef, int>>.Return(thingCountPairs);
		return result;
	}

	public static string ToStringThingLabels(this IList<Thing> things, string separator = ", ",
		bool includeCount = true, Comparison<KeyValuePair<ThingDef, int>>? sortBy = null, int max = int.MaxValue,
		string? overflowText = null)
	{
		using var stringBuilder = new PooledStringBuilder();
		AppendStoredThings(stringBuilder.Builder, things, separator, includeCount, max, sortBy, overflowText);
		return stringBuilder.ToString();
	}

	public static string[] ToStringsThingLabels(this IList<Thing> things, bool includeCount = true, bool sort = false,
		int max = int.MaxValue)
		=> things.ToStringsThingLabels(includeCount, sort
			? static (x, y)
				=> y.Value.CompareTo(x.Value) is var countResult && countResult != 0
					? countResult
					: string.CompareOrdinal(x.Key.label, y.Key.label)
			: null, max, Strings.Translated.ASF_OtherItems);

	public static string[] ToStringsThingLabels(this IList<Thing> things, bool includeCount = true,
		Comparison<KeyValuePair<ThingDef, int>>? sortBy = null, int max = int.MaxValue, string? overflowText = null)
		=> things.ToStringThingLabels("\n", includeCount, sortBy, max, overflowText).Split('\n');

	private static void AppendLinkedStorageSettings(StorageGroup group, string text, StringBuilder stringBuilder)
	{
		if (!text.NullOrEmpty())
			stringBuilder.Append('\n');

#if !V1_5
		stringBuilder.Append(Strings.Translated.LinkedStorageSettings)
			.Append(": ")
			.Append(Strings.StorageBuildingCount(group.MemberCount));
#else
		stringBuilder.Append(Strings.Translated.StorageGroupLabel)
			.Append(": ")
			.Append(group.RenamableLabel.CapitalizeFirst())
			.Append(" (")
			.Append(group.MemberCount is > 1 and var memberCount
				? Strings.StorageBuildingCount(memberCount)
				: Strings.Translated.OneBuilding)
			.Append(')');
#endif
	}

	private static readonly Func<ThingWithComps, string> _thingWithCompsGetInspectString
		= AccessTools.MethodDelegate<Func<ThingWithComps, string>>(
			AccessTools.DeclaredMethod(typeof(ThingWithComps), nameof(ThingWithComps.GetInspectString)),
			virtualCall: false);
}