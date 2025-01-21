// Copyright (c) 2024 bradson
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

namespace AdaptiveStorage.Fishery.Collections.Internal;

public struct Tails
{
	public const uint
		EMPTY = 0u,
		SOLO = 1u;

	internal NibbleArray _values;

	public Tails(uint length) => _values = new(length);

	public uint Length => _values.Length;

	public uint this[int index]
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => _values[(uint)index];

		set => _values[(uint)index] = value;
	}

	public void Reset() => _values.Clear();

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsEmpty(int index) => _values[(uint)index] == EMPTY;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsEmpty(uint tail) => tail == EMPTY;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsSolo(int index) => _values[(uint)index] == SOLO;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsSolo(uint tail) => tail == SOLO;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsSoloOrEmpty(int index) => _values[(uint)index] <= SOLO;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsSoloOrEmpty(uint tail) => tail <= SOLO;

	public void SetSolo(int index) => _values[(uint)index] = SOLO;

	public void SetEmpty(int index) => _values[(uint)index] = EMPTY;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool TryContinueWithTail(ref int entryIndex, int wrapAroundMask)
	{
		var tail = _values.UnsafeLoad((uint)entryIndex);
		if (tail <= SOLO)
			return false;

		entryIndex += GetJumpDistance(tail);
		entryIndex &= wrapAroundMask;
		return true;
	}

	public int GetTailIndex(int entryIndex, int wrapAroundMask)
		=> GetTailIndex(entryIndex, this[entryIndex], wrapAroundMask);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int GetTailIndex(int entryIndex, uint tail, int wrapAroundMask)
		=> (entryIndex + GetJumpDistance(tail)) & wrapAroundMask;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int GetJumpDistance(uint tail) => tail - 2u >= 5u ? GetJumpDistanceLong(tail) : (int)tail - 1;

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static int GetJumpDistanceLong(uint tail)
		=> tail switch
		{
			7 => 8,
			8 => 13,
			9 => 21,
			10 => 34,
			11 => 55,
			12 => 89,
			13 => 144,
			14 => 233,
			15 => 377,
			_ => CollectionThrowHelper.ThrowInvalidTailValueInvalidOperationException(tail)
		};
}

public static class TailExtensions
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static uint UnsafeLoad(this in Tails tails, uint index) => tails._values.UnsafeLoad(index);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void UnsafeStore(this in Tails tails, uint index, uint value)
		=> tails._values.UnsafeStore(index, value);
}