﻿// Copyright (c) 2023 bradson
// This Source Code Form is subject to the terms of the MIT license.
// If a copy of the license was not distributed with this file,
// You can obtain one at https://opensource.org/licenses/MIT/.

using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using AdaptiveStorage.Fishery;
using AdaptiveStorage.Fishery.Collections;
using AdaptiveStorage.Fishery.Pools;
using AdaptiveStorage.Fishery.Utility.Diagnostics;
using AdaptiveStorage.PrintDatas;

namespace AdaptiveStorage;

[PublicAPI]
public class StorageRenderer : ITransformable.ITransformable
{
	public ThingClass Parent { get; }

	public List<GraphicsDef>? AllGraphics { get; private set; }
	
	public ReadOnlyCollection<PrintData> Printables { get; }
	
	public ReadOnlyCollection<PrintData> Drawables { get; }

	public ReadOnlyCollection<PrintData> CurrentBuildingGraphics { get; }

	public bool ShowContainedItems { get; private set; } = true;
	
	private ThingCollection StoredThings { get; }

	public int CurrentGraphicIndex
	{
		get;
		set
		{
			if (field == value)
				return;

			field = value;
			CurrentGraphicChanged?.Invoke();
		}
	} = -1;

	public int CurrentVariationIndex
	{
		get;
		set
		{
			if (field == value)
				return;

			field = value;
			CurrentGraphicChanged?.Invoke();
		}
	} = -1;

	[MemberNotNull(nameof(CurrentVariationGraphics))]
	public GraphicsDef? CurrentGraphicVariation { get; set; }

	public List<StorageGraphic>? CurrentVariationGraphics => CurrentGraphicVariation?.graphics;

	[MemberNotNull(nameof(CurrentGraphicVariation))]
	public StorageGraphic? CurrentGraphic { get; set; }

	public event Action?
		CurrentGraphicChanged,
		PostDraw;

	public event Action<SectionLayer>? PostPrint;

	private bool ShouldRealTimeDraw
		=> Parent.SpawnedParentOrMe is not { } spawnedParent
			|| spawnedParent.def.drawerType == DrawerType.RealtimeOnly;
	
	public Color[]? ContentColors { get; private set; }

	public bool ContentColorsDirty
	{
		get => ContentColors is { } colors && float.IsNaN(colors[0].r);
		set
		{
			if (ContentColors is not { } colors)
				return;

			ref var dirtyFlag = ref colors[0].r;

			if (value)
			{
				dirtyFlag = float.NaN;
				BuildingGraphicsDirty = true;
			}
			else if (float.IsNaN(dirtyFlag))
			{
				UpdateContentColors();
				UpdateBuildingGraphics();
			}
		}
	}

	public bool VisibleBaseGraphic
		=> CurrentGraphic is not { } currentGraphic
			|| (currentGraphic.showBaseGraphic ?? CurrentGraphicVariation.showBaseGraphic ?? false);

	private readonly List<PrintData>
		_printables = [],
		_drawables = [],
		_buildingGraphics = [];

	private readonly IntFishTable<PrintData> _printDatasByThing = [];

	private int _lastMapMeshDirtyFrame = -1;

	private Thing? _previousSpawnedParent;

	public bool AnyPrintDatasDirty { get; private set; } = true;

	public bool BuildingGraphicsDirty
	{
		get => field || ParentStatsDirty;
		set
		{
			field = value;
			var parent = Parent;
			_previousColorOne = parent.DrawColor;
			_previousColorTwo = parent.DrawColorTwo;
		}
	} = true;

	private bool ParentStatsDirty // TODO: Move into GraphicsDefSelector
		=> Parent is var parent
			&& (parent.DrawColor != _previousColorOne
				|| parent.DrawColorTwo != _previousColorTwo
				|| (CurrentGraphicVariation is { buildingFilter: { } filter } && !filter.Allows(parent)));

	private Color
		_previousColorOne,
		_previousColorTwo;

