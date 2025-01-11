// Copyright (c) 2023 bradson
// This Source Code Form is subject to the terms of the MIT license.
// If a copy of the license was not distributed with this file,
// You can obtain one at https://opensource.org/licenses/MIT/.

using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using AdaptiveStorage.Fishery;
using AdaptiveStorage.Fishery.Pools;
using AdaptiveStorage.PrintDatas;

namespace AdaptiveStorage;

[PublicAPI]
public class StorageRenderer : ITransformable
{
	public ThingClass Parent { get; }

	public List<GraphicsDef>? AllGraphics { get; private set; }
	
	public ReadOnlyCollection<PrintData> Printables { get; }
	
	public ReadOnlyCollection<PrintData> Drawables { get; }

	public bool ShowContainedItems { get; private set; } = true;

	public int CurrentGraphicIndex
	{
		get => _currentGraphicIndex;
		set
		{
			if (_currentGraphicIndex == value)
				return;

			_currentGraphicIndex = value;
			CurrentGraphicChanged?.Invoke();
		}
	}

	public int CurrentVariationIndex
	{
		get => _currentVariationIndex;
		set
		{
			if (_currentVariationIndex == value)
				return;

			_currentVariationIndex = value;
			CurrentGraphicChanged?.Invoke();
		}
	}

	[MemberNotNull(nameof(CurrentVariationGraphics))]
	public GraphicsDef? CurrentGraphicVariation { get; set; }

	public List<StorageGraphic>? CurrentVariationGraphics => CurrentGraphicVariation?.graphics;

	[MemberNotNull(nameof(CurrentGraphicVariation))]
	public StorageGraphic? CurrentGraphic { get; set; }

	public event Action? CurrentGraphicChanged;

	private bool ShouldRealTimeDraw => Parent.def.drawerType == DrawerType.RealtimeOnly || !Parent.Spawned;
	
	public Color[]? ContentColors { get; private set; }

	public bool ContentColorsDirty
	{
		get => ContentColors?[0] is { r: float.NaN };
		set
		{
			if (ContentColors is not { } colors)
				return;

			if (value)
				colors[0].r = float.NaN;
			else
				UpdateDirtyData();
		}
	}

	public bool VisibleBaseGraphic
		=> CurrentGraphic is not { } currentGraphic
			|| (currentGraphic.showBaseGraphic ?? CurrentGraphicVariation.showBaseGraphic);

	private readonly List<PrintData>
		_printables = [],
		_drawables = [];

	private int
		_currentVariationIndex = -1,
		_currentGraphicIndex = -1,
		_lastMapMeshDirtyFrame = -1;
	
	public bool AnyPrintDatasDirty { get; private set; }

	public StorageRenderer(ThingClass parent)
	{
		Parent = parent;
		Printables = new(_printables);
		Drawables = new(_drawables);
		CurrentGraphicChanged += OnCurrentGraphicChanged;
		InitializeAllGraphics();
	}

	private void InitializeAllGraphics()
	{
		var graphics = AllGraphics = GraphicsDef.Database!.TryGetValue(Parent.def);

		if (graphics is not [_, ..])
			return;

		var maxColorSourceIndex = graphics.Max(static def
				=> def.graphics.Max(static graphic
					=> graphic.graphicDatas.Max(static graphicData
						=> graphicData is null
							? 0
							: Math.Max((int)graphicData.colorOneSource, (int)graphicData.colorTwoSource))));

		if (maxColorSourceIndex >= 1 || graphics.Exists(static graphicsDef
			=> (int)graphicsDef.useDominantContentColor > 0
			|| graphicsDef.graphics.Exists(static graphic
				=> graphic.useDominantContentColor > 0)))
		{
			ContentColors = new Color[Math.Max(maxColorSourceIndex + 1, 1)];
			ContentColorsDirty = true;
		}
	}

#if !V1_4
	public virtual void Notify_DefsHotReloaded() => InitializeAllGraphics();

