// Copyright (c) 2023 bradson
// This Source Code Form is subject to the terms of the MIT license.
// If a copy of the license was not distributed with this file,
// You can obtain one at https://opensource.org/licenses/MIT/.

using System.Collections;
using System.Globalization;

namespace AdaptiveStorage;

/// <summary>
/// mostly a copy of <see cref="Verse.CellRect"/>, but with IntVec2 as enumerator.Current
/// </summary>
public record struct StorageCellRect : IEnumerable<IntVec2>
{
	public int minX;

	public int maxX;

	public int minZ;

	public int maxZ;

	public static StorageCellRect Empty => new(0, 0, 0, 0);

	public bool IsEmpty => Width <= 0 || Height <= 0;

	public int Area => Width * Height;

	public int Width
	{
		get => minX > maxX ? 0 : maxX - minX + 1;
		set => maxX = minX + Mathf.Max(value, 0) - 1;
	}

	public int Height
	{
		get => minZ > maxZ ? 0 : maxZ - minZ + 1;
		set => maxZ = minZ + Mathf.Max(value, 0) - 1;
	}

	public IEnumerable<IntVec2> Corners
	{
		get
		{
			if (IsEmpty)
				yield break;
			
			yield return new(minX, minZ);
			
			if (Width > 1)
				yield return new(maxX, minZ);
			
			if (Height > 1)
			{
				yield return new(minX, maxZ);
				
				if (Width > 1)
					yield return new(maxX, maxZ);
			}
		}
	}

	public IntVec2 Min => new(minX, minZ);

	public IntVec2 Max => new(maxX, maxZ);

	public IntVec2 RandomCell => new(Rand.RangeInclusive(minX, maxX), Rand.RangeInclusive(minZ, maxZ));

	public IntVec2 CenterCell => new(minX + (Width / 2), minZ + (Height / 2));

	public Vector3 CenterVector3 => new(minX + (Width / 2f), 0f, minZ + (Height / 2f));

	public Vector3 RandomVector3 => new(Rand.Range(minX, maxX + 1f), 0f, Rand.Range(minZ, maxZ + 1f));

	public IEnumerable<IntVec2> Cells
	{
		get
		{
			for (var z = minZ; z <= maxZ; z++)
			{
				for (var x = minX; x <= maxX; x++)
					yield return new(x, z);
			}
		}
	}

	public IEnumerable<IntVec2> EdgeCells
	{
		get
		{
			if (IsEmpty)
				yield break;

			var x = minX;
			var z = minZ;
			for (; x <= maxX; x++)
				yield return new(x, z);

			x--;
			for (z++; z <= maxZ; z++)
				yield return new(x, z);

			z--;
			for (x--; x >= minX; x--)
				yield return new(x, z);

			x++;
			for (z--; z > minZ; z--)
				yield return new(x, z);
		}
	}

	public int EdgeCellsCount
		=> Area switch
		{
			0 => 0,
			1 => 1,
			_ => (Width * 2) + ((Height - 2) * 2)
		};

	public IEnumerable<IntVec2> AdjacentCellsCardinal
	{
		get
		{
			if (IsEmpty)
				yield break;

			for (var x = minX; x <= maxX; x++)
			{
				yield return new(x, minZ - 1);
				yield return new(x, maxZ + 1);
			}

			for (var x = minZ; x <= maxZ; x++)
			{
				yield return new(minX - 1, x);
				yield return new(maxX + 1, x);
			}
		}
	}

	public IEnumerable<IntVec2> AdjacentCells
	{
		get
		{
			if (IsEmpty)
				yield break;
			
			foreach (var item in AdjacentCellsCardinal)
				yield return item;
			
			yield return new(minX - 1, minZ - 1);
			yield return new(maxX + 1, minZ - 1);
			yield return new(minX - 1, maxZ + 1);
			yield return new(maxX + 1, maxZ + 1);
		}
	}

	public bool AreSidesEqualOrGreater(int width, int height) => Width >= width && Height >= height;

	public IntVec2 FarthestPoint(IntVec2 startingPoint, Rot4 direction)
		=> !Contains(startingPoint)
			? throw new ArgumentException(startingPoint.ToString())
			: direction.AsInt switch
			{
				Rot4.NorthInt => new(startingPoint.x, maxZ),
				Rot4.EastInt => new(maxX, startingPoint.z),
				Rot4.SouthInt => new(startingPoint.x, minZ),
				Rot4.WestInt => new(minX, startingPoint.z),
				_ => throw new ArgumentException(direction.ToString()),
			};

	public StorageCellRect(int minX, int minZ, int width, int height)
	{
		this.minX = minX;
		this.minZ = minZ;
		maxX = minX + width - 1;
		maxZ = minZ + height - 1;
	}

	public static StorageCellRect FromLimits(int minX, int minZ, int maxX, int maxZ)
	{
		var result = default(StorageCellRect);
		result.minX = Mathf.Min(minX, maxX);
		result.minZ = Mathf.Min(minZ, maxZ);
		result.maxX = Mathf.Max(maxX, minX);
		result.maxZ = Mathf.Max(maxZ, minZ);
		return result;
	}

