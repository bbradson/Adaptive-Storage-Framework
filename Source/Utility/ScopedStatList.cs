// Copyright (c) 2023 bradson
// This Source Code Form is subject to the terms of the MIT license.
// If a copy of the license was not distributed with this file,
// You can obtain one at https://opensource.org/licenses/MIT/.

using AdaptiveStorage.Fishery;
using AdaptiveStorage.Fishery.Pools;

namespace AdaptiveStorage.Utility;

public record struct ScopedStatList : IDisposable
{
	private PooledList<float> _list;

	public ScopedStatList(IList<Thing> things, StatDef stat)
	{
		_list = new();

		var thingCount = things.Count;
		for (var i = 0; i < thingCount; i++)
		{
			try
			{
				_list.Add(things[i] is { } storedThing
					? storedThing.GetStatValue(stat) * storedThing.stackCount
					: 0f);
			}
			catch (Exception ex)
			{
				Log.Error(ex.ToString());
			}
		}
	}

	public float this[int index] => _list[index];

	public float Sum
	{
		get
		{
			var sum = 0f;

			_list.List.UnwrapReadOnlyArray(out var array, out var count);
			for (var i = count; --i >= 0;)
				sum += array.UnsafeLoad(i);

			return sum;
		}
	}
	
	public void Dispose() => _list.Dispose();
}