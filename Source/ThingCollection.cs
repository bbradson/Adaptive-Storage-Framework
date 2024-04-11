// Copyright (c) 2023 bradson
// This Source Code Form is subject to the terms of the MIT license.
// If a copy of the license was not distributed with this file,
// You can obtain one at https://opensource.org/licenses/MIT/.

using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace AdaptiveStorage;

public class ThingCollection : IList<Thing>, IReadOnlyList<Thing>, IList<ThingDef>, IReadOnlyList<ThingDef>
{
	private ThingDef[] _defs = Array.Empty<ThingDef>();
	private Thing[] _things = Array.Empty<Thing>();
	private int _count;

	public int Count => _count;

	public bool IsReadOnly => false;

	public int Capacity => _defs.Length;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public ThingDef DefAt(int index) => _defs[index];

	public Thing this[int index]
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => _things[index];
		set
		{
			if (index >= _count)
				ThrowIndexOutOfRangeException();

			_things[index] = value;
			_defs[index] = value.def;
		}
	}

	ThingDef IList<ThingDef>.this[int index]
	{
		get => DefAt(index);
		set => throw new NotSupportedException();
	}

	ThingDef IReadOnlyList<ThingDef>.this[int index] => DefAt(index);

	public void Add(Thing thing)
	{
		if (_count == Capacity)
			Expand();

		_things[_count] = thing;
		_defs[_count] = thing.def;
		_count++;
	}

	void ICollection<ThingDef>.Add(ThingDef item) => throw new NotSupportedException();

	void ICollection<ThingDef>.CopyTo(ThingDef[] array, int arrayIndex)
		=> Array.Copy(_defs, 0, array, arrayIndex, _count);

	public void CopyTo(Thing[] array, int arrayIndex)
		=> Array.Copy(_things, 0, array, arrayIndex, _count);

	public bool Remove(Thing thing)
	{
		for (var i = _count; i-- > 0;)
		{
			if (_things[i] != thing)
				continue;

			RemoveAt(i);
			return true;
		}

		return false;
	}

	bool ICollection<ThingDef>.Remove(ThingDef item)
	{
		var index = IndexOf(item);
		if (index < 0)
			return false;
		
		RemoveAt(index);
		return true;
	}

	public void Clear()
	{
		Array.Clear(_things, 0, _count);
		Array.Clear(_defs, 0, _count);
		_count = 0;
	}

	public bool Contains(ThingDef item) => IndexOf(item) >= 0;
	public bool Contains(Thing item) => IndexOf(item) >= 0;

	[MethodImpl(MethodImplOptions.NoInlining)]
	private void Expand()
	{
		if (Capacity == 0)
		{
			_defs = new ThingDef[4];
			_things = new Thing[4];
		}
		else
		{
			var newSize = Capacity << 1;
			Array.Resize(ref _defs, newSize);
			Array.Resize(ref _things, newSize);
		}
	}

	IEnumerator<ThingDef> IEnumerable<ThingDef>.GetEnumerator() => ((IList<ThingDef>)_defs).GetEnumerator();

	public IEnumerator<Thing> GetEnumerator() => ((IList<Thing>)_things).GetEnumerator();

	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

	public int IndexOf(ThingDef item) => Array.IndexOf(_defs, item, 0, _count);
	public int IndexOf(Thing item) => Array.IndexOf(_things, item , 0, _count);

	public void Insert(int index, Thing item)
	{
		if (index >= _count)
			ThrowIndexOutOfRangeException();
			
		if (_count == Capacity)
			Expand();
			
		var lastIndex = _count;
		_things[lastIndex] = _things[index];
		_defs[lastIndex] = _defs[index];
		_things[index] = item;
		_defs[index] = item.def;
		_count++;
	}

	void IList<ThingDef>.Insert(int index, ThingDef item) => throw new NotSupportedException();

	public void RemoveAt(int index)
	{
		if (index >= _count)
			ThrowIndexOutOfRangeException();
			
		var lastIndex = _count - 1;
		_things[index] = _things[lastIndex];
		_defs[index] = _defs[lastIndex];
		_things[lastIndex] = null!;
		_defs[lastIndex] = null!;
		_count--;
	}

	public bool Any() => _count > 0;

	[DoesNotReturn]
	private static void ThrowIndexOutOfRangeException() => throw new IndexOutOfRangeException();
}