	public StorageRenderer(ThingClass parent, ThingCollection thingCollection)
	{
		Parent = parent;
		var storedThings = StoredThings = thingCollection;
		Printables = new(_printables);
		Drawables = new(_drawables);
		CurrentBuildingGraphics = new(_buildingGraphics);
		CurrentGraphicChanged += OnCurrentGraphicChanged;
		InitializeAllGraphics();

		storedThings.Added += AssignThingGraphic;
		storedThings.Removed += FreeThingGraphic;
		parent.PostSpawned += UpdateGraphicAfterSpawning;
	}

	private void UpdateGraphicAfterSpawning(Map map, SpawnMode spawnMode) => UpdateGraphicAfterSpawning();

	public void UpdateGraphicAfterSpawning()
	{
		if (!TryUpdateCurrentGraphic())
			SetAllPrintDatasDirty();

		_previousSpawnedParent = Parent.SpawnedParentOrMe;
	}

	private void InitializeAllGraphics()
	{
		var graphics = AllGraphics = GraphicsDef.Database!.TryGetValue(Parent.def);

		if (graphics is not [_, ..])
			return;
		
		InitializeParentComps();

		if (graphics.Max(static def => def.MaxColorSourceIndex()) is >= 1 and var maxColorSourceIndex)
		{
			ContentColors = new Color[Math.Max(maxColorSourceIndex, 1)];
			ContentColorsDirty = true;
		}
	}

	private void InitializeParentComps()
	{
		PostDraw = null;
		PostPrint = null;
		
		var comps = Parent.AllComps;
		foreach (var comp in comps)
		{
			if (comp.OverridesPostDraw())
				PostDraw += comp.PostDraw;

			if (comp.OverridesPostPrint())
				PostPrint += comp.PostPrintOnto;
		}
	}

	public virtual void Notify_DefsHotReloaded()
	{
		InitializeAllGraphics();
		InitializeStoredThingGraphics();
		CurrentGraphicChanged?.Invoke();
	}

#if !V1_4
	public virtual void DynamicDrawPhaseAt(DrawPhase phase, in TransformData transform)
	{
		if (phase != DrawPhase.ParallelPreDraw)
			UpdateDirtyData();

		if (CurrentGraphic is null)
		{
			Parent.BaseDynamicDrawPhaseAt(phase, transform.Position, transform.IsFlipped);
			return;
		}

		if (phase == DrawPhase.Draw && ShouldRealTimeDraw)
		{
			DrawBuilding(transform);
			DrawItems(_printables, transform);
		}

		DynamicDrawPhaseOnItems(_drawables, phase, transform);

		if (phase == DrawPhase.Draw)
			PostDraw?.Invoke();
	}

	private void DynamicDrawPhaseOnItems(List<PrintData> printDatas, DrawPhase phase, in TransformData transformData)
		=> ForEachAtItemBase(printDatas, phase, transformData,
			static (data, phase, in transform) => data.DynamicDrawPhaseAt(phase, transform));
#endif

	public virtual void DrawAt(in TransformData transformData)
	{
		UpdateDirtyData();

		if (CurrentGraphic is null)
		{
			Parent.BaseDrawAt(transformData.Position, transformData.IsFlipped);
			return;
		}

		if (ShouldRealTimeDraw)
		{
			DrawBuilding(transformData);
			DrawItems(_printables, transformData);
		}

		DrawItems(_drawables, transformData);

		PostDraw?.Invoke();
	}

	private void DrawBuilding(in TransformData transform)
	{
		var storageGraphicWorker = CurrentGraphic!.Worker;
		_buildingGraphics.UnwrapReadOnlyArray(out var array, out var count);
		for (var i = 0; i < count; i++)
			storageGraphicWorker.DrawAt(array[i], transform);
	}

	private void DrawItems(List<PrintData> printDatas, in TransformData transformData)
		=> ForEachAtItemBase(printDatas, 0, transformData, static (data, _, in transform) => data.DrawAt(transform));

	private void ForEachAtItemBase<T>(List<PrintData> printDatas, T context, in TransformData transformData,
		PrintAction<T> action)
	{
		var transform = transformData;
		transform.Rot4 = Rot4.North;
		transform.Position.y -= Parent.DrawPos.y;

		printDatas.UnwrapReadOnlyArray(out var array, out var count);
		for (var i = count; --i >= 0;)
			action(array[i], context, transform);
	}

