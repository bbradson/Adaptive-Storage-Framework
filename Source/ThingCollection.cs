// Copyright (c) 2023 bradson
// This Source Code Form is subject to the terms of the MIT license.
// If a copy of the license was not distributed with this file,
// You can obtain one at https://opensource.org/licenses/MIT/.

using System.Collections;
using System.Collections.ObjectModel;
using AdaptiveStorage.Fishery;
using AdaptiveStorage.Fishery.Collections;
using AdaptiveStorage.Fishery.Utility.Diagnostics;

namespace AdaptiveStorage;

public class ThingCollection : IList<Thing>, IReadOnlyList<Thing>, IList<ThingDef>, IReadOnlyList<ThingDef>, IExposable
{
	private readonly ThingClass _parent;
	private readonly int _parentSizeX, _parentSizeZ;
	private ThingDef[] _defs = Array.Empty<ThingDef>();
	private Thing[] _things = Array.Empty<Thing>();
	private int[] _positions = Array.Empty<int>();
	private int[] _thingIDNumbers = Array.Empty<int>();
	private readonly List<int> _cellWiseThingIDNumbers = [];
	private readonly List<Thing> _cellWiseThings = [];
	private readonly List<Thing>[] _validThingsPerCell, _thingsPerCell;
	private readonly List<StorageCell> _freeSlots;
	private readonly IntFishSet _validStoredThings = [];
	private int _count;

	public ThingClass Parent => _parent;

	public int Count => _count;

	public int CellWiseCount => _cellWiseThingIDNumbers.Count;

	public bool IsReadOnly => true;

	public int Capacity => _defs.Length;

	public CellWise AsCellWise => new(this);

	public int TotalThingCount
	{
		get
		{
			var count = 0;
			for (var i = _count; --i >= 0;)
				count += _things[i].stackCount;

			return count;
		}
	}

	public ReadOnlyCollection<StorageCell> FreeStorageSlots { get; }

	public IEnumerable<IntVec3> FreeMapCells
	{
		get
		{
			var parent = _parent;
			var parentPosition = parent.BottomLeftCell;
			var storageSlots = FreeStorageSlots;
			var count = storageSlots.Count;
			
			for (var i = 0; i < count; i++)
				yield return storageSlots[i].AsIntVec3.RotatedFor(parent) + parentPosition;
		}
	}

	public StorageCellRect StorageCells => new(0, 0, _parentSizeX, _parentSizeZ);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public ThingDef DefAt(int index) => _defs[index];

	public IntVec3 MapPositionOf(Thing thing) => _parent.GetMapCell(StoragePositionOf(thing));

	public StorageCell StoragePositionOf(Thing thing) => StoragePositionAt(IndexOf(thing));

	public StorageCell StoragePositionAt(int index) => new(_parentSizeX, _positions[index]);
	
	public bool TryGetStoragePositionOf(Thing thing, out StorageCell position)
	{
		if (IndexOf(thing) is >= 0 and var index)
		{
			position = StoragePositionAt(index);
			return true;
		}
		else
		{
			position = default;
			return false;
		}
	}

