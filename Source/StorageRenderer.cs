// Copyright (c) 2023 bradson
// This Source Code Form is subject to the terms of the MIT license.
// If a copy of the license was not distributed with this file,
// You can obtain one at https://opensource.org/licenses/MIT/.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using AdaptiveStorage.ModCompatibility;
using AdaptiveStorage.Pools;

namespace AdaptiveStorage;

public class StorageRenderer
{
	public ThingClass Parent { get; }

	public List<GraphicsDef>? AllGraphics { get; set; }

	public List<PrintData> Printables { get; } = [];

	public List<PrintData> Drawables { get; } = [];

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

	private int
		_currentVariationIndex = -1,
		_currentGraphicIndex = -1,
		_lastMapMeshDirtyFrame = -1;

	private static Vector2 _drawScale = Vector2.one;

	public StorageRenderer(ThingClass parent)
	{
		Parent = parent;
		AllGraphics = GraphicsDef.Database!.TryGetValue(parent.def);
		CurrentGraphicChanged += OnCurrentGraphicChanged;
	}

#if !V1_4
	public virtual void DynamicDrawPhaseAt(DrawPhase phase, in Vector3 drawLoc, bool flip = false)
	{
		if (CurrentGraphic is null)
		{
			Parent.BaseDynamicDrawPhaseAt(phase, drawLoc, flip);
			return;
		}

		if (phase == DrawPhase.Draw && ShouldRealTimeDraw)
		{
			DrawBuilding(drawLoc);
			DrawPrintableThings(drawLoc, Printables);
		}

		DynamicDrawPhaseOnThings(phase, drawLoc, flip, Drawables);

		if (phase == DrawPhase.Draw)
			Parent.Comps_PostDraw();
	}

	private static void DynamicDrawPhaseOnThings(DrawPhase phase, in Vector3 drawLoc, bool flip,
		List<PrintData> drawables)
	{
		for (var i = drawables.Count; i-- > 0;)
		{
			var drawable = drawables[i];
			drawable.Thing.DynamicDrawPhaseAt(phase, drawLoc + drawable.DrawOffset, flip);
		}
	}
#endif

	public void DrawAt(in Vector3 drawLoc, bool flip = false)
	{
		if (CurrentGraphic is null)
		{
			Parent.BaseDrawAt(drawLoc, flip);
			return;
		}

		if (ShouldRealTimeDraw)
		{
			DrawBuilding(drawLoc);
			DrawPrintableThings(drawLoc, Printables);
		}

		DrawThings(drawLoc, flip, Drawables);

		Parent.Comps_PostDraw();
	}

	private void DrawBuilding(in Vector3 drawLoc)
	{
		GetBuildingDrawColors(out var drawColor, out var drawColorTwo);

		var parent = Parent;
		var rotation = parent.Rotation;
		if (!TryGetStyleGraphic(drawColor, drawColorTwo, out var styleGraphic))
		{
			var graphicDatas = CurrentGraphic!.graphicDatas;
			for (var i = 0; i < graphicDatas.Count; i++)
				graphicDatas[i].GraphicColoredFor(drawColor, drawColorTwo).Draw(drawLoc, rotation, parent);
		}
		else
		{
			styleGraphic.Draw(drawLoc, rotation, parent);
		}
	}

	private static void DrawThings(in Vector3 drawLoc, bool flip, List<PrintData> drawables)
	{
		for (var i = drawables.Count; i-- > 0;)
		{
			var drawable = drawables[i];
			drawable.Thing.DrawNowAt(drawLoc + drawable.DrawOffset, flip);
		}
	}

	private static void DrawPrintableThings(in Vector3 drawLoc, List<PrintData> printables)
	{
		for (var i = printables.Count; i-- > 0;)
			printables[i].DrawAt(drawLoc);
	}