	public delegate void PrintAction<T>(PrintData printData, T context, in TransformData transformData);

	public Color GetColorFromSource(ContentColorSource colorSource)
		=> colorSource switch
		{
			ContentColorSource.ColorOne => Parent.DrawColor,
			ContentColorSource.ColorTwo => Parent.DrawColorTwo,
			ContentColorSource.White => Color.white,
			ContentColorSource.Stuff => Parent.Stuff is { } stuff ? Parent.def.GetColorForStuff(stuff) : Color.white,
			ContentColorSource.GraphicData => Parent.def.graphicData?.color ?? Color.white,
			_ => ContentColors![(int)colorSource - 1]
		};

	[MethodImpl(MethodImplOptions.NoInlining)]
	private void LogFailedPrintAttempt(in Vector3 drawLoc)
		=> Log.Error($"Print for '{Parent}' at drawLoc '{drawLoc}' had null section layer passed in. Sections "
			+ $"should never be null. This could be an indicator for parts of the map having failed to load.\n{
				new StackTrace(true)}");

	public void PrintAt(SectionLayer layer, in Vector3 drawLoc) => PrintAt(layer, new TransformData(drawLoc));

	public virtual void PrintAt(SectionLayer layer, in TransformData transformData)
	{
		if (layer == null!)
		{
			LogFailedPrintAttempt(transformData.Position);
			return;
		}

		try
		{
			_currentlyPrinting = true;
			UpdateDirtyData();

			if (CurrentGraphic is null)
			{
				Parent.BasePrint(layer);
				return;
			}

			PrintBuilding(layer, transformData);
			PrintItems(_printables, layer, transformData);
		}
		finally
		{
			_currentlyPrinting = false;
		}

		PostPrint?.Invoke(layer);
	}

	private bool _currentlyPrinting;

	private void PrintItems(List<PrintData> printDatas, SectionLayer layer, in TransformData transformData)
		=> ForEachAtItemBase(printDatas, layer, transformData,
			static (data, layer, in transform) => data.PrintAt(layer, transform));

	private void PrintBuilding(SectionLayer layer, in TransformData transform)
	{
		var storageGraphicWorker = CurrentGraphic!.Worker;
		_buildingGraphics.UnwrapReadOnlyArray(out var array, out var count);
		for (var i = 0; i < count; i++)
			storageGraphicWorker.PrintAt(layer, array[i], transform);
	}

	private Graphic? TryGetStyleGraphic()
	{
		var parent = Parent;
		return parent.StyleDef?.graphicData?.GraphicColoredFor(parent.DrawColor, parent.DrawColorTwo);
	}

	private void UpdateDirtyData()
	{
		if (Parent.SpawnedParentOrMe != _previousSpawnedParent)
			UpdateGraphicAfterSpawning();
		
		if (ContentColorsDirty)
			UpdateContentColors();
		
		if (BuildingGraphicsDirty)
			UpdateBuildingGraphics();

		if (AnyPrintDatasDirty)
			UpdateDirtyPrintDatas();
	}

	private void UpdateBuildingGraphics()
	{
		var graphicIndex = 0;

		if (TryGetStyleGraphic() is not { } styleGraphic)
		{
			if (VisibleBaseGraphic)
				UpdateBuildingGraphicAtIndex(graphicIndex++, Parent.Graphic, null);

			var storageGraphicWorker = CurrentGraphic!.Worker;
			var storageGraphicDatas = CurrentGraphic!.graphicDatas;
			for (var i = 0; i < storageGraphicDatas.Count; i++)
			{
				var storageGraphicData = storageGraphicDatas[i];
				if (storageGraphicWorker.GetGraphicFor(storageGraphicData, this) is { } graphic)
					UpdateBuildingGraphicAtIndex(graphicIndex++, graphic, storageGraphicData);
			}
		}
		else
		{
			UpdateBuildingGraphicAtIndex(graphicIndex++, styleGraphic, null);
		}

		var buildingGraphics = _buildingGraphics;
		if (buildingGraphics.Count != graphicIndex)
			buildingGraphics.RemoveRange(graphicIndex, buildingGraphics.Count - graphicIndex);

		BuildingGraphicsDirty = false;
	}

