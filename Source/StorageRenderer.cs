// Copyright (c) 2023 bradson
// This Source Code Form is subject to the terms of the MIT license.
// If a copy of the license was not distributed with this file,
// You can obtain one at https://opensource.org/licenses/MIT/.

using System.Diagnostics.CodeAnalysis;
using AdaptiveStorage.ModCompatibility;

namespace AdaptiveStorage;

public class StorageRenderer
{
	public ThingClass Parent { get; }
		
	public List<GraphicsDef>? AllGraphics { get; set; }
		
	public List<Thing> Printables { get; } = new();

	public List<Thing> Drawables { get; } = new();
		
	public bool ShowContainedItems { get; private set; }

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
		
	private int
		_currentVariationIndex = -1,
		_currentGraphicIndex = -1,
		_lastMapMeshDirtyFrame = -1;

	public StorageRenderer(ThingClass parent)
	{
		Parent = parent;
		AllGraphics = GraphicsDef.Database!.TryGetValue(parent.def);
		CurrentGraphicChanged += OnCurrentGraphicChanged;
	}

	public void Draw()
	{
		if (ShowContainedItems)
		{
			var drawables = Drawables;
			for (var i = drawables.Count; i-- > 0;)
				DrawThing(drawables[i]);
		}

		Parent.Comps_PostDraw();
	}

	public void Print(SectionLayer layer)
	{
		if (CurrentGraphic is not { } currentGraphic)
		{
			Parent.BasePrint(layer);
			return;
		}

		var drawColor = currentGraphic.useDominantContentColor
			?? CurrentGraphicVariation.useDominantContentColor
				? ComputeDominantDrawColor()
				: Parent.DrawColor;

		var drawColorTwo = Parent.DrawColorTwo;
		var graphicDatas = currentGraphic.graphicDatas;
		for (var i = 0; i < graphicDatas.Count; i++)
			GraphicColoredFor(graphicDatas[i], drawColor, drawColorTwo).Print(layer, Parent, 0f);

		if (ShowContainedItems)
		{
			for (var i = Printables.Count; i-- > 0;)
				PrintThing(Printables[i], layer);
		}

		PostPrintComps(layer);
	}

	// public void PostPostMake(List<GraphicsDef> graphicsDefs)
	// {
	// 	AllGraphics = graphicsDefs;
	// 	CurrentVariationIndex = GetGraphicVariationIndexForStoredThings();
	// }
		
	public void InitializeStoredThingGraphics()
	{
		Printables.Clear();
		Drawables.Clear();
		
		for (var i = Parent.StoredThings.Count; i-- > 0;)
			AssignThingGraphic(Parent.StoredThings[i]);
		
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
		Span<int> span = stackalloc int[allGraphics.Count],
			fallBackSpan = stackalloc int[span.Length];
		var storedThings = Parent.StoredThings;
		var spanIndex = 0;
		var fallBackSpanIndex = 0;
		
		for (var i = allGraphics.Count; i-- > 0;)
		{
			var graphicsDef = allGraphics[i];
			if (!graphicsDef.Allows(storedThings))
				continue;

			if (graphicsDef.randomSelectionWeight < 1)
			{
				fallBackSpan[fallBackSpanIndex++] = i;
			}
			else
			{
				for (var j = graphicsDef.randomSelectionWeight; j-- > 0;) // TODO: fix this bug
					span[spanIndex++] = i; // it's going to throw out of bounds exceptions
			}
		}

		return spanIndex > 0 ? span[GetSeededIndex(spanIndex)]
			: fallBackSpanIndex > 0 ? fallBackSpan[GetSeededIndex(fallBackSpanIndex)]
			: 0;
	}
		
	private int GetSeededIndex(int maxExclusive) => (int)((uint)Parent.RandomSeed % (uint)maxExclusive);

	private void AssignToDrawables(Thing newItem)
	{
		Drawables.Add(newItem);
		Parent.Map.dynamicDrawManager.DeRegisterDrawable(newItem);
	}
	
	public void AssignThingGraphic(Thing newItem)
	{
		var newItemDef = newItem.def;
		var drawerType = newItemDef.drawerType;

		var drawable = drawerType is DrawerType.MapMeshAndRealTime or DrawerType.RealtimeOnly;
		var printable = drawerType is DrawerType.MapMeshOnly or DrawerType.MapMeshAndRealTime;

		if (drawable)
			AssignToDrawables(newItem);

		if (printable)
		{
			Printables.Add(newItem);
			TryDirtyParentMapMesh();
		}

		if (!ThingRequestGroup.HasGUIOverlay.Includes(newItem.def))
			return;

		var lister = Parent.Map.listerThings;
		
		if (!PerformanceFish.RemoveFromGroupList(lister, newItem, ThingRequestGroup.HasGUIOverlay))
			lister.ThingsInGroup(ThingRequestGroup.HasGUIOverlay).Remove(newItem);
	}

	public void UpdateCurrentGraphic()
	{
		var currentIndex = CurrentGraphicIndex;
		CalculateGraphicIndex();
		if (currentIndex != CurrentGraphicIndex)
			TryDirtyParentMapMesh();
	}
		
	public void FreeThingGraphic(Thing newItem)
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
		if (Parent.comps is not { } comps)
			return;

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

