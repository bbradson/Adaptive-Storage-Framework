// Copyright (c) 2023 bradson
// This Source Code Form is subject to the terms of the MIT license.
// If a copy of the license was not distributed with this file,
// You can obtain one at https://opensource.org/licenses/MIT/.

using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using AdaptiveStorage.ModCompatibility;

namespace AdaptiveStorage;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class ThingClass : Building_Storage, ISlotGroupParent, IPrintable
{
	public List<GraphicsDef>? AllGraphics { get => Renderer.AllGraphics; private set => Renderer.AllGraphics = value; }

	public int RandomSeed => thingIDNumber;

	public event Action<Thing>?
		ReceivedThing,
		LostThing;

	public ThingCollection StoredThings { get; } = [];

	public List<IntVec2> FreeSlots { get; } = [];
	
	public int CurrentSlotLimit
	{
		get => Math.Min(_currentSlotLimit, TotalSlots);
		set
		{
			_currentSlotLimitPerCell = ((value - 1) / CellCount) + 1;
			_currentSlotLimit = value;
			UpdateMaxItemsInCell();
			UpdateFreeSlots();
		}
	}

	public bool AnyFreeSlots => StoredThings.Count < CurrentSlotLimit;

	public int TotalSlots { get; private set; }

	public int TotalThingCount
	{
		get
		{
			var count = 0;
			var storedThings = StoredThings;
			for (var i = storedThings.Count; i-- > 0;)
				count += storedThings[i].stackCount;

			return count;
		}
	}

	public virtual string GUIOverlayLabel
	{
		get
		{
			var currentTotalThingCount = TotalThingCount;

			return currentTotalThingCount == _cachedTotalThingCount
				? _cachedGUIOverlayLabel!
				: UpdateGUIOverlayLabel(currentTotalThingCount);
		}
	}

	public override string LabelNoCount
		=> HitPoints == _cachedLabelHitPoints && _cachedLabel is { } text ? text : UpdateLabelNoCount();

	public override int MaxItemsInCell => _maxItemsInCell;

	public virtual SectionLayer? CurrentSectionLayer
		=> SpawnedOrAnyParentSpawned && MapHeld.mapDrawer is { sections: not null} drawer
			? drawer.SectionAt(PositionHeld).GetLayer(typeof(SectionLayer_ThingsGeneral))
			: null;

	public IntVec3 BottomLeftCell { get; private set; }

	public virtual int GetMaxItemsForCell(in IntVec3 cell) => MaxItemsForCell(cell);

	private ref int MaxItemsForCell(in IntVec3 cell)
	{
		var position = BottomLeftCell;
		return ref _maxItemsByCell[((cell.z - position.z) * RotatedSize.x) + cell.x - position.x];
	}

	private void UpdateMaxItemsInCell()
	{
		var itemsPerCell = _maxItemsInCell = Math.Min(DefaultMaxItemsInCell(), _currentSlotLimitPerCell);
		var cells = AllSlotCellsList();
		var cellCount = cells.Count;
		
		Array.Fill(_maxItemsByCell, itemsPerCell);

		var assignedSlots = itemsPerCell * cellCount;
		var slotLimit = CurrentSlotLimit;
		for (var i = cellCount; i-- > 0 && assignedSlots > slotLimit;)
		{
			MaxItemsForCell(cells[i])--;
			assignedSlots--;
		}
	}

	private int DefaultMaxItemsInCell()
		=> Extension?.maxItemsPerCellByQuality is { } maxItemsPerCellByQuality
			&& CompQuality?.Quality is { } quality
				? maxItemsPerCellByQuality.GetFor(quality)
				?? ErrorForInvalidMaxItemsPerCellByQuality(quality)
				: LWM.Active && LWM.GetCompProperties(def) is { } lwmProps
					? Math.Max(LWM.GetMaxStacksPerCell(lwmProps), base.MaxItemsInCell)
					: base.MaxItemsInCell;