	private void UpdateBuildingGraphicAtIndex(int graphicIndex, Graphic graphic, StorageGraphicData? storageGraphicData)
	{
		var buildingGraphics = _buildingGraphics;
		EnsureDataAtIndexMatches(buildingGraphics, graphicIndex, graphic, Parent);
		ApplyStorageGraphicWorker(CurrentGraphic!.Worker, buildingGraphics[graphicIndex], storageGraphicData);
	}

	private void EnsureDataAtIndexMatches(List<PrintData> printDatas, int index, Graphic graphic, Thing thing)
	{
		Guard.IsGreaterThanOrEqualTo(printDatas.Count, index);

		if (printDatas.Count == index)
		{
			printDatas.Add(PrintData.Create(thing, graphic, true));
		}
		else if (printDatas[index] is not { } printData
			|| printData.Graphic != graphic
			|| printData.Thing != thing)
		{
			printDatas[index] = PrintData.Create(thing, graphic, true);
		}
		else
		{
			printDatas[index].NotifyMaterialPossiblyChanged();
		}
	}

	private void ApplyStorageGraphicWorker(StorageGraphicWorker storageGraphicWorker, PrintData printData,
		StorageGraphicData? storageGraphicData)
	{
		try
		{
			storageGraphicWorker.UpdatePrintData(printData, storageGraphicData);
		}
		catch (Exception ex)
		{
			Log.Error($"Exception updating {printData} with {storageGraphicWorker} for {Parent} at {
				Parent.PositionHeld.ToString()}\n{ex}");
		}
	}

	public void InitializeStoredThingGraphics()
	{
		_printables.Clear();
		_drawables.Clear();
		_printDatasByThing.Clear();

		var storedThings = Parent.StoredThings;
		for (var i = storedThings.Count; --i >= 0;)
			AssignThingGraphic(storedThings[i], storedThings.StoragePositionAt(i), false);

		TryUpdateCurrentGraphic();
	}

	private void OnCurrentGraphicChanged()
	{
		if (AllGraphics is null)
			return;

		CurrentGraphicVariation = AllGraphics[CurrentVariationIndex];
		CurrentGraphic = GetStorageGraphicAt(CurrentVariationGraphics, CurrentGraphicIndex);
		
		ShowContainedItems = CurrentGraphic?.showContainedItems ?? CurrentGraphicVariation.showContainedItems;
		
		SetAllPrintDatasDirty();
		TryDirtyParentMapMesh();
	}

	private static StorageGraphic? GetStorageGraphicAt(List<StorageGraphic> currentVariationGraphics, int index)
	{
		var currentVariationGraphicCount = currentVariationGraphics.Count;
		return currentVariationGraphicCount > 0
			? currentVariationGraphics[Mathf.Clamp(index, 0, currentVariationGraphicCount - 1)]
			: null;
	}

	private void CalculateGraphicIndex()
	{
		CurrentVariationIndex = GetGraphicVariationIndexForStoredThings();

		var itemCount = Parent.StoredThings.CellWiseCount;
		var currentVariationGraphics = CurrentVariationGraphics;

		if (currentVariationGraphics is null)
		{
			CurrentGraphicIndex = 0;
			return;
		}

		for (var i = currentVariationGraphics.Count; --i >= 0;)
		{
			if (currentVariationGraphics[i].minimumStackCount > itemCount)
				continue;

			CurrentGraphicIndex = i;
			break;
		}
	}

