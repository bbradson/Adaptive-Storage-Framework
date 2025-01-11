// Copyright (c) 2024 bradson
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using AdaptiveStorage.Fishery.Utility.Diagnostics;

namespace AdaptiveStorage.Fishery.Pools;

[PublicAPI]
public struct PooledList<T>()
	: IDisposable, IEquatable<PooledList<T>>, IEquatable<PooledIList<List<T>>>,
		IEquatable<List<T>> //, IList<T>, IList, IReadOnlyList<T>
// prevent boxing this, as doing so usually defeats the purpose of pooling
{
	private PooledIList<List<T>> _pooledList = new();

	public List<T> List
	{
		[Pure]
		get => _pooledList.List;
	}

	public void Dispose() => _pooledList.Dispose();

	public int Count
	{
		[Pure]
		get => List.Count;
	}

	public ReadOnlySpan<T> ReadOnlySpan
	{
		[Pure]
		get => List.AsReadOnlySpan();
	}

	public T this[int index]
	{
		[Pure]
		get => List[index];
		set => List[index] = value;
	}

	public PooledList(int minimumCapacity) : this() => List.EnsureCapacity(minimumCapacity);

	public PooledList([InstantHandle] IEnumerable<T> collection) : this() => List.AddRange(collection);

	public PooledList(List<T> collection) : this() => List.AddRangeFast(collection);

	public PooledList(T[] collection) : this() => List.AddRangeFast(collection);

	public PooledList(in ReadOnlySpan<T> collection) : this() => List.AddRange(collection);

	public PooledList(in Span<T> collection) : this() => List.AddRange(collection);

	/// <summary>Determines whether an element is in the <see cref="List{T}" />.</summary>
	/// <param name="item">
	/// The object to locate in the <see cref="List{T}" />. The value can be <see langword="null" /> for reference
	/// types.
	/// </param>
	/// <returns>
	/// <see langword="true" /> if <paramref name="item" /> is found in the <see cref="List{T}" />; otherwise,
	/// <see langword="false" />.
	/// </returns>
	[Pure]
	public unsafe bool Contains(T item)
	{
		List.UnwrapReadOnlyArray(out var items, out var count);

		var equalityComparer = FunctionPointers.Equals<T>.Default;
		for (var i = 0; i < count; i++)
		{
			if (equalityComparer(items.UnsafeLoad(i), item))
				return true;
		}

		return false;
	}

	public void Clear() => List.Clear();

	public void Add(T item) => List.Add(item);

	public void AddRange(List<T> collection) => List.AddRangeFast(collection);

	public void AddRange(T[] collection) => List.AddRangeFast(collection);

	public void AddRange(in ReadOnlySpan<T> collection) => List.AddRange(collection);

	public void AddRange([InstantHandle] IEnumerable<T> collection) => List.AddRange(collection);

	public void AddRange<TInput>(List<TInput> collection, [InstantHandle] Func<TInput, T> selector)
		=> List.AddRange(collection, selector);

	public void AddRange<TInput>(TInput[] collection, [InstantHandle] Func<TInput, T> selector)
		=> List.AddRange(collection, selector);

	public void AddRange<TInput>(in ReadOnlySpan<TInput> collection, [InstantHandle] Func<TInput, T> selector)
		=> List.AddRange(collection, selector);

	public void AddRange<TInput>([InstantHandle] IEnumerable<TInput> collection,
		[InstantHandle] Func<TInput, T> selector)
		=> List.AddRange(collection, selector);

	public void Insert(int index, T item) => List.Insert(index, item);

	public void InsertRange(int index, [InstantHandle] IEnumerable<T> collection)
		=> List.InsertRange(index, collection);

	[Pure]
	public T[] ToArray() => List.ToArray();

	public void CopyTo(T[] array) => List.CopyTo(array);

	public void CopyTo(T[] array, int arrayIndex) => List.CopyTo(array, arrayIndex);

	public void CopyTo(int index, T[] array, int arrayIndex, int count) => List.CopyTo(index, array, arrayIndex, count);

	[Pure]
	public PooledList<T> Copy() => new(List);

	[Pure]
	public List<T> CopyList() => List.Copy();

	/// <summary>
	/// Converts the elements in the current <see cref="PooledList{T}" /> to another type, and returns a list containing
	/// the converted elements.
	/// </summary>
	/// <param name="converter">
	/// A <see cref="Converter{TInput, TOutput}" /> delegate that converts each element from one type to another type.
	/// </param>
	/// <typeparam name="TOutput">The type of the elements of the target array.</typeparam>
	/// <returns>
	/// A <see cref="PooledList{T}" /> of the target type containing the converted elements from the current
	/// <see cref="PooledList{T}" />.
	/// </returns>
	/// <exception cref="ArgumentNullException">
	/// <paramref name="converter" /> is <see langword="null" />.</exception>
	[Pure]
	public PooledList<TOutput> ConvertAll<TOutput>([InstantHandle] Converter<T, TOutput> converter)
	{
		Guard.IsNotNull(converter);

		List.UnwrapReadOnlyArray(out var inputItems, out var count);
		var output = new PooledList<TOutput>(count);
		var outputList = output.List;
		var outputItems = outputList._items;

		for (var i = 0; i < count; i++)
			outputItems.UnsafeStore(i, converter(inputItems.UnsafeLoad(i)));

		outputList._size = count;
		outputList._version++;
		return output;
	}

	public void Fill(T value, int count) => List.Fill(value, count);

	[Pure]
	public bool Exists([InstantHandle] Predicate<T> match) => List.Exists(match);

	[Pure]
	public T? Find([InstantHandle] Predicate<T> match) => List.Find(match);

	[Pure]
	public T? Find<TContext>(TContext context, [InstantHandle] Predicate<TContext, T> match)
		=> List.Find(context, match);

	public T? Find<TContext>(ref TContext context, [InstantHandle] PredicateWithRefContext<TContext, T> match)
		=> List.Find(ref context, match);

	[Pure]
	public T? FindLast([InstantHandle] Predicate<T> match) => List.FindLast(match);

	[Pure]
	public T? FindLast<TContext>(TContext context, [InstantHandle] Predicate<TContext, T> match)
		=> List.FindLast(context, match);

	public T? FindLast<TContext>(ref TContext context, [InstantHandle] PredicateWithRefContext<TContext, T> match)
		=> List.FindLast(ref context, match);

	[Pure]
	public int FindIndex([InstantHandle] Predicate<T> match) => List.FindIndex(match);

	[Pure]
	public int FindIndex(int startIndex, [InstantHandle] Predicate<T> match) => List.FindIndex(startIndex, match);

	[Pure]
	public int FindIndex(int startIndex, int count, [InstantHandle] Predicate<T> match)
		=> List.FindIndex(startIndex, count, match);

	[Pure]
	public int FindLastIndex([InstantHandle] Predicate<T> match) => List.FindLastIndex(match);

	[Pure]
	public int FindLastIndex(int startIndex, [InstantHandle] Predicate<T> match)
		=> List.FindLastIndex(startIndex, match);

	[Pure]
	public int FindLastIndex(int startIndex, int count, [InstantHandle] Predicate<T> match)
		=> List.FindLastIndex(startIndex, count, match);

	/// <summary>Retrieves all the elements that match the conditions defined by the specified predicate.</summary>
	/// <param name="match">
	/// The <see cref="Predicate{T}" /> delegate that defines the conditions of the elements to search for.
	/// </param>
	/// <returns>
	/// A <see cref="List{T}" /> containing all the elements that match the conditions defined by the specified
	/// predicate, if found; otherwise, an empty
	/// <see cref="List{T}" />.
	/// </returns>
	/// <exception cref="ArgumentNullException">
	/// <paramref name="match" /> is <see langword="null" />.</exception>
	[Pure]
	public PooledList<T> FindAll([InstantHandle] Predicate<T> match)
	{
		Guard.IsNotNull(match);

		var result = new PooledList<T>();
		List.UnwrapReadOnlyArray(out var items, out var count);

		for (var i = 0; i < count; i++)
		{
			if (match(items.UnsafeLoad(i)))
				result.Add(items.UnsafeLoad(i));
		}

		return result;
	}

	public bool Remove(T item) => List.Remove(item);

	public void RemoveAt(int index) => List.RemoveAt(index);

	public void RemoveRange(int index, int count) => List.RemoveRange(index, count);

	public void RemoveAll([InstantHandle] Predicate<T> match) => List.RemoveAll(match);

	public void RemoveAll<TContext>(TContext context, [InstantHandle] Predicate<TContext, T> match)
		=> List.RemoveAll(context, match);

	public void RemoveAll<TContext>(ref TContext context, [InstantHandle] PredicateWithRefContext<TContext, T> match)
		=> List.RemoveAll(ref context, match);

	[Pure]
	public int IndexOf(T item) => List.IndexOf(item);

	[Pure]
	public int IndexOf(T item, int index) => List.IndexOf(item, index);

	[Pure]
	public int IndexOf(T item, int index, int count) => List.IndexOf(item, index, count);

	[Pure]
	public int LastIndexOf(T item) => List.LastIndexOf(item);

	[Pure]
	public int LastIndexOf(T item, int index) => List.LastIndexOf(item, index);

	[Pure]
	public int LastIndexOf(T item, int index, int count) => List.LastIndexOf(item, index, count);

	public void ForEach([InstantHandle] Action<T> action) => List.ForEach(action);

	[Pure]
	public bool TrueForAll([InstantHandle] Predicate<T> match) => List.TrueForAll(match);

	[Pure]
	public int BinarySearch(T item) => List.BinarySearch(item);

	[Pure]
	public int BinarySearch(T item, [InstantHandle] IComparer<T> comparer) => List.BinarySearch(item, comparer);

	[Pure]
	public int BinarySearch(int index, int count, T item, [InstantHandle] IComparer<T> comparer)
		=> List.BinarySearch(index, count, item, comparer);

	public void Reverse() => List.Reverse();

	public void Reverse(int index, int count) => List.Reverse(index, count);

	[Pure]
	public List<T>.Enumerator GetEnumerator() => List.GetEnumerator();

	[Pure]
	public IEnumerable<T> AsEnumerable() => List;

	[Pure]
	public override bool Equals(object? obj) => obj == List;

	[Pure]
	public bool Equals(PooledList<T> other) => List == other.List;

	[Pure]
	public bool Equals(PooledIList<List<T>> other) => List == other.List;

	[Pure]
	public bool Equals(List<T> other) => List == other;

	[Pure]
	public override int GetHashCode() => List.GetHashCode();

	// IEnumerator<T> IEnumerable<T>.GetEnumerator() => List.GetEnumerator();
	//
	// IEnumerator IEnumerable.GetEnumerator() => List.GetEnumerator();
	//
	// object IList.this[int index]
	// {
	// 	get => ((IList)List)[index];
	// 	set => ((IList)List)[index] = value;
	// }
	//
	// object ICollection.SyncRoot => ((ICollection)List).SyncRoot;
	//
	// bool ICollection.IsSynchronized => ((ICollection)List).IsSynchronized;
	//
	// bool IList.IsReadOnly => ((IList)List).IsReadOnly;
	//
	// bool ICollection<T>.IsReadOnly => ((ICollection<T>)List).IsReadOnly;
	//
	// bool IList.IsFixedSize => ((IList)List).IsFixedSize;
	//
	// int IList.Add(object? value) => ((IList)List).Add(value);
	//
	// bool IList.Contains(object? value) => ((IList)List).Contains(value);
	//
	// int IList.IndexOf(object value) => ((IList)List).IndexOf(value);
	//
	// void IList.Insert(int index, object value) => ((IList)List).Insert(index, value);
	//
	// void IList.Remove(object value) => ((IList)List).Remove(value);
	//
	// void ICollection.CopyTo(Array array, int index) => ((ICollection)List).CopyTo(array, index);

	[Pure]
	public static implicit operator List<T>(PooledList<T> pooledList) => pooledList.List;

	[Pure]
	public static implicit operator ReadOnlySpan<T>(PooledList<T> pooledList) => pooledList.ReadOnlySpan;

	[Pure]
	public static bool operator ==(PooledList<T> x, PooledList<T> y) => x.List == y.List;

	[Pure]
	public static bool operator !=(PooledList<T> x, PooledList<T> y) => x.List != y.List;

	[Pure]
	public static bool operator ==(PooledList<T> x, List<T>? y) => x.List == y;

	[Pure]
	public static bool operator !=(PooledList<T> x, List<T>? y) => x.List != y;

	[Pure]
	public static bool operator ==(List<T>? x, PooledList<T> y) => x == y.List;

	[Pure]
	public static bool operator !=(List<T>? x, PooledList<T> y) => x != y.List;
}