// Copyright (c) 2023 bradson
// This Source Code Form is subject to the terms of the MIT license.
// If a copy of the license was not distributed with this file,
// You can obtain one at https://opensource.org/licenses/MIT/.

using AdaptiveStorage.PrintDatas;

namespace AdaptiveStorage;

[PublicAPI]
public class ItemGraphicWorker(ItemGraphic graphic, GraphicsDef? def)
{
	public virtual Graphic? GetGraphicFor(Thing thing, StorageRenderer? renderer) => thing.Graphic;
	
	public virtual void UpdatePrintData(PrintData printData, ThingClass building)
	{
		var thing = printData.Thing;

		var thingRotation = printData.ThingRotation = graphic.textureOrientation ?? thing.Rotation;
		printData.NotifyMaterialPossiblyChanged();

		printData.DrawOffset = DrawOffsetForItem(building, thing, thingRotation, out var stackRotation);

		printData.SetDrawScale(DrawScaleForItem(thing, building), graphic.maxDrawSize);
		
		printData.ExtraRotation = graphic.rotation + stackRotation;
		printData.DrawShadow = graphic.drawShadow;
	}

	protected virtual Vector2 DrawScaleForItem(Thing item, ThingClass building)
		=> MultipleItemsDrawnAtCell(item, building)
			? graphic.drawScale * graphic.multipleItemsDrawSizeFactor
			: graphic.drawScale;

	protected static bool MultipleItemsDrawnAtCell(Thing thing, ThingClass building)
		=> building.StoredThings is var storedThings
			&& storedThings.TryGetStoragePositionOf(thing, out var storagePosition)
			&& storedThings.ItemCountAtStorageCell(storagePosition) >= 2;

	public virtual Vector3 DrawOffsetForItem(ThingClass building, Thing item, Rot4 thingRotation,
		out float stackRotation)
	{
		var buildingRotation = building.Rotation;
		var storedThings = building.StoredThings;
		var storageCell = storedThings.StoragePositionOf(item);
		var position = ItemOffsetAt(storageCell, storedThings, buildingRotation, item, out stackRotation);

		if (storedThings.ContainsAndAllows(item))
			position += graphic.DrawOffsetForRot(buildingRotation);

		position += building.GetOffsetFromCenter(storageCell.AsIntVec2)
				+ (GetGraphicFor(item, building.Renderer)?.DrawOffset(thingRotation).WithY(0f) ?? default(Vector3));

		position.y += AltitudeLayer.Item.AltitudeFor(); // default would be item.def.Altitude

		return position;
	}

	protected virtual Vector3 ItemOffsetAt(StorageCell storageCell, ThingCollection storedThings, Rot4 parentRotation,
		Thing item, out float stackRotation)
	{
		if (!storedThings.ContainsAndAllows(item))
			return GetOffsetForUnstoredItem(item, storageCell, storedThings, out stackRotation);
		
		var validStoredThings = storedThings.ValidItemsAtStorageCell(storageCell);
		var precedingItemCount = 0;
		var stackBehaviour = graphic.stackBehaviour;
		if (stackBehaviour == StackBehaviour.Default && def != null)
			stackBehaviour = def.stackBehaviour;

		var anyIsWeaponButNotWood = false; // fish and beer is fine
		var defMatchesForAllItems = true;
		ThingDef? firstValidThingDefInCell = null;
		var thingIDNumber = item.thingIDNumber;

		var itemCount = validStoredThings.Length;
		for (var i = 0; i < itemCount; i++)
		{
			var storedItem = validStoredThings[i];

			if (storedItem.thingIDNumber < thingIDNumber)
				precedingItemCount++;

			if (stackBehaviour != StackBehaviour.Default || anyIsWeaponButNotWood)
				continue;

			var storedItemDef = storedItem.def;
			if (IsWeaponButNotWood(storedItemDef))
				anyIsWeaponButNotWood = true;

			firstValidThingDefInCell ??= storedItemDef;
			if (storedItemDef != firstValidThingDefInCell)
				defMatchesForAllItems = false;
		}

		var precedingItemCountFloat = (float)precedingItemCount;
		var stackOffset = graphic.StackOffsetForRot(parentRotation);

		stackOffset.y = precedingItemCountFloat
			* (stackOffset.y == 0f ? ItemGraphic.DEFAULT_STACK_OFFSET_Y : stackOffset.y);

		if (itemCount <= 1)
		{
			stackRotation = stackOffset.x = stackOffset.z = 0f;
			return stackOffset;
		}

		if (stackBehaviour == StackBehaviour.Default)
		{
			stackBehaviour = anyIsWeaponButNotWood ? StackBehaviour.Weapons // vanilla to only require any, not all
				: defMatchesForAllItems ? StackBehaviour.Stack
				: StackBehaviour.Circle;
		}

		var itemPosition = item.Spawned ? storedThings.GetMapCell(storageCell).ToIntVec2 : storageCell.AsIntVec2;

		var stackBehaviourOffset = stackBehaviour switch
		{
			StackBehaviour.Weapons => ComputeStackOffsetForWeapons(itemPosition, itemCount, precedingItemCountFloat,
				precedingItemCount),
			StackBehaviour.Stack => ComputeStackOffsetForStack(ref stackOffset, precedingItemCountFloat),
			// ReSharper disable once PatternIsRedundant
			StackBehaviour.Circle or _ => ComputeStackOffsetForCircle(itemPosition, itemCount, precedingItemCount)
		};

		stackBehaviourOffset *= graphic.stackOffsetFactor;

		stackOffset.x = stackBehaviourOffset.x;
		stackOffset.z = stackBehaviourOffset.y;

		stackRotation = precedingItemCountFloat * graphic.stackRotation;

		var itemDef = item.def;
		if (IsWeaponButNotWood(itemDef) && !itemDef.IsIngestible) // beer has an angle of -150 ???
		{
			stackRotation += itemDef.equippedAngleOffset; // GraphicData seems to lack fields for this
			if (!itemDef.IsRangedWeapon)
				stackRotation += 65f; // default aim angle for vanilla swords
		}

		ApplyRotateInShelvesValue(item, ref stackRotation);

		return stackOffset;
	}

