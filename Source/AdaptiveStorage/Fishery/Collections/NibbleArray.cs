// Copyright (c) 2024 bradson
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using AdaptiveStorage.Fishery.Utility.Diagnostics;

namespace AdaptiveStorage.Fishery.Collections;

[PublicAPI]
public record struct NibbleArray
{
	private uint _length;
	internal uint[] _data;

	public uint this[uint index]
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get
		{
			Guard.IsLessThan(index, _length);
			return this.UnsafeLoad(index);
		}
		set
		{
			Guard.IsLessThan(value, 16u);
			Guard.IsLessThan(index, _length);
			this.UnsafeStore(index, value);
		}
	}

	public uint Length
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => _length;
	}

	public NibbleArray(uint length)
	{
		_length = length;
		_data = new uint[((length - 1u) >> 3) + 1];
	}

	public void Resize(uint newSize)
	{
		_length = newSize;
		Array.Resize(ref _data, (int)((newSize - 1u) >> 3) + 1);
	}

	public void Clear() => _data.Clear();
	
	public void Initialize(uint value)
	{
		Guard.IsLessThan(value, 16u);
		Unsafe.InitBlockUnaligned(ref Unsafe.As<uint, byte>(ref _data[0u]), (byte)(value | (value << 4)),
			(uint)_data.Length << 2);
	}
}

public static class NibbleArrayExtensions
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static uint UnsafeLoad(this in NibbleArray array, uint index)
		=> (array._data.UnsafeLoad((int)(index >> 3)) >> (int)((index & 7u) << 2)) & 0b1111u;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void UnsafeStore(this in NibbleArray array, uint index, uint value)
	{
		ref var bucket = ref array._data[index >> 3];
		index &= 7u;
		index <<= 2;
		bucket &= ~(0b1111u << (int)index);
		bucket |= value << (int)index;
	}
}