	[MethodImpl(MethodImplOptions.NoInlining)]
	private int ErrorForInvalidMaxItemsPerCellByQuality(QualityCategory quality)
	{
		Log.ErrorOnce($"Building '{this}' with def '{
			def}' has maxItemsPerCellByQuality and CompQuality, but no possible match for '{quality}'\n{
				new StackTrace(true)}", thingIDNumber * def.defName.GetHashCode());
		return base.MaxItemsInCell;
	}

	public StorageRenderer Renderer { get; private set; } = null!;
	
	public Extension? Extension { get; private set; }
	
	public CompQuality? CompQuality { get; private set; }

	public int CellCount => AllSlotCellsList().Count;

	private int[] _maxItemsByCell = [];

	private int
		_cachedLabelHitPoints = -69,
		_cachedTotalThingCount = -1,
		_maxItemsInCell,
		_currentSlotLimit = int.MaxValue,
		_currentSlotLimitPerCell = int.MaxValue;

	private string? _cachedLabel, _cachedGUIOverlayLabel;

	private StorageSettings? _fixedStorageSettings;
	
	protected virtual void PostInitialize() // why does this not exist on Thing ???
	{
		Extension = def.GetModExtension<Extension>();
		CompQuality = GetComp<CompQuality>();
		
		if (_maxItemsByCell.Length == 0)
		{
			_maxItemsByCell = new int[def.Size.Area];
			Array.Fill(_maxItemsByCell, _maxItemsInCell = Math.Min(DefaultMaxItemsInCell(), _currentSlotLimitPerCell));
		}
		
		Renderer = new(this);
		_godModeGizmos = new(this);
		_statDrawEntries = new(this);
	}

	private string UpdateGUIOverlayLabel(int newTotalThingCount)
		=> _cachedGUIOverlayLabel = (_cachedTotalThingCount = newTotalThingCount) > 0
			? string.Concat("[ ", newTotalThingCount.ToStringCached(), " ]")
			: string.Empty;

	private string UpdateLabelNoCount()
	{
		_cachedLabelHitPoints = HitPoints;
		return _cachedLabel = Stuff is null
			? base.LabelNoCount
			: (Extension?.labelFormat ?? LabelFormat.Default) switch
			{
				LabelFormat.NoStuff => def.label + GenLabel.LabelExtras(this,
#if V1_4
					1,
#endif
					true, true),
				LabelFormat.StuffAsNoun => (string)Strings.Translated.ThingMadeOfStuffLabel.Formatted(Stuff.label,
						def.label)
					+ GenLabel.LabelExtras(this,
#if V1_4
						1,
#endif
						true, true),
				LabelFormat.Default => base.LabelNoCount,
				var labelFormat => throw new($"Invalid LabelFormat: {labelFormat}")
			};
	}
	
	// IStoreSettingsParent, this returns the fixed baseline settings
	public new StorageSettings GetParentStoreSettings() => _fixedStorageSettings ??= PrepareFixedStorageSettings();

	private StorageSettings PrepareFixedStorageSettings()
		=> Stuff != null && def.GetModExtension<Extension>() is { lockStorageSettingsToStuff: true }
			? CreateStuffLockedStorageSettings()
			: base.GetParentStoreSettings();

	private StorageSettings CreateStuffLockedStorageSettings()
	{
		var fixedStorageSettings = new StorageSettings();
		var filter = fixedStorageSettings.filter = ThingFilter.CreateOnlyEverStorableThingFilter();
		filter.SetDisallowAll();
		filter.SetAllow(Stuff, true);
		return fixedStorageSettings;
	}

	public new bool Accepts(Thing t) => HasCapacityForThing(t) && base.Accepts(t);

	public bool HasCapacityForThing(Thing t)
		=> AnyFreeSlots || t.StoringThing() == this || AcceptsForStacking(t);

	public bool AcceptsForStacking(Thing t)
	{
		var storedThings = StoredThings;
		var thingDef = t.def;

		for (var i = storedThings.Count; i-- > 0;)
		{
			var storedThingDef = storedThings.DefAt(i);
			if (storedThingDef != thingDef)
				continue;

			var storedThing = storedThings[i];
			if (storedThing.stackCount < storedThingDef.stackLimit && storedThing.CanStackWith(t))
				return true;
		}

		return false;
	}

	public override void PostMake()
	{
		base.PostMake();
		PostInitialize();
	}

	public override void SpawnSetup(Map map, bool respawningAfterLoad)
	{
		BottomLeftCell = this.OccupiedRect()
#if V1_4
			.BottomLeft;
#else
			.Min; // wtf? why was this renamed?
#endif

		base.SpawnSetup(map, respawningAfterLoad);

		TotalSlots = CellCount * DefaultMaxItemsInCell();
		CurrentSlotLimit = _currentSlotLimit;
		InitializeStoredThings();
		Renderer.InitializeStoredThingGraphics(CurrentSectionLayer);
	}

	public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
	{
		for (var i = 0; i < StoredThings.Count; i++)
			Renderer.RegenerateThingGraphic(StoredThings[i]);

		base.DeSpawn(mode);
	}

	private void InitializeStoredThings()
	{
		StoredThings.Clear();
		FreeSlots.Clear();
		var cells = AllSlotCellsList();

		for (var i = cells.Count; i-- > 0;)
		{
			var thingListAtCell = cells[i].GetThingListUnchecked(Map);
			var storedThingsAtCell = 0;

			for (var j = thingListAtCell.Count; j-- > 0;)
			{
				var thing = thingListAtCell[j];
				if (!thing.IsItem())
					continue;

				StoredThings.Add(thing);
				storedThingsAtCell++;
			}

			if (storedThingsAtCell < MaxItemsInCell)
				FreeSlots.Add(cells[i].ToIntVec2);
		}
	}

	private void UpdateFreeSlots()
	{
		var cells = AllSlotCellsList();
		for (var i = cells.Count; i-- > 0;)
			UpdateFreeSlotsAt(cells[i].ToIntVec2);
	}

	private void UpdateFreeSlotsAt(IntVec2 cell)
	{
		var thingListAtCell = cell.GetThingListUnchecked(Map);
		var storedThingsAtCell = 0;

		for (var j = thingListAtCell.Count; j-- > 0;)
		{
			var thing = thingListAtCell[j];
			if (!thing.IsItem())
				continue;

			storedThingsAtCell++;
		}
		
		if (storedThingsAtCell >= MaxItemsInCell)
		{
			FreeSlots.Remove(cell);
		}
		else
		{
			if (!FreeSlots.Contains(cell))
				FreeSlots.Add(cell);
		}
	}

	public override void ExposeData()
	{
		base.ExposeData();
		
		Scribe_Values.Look(ref _currentSlotLimit, nameof(CurrentSlotLimit), int.MaxValue);

		if (Scribe.mode != LoadSaveMode.LoadingVars)
			return;
		
		PostInitialize();
	}

	public override void Notify_ReceivedThing(Thing newItem)
	{
		if (!newItem.IsItem())
			return;
		
		base.Notify_ReceivedThing(newItem);
		StoredThings.Add(newItem);
		Renderer.AssignThingGraphic(newItem, CurrentSectionLayer);
		Renderer.UpdateCurrentGraphic();
		// UpdateFreeSlotsAt(newItem.Position.ToIntVec2); // Stacking can result in multiple positions changing
		UpdateFreeSlots();
		
		ReceivedThing?.Invoke(newItem);
	}

	public override void Notify_LostThing(Thing newItem)
	{
		if (!newItem.IsItem())
			return;
		
		base.Notify_LostThing(newItem);
		StoredThings.Remove(newItem);
		Renderer.FreeThingGraphic(newItem, CurrentSectionLayer);
		Renderer.UpdateCurrentGraphic();
		// UpdateFreeSlotsAt(newItem.Position.ToIntVec2); // Stacking can result in multiple positions changing
		UpdateFreeSlots();

		LostThing?.Invoke(newItem);
	}

	internal void BasePrint(SectionLayer layer) => base.Print(layer);

	internal void BaseDrawAt(in Vector3 drawLoc, bool flip = false) => base.DrawAt(drawLoc, flip);

	public override void Print(SectionLayer layer) => Renderer.PrintAt(layer, DrawPos);

	public virtual void PrintAt(SectionLayer layer, in Vector3 drawLoc) => Renderer.PrintAt(layer, drawLoc);

	public virtual void PrintAt(SectionLayer layer, in Vector3 drawLoc, in Vector2 drawSize)
		=> Renderer.PrintAt(layer, drawLoc, drawSize);