	public static StorageCellRect FromLimits(IntVec2 first, IntVec2 second)
	{
		var result = default(StorageCellRect);
		result.minX = Mathf.Min(first.x, second.x);
		result.minZ = Mathf.Min(first.z, second.z);
		result.maxX = Mathf.Max(first.x, second.x);
		result.maxZ = Mathf.Max(first.z, second.z);
		return result;
	}

	public static StorageCellRect FromCellList(IEnumerable<IntVec2> cells)
	{
		var cellRect = default(StorageCellRect);
		cellRect.minX = int.MaxValue;
		cellRect.minZ = int.MaxValue;
		var result = cellRect;
		
		foreach (var cell in cells)
		{
			if (cell.x < result.minX)
				result.minX = cell.x;
			
			if (cell.z < result.minZ)
				result.minZ = cell.z;
			
			if (cell.x > result.maxX)
				result.maxX = cell.x;
			
			if (cell.z > result.maxZ)
				result.maxZ = cell.z;
		}
		return result;
	}

	public static StorageCellRect CenteredOn(IntVec2 center, int radius)
	{
		var result = default(StorageCellRect);
		result.minX = center.x - radius;
		result.maxX = center.x + radius;
		result.minZ = center.z - radius;
		result.maxZ = center.z + radius;
		return result;
	}

	public static StorageCellRect CenteredOn(IntVec2 center, int width, int height)
	{
		var result = default(StorageCellRect);
		result.minX = center.x - (width / 2);
		result.minZ = center.z - (height / 2);
		result.maxX = result.minX + width - 1;
		result.maxZ = result.minZ + height - 1;
		return result;
	}

	public static StorageCellRect CenteredOn(IntVec2 center, IntVec2 size) => CenteredOn(center, size.x, size.z);

	public static StorageCellRect SingleCell(IntVec2 c) => new(c.x, c.z, 1, 1);

	public bool FullyContainedWithin(StorageCellRect within)
	{
		var cellRect = this;
		cellRect.ClipInsideRect(within);
		return this == cellRect;
	}

	public bool Overlaps(StorageCellRect other)
		=> !IsEmpty
			&& !other.IsEmpty
			&& minX <= other.maxX
			&& maxX >= other.minX
			&& maxZ >= other.minZ
			&& minZ <= other.maxZ;

	public bool IsOnEdge(IntVec2 c)
		=> (c.x == minX && c.z >= minZ && c.z <= maxZ)
			|| (c.x == maxX && c.z >= minZ && c.z <= maxZ)
			|| (c.z == minZ && c.x >= minX && c.x <= maxX)
			|| (c.z == maxZ && c.x >= minX && c.x <= maxX);

	public bool IsOnEdge(IntVec2 c, Rot4 rot)
		=> rot.AsInt switch
		{
			Rot4.WestInt => c.x == minX && c.z >= minZ && c.z <= maxZ,
			Rot4.EastInt => c.x == maxX && c.z >= minZ && c.z <= maxZ,
			Rot4.SouthInt => c.z == minZ && c.x >= minX && c.x <= maxX,
			_ => c.z == maxZ && c.x >= minX && c.x <= maxX
		};

	public bool IsOnEdge(IntVec2 c, int edgeWidth)
		=> Contains(c)
			&& (c.x < minX + edgeWidth
				|| c.z < minZ + edgeWidth
				|| c.x >= maxX + 1 - edgeWidth
				|| c.z >= maxZ + 1 - edgeWidth);

	public bool IsCorner(IntVec2 c)
		=> (c.x == minX && c.z == minZ)
			|| (c.x == maxX && c.z == minZ)
			|| (c.x == minX && c.z == maxZ)
			|| (c.x == maxX && c.z == maxZ);

	public Rot4 GetClosestEdge(IntVec2 c)
		=> GenMath.MinBy(Rot4.West, Mathf.Abs(c.x - minX), Rot4.East, Mathf.Abs(c.x - maxX), Rot4.North,
			Mathf.Abs(c.z - maxZ), Rot4.South, Mathf.Abs(c.z - minZ));

	public IntVec2 GetCenterCellOnEdge(Rot4 rot)
		=> rot.AsInt switch
		{
			Rot4.NorthInt => new(CenterCell.x, maxZ),
			Rot4.EastInt => new(maxX, CenterCell.z),
			Rot4.SouthInt => new(CenterCell.x, minZ),
			Rot4.WestInt => new(minX, CenterCell.z),
			_ => IntVec2.Invalid
		};

	public IntVec2 GetCenterCellOnEdge(Rot4 rot, int offset)
		=> rot.AsInt switch
		{
			Rot4.NorthInt => new(CenterCell.x + offset, maxZ),
			Rot4.EastInt => new(maxX, CenterCell.z),
			Rot4.SouthInt => new(CenterCell.x + offset, minZ),
			Rot4.WestInt => new(minX, CenterCell.z),
			_ => IntVec2.Invalid
		};

	public IEnumerable<IntVec2> GetCenterCellsOnEdge(Rot4 rot, int range)
	{
		for (var i = 0; i < (range * 2) + 1; i++)
			yield return GetCenterCellOnEdge(rot, i - range);
	}

