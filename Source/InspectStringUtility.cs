// Copyright (c) 2023 bradson
// This Source Code Form is subject to the terms of the MIT license.
// If a copy of the license was not distributed with this file,
// You can obtain one at https://opensource.org/licenses/MIT/.

using System.Text;
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

		if (building.storageGroup != null)
			AppendLinkedStorageSettings(building, text, stringBuilder);

		if (!text.NullOrEmpty() || stringBuilder.Length > 0)
			stringBuilder.Append('\n');

		var storedThings = building.StoredThings();

		text += AppendCount(storedThings.Count > 0
				? AppendStoredThings(stringBuilder, storedThings)
				: stringBuilder.Append(Strings.TranslatedWithBackup.Empty.CapitalizeFirst()), building, storedThings)
			.ToString();

		stringBuilder.Clear();
		SimplePool<StringBuilder>.Return(stringBuilder);
		return text;
	}

	private static StringBuilder AppendCount(StringBuilder stringBuilder, Building_Storage building,
		IList<Thing> storedThings)
		=> stringBuilder
			.Append(". (")
			.Append(Strings.Stacks(storedThings.Count, building.TotalSlots()))
			.Append(')');

	private static StringBuilder AppendStoredThings(StringBuilder stringBuilder, IList<Thing> storedThings)
	{
		stringBuilder.Append(Strings.Translated.StoresThings)
			.Append(": ");

		var storedThingCounts = SimplePool<Dictionary<ThingDef, int>>.Get();
		storedThingCounts.Clear();

		for (var i = storedThings.Count; i-- > 0;)
		{
			var storedThingDef = storedThings[i].def;
			storedThingCounts[storedThingDef]
				= storedThingCounts.TryGetValue(storedThingDef) + storedThings[i].stackCount;
		}

		var firstItem = true;
		foreach (var storedThingCountPair in storedThingCounts)
		{
			if (!firstItem)
				stringBuilder.Append(", ");

			stringBuilder.Append(GenLabel.ThingLabel(storedThingCountPair.Key, null, storedThingCountPair.Value)
				.CapitalizeFirst());

			firstItem = false;
		}

		storedThingCounts.Clear();
		SimplePool<Dictionary<ThingDef, int>>.Return(storedThingCounts);
		return stringBuilder;
	}

	private static void AppendLinkedStorageSettings(Building_Storage building, string text, StringBuilder stringBuilder)
	{
		if (!text.NullOrEmpty())
			stringBuilder.Append('\n');

		stringBuilder.Append(Strings.Translated.LinkedStorageSettings)
			.Append(": ")
			.Append(Strings.Translated.NumBuildings.Formatted(building.storageGroup.MemberCount).CapitalizeFirst());
	}

	private static Func<ThingWithComps, string> _thingWithCompsGetInspectString
		= AccessTools.MethodDelegate<Func<ThingWithComps, string>>(
			AccessTools.DeclaredMethod(typeof(ThingWithComps), nameof(ThingWithComps.GetInspectString)),
			virtualCall: false);
}