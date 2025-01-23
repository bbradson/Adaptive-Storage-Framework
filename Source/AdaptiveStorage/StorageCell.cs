// Copyright (c) 2024 bradson
// This Source Code Form is subject to the terms of the MIT license.
// If a copy of the license was not distributed with this file,
// You can obtain one at https://opensource.org/licenses/MIT/.

namespace AdaptiveStorage;

public record struct StorageCell
{
	private readonly int _sizeX;
	private int _cellIndex;

	public int Index
	{
		get => _cellIndex;
		set => _cellIndex = value;
	}

	public IntVec2 AsIntVec2
	{
		get
		{
			IntVec2 result;
			result.z = _cellIndex / _sizeX;
			result.x = _cellIndex % _sizeX;
			return result;
		}
		set => _cellIndex = (value.z * _sizeX) + value.x;
	}

	public IntVec3 AsIntVec3
	{
		get
		{
			IntVec3 result;
			result.y = 0;
			result.z = _cellIndex / _sizeX;
			result.x = _cellIndex % _sizeX;
			return result;
		}
		set => _cellIndex = (value.z * _sizeX) + value.x;
	}

	public bool ContainedWithin(ThingClass building) => ContainedWithin(building.Size);

	public bool ContainedWithin(Thing building) => ContainedWithin(building.def.size);

	public bool ContainedWithin(IntVec2 storageRect)
		=> (storageRect.z > 0)
			& (_sizeX == storageRect.x)
			& ((uint)_cellIndex < (uint)(storageRect.z * _sizeX));

	public bool ContainedWithin(StorageCellRect storageRect) => storageRect.Contains(AsIntVec2);

	public StorageCell ForBuilding(Thing building)
		=> building.def.size.x is var buildingSize
			&& _sizeX == buildingSize ? this : new(buildingSize, AsIntVec2);

	public StorageCell(ThingClass building, IntVec2 storageCell) : this(building.Size.x, storageCell)
	{
	}

	public StorageCell(Thing building, IntVec2 storageCell) : this(building.def.size.x, storageCell)
	{
	}

	public StorageCell(IntVec2 size, IntVec2 storageCell) : this(size.x, storageCell)
	{
	}

	public StorageCell(int sizeX, IntVec2 storageCell)
	{
		_sizeX = sizeX;
		_cellIndex = (storageCell.z * _sizeX) + storageCell.x;
	}

	public StorageCell(ThingClass building, int index) : this(building.Size.x, index)
	{
	}

	public StorageCell(int sizeX, int index)
	{
		_sizeX = sizeX;
		_cellIndex = index;
	}

	public StorageCell(ThingClass building, IntVec3 mapCell) => this = building.GetStorageCell(mapCell);

	public StorageCell(Thing building, IntVec3 mapCell)
	{
		var bottomLeftCell = building.OccupiedRect()
#if V1_4
			.BottomLeft;
#else
			.Min;
#endif
		
		IntVec2 storageCell;
		storageCell.x = mapCell.x - bottomLeftCell.x;
		storageCell.z = mapCell.z - bottomLeftCell.z;

		if (building.Rotation.IsHorizontal)
			(storageCell.x, storageCell.z) = (storageCell.z, storageCell.x);

		_sizeX = building.def.size.x;
		_cellIndex = (storageCell.z * _sizeX) + storageCell.x;
	}
}