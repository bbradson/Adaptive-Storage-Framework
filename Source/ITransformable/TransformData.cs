// Copyright (c) 2025 bradson
// This Source Code Form is subject to the terms of the MIT license.
// If a copy of the license was not distributed with this file,
// You can obtain one at https://opensource.org/licenses/MIT/.

namespace ITransformable;

[PublicAPI]
public record struct TransformData
{
	public Vector3 Position;
	public Vector2 Scale;
	private RotationAngle _rotation;

	public RotationAngle CombinedRotation
	{
		get => _rotation;
		set => _rotation = value;
	}

	public RotationAngle ExtraRotation
	{
		get => _rotation - _rotation.ToCardinal();
		set => _rotation = _rotation.ToCardinal() + value;
	}

	public Rot4 Rot4
	{
		get => _rotation.AsRot4;
		set => _rotation = RotationAngle.FromRot4(value) + ExtraRotation;
	}

	public RotationDirection RotationDirection
	{
		get => _rotation.AsRotationDirection;
		set => _rotation = RotationAngle.FromRotationDirection(value) + ExtraRotation;
	}

	public bool IsFlipped => Scale.y < 0f;

	public bool IsMirrored => Scale.x < 0f;

	public TransformData()
	{
		Position = default;
		Scale = Vector2.one;
		_rotation = default;
	}

	public TransformData(in Vector3 position)
	{
		Position = position;
		Scale = Vector2.one;
		_rotation = default;
	}

	public TransformData(RotationAngle rotation)
	{
		Position = default;
		Scale = Vector2.one;
		_rotation = rotation;
	}

	public TransformData(in Vector3 position, Vector2 scale, RotationAngle rotation = default)
	{
		Position = position;
		Scale = scale;
		_rotation = rotation;
	}

	public TransformData(in Vector3 position, Vector2 scale, float rotation)
	{
		Position = position;
		Scale = scale;
		_rotation.AsFloat = rotation;
	}

	public void Flip() => Scale.y = -Scale.y;

	public void Mirror() => Scale.x = -Scale.x;

	public static readonly TransformData Default = new(Vector3.zero, Vector2.one);

	public static TransformData DefaultAt(in Vector3 position) => new(position, Vector2.one);
}