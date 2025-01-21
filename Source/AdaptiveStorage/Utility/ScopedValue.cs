// Copyright (c) 2023 bradson
// This Source Code Form is subject to the terms of the MIT license.
// If a copy of the license was not distributed with this file,
// You can obtain one at https://opensource.org/licenses/MIT/.

using HarmonyLib;

namespace AdaptiveStorage.Utility;

public readonly record struct ScopedValue<T> : IDisposable
{
	private readonly AccessTools.FieldRef<T> _field;

	private readonly T _previousValue;

	public ScopedValue(AccessTools.FieldRef<T> fieldGetter, T value)
	{
		_field = fieldGetter;
		ref var currentValue = ref fieldGetter();
		_previousValue = currentValue;
		currentValue = value;
	}
	
	public void Dispose() => _field() = _previousValue;
}