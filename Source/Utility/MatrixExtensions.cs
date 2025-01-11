// Copyright (c) 2023 bradson
// This Source Code Form is subject to the terms of the MIT license.
// If a copy of the license was not distributed with this file,
// You can obtain one at https://opensource.org/licenses/MIT/.

namespace AdaptiveStorage.Utility;

public static class MatrixExtensions
{
	public static void SetPosition(this ref Matrix4x4 matrix, in Vector3 position)
	{
		matrix.m03 = position.x;
		matrix.m13 = position.y;
		matrix.m23 = position.z;
	}

	public static Vector3 GetPosition(this in Matrix4x4 matrix)
	{
		Vector3 result;
		result.x = matrix.m03;
		result.y = matrix.m13;
		result.z = matrix.m23;
		return result;
	}
}