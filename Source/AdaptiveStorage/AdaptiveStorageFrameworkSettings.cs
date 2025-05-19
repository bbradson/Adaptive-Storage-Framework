// Copyright (c) 2023 bradson
// This Source Code Form is subject to the terms of the MIT license.
// If a copy of the license was not distributed with this file,
// You can obtain one at https://opensource.org/licenses/MIT/.

using System.Linq;
// ReSharper disable SpecifyACultureInStringConversionExplicitly

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
			: GenTypes.GetTypeInAnyAssembly(_contentsTabTypeName) ?? typeof(ContentsITab);

	public static bool ShowContentsTab => typeof(InspectTabBase).IsAssignableFrom(ContentsTabType);

	public static bool AutomaticallyOpenContentsTab => _automaticallyOpenContentsTab;

	public static bool HideLabelsUntilMouseOver => _hideLabelsUntilMouseOver;

	public static bool HideLabelsWhenZoomedOut => _hideLabelsWhenZoomedOut;

	private static InspectTabBase? _contentsTab;

	private static Type? _contentsTabType;

	private static ContentLabelStyleDef? _contentLabelStyle;
	
	private static string?
		_contentsTabTypeName = typeof(ContentsITab).FullName,
		_contentLabelStyleName;

	private static bool
		_automaticallyOpenContentsTab = true,
		_hideLabelsWhenZoomedOut = true,
		_hideLabelsUntilMouseOver;

	private static int _labelHidingMaxZoomLevel = 1;

	public static int LabelHidingMaxZoomLevel => _labelHidingMaxZoomLevel;

	public static double LabelHidingMaxZoomValue => GetZoomLevelValue(LabelHidingMaxZoomLevel);

	public static ContentLabelStyleDef? ContentLabelStyle
		=> _contentLabelStyleName != null
			? _contentLabelStyle ??= DefDatabase<ContentLabelStyleDef>.GetNamed(_contentLabelStyleName)
			: null;

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
	private static readonly GUIScope.ScrollViewStatus _scrollViewStatus = new();

	private static readonly int[] _cameraZoomRangeSteps = Enumerable.Range(0, 10).ToArray();

	public static void DoSettingsWindowContents(Rect inRect)
	{
		using var scrollView = new GUIScope.ScrollView(inRect, _scrollViewStatus);
		
		var listing = new Listing_Standard();
		
		listing.Begin(scrollView.Rect);
		listing.Label(Strings.Translated.ASF_SettingContentsTabSelection,
			tooltip: Strings.Translated.ASF_SettingContentsTabDescription);

		foreach (var (loadedContentsTabType, contentPack) in LoadedContentsTabTypes)
		{
			if (listing.RadioButtonConsistent(contentPack?.Name ?? loadedContentsTabType.Name,
				ContentsTabType == loadedContentsTabType))
			{
				ContentsTabType = loadedContentsTabType;
			}
		}
		
		if (listing.RadioButtonConsistent(Strings.Translated.None, ContentsTabType == typeof(Building_Storage)))
			ContentsTabType = typeof(Building_Storage);
		
		listing.Gap();
		listing.CheckboxLabeled(Strings.Translated.ASF_AutomaticallyOpenContentsTab,
			ref _automaticallyOpenContentsTab, Strings.Translated.ASF_AutomaticallyOpenContentsTabDescription);
		
		listing.Gap();
		listing.Label(Strings.Translated.ASF_DefaultLabelStyleSetting,
			tooltip: Strings.Translated.ASF_DefaultLabelStyleDescription);
		
		foreach (var labelStyleDef in DefDatabase<ContentLabelStyleDef>.AllDefsListForReading)
		{
			if (listing.RadioButtonConsistent(labelStyleDef.LabelCap, ContentLabelStyle == labelStyleDef
				|| (_contentLabelStyleName is null && labelStyleDef.defName == "Automatic"),
				tooltip: labelStyleDef.description))
			{
				SetDefaultLabelStyle(labelStyleDef);
			}
		}

		listing.Gap();
		listing.CheckboxLabeled(Strings.Translated.ASF_HideLabelsZoomSetting, ref _hideLabelsWhenZoomedOut,
			Strings.Translated.ASF_HideLabelsZoomDescription);
		
		if (_hideLabelsWhenZoomedOut)
		{
			var cameraZoomRangeIndex = Array.IndexOf(_cameraZoomRangeSteps, _labelHidingMaxZoomLevel);
			listing.CollectionSlider(_cameraZoomRangeSteps, ref cameraZoomRangeIndex,
				Strings.Translated.ASF_MaxZoomLevelSetting, Strings.Translated.ASF_MaxZoomLevelDescription,
				static zoomLevel => Current.CameraDriver is var driver && driver != null && driver.config is { } config
					? GetZoomLevelValue(zoomLevel, config.sizeRange).ToString()
					: string.Empty);
			_labelHidingMaxZoomLevel = _cameraZoomRangeSteps[cameraZoomRangeIndex];
		}

		listing.Gap();
		listing.CheckboxLabeled(Strings.Translated.ASF_HideLabelsMouseOverSetting, ref _hideLabelsUntilMouseOver,
			Strings.Translated.ASF_HideLabelsMouseOverDescription);

		scrollView.Height = listing.CurHeight;
		listing.End();
	}

	public static float GetZoomLevelValue(int zoomLevelStep)
		=> GetZoomLevelValue(zoomLevelStep, Current.CameraDriver.config.sizeRange);

	public static float GetZoomLevelValue(int zoomLevelStep, FloatRange cameraConfigSizeRange)
		=> zoomLevelStep switch
		{
			0 => cameraConfigSizeRange.min + 0.1f,
			1 => cameraConfigSizeRange.min + 1f,
			9 => cameraConfigSizeRange.max - 1f,
			_ => ((cameraConfigSizeRange.max - cameraConfigSizeRange.min - 2f) * (zoomLevelStep - 1) * (1f / 8f))
				+ cameraConfigSizeRange.min
		};

	public static void SetDefaultLabelStyle(ContentLabelStyleDef labelStyleDef)
	{
		_contentLabelStyle = labelStyleDef;
		_contentLabelStyleName = labelStyleDef.defName;
		foreach (var def in DefDatabase<GraphicsDef>.AllDefsListForReading)
			def.UpdateActiveLabelStyle();
	}

	public override void ExposeData()
	{
		Scribe_Values.Look(ref _contentsTabTypeName, nameof(ContentsTabType));
		Scribe_Values.Look(ref _automaticallyOpenContentsTab, nameof(AutomaticallyOpenContentsTab), true);
		Scribe_Values.Look(ref _contentLabelStyleName, nameof(ContentLabelStyle));
		Scribe_Values.Look(ref _hideLabelsUntilMouseOver, nameof(HideLabelsUntilMouseOver));
		Scribe_Values.Look(ref _hideLabelsWhenZoomedOut, nameof(HideLabelsWhenZoomedOut), true);
		Scribe_Values.Look(ref _labelHidingMaxZoomLevel, nameof(LabelHidingMaxZoomLevel), 1);
	}
}
