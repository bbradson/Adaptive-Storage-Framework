// Copyright (c) 2023 bradson
// This Source Code Form is subject to the terms of the MIT license.
// If a copy of the license was not distributed with this file,
// You can obtain one at https://opensource.org/licenses/MIT/.

namespace AdaptiveStorage.Utility;

public static class VectorExtensions
{
	public static int Max(this IntVec2 vector) => Math.Max(vector.x, vector.z);

	public static int Max(this IntVec3 vector) => Math.Max(Math.Max(vector.x, vector.y), vector.z);

	public static float Max(this Vector2 vector) => Math.Max(vector.x, vector.y);

	public static float Max(this Vector3 vector) => Math.Max(Math.Max(vector.x, vector.y), vector.z);

	public static int Min(this IntVec2 vector) => Math.Min(vector.x, vector.z);

	public static int Min(this IntVec3 vector) => Math.Min(Math.Min(vector.x, vector.y), vector.z);

	public static float Min(this Vector2 vector) => Math.Min(vector.x, vector.y);

	public static float Min(this Vector3 vector) => Math.Min(Math.Min(vector.x, vector.y), vector.z);

	public static int Average(this IntVec2 vector) => Convert.ToInt32((vector.x + vector.z) / 2d);

	public static int Average(this IntVec3 vector) => Convert.ToInt32((vector.x + vector.y + vector.z) / 3d);

	public static float Average(this Vector2 vector) => (vector.x + vector.y) / 2f;

	public static float Average(this Vector3 vector) => (vector.x + vector.y + vector.z) / 3f;

	public static StorageCell ToStorageCell(this IntVec2 storageCell, ThingClass building)
		=> new(building, storageCell);

	public static StorageCell ToStorageCell(this IntVec2 storageCell, Thing building)
		=> new(building, storageCell);

	public static Vector2 Bounded(this Vector2 vector, Vector2 boundary)
	{
		if (vector.x > boundary.x + 0.001f)
			vector *= boundary.x / vector.x;

		if (vector.y > boundary.y + 0.001f)
			vector *= boundary.y / vector.y;

		return vector;
	}

	public static Vector3 ScaledBy(this Vector3 vector, Vector2 scale)
	{
		vector.x *= scale.x;
		vector.z *= scale.y;
		return vector;
	}

	public static IntVec2 Flip(this IntVec2 vector)
	{
		vector.z = -vector.z;
		return vector;
	}

	public static IntVec2 Mirror(this IntVec2 vector)
	{
		vector.x = -vector.x;
		return vector;
	}

	public static bool IsFlipped(this IntVec2 vector) => vector.z < 0;

	public static bool IsMirrored(this IntVec2 vector) => vector.x < 0;

	public static Vector2 Flip(this Vector2 vector)
	{
		vector.y = -vector.y;
		return vector;
	}

	public static Vector2 Mirror(this Vector2 vector)
	{
		vector.x = -vector.x;
		return vector;
	}

	public static bool IsFlipped(this Vector2 vector) => vector.y < 0f;

	public static bool IsMirrored(this Vector2 vector) => vector.x < 0f;

#if V1_4
	public static Vector3 ToVector3(this Vector2 vector)
	{
		Vector3 result;
		result.x = vector.x;
		result.z = vector.y;
		result.y = 0f;
		return result;
	}

    public static Vector3 WithY(this Vector3 v3, float y) => new Vector3(v3.x, y, v3.z);
#endif
}