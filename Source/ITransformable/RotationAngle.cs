// Copyright (c) 2025 bradson
// This Source Code Form is subject to the terms of the MIT license.
// If a copy of the license was not distributed with this file,
// You can obtain one at https://opensource.org/licenses/MIT/.

using System.Runtime.CompilerServices;

namespace ITransformable;

[PublicAPI]
public record struct RotationAngle
{
	private uint _value;
	private const uint
		Precision = 10_000_000u,
		NorthInt = 0u,
		NorthEastInt = NorthInt + (45u * Precision),
		EastInt = 90u * Precision,
		SouthEastInt = EastInt + (45u * Precision),
		SouthInt = 180u * Precision,
		SouthWestInt = SouthInt + (45u * Precision),
		WestInt = 270u * Precision,
		NorthWestInt = WestInt + (45u * Precision),
		MaxValue = 360u * Precision;

	public static readonly RotationAngle
		Zero,
		Min = FromDirectValue(1u),
		One = FromDirectValue(Precision),
		North = FromDirectValue(NorthInt),
		NorthEast = FromDirectValue(NorthEastInt),
		East = FromDirectValue(EastInt),
		SouthEast = FromDirectValue(SouthEastInt),
		South = FromDirectValue(SouthInt),
		SouthWest = FromDirectValue(SouthWestInt),
		West = FromDirectValue(WestInt),
		NorthWest = FromDirectValue(NorthWestInt),
		Max = FromDirectValue(MaxValue - 1u);

	public double AsDouble
	{
		get => _value * (1d / Precision);
		set => _value = Convert.ToUInt32(((value * 10_000_000d % MaxValue) + MaxValue) % MaxValue);
	}

	public decimal AsDecimal
	{
		get => _value * (1m / Precision);
		set => _value = Convert.ToUInt32(((value * 10_000_000m % MaxValue) + MaxValue) % MaxValue);
	}

	public float AsFloat
	{
		get => (float)AsDouble;
		set => AsDouble = value;
	}

	public uint AsUInt
	{
		get => (_value + (Precision / 2u)) / Precision;
		set => _value = value % 360u * Precision;
	}

	public int AsInt
	{
		get => (int)AsUInt;
		set => _value = (uint)GenMath.PositiveMod(value, 360) * Precision;
	}

	public Rot4 AsRot4
	{
		get
			=> _value switch
			{
				< NorthEastInt + 1u => Rot4.North,
				< SouthEastInt => Rot4.East,
				< SouthWestInt + 1u => Rot4.South,
				< NorthWestInt => Rot4.West,
				_ => Rot4.North
			};
		set => this = FromRot4(value);
	}

	public RotationDirection AsRotationDirection
	{
		get
			=> _value switch
			{
				< NorthEastInt + 1u => RotationDirection.None,
				< SouthEastInt => RotationDirection.Clockwise,
				< SouthWestInt + 1u => RotationDirection.Opposite,
				< NorthWestInt => RotationDirection.Counterclockwise,
				_ => RotationDirection.None
			};
		set => this = FromRotationDirection(value);
	}

	public Quaternion AsQuaternion
	{
		get
		{
			Vector3 vector;
			vector.x = vector.z = 0f;
			vector.y = AsFloat;
			return Quaternion.Euler(vector);
		}
		set => AsFloat = value.eulerAngles.y;
	}

	public bool IsCardinal => _value % EastInt == NorthInt;

	public RotationAngle(double value) => AsDouble = value;

	public RotationAngle(float value) => AsFloat = value;

	public RotationAngle(uint value) => AsUInt = value;

	public RotationAngle(int value) => AsInt = value;

	public RotationAngle(ulong value) => _value = (uint)(value % 360UL) * Precision;

	public RotationAngle(long value) => _value = (uint)GenMath.PositiveMod(value, 360L) * Precision;

	public RotationAngle(Rot4 value) => this = FromRot4(value);

	public RotationAngle(RotationDirection value) => this = FromRotationDirection(value);

	public static RotationAngle FromRot4(Rot4 rot4)
		=> rot4.AsInt switch
		{
			Rot4.EastInt => East,
			Rot4.SouthInt => South,
			Rot4.WestInt => West,
			_ => North
		};

	public static RotationAngle FromRotationDirection(RotationDirection rotation)
		=> rotation switch
		{
			RotationDirection.Clockwise => East,
			RotationDirection.Opposite => South,
			RotationDirection.Counterclockwise => West,
			_ => North
		};

	public static RotationAngle FromDirectValue(uint value)
	{
		if (value >= MaxValue)
			ThrowArgumentOutOfRangeExceptionForIsLessThan(value, MaxValue, nameof(value));
		
		RotationAngle result;
		result._value = value;
		return result;
	}

	public RotationAngle ToCardinal() => FromRotationDirection(AsRotationDirection);

	public static RotationAngle operator +(RotationAngle x, RotationAngle y)
	{
		var result = (ulong)x._value + y._value;
		x._value = (uint)(result < MaxValue ? result : result - MaxValue);
		return x;
	}

	public static RotationAngle operator ++(RotationAngle x) => x + One;

	public static RotationAngle operator -(RotationAngle x, RotationAngle y)
	{
		var result = (long)x._value - y._value;
		x._value = (uint)(result >= 0 ? result : result + MaxValue);
		return x;
	}

	public static RotationAngle operator --(RotationAngle x) => x - One;

	public static RotationAngle operator *(RotationAngle x, RotationAngle y)
	{
		var result = (ulong)x._value * y._value / Precision;
		x._value = (uint)(result % MaxValue);
		return x;
	}

	public static RotationAngle operator /(RotationAngle x, RotationAngle y)
	{
		x._value = (uint)((ulong)x._value * Precision / y._value);
		return x;
	}

	public static RotationAngle operator %(RotationAngle x, RotationAngle y)
	{
		var resultInteger = (x / y)._value / Precision;
		x._value -= y._value * resultInteger;
		return x;
	}

	public static bool operator <(RotationAngle x, RotationAngle y) => x._value < y._value;

	public static bool operator <=(RotationAngle x, RotationAngle y) => x._value <= y._value;

	public static bool operator >(RotationAngle x, RotationAngle y) => x._value > y._value;

	public static bool operator >=(RotationAngle x, RotationAngle y) => x._value >= y._value;

	public static implicit operator RotationAngle(float value) => new(value);

	public static implicit operator RotationAngle(double value) => new(value);

	public static implicit operator RotationAngle(int value) => new(value);

	public static implicit operator RotationAngle(uint value) => new(value);

	public static implicit operator RotationAngle(long value) => new(value);

	public static implicit operator RotationAngle(ulong value) => new(value);

	public static implicit operator RotationAngle(Rot4 value) => FromRot4(value);

	public static implicit operator RotationAngle(RotationDirection value) => FromRotationDirection(value);

	public bool Equals(RotationAngle other) => _value == other._value;

	public override int GetHashCode() => _value.GetHashCode();

	// ReSharper disable once SpecifyACultureInStringConversionExplicitly
	public override string ToString() => AsDecimal.ToString();

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static void ThrowArgumentOutOfRangeExceptionForIsLessThan<T>(T value, T maximum, string name)
		=> throw new ArgumentOutOfRangeException(name, value,
			$"Parameter {AssertString(name)} ({typeof(T).FullName}) must be less than {
				AssertString(maximum)}, was {AssertString(value)}.");

	private static string AssertString(object? obj)
		=> obj switch
		{
			string _ => $"\"{obj}\"",
			null => "null",
			_ => $"<{obj}>"
		};
}