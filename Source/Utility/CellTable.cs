// Copyright (c) 2023 bradson
// This Source Code Form is subject to the terms of the MIT license.
// If a copy of the license was not distributed with this file,
// You can obtain one at https://opensource.org/licenses/MIT/.

using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Xml;

namespace AdaptiveStorage.Utility;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class CellTable<T> where T : IHasPosition, new()
{
	public List<CellRows> columns = [];

	public T? defaults;

	[Unsaved]
	private int _height;

	public int Height => _height;

	public int Width => columns.Count;

	public int Area => Width * Height;

	public T this[IntVec2 cell]
	{
		get => columns[cell.x].rows[cell.z];
		set => columns[cell.x].rows[cell.z] = value;
	}
	
	public T this[int column, int row]
	{
		get => columns[column].rows[row];
		set => columns[column].rows[row] = value;
	}

	public T this[ThingClass parent, IntVec2 thingPosition]
	{
		get
		{
			thingPosition -= parent.BottomLeftCell.ToIntVec2;

			return this[thingPosition.RotatedFor(parent)];
		}
		set
		{
			thingPosition -= parent.BottomLeftCell.ToIntVec2;

			this[thingPosition.RotatedFor(parent)] = value;
		}
	}

	public void Initialize(List<ThingDef> targetDefs)
	{
		var targetColumnCount = targetDefs.Max(static def => def.size.x);
		var targetRowCount = _height = targetDefs.Max(static def => def.size.z);

		var lastColumnPosition = -1;

		for (var i = 0; i < targetColumnCount; i++)
		{
			var rows = FillAndShuffle(columns, ref i, ref lastColumnPosition).rows;

			var lastRowPosition = -1;

			for (var j = 0; j < targetRowCount; j++)
				FillAndShuffle(rows, ref j, ref lastRowPosition);
		}

		for (var i = columns.Count; --i >= 0;)
		{
			if (columns[i].rows.Count != Height)
			{
				Log.Error($"CellTable in AdaptiveStorage.GraphicsDef initialized with invalid result at column {
					i.ToStringCached()} for targetDefs '{targetDefs.ToStringSafeEnumerable()}' from mods '{
						targetDefs.Select(static def => def.modContentPack?.Name).ToStringSafeEnumerable()}'");
			}
		}
	}

	public void ForEach(Action<T> action)
	{
		for (var x = 0; x < columns.Count; x++)
		{
			var rows = columns[x].rows;
			for (var y = 0; y < rows.Count; y++)
				action(rows[y]);
		}
	}

	private static TItem FillAndShuffle<TItem>(List<TItem> list, ref int i, ref int lastPosition)
		where TItem : IHasPosition, new()
	{
		while (list.Count <= i)
			list.Add(new());

		var item = list[i];
		item.Position ??= lastPosition + 1;
		var itemPosition = item.Position.Value;

		if (itemPosition < i)
		{
			list.RemoveAt(i);
			list.Insert(itemPosition, item);

			if (itemPosition == list[itemPosition + 1].Position)
				list.RemoveAt(itemPosition + 1);

			i = itemPosition;
		}
		else
		{
			while (itemPosition > i)
				list.Insert(i++, new());
		}

		lastPosition = itemPosition;
		return item;
	}

	public T[] ToArray(bool rotated = false)
	{
		var result = new T[Area];
		ToArrayInternal(result, rotated);
		return result;
	}
	
	public void ToArray(T[] array, bool rotated = false)
	{
		if (array.Length != Area)
			ThrowForInvalidArrayLength(array);
		
		ToArrayInternal(array, rotated);
	}

	private void ToArrayInternal(T[] array, bool rotated)
	{
		var sizeX = Width;
		var sizeZ = Height;

		for (var x = sizeX; --x >= 0;)
		{
			var rows = columns[x].rows;
			for (var z = sizeZ; --z >= 0;)
				array[rotated ? (x * sizeZ) + z : (z * sizeX) + x] = rows[z];
		}
	}

	public TResult[] ToArray<TResult>(Func<T, TResult> selector, bool rotated = false)
	{
		var result = new TResult[Area];
		ToArrayInternal(result, selector, rotated);
		return result;
	}
	
	public void ToArray<TResult>(TResult[] array, Func<T, TResult> selector, bool rotated = false)
	{
		if (array.Length != Area)
			ThrowForInvalidArrayLength(array);
		
		ToArrayInternal(array, selector, rotated);
	}
	
	private void ToArrayInternal<TResult>(TResult[] array, Func<T, TResult> selector, bool rotated)
	{
		var sizeX = Width;
		var sizeZ = Height;

		for (var x = sizeX; --x >= 0;)
		{
			var rows = columns[x].rows;
			for (var z = sizeZ; --z >= 0;)
				array[rotated ? (x * sizeZ) + z : (z * sizeX) + x] = selector(rows[z]);
		}
	}

	[DoesNotReturn]
	[MethodImpl(MethodImplOptions.NoInlining)]
	private void ThrowForInvalidArrayLength(Array array)
		=> throw new ArgumentException($"Array passed into CellTable.ToArray has an invalid length. Expected {
			Area}, got {array.Length} instead.", nameof(array));

	public int Max(Func<T, int> selector)
	{
		var max = int.MinValue;
		var sizeX = Width;
		var sizeZ = Height;

		for (var x = sizeX; --x >= 0;)
		{
			var rows = columns[x].rows;
			for (var z = sizeZ; --z >= 0;)
				max = Math.Max(max, selector(rows[z]));
		}

		return max;
	}

	public override string ToString() => string.Concat(base.ToString(), "\n", GetContentString());

	public string GetContentString(Func<T, string>? contentSelector = null)
	{
		var width = Width;
		var height = Height;
		contentSelector ??= static entry => entry.ToString();

		var separator = new string('-', (width * 2) + 2);
		var stringBuilder = new StringBuilder()
			.Append(separator);
		
		for (var y = 0; y < height; y++)
		{
			stringBuilder.Append("\n| ");
			for (var x = 0; x < width; x++)
			{
				var cellText = contentSelector(this[x, y]);
				if (cellText.Length < 3)
					stringBuilder.Append(' ');
				
				stringBuilder.Append(cellText).Append(cellText.Length < 2 ? "  |" : " |");
			}

			stringBuilder.Append('\n').Append(separator);
		}

		return stringBuilder.ToString();
	}

	[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
	public class CellRows : IHasPosition
	{
		public int? position;
		public List<T> rows = [];
	
		int? IHasPosition.Position
		{
			get => position;
			set => position = value;
		}
	
		public void LoadDataFromXmlCustom(XmlNode xmlRoot)
		{
			if (xmlRoot.Name.Length > 1 && int.TryParse(xmlRoot.Name[1..], out var value))
				position = value;

			foreach (XmlNode childNode in xmlRoot.ChildNodes)
			{
				if (childNode.NodeType == XmlNodeType.Comment || childNode.Name != nameof(rows))
					continue;
			
				rows = DirectXmlToObject.ObjectFromXml<List<T>>(childNode, false);
			}
		}
	}
}