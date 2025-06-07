// Copyright (c) 2023 bradson
// This Source Code Form is subject to the terms of the MIT license.
// If a copy of the license was not distributed with this file,
// You can obtain one at https://opensource.org/licenses/MIT/.

using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using AdaptiveStorage.Fishery;
using AdaptiveStorage.ModCompatibility;

namespace AdaptiveStorage;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class ThingClass : Building_Storage, ISlotGroupParent, ITransformable.ITransformable, IThingHolder
{
	public List<GraphicsDef>? AllGraphics => Renderer?.AllGraphics;

	public int RandomSeed => thingIDNumber;

	public event Action<Thing>?
		ReceivedThing,
		LostThing,
		ItemStackChanged;

	public static event Action<Thing>? Initialized;

	public event ThingGridEventHandler?
		ItemRegisteredAtCell,
		ItemDeregisteredAtCell;

	public event Action<Map, SpawnMode>?
		Spawning,
		PostSpawned;

	public event Action<DestroyMode, SpawnMode>?
		DeSpawning,
		DeSpawned;

	public event Action?
		StorageSettingsChanged,
		SlotLimitChanged;

	public event Action<StorageCell>? SlotLimitChangedAtCell;

	public ThingCollection StoredThings => _storedThings;

	public ReadOnlyCollection<StorageCell> FreeStorageSlots => _storedThings.FreeStorageSlots;

	public IEnumerable<IntVec3> FreeMapCells => _storedThings.FreeMapCells;

	public int CurrentSlotLimit
	{
		get => Math.Min(_currentSlotLimit, TotalSlots);
		set
		{
			if (value == _currentSlotLimit)
				return;

			_currentSlotLimitPerCell = ((value - 1) / CellCount) + 1;
			_currentSlotLimit = value;
			UpdateMaxItemsInCell();
			Notify_SettingsChanged();
			SlotLimitChanged?.Invoke();
		}
	}

	public bool AnyFreeSlots => StoredThings.CellWiseCount < CurrentSlotLimit;

	public int TotalSlots { get; private set; }

	public bool ContentsPacked => _contentsPacked;

	public int TotalThingCount => StoredThings.TotalStackCount;

	public virtual string?[]? GUIOverlayLabels => _cachedGUIOverlayLabels;

	public override string LabelNoCount
		=> HitPoints == _cachedLabelHitPoints && _cachedLabel is { } text ? text : UpdateLabelNoCount();

	public override int MaxItemsInCell => _maxItemsInCell;

	public virtual SectionLayer? CurrentSectionLayer
		=> SpawnedParentOrMe?.Map.mapDrawer is { sections: not null } drawer
			? drawer.SectionAt(PositionHeld).GetLayer(typeof(SectionLayer_ThingsGeneral))
			: null;

	public CellRect OccupiedRect { get; private set; }

	public IntVec3 BottomLeftCell { get; private set; }

	public IntVec2 Size { get; private set; }