	public virtual void DynamicDrawPhaseAt(DrawPhase phase, in TransformData transform)
	{
		if (CurrentGraphic is null)
		{
			Parent.BaseDynamicDrawPhaseAt(phase, transform.Position, transform.IsFlipped);
			return;
		}

		UpdateDirtyData();

		if (phase == DrawPhase.Draw && ShouldRealTimeDraw)
		{
			DrawBuilding(transform);
			DrawItems(_printables, transform);
		}

		DynamicDrawPhaseOnItems(_drawables, phase, transform);

		if (phase == DrawPhase.Draw)
			Parent.Comps_PostDraw();
	}

	private void DynamicDrawPhaseOnItems(List<PrintData> printDatas, DrawPhase phase, in TransformData transformData)
		=> ForEachAtItemBase(printDatas, phase, transformData,
			static (PrintData data, DrawPhase phase, in TransformData transform)
				=> data.DynamicDrawPhaseAt(phase, transform));
#endif

	public virtual void DrawAt(in TransformData transformData)
	{
		if (CurrentGraphic is null)
		{
			Parent.BaseDrawAt(transformData.Position, transformData.IsFlipped);
			return;
		}

		UpdateDirtyData();

		if (ShouldRealTimeDraw)
		{
			DrawBuilding(transformData);
			DrawItems(_printables, transformData);
		}

		DrawItems(_drawables, transformData);

		Parent.Comps_PostDraw();
	}

	private void DrawBuilding(in TransformData transform)
	{
		var parent = Parent;
		var drawColor = parent.DrawColor;
		var drawColorTwo = parent.DrawColorTwo;
		var rotation = parent.Rotation.Rotated(transform.RotationDirection);
		
		if (!TryGetStyleGraphic(drawColor, drawColorTwo, out var styleGraphic))
		{
			if (VisibleBaseGraphic)
				parent.Graphic.Draw(transform.Position, rotation, parent);
			
			CurrentGraphic!.Worker.DrawAt(this, transform);
		}
		else
		{
			styleGraphic.Draw(transform.Position, rotation, parent);
		}
	}

	private void DrawItems(List<PrintData> printDatas, in TransformData transformData)
		=> ForEachAtItemBase(printDatas, 0, transformData,
			static (PrintData data, int _, in TransformData transform) => data.DrawAt(transform));

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
			_ => ContentColors![(int)colorSource]
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

		if (CurrentGraphic is null)
		{
			Parent.BasePrint(layer);
			return;
		}

		UpdateDirtyData(layer);

		PrintBuilding(layer, transformData);
		PrintItems(_printables, layer, transformData);