	protected virtual void ApplyRotateInShelvesValue(Thing item, ref float rotation)
	{
		var itemDef = item.def;
		switch (graphic.rotateInShelvesMode)
		{
			case RotateInShelvesMode.Ignore:
			{
				break;
			}
			case RotateInShelvesMode.Reverse:
			{
				if (!RotatesInShelvesByDefault(item))
					break;
				else
					goto case RotateInShelvesMode.ForceReverse;
			}
			case RotateInShelvesMode.ForceReverse:
			{
				if (!itemDef.rotateInShelves)
					rotation -= RotateInShelvesValue;

				break;
			}
			case RotateInShelvesMode.Default:
			default:
			{
				if (!RotatesInShelvesByDefault(item))
					break;
				else
					goto case RotateInShelvesMode.Force;
			}
			case RotateInShelvesMode.Force:
			{
				if (itemDef.rotateInShelves)
					rotation += RotateInShelvesValue;
				
				break;
			}
		}
	}

	protected virtual bool RotatesInShelvesByDefault(Thing item)
		=> item.def is var itemDef
			&& IsWeaponButNotWood(itemDef)
			&& item.Graphic is Graphic_RandomRotated;

	protected static bool IsWeaponButNotWood(ThingDef itemDef)
		=> itemDef.IsWeapon
			&& itemDef != ThingDefOf.WoodLog;

	protected virtual float RotateInShelvesValue => -90f; // see Graphic_RandomRotated.GetRotInRack

	protected static Vector3 GetOffsetForUnstoredItem(Thing item, StorageCell storageCell, ThingCollection storedThings,
		out float stackRotation)
	{
		var thingIDNumber = item.thingIDNumber;
		var itemsAtCell = storedThings.ItemsAtStorageCell(storageCell);
		var itemCount = itemsAtCell.Length;
		var itemIndex = -1;
		
		if (itemCount == 0)
			itemCount = thingIDNumber & 7;
		else
			itemIndex = itemsAtCell.IndexOf(item);

		if (itemIndex < 0)
			itemIndex = thingIDNumber % itemCount;

		stackRotation = Mathf.Lerp(-60f, 60f,
			Utility.CollectionExtensions.RandomizeUsingPcgRxsMXs((ulong)thingIDNumber) / (float)ulong.MaxValue);
		
		var circularOffset = GenGeo.RegularPolygonVertexPosition(itemCount, itemIndex);
		circularOffset *= 0.35f;
		return new(circularOffset.x, 0f, circularOffset.y);
	}

	protected static Vector2 ComputeStackOffsetForWeapons(IntVec2 position, int itemCount,
		float precedingItemCountFloat, int precedingItemCount)
		=> new(-0.5f + ((1f / itemCount) * (precedingItemCountFloat + 0.5f)),
			((((itemCount & 1) == 0 ? 0 : position.x) + precedingItemCount) & 1) == 0 ? -0.02f : 0.2f);

	protected static Vector2 ComputeStackOffsetForStack(ref Vector3 stackOffset, float precedingItemCountFloat)
		=> new((precedingItemCountFloat * stackOffset.x) - (stackOffset.x / 1.375f), // default - 0.08f
			(precedingItemCountFloat * stackOffset.z) - (stackOffset.z / 4.8f));     // default - 0.05f

	protected static Vector2 ComputeStackOffsetForCircle(IntVec2 position, int itemCount, int precedingItemCount)
		=> GenGeo.RegularPolygonVertexPosition(itemCount, precedingItemCount,
				((position.x + position.z) & 1) == 0 ? 0f : 60f)
			* 0.3f;
}