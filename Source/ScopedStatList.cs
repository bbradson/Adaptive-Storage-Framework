// Copyright (c) 2023 bradson
// This Source Code Form is subject to the terms of the MIT license.
// If a copy of the license was not distributed with this file,
// You can obtain one at https://opensource.org/licenses/MIT/.

namespace AdaptiveStorage;

public readonly record struct ScopedStatList : IDisposable
{
	private readonly List<float> _list;

	public ScopedStatList(IList<Thing> things, StatDef stat)
	{
		_list = SimplePool<List<float>>.Get();
		_list.Clear();
			
		for (var i = 0; i < things.Count; i++)
		{
			_list.Add(things[i] is { } storedThing
				? storedThing.GetStatValue(stat) * storedThing.stackCount
				: 0f);
		}
	}

	public float this[int index] => _list[index];

	public float Sum
	{
		get
		{
			var sum = 0f;

			for (var i = _list.Count; i-- > 0;)
				sum += _list[i];

			return sum;
		}
	}
	
	public void Dispose()
	{
		_list.Clear();
		SimplePool<List<float>>.Return(_list);
	}
}