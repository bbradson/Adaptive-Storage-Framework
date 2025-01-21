// Copyright (c) 2024 bradson
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using System.Diagnostics.CodeAnalysis;
using AdaptiveStorage.Fishery.Utility.Diagnostics;

namespace AdaptiveStorage.Fishery.Collections.Internal;

public static class CollectionThrowHelper
{
	[DoesNotReturn]
	public static void ThrowInvalidOperationException_InvalidOperation_EnumFailedVersion()
		=> throw new InvalidOperationException("Collection was modified; enumeration operation may not execute.");

	[DoesNotReturn]
	public static void ThrowInvalidOperationException_InvalidOperation_EnumOpCantHappen()
		=> throw new InvalidOperationException("Enumeration has either not started or has already finished.");

	[DoesNotReturn]
	public static void ThrowInvalidOperationException_ConcurrentOperationsNotSupported()
		=> throw new InvalidOperationException(
			"Operations that change non-concurrent collections must have exclusive access. A concurrent "
			+ "update was performed on this collection and corrupted its state. The collection's state is no "
			+ "longer correct.");

	[DoesNotReturn]
	public static T ThrowInvalidOperationException_ConcurrentOperationsNotSupported<T>()
	{
		ThrowInvalidOperationException_ConcurrentOperationsNotSupported();
		return default;
	}

	[DoesNotReturn]
	public static void ThrowKeyNotFoundException<T>(T key)
	{
		Guard.IsNotNull(key);
		ThrowKeyNotFoundException((object?)key);
	}

	[DoesNotReturn]
	public static void ThrowKeyNotFoundException(object? key)
		=> throw new KeyNotFoundException($"The given key '{key}' was not present in the dictionary.");

	[DoesNotReturn]
	public static void ThrowInvalidKeyEqualsArgumentException<T>(T key)
	{
		var keyValue = key.ToStringSafe();
		throw new ArgumentException($"'{keyValue}' == '{keyValue}' returned false. A value that is not equal to "
			+ $"itself cannot be used as a key in a hash based collection.");
	}

	[DoesNotReturn]
	public static void ThrowWrongValueTypeArgumentException<TValue>(object value)
		=> throw new ArgumentException($"The value \"{value}\" is not of type \"{
			typeof(TValue)}\" and cannot be used in this generic collection.",
			nameof(value));

	[DoesNotReturn]
	public static void ThrowWrongKeyTypeArgumentException<TKey>(object? key)
	{
		Guard.IsNotNull(key);
		throw new ArgumentException($"The value \"{key}\" is not of type \"{
			typeof(TKey)}\" and cannot be used in this generic collection.",
			nameof(key));
	}

	[DoesNotReturn]
	public static bool ThrowAddingDuplicateWithKeyArgumentException<T>(T key)
	{
		Guard.IsNotNull(key);
		ThrowAddingDuplicateWithKeyArgumentException((object?)key);
		return true;
	}

	[DoesNotReturn]
	public static void ThrowInvalidKeysValuesSizeArgumentException()
		=> throw new ArgumentException(
			"Input collections have different sizes. AddRange expects a matching Count for keys and values");

	[DoesNotReturn]
	public static int ThrowInvalidTailValueInvalidOperationException(uint tail)
		=> throw new InvalidOperationException($"Tried computing jump distance from a tails value of '{
			tail}'. This value is reserved for {(tail == Tails.SOLO ? "entries without tail." : "empty buckets.")}");

	[DoesNotReturn]
	public static void ThrowAddingDuplicateWithKeyArgumentException(object? key)
		=> throw new ArgumentException($"An item with the same key has already been added. Key: {key}");
}