	private int GetGraphicVariationIndexForStoredThings()
	{
		var allGraphics = AllGraphics;
		if (allGraphics is null)
			return 0;
		
		var building = Parent;
		var seed = building.RandomSeed;

		using var allowedGraphicsPooled = new PooledList<GraphicsDef>();
		using var forbiddenGraphicsPooled = new PooledList<GraphicsDef>();
		var allowedGraphics = allowedGraphicsPooled.List;
		var forbiddenGraphics = forbiddenGraphicsPooled.List;

		for (var i = allGraphics.Count; i-- > 0;)
		{
			var graphic = allGraphics[i];
			if (graphic.Worker.AllowedFor(building))
				allowedGraphics.Add(graphic);
			else
				forbiddenGraphics.Add(graphic);
		}
		
		for (var i = 0; i < _weightSelectorsInOrder.Length; i++)
		{
			var resultIndex = allowedGraphics.TryGetSeededIndex(seed, _weightSelectorsInOrder[i]);
			if (resultIndex >= 0)
				return allGraphics.IndexOf(allowedGraphics[resultIndex]);
		}

		for (var i = _weightSelectorsInOrder.Length; i-- > 0;)
		{
			var resultIndex = forbiddenGraphics.TryGetSeededIndex(seed, _weightSelectorsInOrder[i]);
			if (resultIndex >= 0)
				return allGraphics.IndexOf(forbiddenGraphics[resultIndex]);
		}

		return 0;
	}

	private static readonly Func<GraphicsDef, uint>[] _weightSelectorsInOrder =
	[
		GraphicsDef.PositiveWeightSelector, GraphicsDef.NegativeWeightSelector, GraphicsDef.NullWeightSelector
	];

	private void AssignThingGraphic(Thing item, StorageCell cell) => AssignThingGraphic(item, cell, true);

	private void AssignThingGraphic(Thing item, StorageCell itemPosition, bool updateOthers)
	{
		if (updateOthers && !TryUpdateCurrentGraphic())
		{
			if (item.def.SingleCell())
			{
				SetPrintDataDirtyAtCell(itemPosition);
			}
			else
			{
				foreach (var cell in item.OccupiedRect())
					SetPrintDataDirtyAtCell(cell);
			}
		}
		else
		{
			SetPrintDataDirty(item);
		}
	}

	public void SetPrintDataDirtyAtCell(IntVec3 position, Thing? except = null)
		=> SetPrintDataDirtyAtCell(Parent.GetStorageCell(position), except);

	public void SetPrintDataDirtyAtCell(StorageCell cell, Thing? except = null)
	{
		var storedThingsAtCell = Parent.StoredThings.ItemsAtStorageCell(cell);
		for (var i = storedThingsAtCell.Length; --i >= 0;)
		{
			var thing = storedThingsAtCell[i];
			if (thing != except)
				SetPrintDataDirty(thing);
		}
	}

	private void UpdateDirtyPrintDatas()
	{
		UpdateDirtyPrintDatas(_printables);
		UpdateDirtyPrintDatas(_drawables);
		AnyPrintDatasDirty = false;
	}

	private void UpdateDirtyPrintDatas(List<PrintData> printDatas)
	{
		for (var i = printDatas.Count; --i >= 0;)
		{
			var printable = printDatas[i];
			if (!printable.Dirty)
				continue;

			printable.Dirty = false;

			var thing = printable.Thing;

			if (GetItemGraphicIfVisible(thing) is not { } itemGraphic)
			{
				printDatas.RemoveAt(i);
			}
			else
			{
				var itemWorker = itemGraphic.Worker;
				var itemWorkerGraphic = itemWorker.GetGraphicFor(thing, this);

				if (printable.Graphic != itemWorkerGraphic)
				{
					RemovePrintData(printable);
					printable = AddPrintData(thing, itemWorkerGraphic);
				}

				try
				{
					itemWorker.UpdatePrintData(printable, Parent);
				}
				catch (Exception ex)
				{
					Log.Error($"Exception updating {printable} with {itemWorker} for {Parent} at {
						Parent.PositionHeld.ToString()}\n{ex}");
				}
				printable.NotifyMaterialPossiblyChanged();
				printable.Dirty = false;
			}
		}
	}

	public void SetAllPrintDatasDirty()
	{
		ContentColorsDirty = true;
		BuildingGraphicsDirty = true;
		
		var storedThings = Parent.StoredThings;
		for (var i = 0; i < storedThings.Count; i++)
			SetPrintDataDirty(storedThings[i]);
	}

