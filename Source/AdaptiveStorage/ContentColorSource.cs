// Copyright (c) 2023 bradson
// This Source Code Form is subject to the terms of the MIT license.
// If a copy of the license was not distributed with this file,
// You can obtain one at https://opensource.org/licenses/MIT/.

// ReSharper disable InconsistentNaming
namespace AdaptiveStorage;

[PublicAPI]
public enum ContentColorSource
{
	Default = 0,
	Null = Default,
	ColorOne = -1,
	ColorTwo = -2,
	False = -3,
	@false = False,
	None = -4,
	White = None,
	First = 1,
	True = First,
	@true = First,
	Second = 2,
	Third = 3,
	Fourth = 4,
	Fifth = 5,
	Sixth = 6,
	Seventh = 7,
	Eighth = 8,
	Ninth = 9,
	Tenth = 10,
	Eleventh = 11,
	Twelfth = 12
}