	public bool HasMouseOver
	{
		get
		{
			var size = Size.RotatedFor(this);
			var bottomLeft = BottomLeftCell;
			var mousePosition = HarmonyPatches.CacheZoomAndMousePosition.Position;

			return ((uint)(mousePosition.x - bottomLeft.x) < (uint)size.x)
				& ((uint)(mousePosition.z - bottomLeft.z) < (uint)size.z);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool ContainsAndAllows(Thing thing) => _storedThings.ContainsAndAllows(thing);

	public StorageCell GetStorageCell(in IntVec3 mapCell)
	{
		var bottomLeftCell = BottomLeftCell;
		IntVec2 result;
		result.x = mapCell.x - bottomLeftCell.x;
		result.z = mapCell.z - bottomLeftCell.z;

		return new(Size.x, Rotation.IsHorizontal ? default(IntVec2) with { x = result.z, z = result.x } : result);
	}

	public IntVec3 GetMapCell(StorageCell storageCell)
	{
		var bottomLeftCell = BottomLeftCell;
		var result = storageCell.AsIntVec3;
		result.x += bottomLeftCell.x;
		result.y = 0;
		result.z += bottomLeftCell.z;

		return Rotation.IsHorizontal ? default(IntVec3) with { x = result.z, z = result.x } : result;
	}

	public Vector3 GetOffsetFromCenter(IntVec2 storageCell) => def.GetOffsetFromCenter(storageCell).RotatedFor(this);

	public int GetMaxItemsForCell(in IntVec3 cell) => GetMaxItemsForStorageCell(GetStorageCell(cell));

	public virtual int GetMaxItemsForStorageCell(StorageCell storageCell) => MaxItemsForBuildingCell(storageCell);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private ref int MaxItemsForBuildingCell(StorageCell storageCell) => ref _maxItemsByCell[storageCell.Index];

	private void UpdateMaxItemsInCell()
	{
		var slotLimit = CurrentSlotLimit;

		var maxItemsByCell = _maxItemsByCell;

		Span<int> previousMaxItemsByCell = stackalloc int[maxItemsByCell.Length];
		maxItemsByCell.CopyTo(previousMaxItemsByCell);

		ResetMaxItemsByCell(Extension?.maxItemsByCell is null ? _currentSlotLimitPerCell : slotLimit);

		var cellCount = maxItemsByCell.Length;
		var assignedSlots = maxItemsByCell.Sum();

		for (var i = cellCount; assignedSlots > slotLimit;)
		{
			i = (i <= 0 ? cellCount : i) - 1;

			ref var maxItemsForCell = ref maxItemsByCell[i];
			if (maxItemsForCell <= 0)
				continue;

			maxItemsForCell--;
			assignedSlots--;
		}

		for (var i = maxItemsByCell.Length; --i >= 0;)
		{
			if (maxItemsByCell[i] != previousMaxItemsByCell[i])
				SlotLimitChangedAtCell?.Invoke(new(Size.x, i));
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

	public StorageRenderer? Renderer { get; private set; }

	private ThingCollection _storedThings = null!;

	public Extension? Extension { get; private set; }

	public CompQuality? CompQuality { get; private set; }

	private Func<bool>[] _temperatureControlConditions = [];

	public event Func<bool> TemperatureControlConditions
	{
		add => _temperatureControlConditions = [.._temperatureControlConditions, value];
		remove
		{
			using var conditions = _temperatureControlConditions.ToPooledList();
			
			if (conditions.Remove(value))
				_temperatureControlConditions = conditions.ToArray();
		}
	}

	public bool SatisfiesTemperatureControlConditions
	{
		get
		{
			var conditions = _temperatureControlConditions;
			for (var i = 0; i < conditions.Length; i++)
			{
				if (!conditions[i]())
					return false;
			}

			return true;
		}
	}

	public QualityCategory QualityCategory => CompQuality?.Quality ?? QualityCategory.Normal;

	public int CellCount => _maxItemsByCell.Length;

	private int[] _maxItemsByCell = [];

	private int
		_cachedLabelHitPoints = -69,
		_cachedTotalThingCount = -1,
		_maxItemsInCell,
		_currentSlotLimit = int.MaxValue,
		_currentSlotLimitPerCell = int.MaxValue;

	private bool _contentsPacked, _packingNow;

	private string? _cachedLabel;

	private string?[]? _cachedGUIOverlayLabels;

	private ContentLabelWorker? _currentLabelStyle;

	private StorageSettings? _fixedStorageSettings;

	protected virtual void PostInitialize() // why does this not exist on Thing?
	{
		try
		{
			Extension = def.GetModExtension<Extension>();
			Size = def.Size;
			CompQuality = GetComp<CompQuality>();
			
			InitializeTemperatureControlConditions();
			InitializeMaxItemsByCell();
			TotalSlots = _maxItemsByCell.Sum();
			
			var currentSlotLimit = _currentSlotLimit;
			_currentSlotLimit = int.MaxValue;
			CurrentSlotLimit = currentSlotLimit;

			var storedThings = _storedThings = new(this);
			storedThings.Added += NotifyReceivedThing;
			storedThings.Removed += NotifyLostThing;

			Renderer = new(this, storedThings);
			Renderer.CurrentGraphicChanged += SetGUIOverlayLabelsDirty;

			_godModeGizmos = new(this);
			_currentGodModeGizmos = GetGodModeGizmos();
			_statDrawEntries = new(this);
			Initialized?.Invoke(this);
		}
		catch (Exception ex)
		{
			Log.Error(ex.ToString());
		}
	}

	protected virtual void InitializeTemperatureControlConditions()
	{
		_temperatureControlConditions.Clear();

		if (Extension is not { temperature: { } temperature })
			return;
		
		if (temperature.requiresPower && GetComp<CompPowerTrader>() is { } compPowerTrader)
			TemperatureControlConditions += () => compPowerTrader.PowerOn;

		if (temperature.requiresSwitchOn && GetComp<CompFlickable>() is { } compFlickable)
			TemperatureControlConditions += () => compFlickable.SwitchIsOn;

		if (temperature.requiresFuel && GetComp<CompRefuelable>() is { } compRefuelable)
			TemperatureControlConditions += () => compRefuelable.HasFuel;
	}

	private void NotifyReceivedThing(Thing thing, StorageCell cell) => ReceivedThing?.Invoke(thing);

	private void NotifyLostThing(Thing thing, StorageCell cell) => LostThing?.Invoke(thing);

	private void InitializeMaxItemsByCell()
	{
		_maxItemsByCell = new int[Size.Area];
		ResetMaxItemsByCell();
	}

	private void ResetMaxItemsByCell(int maxValue = int.MaxValue)
	{
		var defaultMaxItemsInCell = Math.Min(DefaultMaxItemsInCell(), maxValue);

		if (Extension?.maxItemsByCell is { } maxItemsByCell)
		{
			var maxValueCopy = maxValue;
			var defaultMaxItemsInCellCopy = defaultMaxItemsInCell;
			var quality = QualityCategory;
			maxItemsByCell.ToArray(_maxItemsByCell, value
				=> Math.Min(value.GetFor(quality) ?? defaultMaxItemsInCellCopy, maxValueCopy));
			_maxItemsInCell = _maxItemsByCell.Max();
		}
		else
		{
			Array.Fill(_maxItemsByCell, _maxItemsInCell = defaultMaxItemsInCell);
		}
	}

	private string?[]? UpdateGUIOverlayLabels(int newTotalThingCount)
	{
		string?[]? newGUIOverlayLabels;
		if ((_cachedTotalThingCount = newTotalThingCount) > 0)
		{
			var graphicsDef = Renderer?.CurrentGraphicVariation;
			newGUIOverlayLabels = (_currentLabelStyle = (graphicsDef?.ActiveLabelStyle
						?? ContentLabelStyleDefOf.Vanilla)?.ContentLabelWorker
					?? ContentLabelWorker.TotalCount.Instance)
				.UpdateLabels(this, newTotalThingCount, graphicsDef);
		}
		else
		{
			newGUIOverlayLabels = [];
		}

		return _cachedGUIOverlayLabels = newGUIOverlayLabels;
	}

	public void SetGUIOverlayLabelsDirty() => _cachedTotalThingCount = -1;

	private string UpdateLabelNoCount()
	{
		_cachedLabelHitPoints = HitPoints;
		return _cachedLabel = Stuff is null
			? base.LabelNoCount
			: (Extension?.labelFormat ?? LabelFormat.Default) switch
			{
				LabelFormat.NoStuff => def.label
					+ GenLabel.LabelExtras(this,
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

	/// <summary>
	/// IStoreSettingsParent, this returns the fixed baseline settings
	/// </summary>
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

	// directly access filter to bypass other mods' patches on AllowedToAccept
	public bool SettingsAllow(Thing t) => GetStoreSettings().filter.Allows(t) && FixedFilterAllows(t);

	public bool FixedFilterAllows(Thing t) => GetParentStoreSettings().filter.Allows(t);

	/// <summary>
	/// for the contents tab
	/// </summary>
	public virtual bool AllowItemForbiddingAccess => true;

	public bool HasCapacityForThing(Thing item)
		=> AnyFreeSlots
			|| (item.Spawned && OccupiedRect.Contains(item.Position)
				? ContainsAndAllows(item)
				: PerformanceFish.Active || StoredThings.AcceptsForStacking(item));

	public override void PostMake()
	{
		base.PostMake();
		PostInitialize();
	}

	public virtual Thing? Eject(Thing item, int count = int.MaxValue, bool forbid = true,
		in IntVec3 dropOffset = default)
		=> ThingMakerUtility.EjectFromStorage(this, item, count, forbid, in dropOffset);

	public sealed override void SpawnSetup(Map map, bool respawningAfterLoad)
		=> OnSpawn(map, (respawningAfterLoad ? SpawnMode.RespawningAfterLoad : SpawnMode.Default)
			| (ContentsPacked ? SpawnMode.PackContents : SpawnMode.Default));

	protected virtual void OnSpawn(Map map, SpawnMode spawnMode)
	{
		BottomLeftCell = (OccupiedRect = this.OccupiedRect())
#if V1_4
			.BottomLeft;
#else
			.Min; // wtf? why was this renamed?
#endif

		Spawning?.Invoke(map, spawnMode);
		base.SpawnSetup(map, (spawnMode & SpawnMode.RespawningAfterLoad) != 0);

		if ((spawnMode & SpawnMode.PackContents) != 0)
			UnpackStoredItems(map);

		InitializeStoredThings();
		PostSpawned?.Invoke(map, spawnMode);
	}

	private void UnpackStoredItems(Map map)
	{
		_contentsPacked = false;
		var storedThings = StoredThings;
		for (var i = 0; i < storedThings.Count; i++)
		{
			var thing = storedThings[i];
			try
			{
				var itemPosition = storedThings.MapPositionOf(thing);
				storedThings.Remove(thing, itemPosition);
				GenSpawn.Spawn(thing, itemPosition, map);
			}
			catch (Exception ex)
			{
				Log.Error($"Exception spawning packed item '{thing}' in storage '{this}' at cell {
					Position}:\n{ex}");
			}
		}

		storedThings.contentsLookMode = LookMode.Undefined;
	}

	public sealed override void DeSpawn(DestroyMode mode = DestroyMode.Vanish) => OnDeSpawn(mode, SpawnMode.Default);

	protected virtual void OnDeSpawn(DestroyMode destroyMode, SpawnMode deSpawnMode)
	{
		if ((deSpawnMode & SpawnMode.PackContents) != 0)
			PackStoredThings();
		else
			_cachedTotalThingCount = -1;

		DeSpawning?.Invoke(destroyMode, deSpawnMode);
		base.DeSpawn(destroyMode);
		DeSpawned?.Invoke(destroyMode, deSpawnMode);
	}

	private void PackStoredThings()
	{
		try
		{
			_packingNow = true;
			DeSpawnStoredThings();
		}
		finally
		{
			_packingNow = false;
			_contentsPacked = true;
		}
	}

	private void DeSpawnStoredThings()
	{
		var storedThings = StoredThings;
		for (var i = storedThings.Count; --i >= 0;)
		{
			var thing = storedThings[i];
			try
			{
				thing.DeSpawn();
				thing.holdingOwner = storedThings;
			}
			catch (Exception ex)
			{
				Log.Error($"Exception despawning packed item '{thing}' in storage '{this}' at cell {
					Position}:\n{ex}");
			}
		}

		storedThings.contentsLookMode = LookMode.Deep;
	}

	public void Minify() => Minify(DestroyMode.Vanish);

	public void Minify(bool packContents)
		=> Minify(DestroyMode.Vanish, packContents);

	public void Minify(DestroyMode destroyMode)
		=> Minify(destroyMode, def.minifiedDef.GetModExtension<MinifiedExtension>()?.packContents ?? false);

	public void Minify(DestroyMode destroyMode, bool packContents)
		=> Minify(destroyMode, packContents ? SpawnMode.PackContents : SpawnMode.Default);

	public void Minify(DestroyMode destroyMode, SpawnMode deSpawnMode)
		=> OnDeSpawn(destroyMode, SpawnMode.Minify | deSpawnMode);

	internal void InitializeStoredThings()
	{
		var storedThings = _storedThings;
		storedThings.Clear();
		var cells = AllSlotCellsList();
		var map = Map;

		for (var i = cells.Count; --i >= 0;)
		{
			var mapCell = cells[i];
			var storageCell = GetStorageCell(mapCell);
			var thingListAtCell = mapCell.GetThingListUnchecked(map);

			for (var j = thingListAtCell.Count; --j >= 0;)
			{
				var thing = thingListAtCell[j];

				if (thing.IsItem())
					storedThings.Add(thing, storageCell, thing.Position == mapCell);
			}
		}
	}

	public override void ExposeData()
	{
		base.ExposeData();

		Scribe_Values.Look(ref _currentSlotLimit, nameof(CurrentSlotLimit), int.MaxValue);
		Scribe_Values.Look(ref _contentsPacked, nameof(ContentsPacked));

		if (Scribe.mode == LoadSaveMode.LoadingVars)
		{
			PostInitialize();
			settings ??= new(this); // in case of xml change
		}

		if (ContentsPacked)
			Exposable.Scribe(_storedThings, nameof(StoredThings));
	}

	public new void Notify_SettingsChanged()
	{
		base.Notify_SettingsChanged();
		if (Renderer is { } renderer)
		{
			renderer.SetAllPrintDatasDirty();
			renderer.TryUpdateCurrentGraphic();
		}

		StorageSettingsChanged?.Invoke();
	}

	[Obsolete("Replaced with Notify_ItemRegisteredAtCell as a workaround for bugs caused by mods not calling this "
		+ "method")]
	public sealed override void Notify_ReceivedThing(Thing newItem)
	{
		base.Notify_ReceivedThing(newItem);
		if (newItem.def.ShouldRealTimeDraw() && newItem.TryGetMap() is { } map && StoredThings.Contains(newItem))
			newItem.DisableItemDrawing(map); // 2nd time because of call order in Thing.SpawnSetup
	}

	[Obsolete("Replaced with Notify_ItemDeregisteredAtCell as a workaround for bugs caused by mods not calling this "
		+ "method")]
	public sealed override void Notify_LostThing(Thing newItem)
	{
		if (!_packingNow)
			base.Notify_LostThing(newItem);
	}

	protected internal virtual void Notify_ItemRegisteredAtCell(Thing item, in IntVec3 cell)
	{
		_cachedTotalThingCount = -1;
		StoredThings.Add(item, cell);

		ItemRegisteredAtCell?.Invoke(item, in cell);
	}

	protected internal virtual void Notify_ItemDeregisteredAtCell(Thing item, in IntVec3 cell)
	{
		if (_packingNow)
			return;

		_cachedTotalThingCount = -1;

		if (!StoredThings.Remove(item, cell))
		{
			LogWarningForFailedRemoval(item);
			return;
		}

		ItemDeregisteredAtCell?.Invoke(item, in cell);
	}

	/// <summary>
	/// listens to ListerMergeables.Notify_ThingStackChanged, which is unfortunately not guaranteed to be called on
	/// changes to stackCount, especially by mods
	/// </summary>
	protected internal virtual void Notify_ItemStackChanged(Thing item)
	{
		_cachedTotalThingCount = -1;
		Renderer?.SetPrintDataDirty(item);
		ItemStackChanged?.Invoke(item);
	}

	public void GetChildHolders(List<IThingHolder> outChildren)
	{
		if (!Spawned)
			ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, StoredThings);
	}

	public ThingOwner GetDirectlyHeldThings() => Spawned ? ThingCollection.Empty : StoredThings;
	// gets null checked everywhere, except for Verse.Selector, where it throws badly enough to make buildings entirely
	// unselectable

	[MethodImpl(MethodImplOptions.NoInlining)]
	private void LogWarningForFailedRemoval(Thing thing)
		=> Log.Warning($"Tried removing thing '{thing}' with position {thing.Position} from storage '{this}' at {
			Position}, where it was not stored in. This is likely the result of a bug.\n{
				new StackTrace(2, true)}");

	internal void BasePrint(SectionLayer layer) => base.Print(layer);

	internal void BaseDrawAt(in Vector3 drawLoc, bool flip = false) => base.DrawAt(drawLoc, flip);

	public sealed override void Print(SectionLayer layer) => PrintAt(layer, new(DrawPos));

	public virtual void PrintAt(SectionLayer layer, in TransformData transformData)
	{
		if (Renderer is { } renderer)
			renderer.PrintAt(layer, transformData);
		else
			base.Print(layer);
	}

#if V1_4
	public
#else
	protected
#endif
		override void DrawAt(Vector3 drawLoc, bool flip = false)
	{
		if (Renderer is { } renderer)
			renderer.DrawAt(new(drawLoc, flip ? Vector2.one.Flip() : Vector2.one, Rotation));
		else
			base.DrawAt(drawLoc, flip);
	}

	public virtual void DrawAt(in TransformData transformData) => Renderer?.DrawAt(transformData);

#if !V1_4
	internal void BaseDynamicDrawPhaseAt(DrawPhase phase, in Vector3 drawLoc, bool flip = false)
		=> base.DynamicDrawPhaseAt(phase, drawLoc, flip);
	
	public override void DynamicDrawPhaseAt(DrawPhase phase, Vector3 drawLoc, bool flip = false)
		=> Renderer?.DynamicDrawPhaseAt(phase, new(drawLoc, flip ? Vector2.one.Flip() : Vector2.one, Rotation));
#endif

	public override IEnumerable<Gizmo> GetGizmos()
	{
		var gizmos = GizmoUtility.Filter(base.GetGizmos());

		if (DebugSettings.godMode)
			gizmos = gizmos.Concat(_currentGodModeGizmos);

		return gizmos;
	}

	protected virtual IEnumerable<Gizmo> GetGodModeGizmos()
	{
		if (AnyFreeSlots && Spawned)
			yield return _godModeGizmos.AddStack;

		if (Renderer is { } renderer && !renderer.AllGraphics.NullOrEmpty())
		{
			yield return _godModeGizmos.EditGraphics;
			yield return _godModeGizmos.UpdateGraphics;
		}
	}

	public override void Notify_ThingSelected() => InspectTabUtility.TryOpen(this);

	// LWM's: https://github.com/lilwhitemouse/RimWorld-LWM.DeepStorage/blob/master/DeepStorage/Deep_Storage_Display.cs#L364-L490
	public override void DrawGUIOverlay()
	{
		if (LabelHidden)
			return;
		
		var cachedTotalThingCount = _cachedTotalThingCount;
		if (cachedTotalThingCount < 0 || (Time.frameCount & 31) == 0)
		{
			var currentTotalThingCount = TotalThingCount;
			if (currentTotalThingCount != cachedTotalThingCount)
				UpdateGUIOverlayLabels(currentTotalThingCount);
		}

		_currentLabelStyle?.DrawGUIOverlayLabels(this);

		var comps = AllComps;
		for (var i = 0; i < comps.Count; i++)
			comps[i].DrawGUIOverlay();
	}

	public virtual bool LabelHidden
		=> (AdaptiveStorageFrameworkSettings.HideLabelsWhenZoomedOut
				&& HarmonyPatches.CacheZoomAndMousePosition.ZoomValue
				>= HarmonyPatches.CacheZoomAndMousePosition.LabelHidingZoom)
			|| !ToggleableOverlays.CheckMouseOver(this);

	public override string GetInspectString() => InspectStringUtility.GetString(this);

#if !V1_4
	public override void Notify_DefsHotReloaded()
	{
		base.Notify_DefsHotReloaded();

		Extension = def.GetModExtension<Extension>();
		Size = def.Size;
		CompQuality = GetComp<CompQuality>();
		InitializeTemperatureControlConditions();
		_fixedStorageSettings = PrepareFixedStorageSettings();
		Renderer!.Notify_DefsHotReloaded();
		_godModeGizmos = new(this);
		_statDrawEntries = new(this);
		_currentGodModeGizmos = GetGodModeGizmos();
		
		try
		{
			_godModeGizmos.UpdateGraphics.action();
		}
		catch (Exception ex)
		{
			Log.Error(ex.ToString());
		}
	}
#endif

	// https://github.com/lilwhitemouse/RimWorld-LWM.DeepStorage/blob/master/DeepStorage/CompProperties.cs#L19-L39
	public override IEnumerable<StatDrawEntry> SpecialDisplayStats()
		=> (LWM.Active ? base.SpecialDisplayStats() : base.SpecialDisplayStats().Append(_statDrawEntries.ItemsPerCell))
			.Append(_statDrawEntries.StorageCapacity);

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

	private IEnumerable<Gizmo> _currentGodModeGizmos = null!;

	private StatDrawEntries _statDrawEntries = null!;

	private InspectTabBase[]? _inspectTabs;

	private InspectTabBase? _shownContentsTab;
}
