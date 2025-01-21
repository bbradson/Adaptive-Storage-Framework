// Copyright (c) 2024 bradson
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using System.Diagnostics.CodeAnalysis;
using System.Security;
using AdaptiveStorage.Fishery.Pools;

namespace AdaptiveStorage.Fishery.Collections;

public partial class IntFishTable<TValue>
{
	// [StructLayout(LayoutKind.Sequential, Pack = 1)]
	// matches KeyValuePair<T,V> and gets reinterpret cast into that in the Enumerator
	internal struct Entry(int key, TValue? value)
	{
		public int Key = key;
		public TValue? Value = value;

		[Pure]
		[UnscopedRef]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[SecuritySafeCritical]
		public ref KeyValuePair<int, TValue> AsKeyValuePair()
			=> ref Unsafe.As<Entry, KeyValuePair<int, TValue>>(ref this);

		public override string ToString()
		{
			using var stringBuilder = new PooledStringBuilder(42);
			
			return stringBuilder
				.Append("IntFishTable<")
				.Append(typeof(TValue))
				.Append(">.Entry { Key = ")
				.Append<int>(Key)
				.Append(", Value = ")
				.Append<TValue>(Value)
				.Append(" }").ToString();
		}

		public static readonly Entry Default = new(0x7F7F7F7F, default);
	}
}