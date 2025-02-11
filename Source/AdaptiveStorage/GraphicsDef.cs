// Copyright (c) 2023 bradson
// This Source Code Form is subject to the terms of the MIT license.
// If a copy of the license was not distributed with this file,
// You can obtain one at https://opensource.org/licenses/MIT/.

using System.Linq;

// ReSharper disable NullCoalescingConditionIsAlwaysNotNullAccordingToAPIContract

namespace AdaptiveStorage;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class GraphicsDef : Def
{
	public List<StorageGraphic> graphics = [];

	public CellTable<ItemGraphic>? itemGraphics;
	
	public ThingFilter
		allowedFilter = null!,
		forbiddenFilter = new();

	public AllowedRequirement
		allowedRequirement = AllowedRequirement.Any,
		forbiddenRequirement = AllowedRequirement.Any;

	public bool
		showContainedItems = true,
		useDominantContentColor,
		onlyAllowRequiredThingDefs;

	public bool? showBaseGraphic;

	public ContentColorSource
		colorOneSource = ContentColorSource.Null,
		colorTwoSource = ContentColorSource.Null;

	public StackBehaviour stackBehaviour;

	public ContentLabelStyleDef? contentLabelStyle;

	public int randomSelectionWeight = 1,
		minimumThingCount = int.MinValue,
		minimumAllowedThingCount = int.MinValue,
		maximumThingCount = int.MaxValue;

	public List<ThingDef>?
		requiredThingDefs,
		disallowedThingDefs;

	public List<Rot4>? allowedRotations;

	public List<ThingCategoryDef>? allowedThingCategories;
	
	public ThingDef? targetDef;

	public List<ThingDef> targetDefs = [];

	[Unsaved]
	private bool
		_hasEmptyStackGraphic,
		_forbidsNothing;

	public Type? workerClass = typeof(GraphicsDefSelector);

	[Unsaved]
	private ContentLabelStyleDef _activeLabelStyle = null!;

	[Unsaved]
	private GraphicsDefSelector _worker = null!;

	public ContentLabelStyleDef ActiveLabelStyle => _activeLabelStyle;

	public GraphicsDefSelector Worker => _worker;

	public bool HasEmptyStackGraphic => _hasEmptyStackGraphic;

	public bool ForbidsNothing => _forbidsNothing;
	
	public static Dictionary<ThingDef, List<GraphicsDef>> Database { get; } = new();

	public static Func<GraphicsDef, uint> PositiveWeightSelector { get; }
		= static graphic => graphic.randomSelectionWeight is var weight and > 0 ? (uint)weight : 0u;

	public static Func<GraphicsDef, uint> NegativeWeightSelector { get; }
		= static graphic => graphic.randomSelectionWeight is var weight and < 0 ? (uint)-weight : 0u;

	public static Func<GraphicsDef, uint> NullWeightSelector { get; }
		= static graphic => graphic.randomSelectionWeight == 0 ? 1u : 0u;

	private static Type? _deepStorageITabType
		= Type.GetType("LWM.DeepStorage.ITab_DeepStorage_Inventory, LWM.DeepStorage");

	public ContentLabelStyleDef AutomaticLabelStyle
		=> AdaptiveStorageFrameworkSettings.ContentLabelStyle is
			{
				ContentLabelWorker: { } settingsStyleWorker
			} settingsStyle
			&& settingsStyleWorker.GetType() != typeof(ContentLabelWorker.Automatic)
				? settingsStyle
				: (ContentsFullyHidden ? ContentLabelStyleDefOf.NamesWithCountOrTotalCount : null)
				?? (DisplaysSingleVisibleItem ? ContentLabelStyleDefOf.Vanilla : ContentLabelStyleDefOf.TotalCount)
				?? DefDatabase<ContentLabelStyleDef>.AllDefsListForReading.First(static def
					=> def.ContentLabelWorker.GetType() != typeof(ContentLabelWorker.Automatic));