	public void SetPrintDataDirty(Thing thing)
	{
		var itemGraphic = GetItemGraphicIfVisible(thing);

		if (itemGraphic == null)
		{
			if (_printDatasByThing.Remove(thing.thingIDNumber, out var printData))
				RemovePrintData(printData);
			else
				return;
		}
		else
		{
			if (TryGetPrintDataOf(thing) is not { } printData)
			{
				printData = AddPrintData(thing,
					UnityData.IsInMainThread ? itemGraphic.Worker.GetGraphicFor(thing, this) : null);
			}

			printData.Dirty = true;
			AnyPrintDatasDirty = true;
		}

		TryDirtyParentMapMesh();
	}

	private PrintData AddPrintData(Thing thing, Graphic? graphic)
	{
		var printData = PrintData.Create(thing, graphic);
		_printDatasByThing[thing.thingIDNumber] = printData;

		if (printData.ShouldPrint)
			_printables.Add(printData);
		
		if (printData.ShouldDraw)
			_drawables.Add(printData);

		return printData;
	}

	private void RemovePrintData(PrintData printData)
	{
		if (printData.ShouldPrint)
			_printables.Remove(printData);
		
		if (printData.ShouldDraw)
			_drawables.Remove(printData);
	}

	public ItemGraphic? GetItemGraphicIfVisible(Thing item)
		=> (ShowContainedItems || !Parent.FixedFilterAllows(item))
			&& (TryGetItemGraphicFor(item) ?? ItemGraphic.Default) is { visible: true } graphic
				? graphic
			: null;

	public PrintData? TryGetPrintDataOf(Thing item) => _printDatasByThing.TryGetValue(item.thingIDNumber);

	public bool TryUpdateCurrentGraphic() // true sets all printDatas and the map mesh to dirty
	{
		var currentVariationIndex = CurrentVariationIndex;
		var currentIndex = CurrentGraphicIndex;
		CalculateGraphicIndex();
		
		return currentIndex != CurrentGraphicIndex || currentVariationIndex != CurrentVariationIndex;
	}

	private void FreeThingGraphic(Thing item, StorageCell cell) => FreeThingGraphic(item, cell, true);

	private void FreeThingGraphic(Thing item, StorageCell itemPosition, bool updateOthers)
	{
		if (_printDatasByThing.Remove(item.thingIDNumber, out var printData))
		{
			RemovePrintData(printData);

			if (printData.ShouldPrint)
				TryDirtyParentMapMesh();
		}

		if (updateOthers && !TryUpdateCurrentGraphic())
		{
			if (item.def.SingleCell())
			{
				SetPrintDataDirtyAtCell(itemPosition, item);
			}
			else
			{
				foreach (var cell in item.OccupiedRect())
					SetPrintDataDirtyAtCell(cell, item);
			}
		}
	}

	public void TryDirtyParentMapMesh()
	{
		if (_currentlyPrinting)
			return;
		
		var parent = Parent.SpawnedParentOrMe;
		if (parent?.TryGetMap() is not { } map
			|| Time.frameCount == _lastMapMeshDirtyFrame
			|| map.mapDrawer is not { sections: not null} drawer
			|| drawer.SectionAt(parent.Position) is null)
		{
			return;
		}

		parent.DirtyMapMesh(map);
		_lastMapMeshDirtyFrame = Time.frameCount;
	}