		PostPrintComps(layer);
	}

	private void PrintItems(List<PrintData> printDatas, SectionLayer layer, in TransformData transformData)
		=> ForEachAtItemBase(printDatas, layer, transformData,
			static (PrintData data, SectionLayer layer, in TransformData transform) => data.PrintAt(layer, transform));

	private void PrintBuilding(SectionLayer layer, in TransformData transform)
	{
		var parent = Parent;
		var drawColor = parent.DrawColor;
		var drawColorTwo = parent.DrawColorTwo;

		if (!TryGetStyleGraphic(drawColor, drawColorTwo, out var styleGraphic))
		{
			if (VisibleBaseGraphic)
				Parent.Graphic.PrintAt(layer, Parent, transform);
			
			CurrentGraphic!.Worker.PrintAt(layer, this, transform);
		}
		else
		{
			styleGraphic.PrintAt(layer, Parent, transform);
		}
	}

	private bool TryGetStyleGraphic(Color drawColor, Color drawColorTwo, [NotNullWhen(true)] out Graphic? styleGraphic)
		=> (styleGraphic = Parent.StyleDef?.graphicData?.GraphicColoredFor(drawColor, drawColorTwo)) != null;

	private void UpdateDirtyData(SectionLayer? layer = null)
	{
		if (ContentColorsDirty)
			ComputeDominantDrawColors();
		
		if (AnyPrintDatasDirty)
			UpdateDirtyPrintDatas(layer ?? Parent.CurrentSectionLayer!);
	}

	public void InitializeStoredThingGraphics(SectionLayer? layer)
	{
		_printables.Clear();
		_drawables.Clear();

		var storedThings = Parent.StoredThings;
		for (var i = storedThings.Count; --i >= 0;)
			AssignThingGraphic(storedThings[i], layer, false);

		UpdateCurrentGraphic();
	}

	internal void NotifyCurrentGraphicChanged() => CurrentGraphicChanged?.Invoke();

	private void OnCurrentGraphicChanged()
	{
		if (AllGraphics is null)
			return;

		CurrentGraphicVariation = AllGraphics[CurrentVariationIndex];
		CurrentGraphic
			= CurrentVariationGraphics[Mathf.Clamp(CurrentGraphicIndex, 0, CurrentVariationGraphics.Count - 1)];
		ShowContainedItems = CurrentGraphic.showContainedItems ?? CurrentGraphicVariation.showContainedItems;
		
		SetAllPrintDatasDirtyNowOrLater();
		TryDirtyParentMapMesh();
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

	public void AssignThingGraphic(Thing item, SectionLayer? layer, bool updateOthers = true)
	{
		if (updateOthers && !TryUpdateCurrentGraphic())
		{
			if (item.def.SingleCell())
			{
				SetPrintDataDirtyAtCellNowOrLater(layer, item.Position);
			}
			else
			{
				foreach (var cell in item.OccupiedRect())
					SetPrintDataDirtyAtCellNowOrLater(layer, cell);
			}
		}
		else
		{
			SetPrintDataDirtyNowOrLater(item);
		}
	}

	private void SetPrintDataDirtyAtCellNowOrLater(SectionLayer? layer, IntVec3 position, Thing? except = null)
	{
		if (UnityData.IsInMainThread)
			SetPrintDataDirtyAtCell(layer, position, except);
		else
			LongEventHandler.ExecuteWhenFinished(() => SetPrintDataDirtyAtCell(layer, position, except));
	}

	private void SetPrintDataDirtyAtCell(SectionLayer? layer, IntVec3 position, Thing? except = null)
	{
		var storedThingsAtCell = Parent.StoredThings.ItemsAtMapCell(position);
		for (var i = storedThingsAtCell.Length; --i >= 0;)
		{
			var thing = storedThingsAtCell[i];
			if (thing != except)
				SetPrintDataDirty(thing);
		}
	}

	private void UpdateDirtyPrintDatas(SectionLayer layer)
	{
		UpdateDirtyPrintDatas(_printables, layer);
		UpdateDirtyPrintDatas(_drawables, layer);
		AnyPrintDatasDirty = false;
	}

	private void UpdateDirtyPrintDatas(List<PrintData> printDatas, SectionLayer? layer)
	{
		var anyUpdated = false;
		for (var i = printDatas.Count; --i >= 0;)
		{
			var printable = printDatas[i];
			if (!printable.Dirty)
				continue;

			printable.Dirty = false;
			anyUpdated = true;

			if (GetItemGraphicIfVisible(printable.Thing) is { } itemGraphic)
				itemGraphic.Worker.UpdatePrintData(printable, Parent);
			else
				printDatas.Remove(printable);
		}

		ContentColorsDirty = anyUpdated;
	}

	public void SetAllPrintDatasDirtyNowOrLater()
	{
		if (UnityData.IsInMainThread)
			SetAllPrintDatasDirty();
		else
			LongEventHandler.ExecuteWhenFinished(SetAllPrintDatasDirty);
	}

	public void SetPrintDataDirtyNowOrLater(Thing thing)
	{
		if (UnityData.IsInMainThread)
			SetPrintDataDirty(thing);
		else
			LongEventHandler.ExecuteWhenFinished(() => SetPrintDataDirty(thing));
	}

	public void SetAllPrintDatasDirty()
	{
		var storedThings = Parent.StoredThings;
		for (var i = 0; i < storedThings.Count; i++)
			SetPrintDataDirty(storedThings[i]);
	}

	public void SetPrintDataDirty(Thing thing) => SetPrintDataDirty(thing, thing.def.drawerType);

	private void SetPrintDataDirty(Thing thing, DrawerType drawerType)
	{
		if (drawerType is DrawerType.MapMeshAndRealTime or DrawerType.RealtimeOnly)
			SetPrintDataDirty(_drawables, thing);

		if (drawerType is DrawerType.MapMeshOnly or DrawerType.MapMeshAndRealTime)
			SetPrintDataDirty(_printables, thing);
	}

	private void SetPrintDataDirty(List<PrintData> printDatas, Thing thing)
	{
		if (GetItemGraphicIfVisible(thing) != null)
		{
			printDatas.GetOrAdd(thing).Dirty = true;
			AnyPrintDatasDirty = true;
		}
		else
		{
			printDatas.Remove(thing);
		}

		TryDirtyParentMapMesh();
	}

	private ItemGraphic? GetItemGraphicIfVisible(Thing thing)
		=> (ShowContainedItems || !Parent.FixedFilterAllows(thing))
			&& (TryGetItemGraphicFor(thing) ?? ItemGraphic.Default) is { visible: true } graphic
				? graphic
			: null;

	public void UpdateCurrentGraphic()
	{
		TryUpdateCurrentGraphic();
		TryDirtyParentMapMesh();
	}

	public bool TryUpdateCurrentGraphic()
	{
		var currentVariationIndex = CurrentVariationIndex;
		var currentIndex = CurrentGraphicIndex;
		CalculateGraphicIndex();
		
		return currentIndex != CurrentGraphicIndex || currentVariationIndex != CurrentVariationIndex;
	}

	public void FreeThingGraphic(Thing item, SectionLayer? layer, bool updateOthers = true)
	{
		var drawerType = item.def.drawerType;

		if (drawerType is DrawerType.MapMeshAndRealTime or DrawerType.RealtimeOnly)
			_drawables.Remove(item);

		if (drawerType is DrawerType.MapMeshOnly or DrawerType.MapMeshAndRealTime)
		{
			_printables.Remove(item);
			TryDirtyParentMapMesh();
		}

		if (updateOthers && !TryUpdateCurrentGraphic())
		{
			if (item.def.SingleCell())
			{
				SetPrintDataDirtyAtCellNowOrLater(layer, item.Position, item);
			}
			else
			{
				foreach (var cell in item.OccupiedRect())
					SetPrintDataDirtyAtCellNowOrLater(layer, cell, item);
			}
		}
	}

	public void TryDirtyParentMapMesh()
	{
		if (Parent.TryGetMap() is not { } map
			|| Time.frameCount == _lastMapMeshDirtyFrame
			|| map.mapDrawer is not { sections: not null})
		{
			return;
		}

		Parent.DirtyMapMesh(map);
		_lastMapMeshDirtyFrame = Time.frameCount;
	}

	private void PostPrintComps(SectionLayer layer)
	{
		Parent.AllComps.UnwrapReadOnlyArray(out var comps, out var count);
		for (var i = count; --i >= 0;)
			comps[i].PostPrintOnto(layer);
	}

	private void ComputeDominantDrawColors()
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
		if (Parent.Stuff == null)
		{
			GetColorFromRecipe();
			return;
		}

		var stuffColor = Parent.def.GetColorForStuff(Parent.Stuff);
		Array.Fill(ContentColors, stuffColor);
	}

	private void GetColorFromRecipe()
	{
		var contentColors = ContentColors!;
		var costList = Parent.def.CostList;
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
						? Parent.def.GetColorForStuff(ingredientDef)
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

		var thingPosition = (thing.Position - Parent.BottomLeftCell).ToIntVec2.RotatedFor(Parent); // TODO: use storage cell

		if (((uint)thingPosition.x < (uint)itemGraphics.Width) & ((uint)thingPosition.z < (uint)itemGraphics.Height))
			return itemGraphics[thingPosition];

		FailureGettingItemGraphicAtPosition(thing);
		return null;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private void FailureGettingItemGraphicAtPosition(Thing thing)
	{
		Log.Warning($"Failed rendering thing '{thing}' with position {thing.Position} within storage building '{
			Parent}', which has a position of {Parent.Position}. This is likely the result of a bug that occured when "
			+ $"the thing's position changed, perhaps through ragdoll or teleportation effects.\n{
				new StackTrace(1, true)}");
		
		Parent.FreeAllThingGraphics();
		Parent.InitializeStoredThings();
		InitializeStoredThingGraphics(Parent.CurrentSectionLayer);
	}
}