#if V1_4
	public
#else
	protected
#endif
		override void DrawAt(Vector3 drawLoc, bool flip = false) => Renderer.DrawAt(drawLoc, flip);

#if !V1_4
	internal void BaseDynamicDrawPhaseAt(DrawPhase phase, in Vector3 drawLoc, bool flip = false)
		=> base.DynamicDrawPhaseAt(phase, drawLoc, flip);
	
	public override void DynamicDrawPhaseAt(DrawPhase phase, Vector3 drawLoc, bool flip = false)
		=> Renderer.DynamicDrawPhaseAt(phase, drawLoc, flip);
#endif

	public override IEnumerable<Gizmo> GetGizmos()
	{
		var gizmos = GizmoUtility.Filter(base.GetGizmos());

		if (DebugSettings.godMode)
			AppendGodModeGizmos(ref gizmos);

		return gizmos;
	}

	private void AppendGodModeGizmos(ref IEnumerable<Gizmo> gizmos)
	{
		if (AnyFreeSlots && Spawned)
			gizmos = gizmos.Append(_godModeGizmos.AddStack);

		if (!Renderer.AllGraphics.NullOrEmpty())
			gizmos = gizmos.Append(_godModeGizmos.EditGraphics).Append(_godModeGizmos.UpdateGraphics);
	}

	public override void Notify_ThingSelected() => InspectTabUtility.TryOpen(this);

	// LWM's: https://github.com/lilwhitemouse/RimWorld-LWM.DeepStorage/blob/master/DeepStorage/Deep_Storage_Display.cs#L364-L490
	public override void DrawGUIOverlay()
	{
		if (Find.CameraDriver.CurrentZoom != CameraZoomRange.Closest
			|| !ToggleableOverlays.CheckMouseOver(this))
		{
			return;
		}

		var overlayLabel = GUIOverlayLabel;
		if (!string.IsNullOrEmpty(overlayLabel))
			GenMapUI.DrawThingLabel(this, overlayLabel);

		var comps = AllComps;
		for (var i = 0; i < comps.Count; ++i)
			comps[i].DrawGUIOverlay();
	}

	public override string GetInspectString() => InspectStringUtility.GetString(this);

	// https://github.com/lilwhitemouse/RimWorld-LWM.DeepStorage/blob/master/DeepStorage/CompProperties.cs#L19-L39
	public override IEnumerable<StatDrawEntry> SpecialDisplayStats()
		=> LWM.Active ? base.SpecialDisplayStats() : base.SpecialDisplayStats().Append(_statDrawEntries.ItemsPerCell);

	public override IEnumerable<InspectTabBase> GetInspectTabs() => InspectTabs;
	
	public InspectTabBase[] InspectTabs
		=> _inspectTabs != null && _shownContentsTab == AdaptiveStorageFrameworkSettings.ContentsTab
			? _inspectTabs
			: PrepareInspectTabs();

	private InspectTabBase[] PrepareInspectTabs()
	{
		_shownContentsTab = AdaptiveStorageFrameworkSettings.ContentsTab;
		return _inspectTabs = InspectTabUtility.Modify(base.GetInspectTabs()).ToArray();
	}

	private GodModeGizmos _godModeGizmos = null!;

	private StatDrawEntries _statDrawEntries = null!;

	private InspectTabBase[]? _inspectTabs;

	private InspectTabBase? _shownContentsTab;

	private record GodModeGizmos(ThingClass Parent)
	{
		public readonly Command_AddStack AddStack = new(Parent)
		{
			defaultLabel = "Add stack",
			action = () => GenSpawn.Spawn(ThingMakerUtility.Make(Parent.GetStoreSettings().filter.AnyAllowedDef
				?? Parent.GetParentStoreSettings().filter.AnyAllowedDef), Parent.FreeSlots[0].ToIntVec3, Parent.Map)
		};

		public readonly CommandWithFloatMenu EditGraphics = new()
		{
			defaultLabel = "Edit graphics",
			action = () => Find.WindowStack.Add(new DefEditorWindow(Parent.Renderer.CurrentGraphicVariation
				?? Parent.Renderer.AllGraphics!.First())),
			floatMenuOptionsInitializer = () => Parent.Renderer.AllGraphics!.Select(static graphic
					=> new FloatMenuOption(graphic.defName, () => Find.WindowStack.Add(new DefEditorWindow(graphic))))
				.ToArray()
		};

		public readonly Command_Action UpdateGraphics = new()
		{
			defaultLabel = "Update graphics", action = () =>
			{
				var renderer = Parent.Renderer;
				foreach (var graphic in renderer.AllGraphics!)
				{
					foreach (var storageGraphic in graphic.graphics)
					{
						foreach (var graphicData in storageGraphic.graphicDatas)
							graphicData.Init();
					}
				}

				Parent.def.graphicData.Init();
				
				renderer.InitializeStoredThingGraphics(Parent.CurrentSectionLayer);
				renderer.NotifyCurrentGraphicChanged();
				LongEventHandler.ExecuteWhenFinished(() => Parent.DirtyMapMesh(Parent.Map));
			}
		};

		public class Command_AddStack : Command_Action
		{
			private (ThingDef def, FloatMenuOption option)[]? _floatMenuOptionsByDef;
			private IEnumerable<FloatMenuOption>? _filteredFloatMenuOptions, _unfilteredFloatMenuOptions;
			private readonly ThingClass _parent;

			private (ThingDef def, FloatMenuOption option)[] FloatMenuOptionsByDef
				=> _floatMenuOptionsByDef
					??= _parent.GetParentStoreSettings().filter.AllowedThingDefs
						.Select(def => (def,
							new FloatMenuOption(def.label, () => GenSpawn.Spawn(ThingMakerUtility.Make(def),
								_parent.FreeSlots[0].ToIntVec3, _parent.Map))))
						.ToArray();

			private IEnumerable<FloatMenuOption> FilteredFloatMenuOptions
				=> _filteredFloatMenuOptions ??= InitializeFilteredFloatMenuOptions();

			private IEnumerable<FloatMenuOption> InitializeFilteredFloatMenuOptions()
			{
				var filter = _parent.GetStoreSettings().filter;
				
				return FloatMenuOptionsByDef
					.Where(tuple => filter.Allows(tuple.def))
					.Select(static tuple => tuple.option);
			}

			private IEnumerable<FloatMenuOption> UnfilteredFloatMenuOptions
				=> _unfilteredFloatMenuOptions ??= FloatMenuOptionsByDef.Select(static tuple => tuple.option);

			public Command_AddStack(ThingClass parent) => _parent = parent;

			public override IEnumerable<FloatMenuOption> RightClickFloatMenuOptions
				=> !_parent.AnyFreeSlots
					? base.RightClickFloatMenuOptions
					: _parent.GetStoreSettings().filter.AnyAllowedDef != null
						? FilteredFloatMenuOptions
						: UnfilteredFloatMenuOptions;
		}
	}

	private record StatDrawEntries(ThingClass Parent)
	{
		public readonly StatDrawEntry ItemsPerCell
			= new(StatCategoryDefOf.Building, Strings.TranslatedWithBackup.MaxNumStacks,
				Parent.DefaultMaxItemsInCell().ToStringCached(), Strings.TranslatedWithBackup.MaxNumStacksDesc, 11);
	}
}