	private bool ContentsFullyHidden
		=> (!showContainedItems || graphics.Exists(static graphic => !graphic.showContainedItems ?? false))
			&& !useDominantContentColor
			&& colorOneSource < ContentColorSource.First
			&& colorTwoSource < ContentColorSource.First
			&& (allowedFilter.AllowedDefCount > 1) | (allowedRequirement == AllowedRequirement.Always)
			&& targetDefs.Exists(static def => def.building?.fixedStorageSettings?.filter?.AllowedDefCount > 1);

	private bool DisplaysSingleVisibleItem
		=> (showContainedItems || graphics.TrueForAll(static graphic => graphic.showContainedItems ?? false))
			&& targetDefs.TrueForAll(static def => def.SingleCell() && def.MaxItemsInAnyCell() <= 1);

	public override void ResolveReferences()
	{
		ResolveWorker();
		
		if (targetDefs.RemoveAll(static def => def is null) is > 0 and var removedCount)
		{
			Log.Error($"GraphicsDef '{defName}' from mod '{modContentPack?.Name}' contains {
				removedCount} null targetDefs. Removing.");
		}

		if (targetDef != null)
			targetDefs.AddDistinct(targetDef);
		else if (targetDefs.NullOrEmpty() && DefDatabase<ThingDef>.GetNamedSilentFail(defName) is { } def)
			targetDefs.Add(def);
		
		ResolveForbiddenFilter();
		ResolveAllowedFilter();
		Worker.ResolveReferences();
	}

	public override void PostLoad()
	{
		if (onlyAllowRequiredThingDefs)
			allowedRequirement = AllowedRequirement.All;
		
		targetDefs ??= [];
		graphics ??= [];
	}

	public void UpdateActiveLabelStyle()
	{
		_activeLabelStyle = contentLabelStyle ?? AutomaticLabelStyle;

		if (Current.ProgramState != ProgramState.Playing || Find.Maps is not { } allMaps)
			return;

		foreach (var map in allMaps)
		{
			foreach (var thingDef in targetDefs)
			{
				foreach (var thing in map.listerThings.ThingsOfDef(thingDef))
					(thing as ThingClass)?.SetGUIOverlayLabelsDirty();
			}
		}
	}

	private void ResolveForbiddenFilter()
	{
		forbiddenFilter ??= new();
		forbiddenFilter.ResolveReferences();
		
		if (disallowedThingDefs is [_,..])
			forbiddenFilter.SetAllowAll(disallowedThingDefs, true);
		
		forbiddenFilter.StripHiddenSpecialThingFilters();

		_forbidsNothing = forbiddenFilter.AllowedDefCount == 0;
	}

	private void ResolveAllowedFilter()
	{
		var hasPresetFilter = allowedFilter != null!;
		
		allowedFilter ??= new();

		if (allowedThingCategories is [_,..])
		{
			(allowedFilter.categories ??= [])
				.AddRangeDistinct(allowedThingCategories.Select(static cat => cat.defName));
			hasPresetFilter = true;
		}

		allowedFilter.ResolveReferences();

		if (requiredThingDefs is [_,..])
			allowedFilter.SetAllowAll(requiredThingDefs, true);
		else if (!hasPresetFilter && allowedFilter.AllowedDefCount == 0)
			allowedRequirement = AllowedRequirement.Always;

		allowedFilter.SetAllowAll(forbiddenFilter.AllowedThingDefs, false);

		allowedFilter.StripHiddenSpecialThingFilters();
	}

	private void ResolveWorker()
		=> _worker = WorkerClassMaker<GraphicsDefSelector>.MakeWorker(workerClass, this, this)
			?? new GraphicsDefSelector(this);