	private bool TryGetCurrentDrawSize(out Vector2 drawSize)
	{
		drawSize = Vector2.one;
		if (CurrentGraphic is not { } currentGraphic)
			return false;

		if (TryGetStyleGraphic(Color.white, Color.white, out var styleGraphic))
		{
			var styleDrawSize = styleGraphic.drawSize;
			if (1f > styleDrawSize.x * styleDrawSize.y)
				return false;

			drawSize = styleDrawSize;
			return true;
		}

		var graphicDatas = currentGraphic.graphicDatas;
		var i = graphicDatas.Count;
		if (i == 0)
			return false;

		var result = false;
		while (i-- > 0)
		{
			var otherDrawSize = graphicDatas[i].drawSize;
			if (drawSize.x * drawSize.y > otherDrawSize.x * otherDrawSize.y)
				continue;

			drawSize = otherDrawSize;
			result = true;
		}

		return result;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private void LogFailedPrintAttempt(in Vector3 drawLoc)
		=> Log.Error($"Print for '{Parent}' at drawLoc '{drawLoc}' had null section layer passed in. Sections "
			+ $"should never be null and could be an indicator for parts of the map having failed to load.\n{
				new StackTrace(true)}");

	public void PrintAt(SectionLayer layer, in Vector3 drawLoc, in Vector2 drawSize)
	{
		_drawScale = TryGetCurrentDrawSize(out var size) ? drawSize / size : Vector2.one;
		PrintAt(layer, drawLoc);
	}

	public void PrintAt(SectionLayer layer, in Vector3 drawLoc)
	{
		if (layer == null!)
		{
			LogFailedPrintAttempt(drawLoc);
			return;
		}
		
		if (CurrentGraphic is null)
		{
			Parent.BasePrint(layer);
			return;
		}

		PrintBuilding(layer, drawLoc);

		UpdateAllPrintDatas(layer);
		for (var i = Printables.Count; i-- > 0;)
			Printables[i].PrintAt(layer, drawLoc);

		PostPrintComps(layer);
	}

	private void PrintBuilding(SectionLayer layer, in Vector3 drawLoc)
	{
		GetBuildingDrawColors(out var drawColor, out var drawColorTwo);

		if (!TryGetStyleGraphic(drawColor, drawColorTwo, out var styleGraphic))
		{
			var graphicDatas = CurrentGraphic!.graphicDatas;
			for (var i = 0; i < graphicDatas.Count; i++)
				PrintGraphicAt(layer, drawLoc, graphicDatas[i].GraphicColoredFor(drawColor, drawColorTwo));
		}
		else
		{
			PrintGraphicAt(layer, drawLoc, styleGraphic);
		}
	}

	private void PrintGraphicAt(SectionLayer layer, in Vector3 drawLoc, Graphic graphic)
		=> graphic.PrintAt(layer, Parent, drawLoc, graphic.drawSize * _drawScale, 0f);

	private bool TryGetStyleGraphic(Color drawColor, Color drawColorTwo, [NotNullWhen(true)] out Graphic? styleGraphic)
		=> (styleGraphic = Parent.StyleDef?.graphicData?.GraphicColoredFor(drawColor, drawColorTwo)) != null;

	private void GetBuildingDrawColors(out Color drawColor, out Color drawColorTwo)
	{
		drawColor = CurrentGraphic!.useDominantContentColor
			?? CurrentGraphicVariation.useDominantContentColor
				? ComputeDominantDrawColor()
				: Parent.DrawColor;

		drawColorTwo = Parent.DrawColorTwo;
	}

	public void InitializeStoredThingGraphics(SectionLayer? layer)
	{
		Printables.Clear();
		Drawables.Clear();

		var storedThings = Parent.StoredThings;
		for (var i = storedThings.Count; i-- > 0;)
			AssignThingGraphic(storedThings[i], layer, false);

		UpdateCurrentGraphic();
	}

	public void RegenerateThingGraphic(Thing thing)
	{
		if (!thing.Spawned)
			return;

		var drawerType = thing.def.drawerType;

		var drawable = drawerType is DrawerType.MapMeshAndRealTime or DrawerType.RealtimeOnly;
		var printable = drawerType is DrawerType.MapMeshOnly or DrawerType.MapMeshAndRealTime;

		if (drawable)
		{
			Drawables.Remove(thing);
			Parent.Map.dynamicDrawManager.RegisterDrawable(thing); // drawThings is a HashSet
		}

		if (printable)
		{
			Printables.Remove(thing);
			TryDirtyParentMapMesh();
		}

		if (!ThingRequestGroup.HasGUIOverlay.Includes(thing.def))
			return;

		var lister = Parent.Map.listerThings;
		var guiOverlayGroup = lister.ThingsInGroup(ThingRequestGroup.HasGUIOverlay);

		if (guiOverlayGroup.Contains(thing))
			return;

		if (!PerformanceFish.AddToGroupList(lister, thing, ThingRequestGroup.HasGUIOverlay))
			guiOverlayGroup.Add(thing);
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
	}

	private void CalculateGraphicIndex()
	{
		CurrentVariationIndex = GetGraphicVariationIndexForStoredThings();

		var itemCount = Parent.StoredThings.Count;
		var currentVariationGraphics = CurrentVariationGraphics;

		if (currentVariationGraphics is null)
		{
			CurrentGraphicIndex = 0;
			return;
		}

		for (var i = currentVariationGraphics.Count; i-- > 0;)
		{
			if (currentVariationGraphics[i].minimumStackCount > itemCount)
				continue;

			CurrentGraphicIndex = i;
			break;
		}
	}

	private int GetGraphicVariationIndexForStoredThings()
	{
		var allGraphics = AllGraphics!;
		var seed = Parent.RandomSeed;
		var storedThings = Parent.StoredThings;

		using var allowedGraphicsPooled = new PooledIList<List<GraphicsDef>>();
		using var forbiddenGraphicsPooled = new PooledIList<List<GraphicsDef>>();
		var allowedGraphics = allowedGraphicsPooled.List;
		var forbiddenGraphics = forbiddenGraphicsPooled.List;

		for (var i = allGraphics.Count; i-- > 0;)
		{
			var graphic = allGraphics[i];
			if (graphic.Allows(storedThings))
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

	private void AssignToDrawablesNowOrLater(Thing newItem, SectionLayer? layer)
	{
		if (UnityData.IsInMainThread)
			AssignToDrawables(newItem, layer);
		else
			LongEventHandler.ExecuteWhenFinished(() => AssignToDrawables(newItem, layer));
	}

	private void AssignToDrawables(Thing newItem, SectionLayer? layer)
	{
		if (MakePrintData(newItem, layer) is { } printData)
			Drawables.Add(printData);

		Parent.Map.dynamicDrawManager.DeRegisterDrawable(newItem);
	}

	private void AssignToPrintablesNowOrLater(Thing newItem, SectionLayer? layer)
	{
		if (UnityData.IsInMainThread)
			AssignToPrintables(newItem, layer);
		else
			LongEventHandler.ExecuteWhenFinished(() => AssignToPrintables(newItem, layer));
	}

	private void AssignToPrintables(Thing newItem, SectionLayer? layer)
	{
		if (MakePrintData(newItem, layer) is not { } printData)
			return;

		Printables.Add(printData);
		TryDirtyParentMapMesh();
	}

	public void AssignThingGraphic(Thing newItem, SectionLayer? layer, bool updateOthers = true)
	{
		var newItemDef = newItem.def;
		var drawerType = newItemDef.drawerType;

		if (drawerType is DrawerType.MapMeshAndRealTime or DrawerType.RealtimeOnly)
			AssignToDrawablesNowOrLater(newItem, layer);

		if (drawerType is DrawerType.MapMeshOnly or DrawerType.MapMeshAndRealTime)
			AssignToPrintablesNowOrLater(newItem, layer);

		if (updateOthers)
			UpdateAllPrintDatasNowOrLater(layer, newItem);

		if (!ThingRequestGroup.HasGUIOverlay.Includes(newItem.def))
			return;

		var lister = Parent.Map.listerThings;

		if (!PerformanceFish.RemoveFromGroupList(lister, newItem, ThingRequestGroup.HasGUIOverlay))
			lister.ThingsInGroup(ThingRequestGroup.HasGUIOverlay).Remove(newItem);
	}

	private void UpdateAllPrintDatasNowOrLater(SectionLayer? layer, Thing? except = null)
	{
		if (UnityData.IsInMainThread)
			UpdateAllPrintDatas(layer, except);
		else
			LongEventHandler.ExecuteWhenFinished(() => UpdateAllPrintDatas(layer, except));
	}

	private void UpdateAllPrintDatas(SectionLayer? layer, Thing? except = null)
	{
		var allStoredThings = Parent.StoredThings;
		for (var i = allStoredThings.Count; i-- > 0;)
		{
			var thing = allStoredThings[i];
			if (thing == except)
				continue;

			var drawerType = allStoredThings.DefAt(i).drawerType;

			if (drawerType is DrawerType.MapMeshAndRealTime or DrawerType.RealtimeOnly)
				UpdatePrintDataFor(Drawables, layer, thing);

			if (drawerType is DrawerType.MapMeshOnly or DrawerType.MapMeshAndRealTime)
				UpdatePrintDataFor(Printables, layer, thing);
		}
	}

	private void UpdatePrintDataFor(List<PrintData> printDatas, SectionLayer? layer, Thing thing)
	{
		var itemGraphic = GetItemGraphicIfVisible(thing);
		if (itemGraphic != null)
		{
			PrintData? printData;
			if ((printData = printDatas.TryGet(thing)) != null)
			{
				if (UpdatePrintData(printData, layer, itemGraphic))
					return;
			}
			else if ((printData = MakePrintData(thing, layer, itemGraphic)) != null)
			{
				printDatas.Add(printData);
				return;
			}
		}

		printDatas.Remove(thing);
	}

	private PrintData? MakePrintData(Thing thing, SectionLayer? layer)
	{
		var itemGraphic = GetItemGraphicIfVisible(thing);
		return itemGraphic is null ? null : MakePrintData(thing, layer, itemGraphic);
	}

	private PrintData MakePrintData(Thing thing, SectionLayer? layer, ItemGraphic itemGraphic)
	{
		var thingRotation = itemGraphic.textureOrientation ?? thing.Rotation;
		var parentDrawLoc = Parent.DrawPos;

		var result = new PrintData(thing, parentDrawLoc,
			DrawOffsetForThing(thing, parentDrawLoc, thingRotation, itemGraphic, out var stackRotation),
			thingRotation, layer,
			thing.MultipleItemsPerCellDrawn() ? itemGraphic.drawScale * 0.8f : itemGraphic.drawScale,
			itemGraphic.rotation + stackRotation, itemGraphic.drawShadow, itemGraphic.maxDrawSize);
		
		if (_drawScale != Vector2.one)
			result.DrawSize *= _drawScale;
		
		return result;
	}

	private bool ParentAccepts(Thing thing) => Parent.GetParentStoreSettings().AllowedToAccept(thing);

	private bool UpdatePrintData(PrintData printData, SectionLayer? layer, ItemGraphic itemGraphic)
	{
		var thing = printData.Thing;
		printData.Layer = layer;

		var thingRotation = printData.ThingRotation = itemGraphic.textureOrientation ?? thing.Rotation;
		var parentDrawLoc = printData.DrawLoc = Parent.DrawPos;

		printData.DrawOffset
			= DrawOffsetForThing(thing, parentDrawLoc, thingRotation, itemGraphic, out var stackRotation);

		printData.SetDrawSize(printData.Graphic.drawSize,
			thing.MultipleItemsPerCellDrawn() ? itemGraphic.drawScale * 0.8f : itemGraphic.drawScale,
			itemGraphic.maxDrawSize);
		
		if (_drawScale != Vector2.one)
			printData.DrawSize *= _drawScale;

		printData.ExtraRotation = itemGraphic.rotation + stackRotation;
		printData.DrawShadow = itemGraphic.drawShadow;

		return true;
	}

	private ItemGraphic? GetItemGraphicIfVisible(Thing thing)
		=> (ShowContainedItems || !ParentAccepts(thing)) && GetItemGraphicFor(thing) is { visible: true } graphic
			? graphic
			: null;

	public void UpdateCurrentGraphic()
	{
		var currentIndex = CurrentGraphicIndex;
		CalculateGraphicIndex();
		if (currentIndex != CurrentGraphicIndex)
			TryDirtyParentMapMesh();
	}

	public void FreeThingGraphic(Thing newItem, SectionLayer? layer, bool updateOthers = true)
	{
		var newItemDef = newItem.def;
		var drawerType = newItemDef.drawerType;

		var drawable = drawerType is DrawerType.MapMeshAndRealTime or DrawerType.RealtimeOnly;
		var printable = drawerType is DrawerType.MapMeshOnly or DrawerType.MapMeshAndRealTime;

		if (drawable)
			Drawables.Remove(newItem);

		if (printable)
		{
			Printables.Remove(newItem);
			TryDirtyParentMapMesh();
		}

		if (updateOthers)
			UpdateAllPrintDatasNowOrLater(layer, newItem);
	}

	public void TryDirtyParentMapMesh()
	{
		if (!Parent.Spawned || Time.frameCount == _lastMapMeshDirtyFrame)
			return;

		Parent.DirtyMapMesh(Parent.Map);
		_lastMapMeshDirtyFrame = Time.frameCount;
	}

	private void PostPrintComps(SectionLayer layer)
	{
		var comps = Parent.AllComps;
		for (var i = comps.Count; i-- > 0;)
			comps[i].PostPrintOnto(layer);
	}

	private Color ComputeDominantDrawColor()
	{
		var storedThings = Parent.StoredThings;
		if (storedThings.Count == 0)
			return GetColorFromIngredients();

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

			dict[color] = dict.TryGetValue(color, out var value) ? value + 1 : 1;
		}

		var dominantColorPair = new KeyValuePair<Color, int>(Color.white, 0);

		if (dict.Count > 0)
		{
			foreach (var pair in dict)
			{
				if (pair.Value > dominantColorPair.Value)
					dominantColorPair = pair;
			}
		}
		else
		{
			dominantColorPair = new(GetColorFromIngredients(), 0);
		}

		dict.Clear();
		SimplePool<Dictionary<Color, int>>.Return(dict);

		return dominantColorPair.Key;
	}

	private Color GetColorFromIngredients()
		=> Parent.Stuff != null
			? Parent.def.GetColorForStuff(Parent.Stuff)
			: GetColorFromRecipe();

	private Color GetColorFromRecipe()
	{
		var costList = Parent.def.CostList;
		if (costList is not [_, ..])
			goto DefaultColor;

		var max = costList[0];
		for (var i = costList.Count; i-- > 0;)
		{
			var thingDefCountClass = costList[i];
			if (thingDefCountClass.count > max.count
				&& thingDefCountClass.thingDef.graphicData != null)
			{
				max = thingDefCountClass;
			}
		}

		if (max.thingDef is { } maxThingDef)
		{
			if (maxThingDef.stuffProps != null)
				return Parent.def.GetColorForStuff(maxThingDef);
			else if (maxThingDef.graphicData is { } graphicData)
				return graphicData.color;
		}

	DefaultColor:
		return Color.white;
	}

	private ItemGraphic GetItemGraphicFor(Thing thing)
	{
		var thingPosition = thing.Position - Parent.BottomLeftCell;

		if (Parent.Rotation.IsHorizontal)
			(thingPosition.x, thingPosition.z) = (thingPosition.z, thingPosition.x);

		return CurrentGraphicVariation?.itemGraphics?.columns[thingPosition.x].rows[thingPosition.z]
			?? ItemGraphic.Default;
	}

	private Vector3 DrawOffsetForThing(Thing thing, in Vector3 parentDrawLoc, Rot4 thingRotation,
		ItemGraphic itemGraphic, out float stackRotation)
	{
		var parentRotation = Parent.Rotation;
		var position = ItemOffsetAt(thing.Position, thing.Map, parentRotation, thing.thingIDNumber, itemGraphic,
			CurrentGraphicVariation, out stackRotation);

		position += itemGraphic.DrawOffsetForRot(parentRotation);

		if (thing is Pawn pawn)
		{
			position += pawn.Drawer.DrawPos;
		}
		else
		{
			position += thing.Position.ToVector3Shifted()
				+ thing.Graphic.DrawOffset(thingRotation);

			position.y += thing.def.Altitude;
		}

		return position - parentDrawLoc;
	}

	private static Vector3 ItemOffsetAt(in IntVec3 position, Map map, Rot4 parentRotation, int thingID,
		ItemGraphic itemGraphic, GraphicsDef? graphicsDef, out float stackRotation)
	{
		var itemCount = 0;
		var precedingItemCount = 0;
		var stackBehaviour = itemGraphic.stackBehaviour;
		if (stackBehaviour == StackBehaviour.Default && graphicsDef != null)
			stackBehaviour = graphicsDef.stackBehaviour;

		var isWeaponButNotWood = false; // fish and beer is fine
		var defMatchesForAllItems = true;
		ThingDef? firstValidThingDefInCell = null;

		var thingList = position.GetThingListUnchecked(map);

		for (var i = 0; i < thingList.Count; i++)
		{
			var thing = thingList[i];
			if (!thing.IsItem())
				continue;

			itemCount++;
			if (thing.thingIDNumber < thingID)
				precedingItemCount++;

			if (stackBehaviour != StackBehaviour.Default || isWeaponButNotWood)
				continue;

			if (thing.def.IsWeapon && thing.def != ThingDefOf.WoodLog)
				isWeaponButNotWood = true;

			firstValidThingDefInCell ??= thing.def;
			if (thing.def != firstValidThingDefInCell)
				defMatchesForAllItems = false;
		}

		var precedingItemCountFloat = (float)precedingItemCount;

		var stackOffset = itemGraphic.StackOffsetForRot(parentRotation);

		stackOffset.y = precedingItemCountFloat
			* (stackOffset.y == 0f ? ItemGraphic.DEFAULT_STACK_OFFSET_Y : stackOffset.y);

		if (itemCount <= 1)
		{
			stackOffset.x = stackOffset.z = 0f;
			stackRotation = 0f;
			return stackOffset;
		}

		if (stackBehaviour == StackBehaviour.Default)
		{
			stackBehaviour = isWeaponButNotWood ? StackBehaviour.Weapons
				: defMatchesForAllItems ? StackBehaviour.Stack
				: StackBehaviour.Circle;
		}

		var stackBehaviourOffset = stackBehaviour switch
		{
			StackBehaviour.Weapons => ComputeStackOffsetForWeapons(position, map, itemCount,
				precedingItemCountFloat,
				precedingItemCount),
			StackBehaviour.Stack => ComputeStackOffsetForStack(ref stackOffset, precedingItemCountFloat),
			// ReSharper disable once PatternIsRedundant
			StackBehaviour.Circle or _ => ComputeStackOffsetForCircle(position, itemCount, precedingItemCount)
		};

		stackBehaviourOffset *= itemGraphic.stackOffsetFactor;

		stackOffset.x = stackBehaviourOffset.x;
		stackOffset.z = stackBehaviourOffset.y;

		stackRotation = precedingItemCountFloat * itemGraphic.stackRotation;
		return stackOffset;
	}

	private static Vector2 ComputeStackOffsetForWeapons(in IntVec3 position, Map map, int itemCount,
		float precedingItemCountFloat, int precedingItemCount)
		=> new(-0.5f + ((1f / itemCount) * (precedingItemCountFloat + 0.5f)),
			((GetRowItemCountForWeapons(new(position.x - 1, position.y, position.z), map) + precedingItemCount) & 1)
			== 0
				? -0.02f
				: 0.2f);

	private static int GetRowItemCountForWeapons(IntVec3 x, Map rowMap) // wtf is this doing? why did ludeon write this?
	{
		if (!x.InBounds(rowMap))
			return 0;

		var rowItemCount = x.GetItemCount(rowMap);
		if (rowItemCount <= 1)
			return 0;

		x.x--;
		return rowItemCount + GetRowItemCountForWeapons(x, rowMap);
	}

	private static Vector2 ComputeStackOffsetForStack(ref Vector3 stackOffset, float precedingItemCountFloat)
		=> new((precedingItemCountFloat * stackOffset.x) - (stackOffset.x / 1.375f), // default - 0.08f
			(precedingItemCountFloat * stackOffset.z) - (stackOffset.z / 4.8f));     // default - 0.05f

	private static Vector2 ComputeStackOffsetForCircle(in IntVec3 position, int itemCount, int precedingItemCount)
		=> GenGeo.RegularPolygonVertexPosition(itemCount, precedingItemCount,
				((position.x + position.z) & 1) == 0 ? 0f : 60f)
			* 0.3f;
}