	public IEnumerable<IntVec2> GetCellsOnEdge(Rot4 rot)
	{
		if (IsEmpty)
			yield break;

		switch (rot.AsInt)
		{
			case Rot4.NorthInt:
			{
				for (var x = minX; x <= maxX; x++)
					yield return new(x, maxZ);

				break;
			}
			case Rot4.EastInt:
			{
				for (var x = minZ; x <= maxZ; x++)
					yield return new(maxX, x);

				break;
			}
			case Rot4.SouthInt:
			{
				for (var x = minX; x <= maxX; x++)
					yield return new(x, minZ);

				break;
			}
			case Rot4.WestInt:
			{
				for (var x = minZ; x <= maxZ; x++)
					yield return new(minX, x);

				break;
			}
		}
	}

	public StorageCellRect ClipInsideRect(StorageCellRect otherRect)
	{
		if (minX < otherRect.minX)
			minX = otherRect.minX;
		
		if (maxX > otherRect.maxX)
			maxX = otherRect.maxX;
		
		if (minZ < otherRect.minZ)
			minZ = otherRect.minZ;
		
		if (maxZ > otherRect.maxZ)
			maxZ = otherRect.maxZ;
		
		return this;
	}

	public bool Contains(IntVec2 c) => (c.x >= minX) & (c.x <= maxX) & (c.z >= minZ) & (c.z <= maxZ);

	public IEnumerable<IntVec2> GetEdgeCells(Rot4 dir)
	{
		switch (dir.AsInt)
		{
			case Rot4.NorthInt:
			{
				for (var x = minX; x <= maxX; x++)
					yield return new(x, maxZ);

				break;
			}
			case Rot4.SouthInt:
			{
				for (var x = minX; x <= maxX; x++)
					yield return new(x, minZ);

				break;
			}
			case Rot4.WestInt:
			{
				for (var x = minZ; x <= maxZ; x++)
					yield return new(minX, x);

				break;
			}
			case Rot4.EastInt:
			{
				for (var x = minZ; x <= maxZ; x++)
					yield return new(maxX, x);

				break;
			}
		}
	}

	public StorageCellRect ExpandedBy(int dist)
	{
		var result = this;
		result.minX -= dist;
		result.minZ -= dist;
		result.maxX += dist;
		result.maxZ += dist;
		return result;
	}

	public StorageCellRect ContractedBy(int dist) => ExpandedBy(-dist);

	public StorageCellRect MovedBy(IntVec2 offset)
	{
		var result = this;
		result.minX += offset.x;
		result.minZ += offset.z;
		result.maxX += offset.x;
		result.maxZ += offset.z;
		return result;
	}

	public StorageCellRect Encapsulate(StorageCellRect otherRect)
	{
		minX = Mathf.Min(minX, otherRect.minX);
		minZ = Mathf.Min(minZ, otherRect.minZ);
		maxX = Mathf.Max(maxX, otherRect.maxX);
		maxZ = Mathf.Max(maxZ, otherRect.maxZ);
		return this;
	}

	public StorageCellRect Encapsulate(IntVec2 point)
	{
		minX = Mathf.Min(minX, point.x);
		minZ = Mathf.Min(minZ, point.z);
		maxX = Mathf.Max(maxX, point.x);
		maxZ = Mathf.Max(maxZ, point.z);
		return this;
	}

	public int IndexOf(IntVec2 location) => location.x - minX + ((location.z - minZ) * Width);

	public Enumerator GetEnumerator() => new(this);

	IEnumerator<IntVec2> IEnumerable<IntVec2>.GetEnumerator() => GetEnumerator();

	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

	public override string ToString() => "(" + minX + "," + minZ + "," + maxX + "," + maxZ + ")";

	public static StorageCellRect FromString(string str)
	{
		str = str.TrimStart('(');
		str = str.TrimEnd(')');
		var array = str.Split(',');
		var invariantCulture = CultureInfo.InvariantCulture;
		var num = Convert.ToInt32(array[0], invariantCulture);
		var num2 = Convert.ToInt32(array[1], invariantCulture);
		return new(num, num2, Convert.ToInt32(array[2], invariantCulture) - num + 1,
			Convert.ToInt32(array[3], invariantCulture) - num2 + 1);
	}

	public record struct Enumerator : IEnumerator<IntVec2>
	{
		private readonly StorageCellRect _parentRect;

		private int _x;

		private int _z;

		public IntVec2 Current => new(_x, _z);

		object IEnumerator.Current => Current;

		public Enumerator(StorageCellRect parentRect)
		{
			_parentRect = parentRect;
			_x = parentRect.minX - 1;
			_z = parentRect.minZ;
		}

		public bool MoveNext()
		{
			if (++_x > _parentRect.maxX)
			{
				_x = _parentRect.minX;
				_z++;
			}

			return _z <= _parentRect.maxZ;
		}

		public void Reset()
		{
			_x = _parentRect.minX - 1;
			_z = _parentRect.minZ;
		}

		void IDisposable.Dispose()
		{
		}
	}
}