// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// ReSharper disable once CheckNamespace
namespace AdaptiveStorage.Fishery.Utility.Diagnostics;

/// <summary>
/// Helpers for working with value types.
/// </summary>
public static class ValueTypeExtensions
{
	/// <summary>
	/// Gets the table of hex characters (doesn't allocate, maps to .text section, see <see href="https://github.com/dotnet/roslyn/pull/24621"/>).
	/// </summary>
	private static ReadOnlySpan<byte> HexCharactersTable
		=> "0123456789ABCDEF"u8;

	/// <summary>
	/// Returns a hexadecimal <see cref="string"/> representation of a given <typeparamref name="T"/> value, left-padded and ordered as big-endian.
	/// </summary>
	/// <typeparam name="T">The input type to format to <see cref="string"/>.</typeparam>
	/// <param name="value">The input value to format to <see cref="string"/>.</param>
	/// <returns>
	/// The hexadecimal representation of <paramref name="value"/> (with the '0x' prefix), left-padded to byte boundaries and ordered as big-endian.
	/// </returns>
	/// <remarks>
	/// As a byte (8 bits) is represented by two hexadecimal digits (each representing a group of 4 bytes), each <see cref="string"/>
	/// representation will always contain an even number of digits. For instance:
	/// <code>
	/// Console.WriteLine(1.ToHexString()); // "0x01"
	/// Console.WriteLine(((byte)255).ToHexString()); // "0xFF"
	/// Console.WriteLine((-1).ToHexString()); // "0xFFFFFFFF"
	/// </code>
	/// </remarks>
	public static unsafe string ToHexString<T>(this T value)
		where T : unmanaged
	{
		var sizeOfT = sizeof(T);
		var bufferSize = (2 * sizeOfT) + 2;
		var p = stackalloc char[bufferSize];

		p[0] = '0';
		p[1] = 'x';

		ref var rh = ref HexCharactersTable.DangerousGetPinnableReference();

		for (int i = 0, j = bufferSize - 2; i < sizeOfT; i++, j -= 2)
		{
			var b = ((byte*)&value)[i];
			var low = b & 0x0F;
			var high = (b & 0xF0) >> 4;

			p[j + 1] = (char)Unsafe.Add(ref rh, low);
			p[j] = (char)Unsafe.Add(ref rh, high);
		}

		return new(p, 0, bufferSize);
	}
}
