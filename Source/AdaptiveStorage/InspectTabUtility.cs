// Copyright (c) 2023 bradson
// This Source Code Form is subject to the terms of the MIT license.
// If a copy of the license was not distributed with this file,
// You can obtain one at https://opensource.org/licenses/MIT/.

using AdaptiveStorage.Fishery;

namespace AdaptiveStorage;

public static class InspectTabUtility
{
	public static IEnumerable<InspectTabBase> Modify(IEnumerable<InspectTabBase>? tabs)
	{
		var hasUnknownTab = false;

		if (tabs != null)
		{
			foreach (var tab in tabs)
			{
				if (tab == null)
					continue;

				if (tab.labelKey.Translate() == ContentsITab.LabelTranslated)
				{
					if (AdaptiveStorageFrameworkSettings.KnownLoadedContentsTabs.Contains(tab))
						continue;
					else
						hasUnknownTab = true;
				}

				yield return tab;
			}
		}

		var settingsContentsTab = AdaptiveStorageFrameworkSettings.ContentsTab;
		if (!hasUnknownTab && settingsContentsTab is { } selectedContentsTab)
			yield return selectedContentsTab;
		
		if (settingsContentsTab is ContentsITab)
			yield return GroupContentsITab.Instance;
	}

	public static void TryOpen(ISelectable selectable) // for automatic tab opening, similar to LWM's
		// https://github.com/lilwhitemouse/RimWorld-LWM.DeepStorage/blob/master/DeepStorage/Deep_Storage_ITab.cs#L237-L306
	{
		if (!AdaptiveStorageFrameworkSettings.AutomaticallyOpenContentsTab || Find.Selector.NumSelected > 1)
			return;

		using var tabs = selectable.GetInspectTabs().ToPooledList();

		if (tabs.Exists(static tab
			=> InspectPaneUtility.IsOpen(tab, (MainTabWindow_Inspect)MainButtonDefOf.Inspect.TabWindow)))
		{
			return;
		}

		var selectedContentsTab = AdaptiveStorageFrameworkSettings.ContentsTab;

		var tab = selectedContentsTab is null
			|| !tabs.Contains(selectedContentsTab)
			|| !selectedContentsTab.IsVisible
				? tabs.Find(static tab => tab is ITab_Storage)
				: selectedContentsTab;

		if (tab is null)
			return;

		InspectPaneUtility.OpenTab(tab.GetType());
	}
}