		for (var i = storedThings.Count; i-- > 0;)
		{
			var color = storedThings.DefAt(i) is { stuffProps: not null } storedThingDef
				? Parent.def.GetColorForStuff(storedThingDef)
				: storedThings[i].DrawColor;

			dict[color] = dict.TryGetValue(color, out var value) ? value + 1 : 1;
		}

		var dominantColorPair = new KeyValuePair<Color, int>(Color.white, 0);

		foreach (var pair in dict)
		{
			if (pair.Value > dominantColorPair.Value)
				dominantColorPair = pair;
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
			goto White;

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

	White:
		return Color.white;
	}

	private static Graphic GraphicColoredFor(GraphicData graphicData, in Color drawColor, in Color drawColorTwo)
		=> drawColor.IndistinguishableFrom(graphicData.Graphic.Color)
			&& drawColorTwo.IndistinguishableFrom(graphicData.Graphic.ColorTwo)
				? graphicData.Graphic
				: graphicData.Graphic.GetColoredVersion(graphicData.Graphic.Shader, drawColor, drawColorTwo);

	private void PrintThing(Thing thing, SectionLayer layer)
	{
		var itemGraphic = GetItemGraphicFor(thing);

		if (!itemGraphic.visible)
			return;

		var thingRotation = itemGraphic.textureOrientation ?? thing.Rotation;

		PrintThingAt(thing, DrawPositionForThing(thing, thingRotation, itemGraphic, out var stackRotation),
			thingRotation, layer,
			thing.MultipleItemsPerCellDrawn() ? itemGraphic.drawScale * 0.8f : itemGraphic.drawScale,
			itemGraphic.rotation + stackRotation, itemGraphic.drawShadow, itemGraphic.maxDrawSize);
	}

	private ItemGraphic GetItemGraphicFor(Thing thing)
	{
		var thingPosition = thing.Position - Parent.AllSlotCellsList()[0];

		if (Parent.Rotation.IsHorizontal)
			(thingPosition.x, thingPosition.z) = (thingPosition.z, thingPosition.x);

		return CurrentGraphicVariation?.itemGraphics?.columns[thingPosition.x].rows[thingPosition.z]
			?? ItemGraphic.Default;
	}

	private void DrawThing(Thing thing) // TODO: scaling and rotation? Not supported through DrawAt
	{
		var itemGraphic = GetItemGraphicFor(thing);

		if (!itemGraphic.visible)
			return;

		thing.DrawAt(DrawPositionForThing(thing, itemGraphic.textureOrientation ?? Parent.Rotation, itemGraphic,
			out _));
	}

	private static void PrintThingAt(Thing thing, in Vector3 drawLoc, Rot4 thingRotation, SectionLayer layer,
		float drawScale, float extraRotation, bool drawShadow, in Vector2 maxDrawSize)
	{
		var graphic = thing.Graphic;
		var drawSize = graphic.drawSize;

		AdjustDrawSize(ref drawSize, drawScale, maxDrawSize);

		var rotation = extraRotation + graphic.AngleFromRot(thingRotation);
		var flipUv = !graphic.ShouldDrawRotated;

		if (flipUv)
		{
			if (thingRotation.IsHorizontal)
				drawSize = drawSize.Rotated();
			
			flipUv = thingRotation.AsInt switch
			{
				Rot4.WestInt => graphic.WestFlipped,
				Rot4.EastInt => graphic.EastFlipped,
				_ => false
			};
		}
		
		if (flipUv && graphic.data != null)
			rotation += graphic.data.flipExtraRotation;

		var material = graphic.MatAt(thingRotation, thing);

		Graphic.TryGetTextureAtlasReplacementInfo(material, thing.def.category.ToAtlasGroup(), flipUv, true,
			out material, out var uvs, out var vertexColor);

		var colors = SimplePool<ColorsArray>.Get();
		Array.Fill(colors.Value, vertexColor);
		
		Printer_Plane.PrintPlane(layer, drawLoc, drawSize, material, rotation, flipUv, uvs, colors.Value);
		
		SimplePool<ColorsArray>.Return(colors);

		if (drawShadow)
			graphic.ShadowGraphic?.Print(layer, thing, 0f);
	}

	private readonly struct ColorsArray
	{
		public readonly Color32[] Value;
		public ColorsArray() => Value = new Color32[4];
	}

	private static void AdjustDrawSize(ref Vector2 drawSize, float drawScale, in Vector2 maxDrawSize)
	{
		drawSize *= drawScale;

		if (drawSize.x > maxDrawSize.x)
			drawSize *= maxDrawSize.x / drawSize.x;

		if (drawSize.y > maxDrawSize.y)
			drawSize *= maxDrawSize.y / drawSize.y;
	}

	private Vector3 DrawPositionForThing(Thing thing, Rot4 thingRotation, ItemGraphic itemGraphic,
		out float stackRotation)
	{
		var position = ItemOffsetAt(thing.Position, thing.Map, thing.thingIDNumber, itemGraphic,
			CurrentGraphicVariation!, out stackRotation);
		
		position += itemGraphic.DrawOffsetForRot(Parent.Rotation);

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

		return position;
	}

	private static Vector3 ItemOffsetAt(in IntVec3 position, Map map, int thingID, ItemGraphic itemGraphic,
		GraphicsDef graphicsDef, out float stackRotation)
	{
		var itemCount = 0;
		var precedingItemCount = 0;
		var stackBehaviour = itemGraphic.stackBehaviour;
		if (stackBehaviour == StackBehaviour.Default)
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

		var stackOffset = itemGraphic.stackOffset;

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