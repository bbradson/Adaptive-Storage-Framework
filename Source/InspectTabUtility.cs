// Copyright (c) 2023 bradson
// This Source Code Form is subject to the terms of the MIT license.
// If a copy of the license was not distributed with this file,
// You can obtain one at https://opensource.org/licenses/MIT/.

using System.Linq;
// ReSharper disable PossibleMultipleEnumeration

namespace AdaptiveStorage;

public static class InspectTabUtility
{
	public static IEnumerable<InspectTabBase> Modify(IEnumerable<InspectTabBase> tabs)
	{
		foreach (var tab in tabs)
		{
			if (tab != null && tab.labelKey.Translate() != ContentsITab.LabelTranslated)
				yield return tab;
		}

		if (AdaptiveStorageFrameworkSettings.ContentsTab is { } selectedContentsTab)
			yield return selectedContentsTab;
	}

	public static void TryOpen(Building_Storage building) // for automatic tab opening, similar to LWM's
		// https://github.com/lilwhitemouse/RimWorld-LWM.DeepStorage/blob/master/DeepStorage/Deep_Storage_ITab.cs#L237-L306
	{
		if (Find.Selector.NumSelected > 1)
			return;

		var tabs = building.GetInspectTabs();

		if (tabs.Any(static tab
			=> InspectPaneUtility.IsOpen(tab, (MainTabWindow_Inspect)MainButtonDefOf.Inspect.TabWindow)))
		{
			return;
		}

		var selectedContentsTab = AdaptiveStorageFrameworkSettings.ContentsTab;

		var tab = selectedContentsTab is null || building.StoredThings().Count < 1
			? tabs.FirstOrDefault(static tab => tab is ITab_Storage)
			: selectedContentsTab;

		if (tab is null)
			return;

		InspectPaneUtility.OpenTab(tab.GetType());
	}
}