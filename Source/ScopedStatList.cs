// Copyright (c) 2023 bradson
// This Source Code Form is subject to the terms of the MIT license.
// If a copy of the license was not distributed with this file,
// You can obtain one at https://opensource.org/licenses/MIT/.

namespace AdaptiveStorage;

public readonly struct ScopedStatList : IDisposable
{
	public readonly List<float> List;

	public ScopedStatList(IList<Thing> things, StatDef stat)
	{
		List = SimplePool<List<float>>.Get();
		List.Clear();
			
		for (var i = 0; i < things.Count; i++)
		{
			List.Add(things[i] is { } storedThing
				? storedThing.GetStatValue(stat) * storedThing.stackCount
				: 0f);
		}
	}

	public float this[int index] => List[index];

	public float Sum
	{
		get
		{
			var sum = 0f;

			for (var i = List.Count; i-- > 0;)
				sum += List[i];

			return sum;
		}
	}
	
	public void Dispose()
	{
		List.Clear();
		SimplePool<List<float>>.Return(List);
	}
}