	private void UpdateContentColors()
	{
		var storedThings = Parent.StoredThings;
		if (storedThings.CellWiseCount == 0)
		{
			GetColorFromIngredients();
			return;
		}

		var dict = SimplePool<Dictionary<Color, int>>.Get();
		dict.Clear();

		var parentStoreSettings = Parent.GetParentStoreSettings();
		var parentDef = Parent.def;
		for (var i = storedThings.Count; i-- > 0;)
		{
			var storedThingDef = storedThings.DefAt(i);
			if (!parentStoreSettings.AllowedToAccept(storedThingDef))
				continue;

			var color = storedThingDef is { stuffProps: not null }
				? parentDef.GetColorForStuff(storedThingDef)
				: storedThings[i].DrawColor;

			var cellCount = storedThingDef.SingleCell() ? 1 : storedThings.CellWiseCountOf(storedThings[i]);
			dict[color] = dict.TryGetValue(color, out var value) ? value + cellCount : cellCount;
		}

		if (dict.Count > 0)
		{
			using var pooledList = new PooledList<KeyValuePair<Color, int>>();
			var sortedList = pooledList.List;
			foreach (var pair in dict)
				sortedList.Add(pair);
			
			sortedList.Sort(static (x, y) => y.Value.CompareTo(x.Value));
			var contentColors = ContentColors!;
			var contentCount = sortedList.Count;
			for (var i = 0; i < contentColors.Length; i++)
				contentColors[i] = sortedList[i % contentCount].Key;
		}
		else
		{
			GetColorFromIngredients();
		}

		dict.Clear();
		SimplePool<Dictionary<Color, int>>.Return(dict);
	}

	private void GetColorFromIngredients()
	{
		if (Parent.Stuff is not { } stuff)
		{
			GetColorFromRecipe();
			return;
		}

		var stuffColor = Parent.def.GetColorForStuff(stuff);
		Array.Fill(ContentColors, stuffColor);
	}

	private void GetColorFromRecipe()
	{
		var contentColors = ContentColors!;
		var parentDef = Parent.def;
		var costList = parentDef.CostList;
		if (costList is not [_, ..])
			goto DefaultColor;

		{
			using var pooledList = costList.ToPooledList();
			var sortedCostList = pooledList.List;
			
			sortedCostList.RemoveAll(static defCount
				=> defCount.thingDef is not { } def || (def.stuffProps is null && def.graphicData is null));

			if (sortedCostList.Count > 0)
			{
				sortedCostList.Sort(static (x, y) => x.count.CompareTo(y.count));

				var ingredientCount = sortedCostList.Count;
				for (var i = 0; i < contentColors.Length; i++)
				{
					var ingredientDef = sortedCostList[i % ingredientCount].thingDef;
					contentColors[i] = ingredientDef.stuffProps != null
						? parentDef.GetColorForStuff(ingredientDef)
						: ingredientDef.graphicData.color;
				}

				return;
			}
		}

	DefaultColor:
		Array.Fill(contentColors, Color.white);
	}

	public ItemGraphic? TryGetItemGraphicFor(Thing thing)
	{
		if (CurrentGraphicVariation?.itemGraphics is not { Area: > 0 } itemGraphics)
			return null;

		if (Parent.StoredThings.TryGetStoragePositionOf(thing, out var storageCell))
		{
			var thingPosition = storageCell.AsIntVec2;

			if (((uint)thingPosition.x < (uint)itemGraphics.Width)
				& ((uint)thingPosition.z < (uint)itemGraphics.Height))
			{
				return itemGraphics[thingPosition];
			}
		}

		FailureGettingItemGraphicAtPosition(thing);
		return null;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private void FailureGettingItemGraphicAtPosition(Thing thing)
	{
		var text = $"Failed rendering thing '{thing}' with position {thing.Position} within storage building '{
			Parent}', which has a position of {Parent.Position}. This is likely the result of a bug that occured when "
			+ $"the thing's position changed, perhaps through ragdoll or teleportation effects.\n{
				new StackTrace(1, true)}";

		if (!_recoveringFromGettingItemGraphicAtPositionError)
		{
			try
			{
				_recoveringFromGettingItemGraphicAtPositionError = true;
				Log.Warning(text);

				Parent.InitializeStoredThings();
				InitializeStoredThingGraphics();
			}
			catch (Exception ex)
			{
				Log.Error($"Exception during error recovery for invalid thing position involving thing '{
					thing}' with position {thing.Position} within storage building '{
						Parent}', which has a position of {Parent.Position}:\n{ex}");
			}
			finally
			{
				_recoveringFromGettingItemGraphicAtPositionError = false;
			}
		}
		else
		{
			Log.Error(text);
		}
	}

	private bool _recoveringFromGettingItemGraphicAtPositionError;
}
