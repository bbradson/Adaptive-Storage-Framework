// Copyright (c) 2023 bradson
// This Source Code Form is subject to the terms of the MIT license.
// If a copy of the license was not distributed with this file,
// You can obtain one at https://opensource.org/licenses/MIT/.

namespace AdaptiveStorage.Utility;

public static class ThingExtensions
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Map? TryGetMap(this Thing thing)
	{
		var maps = Current.Game.Maps;
		var mapIndex = (uint)thing.mapIndexOrState;
		
		return mapIndex < (uint)maps.Count ? maps[(int)mapIndex] : null;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ThingClass? StoringAdaptiveStorage(this Thing thing)
		=> thing.IsItem()
			&& thing.TryGetMap() is { } map
			&& thing is not ISlotGroupParent
				? map.haulDestinationManager.SlotGroupParentAt(thing.Position) as ThingClass
				: null;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsInAnyStorageBuilding(this Thing thing)
		=> thing.IsItem()
			&& thing.TryGetMap() is { } map
			&& thing is not ISlotGroupParent
			&& map.haulDestinationManager.SlotGroupParentAt(thing.Position) is Building_Storage;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool InMapBounds(this Thing thing)
	{
		var cellIndices = thing.TryGetMap()?.cellIndices;
		if (cellIndices is null)
			return false;

		var position = thing.Position;
		return ((uint)position.x < (uint)cellIndices.mapSizeX) & ((uint)position.z < (uint)cellIndices.mapSizeZ);
	}

	public static TextureAtlasGroup GetAtlasGroup(this Thing thing) => thing.def.category.ToAtlasGroup();
	
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsItem(this Thing thing) => thing.def.category == ThingCategory.Item;

#if V1_4
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void DrawNowAt(this Thing thing, Vector3 drawLoc, bool flip = false)
		=> thing.DrawAt(drawLoc, flip);
#endif

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static IntVec2 RotatedFor(this in IntVec2 position, Thing thing)
		=> thing.Rotation.IsHorizontal ? default(IntVec2) with { x = position.z, z = position.x } : position;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static IntVec3 RotatedFor(this in IntVec3 position, Thing thing)
		=> thing.Rotation.IsHorizontal ? default(IntVec3) with { x = position.z, z = position.x } : position;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector2 RotatedFor(this in Vector2 position, Thing thing)
		=> thing.Rotation.IsHorizontal ? default(Vector2) with { x = position.y, y = position.x } : position;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector3 RotatedFor(this in Vector3 position, Thing thing)
		=> thing.Rotation.IsHorizontal ? default(Vector3) with { x = position.z, z = position.x } : position;

	public static StorageCellRect OccupiedStorageRect(this Thing t)
		=> OccupiedStorageRect(t.Position.ToIntVec2, t.Rotation, t.def.size);

	public static StorageCellRect OccupiedStorageRect(IntVec2 center, Rot4 rot, IntVec2 size)
	{
		if ((size.x != 1) | (size.z != 1))
			AdjustForRotation(ref center, ref size, rot);
		
		return new(center.x - ((size.x - 1) >> 1), center.z - ((size.z - 1) >> 1), size.x, size.z);
	}

	private static void AdjustForRotation(ref IntVec2 center, ref IntVec2 size, Rot4 rot)
	{
		if (rot.IsHorizontal)
			(size.x, size.z) = (size.z, size.x);

		switch (rot.AsInt)
		{
			case Rot4.EastInt:
				center.z += (size.z & 1) - 1;
				return;
			case Rot4.SouthInt:
				center.z += (size.z & 1) - 1;
				goto case Rot4.WestInt;
			case Rot4.WestInt:
				center.x += (size.x & 1) - 1;
				return;
			case Rot4.NorthInt:
				return;
		}
	}
}