	public virtual void Initialize()
	{
		foreach (var def in targetDefs)
			InitializeOnDef(def);

		if (showBaseGraphic is null && graphics.Count == 0
			&& StorageGraphicData.GetOrMakeFor(targetDefs.FirstOrDefault()?.graphicData) is { } targetDefGraphic)
		{
			var storageGraphic = new StorageGraphic(); // needed for colorSource support
			storageGraphic.graphicDatas.Add(targetDefGraphic);
			graphics.Add(storageGraphic);
		}

		foreach (var graphic in graphics)
			graphic.Initialize(this);

		graphics.Sort(static (x, y)
			=> x.minimumStackCount.CompareTo(y.minimumStackCount));

		if (targetDefs.Count > 0 && itemGraphics is { } items)
		{
			items.Initialize(targetDefs);
			items.ForEach(itemGraphic => itemGraphic.Initialize(this));
		}

		var minimumStackCountOnGraphics = graphics.Min(static g => g.minimumStackCount);
		_hasEmptyStackGraphic = minimumStackCountOnGraphics < 1;
		
		if (minimumAllowedThingCount < 1 && !HasEmptyStackGraphic)
			minimumAllowedThingCount = 1;

		if (minimumThingCount < minimumAllowedThingCount)
			minimumThingCount = minimumAllowedThingCount;

		if (minimumThingCount < minimumStackCountOnGraphics)
			minimumThingCount = minimumStackCountOnGraphics;
		
		UpdateActiveLabelStyle();
	}
	
	protected virtual void InitializeOnDef(ThingDef def)
	{
		if (!typeof(ThingClass).IsAssignableFrom(def.thingClass))
			def.thingClass = typeof(ThingClass);

		var contentsITabType = typeof(ContentsITab);
		var inspectorTabs = def.inspectorTabs;
		if (!inspectorTabs.Contains(contentsITabType) && !inspectorTabs.Contains(_deepStorageITabType))
		{
			inspectorTabs.Add(contentsITabType);
			(def.inspectorTabsResolved ??= []).Add(InspectTabManager.GetSharedInstance(contentsITabType));
		}

		if (!def.drawGUIOverlay && contentLabelStyle?.ContentLabelWorker is not ContentLabelWorker.None)
			def.drawGUIOverlay = true;

		if (!Database.TryGetValue(def, out var list))
			Database[def] = list = [];

		if (def.building is { } buildingProperties)
		{
			buildingProperties.fixedStorageSettings?.filter?.StripHiddenSpecialThingFilters();
			buildingProperties.defaultStorageSettings?.filter?.StripHiddenSpecialThingFilters();
		}

		list.AddDistinct(this);
	}

	public override IEnumerable<string> ConfigErrors()
	{
		foreach (var configError in base.ConfigErrors())
			yield return configError;
		
		if (targetDefs is not [_,..])
		{
			yield return ErrorForDef("has no valid target ThingDefs. It will not be used anywhere");
			yield break;
		}

		foreach (var defToCheck in targetDefs)
		{
			if (!typeof(Building_Storage).IsAssignableFrom(defToCheck.thingClass))
			{
				yield return ErrorForDef($"contains the target def {defToCheck} with a thing class that is not "
					+ "assignable to Building_Storage. It will get replaced with AdaptiveStorage.ThingClass and may "
					+ "not work correctly");
			}
			else if (!typeof(ThingClass).IsAssignableFrom(defToCheck.thingClass))
			{
				if (defToCheck.thingClass != typeof(Building_Storage))
				{
					yield return ErrorForDef($"contains the target def {defToCheck} with a custom "
						+ "Building_Storage subclass that is not assignable to AdaptiveStorage.ThingClass. It will "
						+ "get replaced and may not work correctly.");
				}
			}
		}

		if (CheckForNullGraphics() is { } nullGraphicsError)
		{
			yield return nullGraphicsError;
			yield break;
		}

		if (graphics != null!)
		{
			foreach (var error in CheckForNullGraphicsInCollection(graphics, "graphics list"))
				yield return error;
		}
	}

	private IEnumerable<string> CheckForNullGraphicsInCollection(List<StorageGraphic> collection, string name)
	{
		for (var i = collection.Count; i-- > 0;)
		{
			if (collection[i]?.graphicDatas is [_, ..])
				continue;

			yield return ErrorForDef($"loaded with missing graphicData or graphicDatas at index {i} in {name}");

			collection.RemoveAt(i);
		}
	}

	private string? CheckForNullGraphics()
		=> graphics != null! ? null : ErrorForDef("loaded with null graphics");

	private string ErrorForDef(string message)
		=> $"AdaptiveStorage.GraphicsDef '{defName}' from mod '{modContentPack?.Name ?? "NULL"}' {message}";
}