	public Thing this[int index]
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => _things[index];
	}

	public ThingCollection(ThingClass parent)
	{
		_parent = parent;
		var parentSize = parent.Size;
		_parentSizeX = parentSize.x;
		_parentSizeZ = parentSize.z;
		
		var buildingArea = parentSize.Area;
		
		_validThingsPerCell = new List<Thing>[buildingArea];
		_thingsPerCell = new List<Thing>[buildingArea];
		
		for (var i = _validThingsPerCell.Length; --i >= 0;)
		{
			_validThingsPerCell[i] = [];
			_thingsPerCell[i] = [];
		}

		_freeSlots = new(buildingArea);
		ResetFreeSlots();

		FreeStorageSlots = new(_freeSlots);

		parent.StorageSettingsChanged += UpdateAllValidStoredItems;
		parent.SlotLimitChangedAtCell += UpdateValidStoredItemsAt;
		parent.ItemStackChanged += UpdateValidStoredItemsAtItemPosition;
		parent.DeSpawning += RemoveAllIfNotPacking;
	}

	private void RemoveAllIfNotPacking(DestroyMode _, SpawnMode spawnMode)
	{
		if ((spawnMode & SpawnMode.PackContents) != 0)
			return;
		
		foreach (var cell in StorageCells)
		{
			var storageCell = cell.ToStorageCell(_parent);
			var list = ItemsForBuildingCell(storageCell);
			for (var i = list.Count; --i >= 0;)
			{
				var item = list[i];
				Remove(item, storageCell);
			}
		}
	}

	private void ResetFreeSlots()
	{
		_freeSlots.Clear();
		foreach (var cell in StorageCells)
			_freeSlots.Add(cell.ToStorageCell(_parent));
	}

	Thing IList<Thing>.this[int index]
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => _things[index];
		// ReSharper disable once ValueParameterNotUsed
		set => ThrowHelper.ThrowNotSupportedException();
	}

	ThingDef IList<ThingDef>.this[int index]
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => _defs[index];
		// ReSharper disable once ValueParameterNotUsed
		set => ThrowHelper.ThrowNotSupportedException();
	}

	ThingDef IReadOnlyList<ThingDef>.this[int index]
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => _defs[index];
	}

	public void Add(Thing thing) => Add(thing, thing.Position);

	public void Add(Thing thing, IntVec3 mapCell) => Add(thing, GetStorageCell(mapCell), mapCell == thing.Position);

	public void Add(Thing thing, StorageCell storageCell, bool isThingPosition)
	{
		if (isThingPosition)
			Add(thing, storageCell);
		else
			AddCellWise(thing, storageCell, false);
	}

	public void Add(Thing thing, StorageCell storageCell)
	{
		AddCellWise(thing, storageCell, true);

		if (!thing.def.SingleCell())
			TryAddToThingsForOtherOccupiedStorageCells(thing, storageCell);

		ExpandIfNeeded();
		SetInternal(_count++, thing, thing.def, thing.thingIDNumber, storageCell);

		thing.DisableItemRendering();
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private void TryAddToThingsForOtherOccupiedStorageCells(Thing thing, StorageCell storageCell)
	{
		foreach (var cell in OccupiedStorageCells(thing, storageCell))
		{
			if (!ItemsForBuildingCell(cell.ToStorageCell(_parent)).Contains(thing))
				TryAddStoredCellWiseThing(thing, storageCell);
		}
	}

	private static StorageCellRect OccupiedStorageCells(Thing thing, StorageCell storageCell)
	{
		var thingRotation = thing.Rotation;
		return ThingExtensions.OccupiedStorageRect(storageCell.AsIntVec2,
			thingRotation/*.Rotated(Rot4.GetRelativeRotation(thingRotation, _parent.Rotation))*/, // TODO: Rotate inner things when minified?
			thing.def.size/*.RotatedFor(_parent)*/);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void SetInternal(int index, Thing thing, ThingDef def, int thingIDNumber, StorageCell storageCell)
	{
		_things[index] = thing;
		_defs[index] = def;
		_thingIDNumbers[index] = thingIDNumber;
		_positions[index] = storageCell.Index;
	}

	private void AddCellWise(Thing thing, StorageCell storageCell, bool changingStoreState)
	{
		Guard.IsTrue(storageCell.ContainedWithin(_parent));

		_cellWiseThings.Add(thing);
		_cellWiseThingIDNumbers.Add(thing.thingIDNumber);

		if (changingStoreState || thing.def.SingleCell() || Contains(thing))
			TryAddStoredCellWiseThing(thing, storageCell);
	}

	private void TryAddStoredCellWiseThing(Thing thing, StorageCell storageCell)
	{
		ItemsForBuildingCell(storageCell).Add(thing);

		var validThingsForBuildingCell = ValidItemsForBuildingCell(storageCell);
		var maxItemsForStorageCell = _parent.GetMaxItemsForStorageCell(storageCell);

		if (validThingsForBuildingCell.Count < maxItemsForStorageCell && _parent.SettingsAllow(thing))
		{
			_validStoredThings.Add(thing.thingIDNumber);
			validThingsForBuildingCell.Add(thing);

			if (validThingsForBuildingCell.Count == maxItemsForStorageCell)
				_freeSlots.Remove(storageCell);
		}
	}

	public void UpdateAllValidStoredItems()
	{
		var area = StorageCells.Area;
		var storageCell = new StorageCell(_parent, 0);
		for (var i = 0; i < area; i++)
		{
			storageCell.Index = i;
			UpdateValidStoredItemsAt(storageCell);
		}
	}

	private void UpdateValidStoredItemsAtItemPosition(Thing item)
	{
		var itemPosition = StoragePositionOf(item);
		var itemDef = item.def;
		if (itemDef.SingleCell())
		{
			UpdateValidStoredItemsAt(itemPosition);
		}
		else
		{
			foreach (var cell in OccupiedStorageCells(item, itemPosition))
				UpdateValidStoredItemsAt(cell.ToStorageCell(_parent));
		}
	}

	private void UpdateValidStoredItemsAt(StorageCell storageCell)
	{
		var thingsInCell = ItemsAtStorageCell(storageCell);
		var maxItemsInCell = _parent.GetMaxItemsForStorageCell(storageCell);
		var validItemsInCell = 0;
		var validStoredItems = _validStoredThings;
		var validItemsForBuildingCell = ValidItemsForBuildingCell(storageCell);
		validItemsForBuildingCell.Clear();

		for (var i = thingsInCell.Length; --i >= 0;)
		{
			var thing = thingsInCell[i];

			if (validItemsInCell >= maxItemsInCell || !_parent.SettingsAllow(thing))
			{
				if (thing.def.SingleCell() || StoragePositionOf(thing) == storageCell)
					validStoredItems.Remove(thing.thingIDNumber);

				continue;
			}

			validItemsForBuildingCell.Add(thing);
			validStoredItems.Add(thing.thingIDNumber);
			validItemsInCell++;
		}

		if (validItemsInCell >= maxItemsInCell)
		{
			_freeSlots.Remove(storageCell);
		}
		else
		{
			if (!_freeSlots.Contains(storageCell))
				_freeSlots.Add(storageCell);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int ValidItemCountAtMapCell(in IntVec3 mapCell) => ValidItemCountAtStorageCell(GetStorageCell(mapCell));

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int ValidItemCountAtStorageCell(in StorageCell storageCell) => ValidItemsForBuildingCell(storageCell).Count;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int ItemCountAtMapCell(in IntVec3 mapCell) => ItemCountAtStorageCell(GetStorageCell(mapCell));

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int ItemCountAtStorageCell(in StorageCell storageCell) => ItemsForBuildingCell(storageCell).Count;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public ReadOnlySpan<Thing> ValidItemsAtMapCell(in IntVec3 mapCell)
		=> ValidItemsForBuildingCell(GetStorageCell(mapCell)).AsReadOnlySpan();

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public ReadOnlySpan<Thing> ValidItemsAtStorageCell(in StorageCell storageCell)
		=> ValidItemsForBuildingCell(storageCell).AsReadOnlySpan();

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private List<Thing> ValidItemsForBuildingCell(StorageCell storageCell)
		=> _validThingsPerCell[storageCell.Index];

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public ReadOnlySpan<Thing> ItemsAtMapCell(in IntVec3 mapCell)
		=> ItemsForBuildingCell(GetStorageCell(mapCell)).AsReadOnlySpan();

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public ReadOnlySpan<Thing> ItemsAtStorageCell(in StorageCell storageCell)
		=> ItemsForBuildingCell(storageCell).AsReadOnlySpan();

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private List<Thing> ItemsForBuildingCell(StorageCell storageCell)
		=> _thingsPerCell[storageCell.Index];

	public StorageCell GetStorageCell(in IntVec3 mapCell) => _parent.GetStorageCell(mapCell);

	public IntVec3 GetMapCell(in StorageCell storageCell) => _parent.GetMapCell(storageCell);

	void ICollection<ThingDef>.CopyTo(ThingDef[] array, int arrayIndex)
		=> Array.Copy(_defs, 0, array, arrayIndex, _count);

	public void CopyTo(Thing[] array, int arrayIndex) => Array.Copy(_things, 0, array, arrayIndex, _count);

	public Thing[] ToArray()
	{
		var destinationArray = new Thing[_count];
		Array.Copy(_things, 0, destinationArray, 0, _count);
		return destinationArray;
	}

	public bool Contains(ThingDef item) => IndexOf(item) >= 0;
	public bool Contains(Thing item) => IndexOf(item) >= 0;

	/// <summary>
	/// Things in storage cells that pass the storage filter and don't exceed item limits
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool ContainsAndAllows(Thing item) => _validStoredThings.Contains(item.thingIDNumber);

	public bool CellWiseContains(Thing item) => _cellWiseThingIDNumbers.AsReadOnlySpan().Contains(item.thingIDNumber);

	public int CellWiseCountOf(Thing item) => _cellWiseThingIDNumbers.AsReadOnlySpan().Count(item.thingIDNumber);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void ExpandIfNeeded()
	{
		if (_count == Capacity)
			Expand();
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private void Expand()
	{
		var size = Capacity;
		size = size == 0 ? 4 : size << 1;

		Array.Resize(ref _defs, size);
		Array.Resize(ref _things, size);
		Array.Resize(ref _thingIDNumbers, size);
		Array.Resize(ref _positions, size);
	}

	public bool Remove(Thing thing) => Remove(thing, thing.Position);

	public bool Remove(Thing thing, in IntVec3 mapCell)
		=> Remove(thing, GetStorageCell(mapCell), mapCell == thing.Position);

	public bool Remove(Thing thing, StorageCell storageCell, bool isThingPosition)
		=> isThingPosition ? Remove(thing, storageCell) : RemoveCellWise(thing, storageCell);

	public bool Remove(Thing thing, StorageCell storageCell)
	{
		if (!RemoveCellWise(thing, storageCell))
			return false;

		var index = _thingIDNumbers.IndexOf(thing.thingIDNumber, _count);
		if (index < 0)
			return false;

		if (_positions[index] != storageCell.Index)
			return true;
		
		RemoveAtInternal(index);
		thing.RestoreItemRendering();

		return true;
	}

	private bool RemoveCellWise(Thing thing, StorageCell storageCell)
	{
		Guard.IsTrue(storageCell.ContainedWithin(_parent));

		var index = _cellWiseThingIDNumbers.AsReadOnlySpan().IndexOf(thing.thingIDNumber);
		if (index < 0)
			return false;
		
		_cellWiseThingIDNumbers.RemoveAtFastUnordered(index);
		_cellWiseThings.RemoveAtFastUnordered(index);

		ItemsForBuildingCell(storageCell).RemoveFastUnordered(thing);

		if ((thing.def.SingleCell() || !CellWiseContains(thing)
				? _validStoredThings.Remove(thing.thingIDNumber)
				: _validStoredThings.Contains(thing.thingIDNumber))
			&& ValidItemsForBuildingCell(storageCell) is var validThingsForBuildingCell
			&& validThingsForBuildingCell.Remove(thing)
			&& validThingsForBuildingCell.Count + 1 == _parent.GetMaxItemsForStorageCell(storageCell))
		{
			_freeSlots.Add(storageCell);
		}

		return true;
	}

	private void RemoveAtInternal(int index)
	{
		ref var count = ref _count;
		Guard.IsLessThan(index, count);

		var lastIndex = count-- - 1;
		Move(lastIndex, index);
		ClearAt(lastIndex);
	}

	private void Move(int from, int to)
	{
		if (from == to)
			return;

		var things = _things;
		things[to] = things[from];

		var defs = _defs;
		defs[to] = defs[from];

		var thingIDNumbers = _thingIDNumbers;
		thingIDNumbers[to] = thingIDNumbers[from];

		var positions = _positions;
		positions[to] = positions[from];
	}

	private void ClearAt(int index) => SetInternal(index, null!, null!, default, default);

	internal void Clear()
	{
		ref var count = ref _count;
		
		Array.Clear(_things, 0, count);
		Array.Clear(_defs, 0, count);
		Array.Clear(_thingIDNumbers, 0, count);
		Array.Clear(_positions, 0, count);
		
		foreach (var validThingsAtCell in _validThingsPerCell)
			validThingsAtCell.Clear();

		foreach (var thingsAtCell in _thingsPerCell)
			thingsAtCell.Clear();
		
		_cellWiseThingIDNumbers.Clear();
		_cellWiseThings.Clear();
		_validStoredThings.Clear();
		
		ResetFreeSlots();
		
		count = 0;
	}

	IEnumerator<ThingDef> IEnumerable<ThingDef>.GetEnumerator() => ((IList<ThingDef>)_defs).GetEnumerator();

	public IEnumerator<Thing> GetEnumerator() => ((IList<Thing>)_things).GetEnumerator();

	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

	public int IndexOf(ThingDef item) => Array.IndexOf(_defs, item, 0, _count);
	public int IndexOf(Thing item) => _thingIDNumbers.IndexOf(item.thingIDNumber, _count);

#region Unsupported interface members
	void ICollection<ThingDef>.Add(ThingDef item) => ThrowHelper.ThrowNotSupportedException();
	bool ICollection<ThingDef>.Remove(ThingDef item) => ThrowHelper.ThrowNotSupportedException<bool>();
	void ICollection<Thing>.Clear() => ThrowHelper.ThrowNotSupportedException();
	void ICollection<ThingDef>.Clear() => ThrowHelper.ThrowNotSupportedException();
	void IList<Thing>.Insert(int index, Thing item) => ThrowHelper.ThrowNotSupportedException();
	void IList<ThingDef>.Insert(int index, ThingDef item) => ThrowHelper.ThrowNotSupportedException();
	void IList<Thing>.RemoveAt(int index) => ThrowHelper.ThrowNotSupportedException();
	void IList<ThingDef>.RemoveAt(int index) => ThrowHelper.ThrowNotSupportedException();
#endregion

	public bool Any() => _count > 0;

	public void ExposeData()
	{
		using var pooledThingList = _things.ToPooledList();
		var thingList = pooledThingList.List;
		Scribe_Collections.Look(ref thingList, "Things", LookMode.Deep);

		using var pooledPositionList = _positions.ToPooledList();
		var positionList = pooledPositionList.List;
		Scribe_Collections.Look(ref positionList, "Positions", LookMode.Value);

		if (Scribe.mode != LoadSaveMode.LoadingVars || thingList == null)
			return;

		for (var i = 0; i < thingList.Count; i++)
		{
			var thing = thingList[i];
			if (thing != null)
				Add(thing, StoragePositionAt(i));
		}
	}

	public readonly record struct CellWise(ThingCollection Collection) : IList<Thing>, IReadOnlyList<Thing>
	{
		public bool Contains(Thing item) => Collection.CellWiseContains(item);

		public int Count => Collection.CellWiseCount;

		bool ICollection<Thing>.IsReadOnly => true;

		public int IndexOf(Thing item)
			=> Collection._cellWiseThingIDNumbers.AsReadOnlySpan().IndexOf(item.thingIDNumber);

		public List<Thing>.Enumerator GetEnumerator() => Collection._cellWiseThings.GetEnumerator();

		IEnumerator<Thing> IEnumerable<Thing>.GetEnumerator() => GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

		void ICollection<Thing>.Add(Thing item) => ThrowHelper.ThrowNotSupportedException();
		void ICollection<Thing>.Clear() => ThrowHelper.ThrowNotSupportedException();
		void ICollection<Thing>.CopyTo(Thing[] array, int arrayIndex) => ThrowHelper.ThrowNotSupportedException();
		bool ICollection<Thing>.Remove(Thing item) => ThrowHelper.ThrowNotSupportedException<bool>();
		void IList<Thing>.Insert(int index, Thing item) => ThrowHelper.ThrowNotSupportedException();
		void IList<Thing>.RemoveAt(int index) => ThrowHelper.ThrowNotSupportedException();

		public Thing this[int index] => Collection._cellWiseThings[index];

		Thing IList<Thing>.this[int index]
		{
			get => Collection._cellWiseThings[index];
			// ReSharper disable once ValueParameterNotUsed
			set => ThrowHelper.ThrowNotSupportedException();
		}
	}
}