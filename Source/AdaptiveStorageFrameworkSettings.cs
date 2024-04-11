// Copyright (c) 2023 bradson
// This Source Code Form is subject to the terms of the MIT license.
// If a copy of the license was not distributed with this file,
// You can obtain one at https://opensource.org/licenses/MIT/.

using System.Linq;
using HarmonyLib;

namespace AdaptiveStorage;

public class AdaptiveStorageFrameworkSettings : ModSettings
{
	public static InspectTabBase? ContentsTab
		=> _contentsTab ??= ShowContentsTab ? InspectTabManager.GetSharedInstance(ContentsTabType) : null;
	
	public static Type ContentsTabType
	{
		get => _contentsTabType ?? InitializeContentsTabType();
		set
		{
			_contentsTabType = value;
			_contentsTabTypeName = value.FullName;
			_contentsTab = ShowContentsTab ? InspectTabManager.GetSharedInstance(value) : null;
		}
	}

	private static Type InitializeContentsTabType()
		=> _contentsTabType = _contentsTabTypeName == typeof(ContentsITab).FullName
			? typeof(ContentsITab)
			: AccessTools.TypeByName(_contentsTabTypeName) ?? typeof(ContentsITab);

	public static bool ShowContentsTab => typeof(InspectTabBase).IsAssignableFrom(ContentsTabType);

	private static InspectTabBase? _contentsTab;

	private static Type? _contentsTabType;
	
	private static string? _contentsTabTypeName = typeof(ContentsITab).FullName;

	public static HashSet<InspectTabBase> KnownLoadedContentsTabs
		=> _knownLoadedContentsTabs ??= PrepareKnownLoadedContentsTabTypes();

	private static (Type type, ModContentPack? contentPack)[] LoadedContentsTabTypes
		=> _loadedContentsTabTypes ??= PrepareLoadedContentsTabTypes();

	private static HashSet<InspectTabBase> PrepareKnownLoadedContentsTabTypes()
		=> InternalDefOf.Shelf.inspectorTabsResolved
			.Where(static tab => tab.labelKey.Translate() == ContentsITab.LabelTranslated
				&& tab.GetType() != typeof(ContentsITab))
			.Append(InspectTabManager.GetSharedInstance(typeof(ContentsITab)))
			.ToHashSet();

	private static (Type, ModContentPack?)[] PrepareLoadedContentsTabTypes()
		=> KnownLoadedContentsTabs
			.Select(static tab => (tab.GetType(), LoadedModManager.RunningModsListForReading.Find(contentPack
				=> contentPack.assemblies.loadedAssemblies.Contains(tab.GetType().Assembly))))
			.ToArray()!;

	private static HashSet<InspectTabBase>? _knownLoadedContentsTabs;
	private static (Type, ModContentPack?)[]? _loadedContentsTabTypes;
	
	public static void DoSettingsWindowContents(Rect inRect)
	{
		var listing = new Listing_Standard();
		
		listing.Begin(inRect);
		listing.Label(Strings.Translated.ASF_SettingContentsTabSelection);

		foreach (var (loadedContentsTabType, contentPack) in LoadedContentsTabTypes)
		{
			if (listing.RadioButton(contentPack?.Name ?? loadedContentsTabType.Name,
				_contentsTabType == loadedContentsTabType))
			{
				ContentsTabType = loadedContentsTabType;
			}
		}
		
		if (listing.RadioButton(Strings.Translated.None, _contentsTabType == typeof(Building_Storage)))
			ContentsTabType = typeof(Building_Storage);

		listing.End();
	}

	public override void ExposeData()
		=> Scribe_Values.Look(ref _contentsTabTypeName, nameof(ContentsTabType));
}