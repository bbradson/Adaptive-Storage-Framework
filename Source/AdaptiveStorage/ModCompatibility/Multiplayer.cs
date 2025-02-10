// Copyright (c) 2025 bradson
// This Source Code Form is subject to the terms of the MIT license.
// If a copy of the license was not distributed with this file,
// You can obtain one at https://opensource.org/licenses/MIT/.

using Multiplayer.API;

namespace AdaptiveStorage.ModCompatibility;

/// <summary>
/// Implements https://github.com/rwmt/Multiplayer-Compatibility/commit/23be30e73c1e0025846ee968cc75f69ae66c9ed7
/// by Sokyran
/// </summary>
[StaticConstructorOnStartup]
public static class Multiplayer
{
	public static bool Active => MP.enabled;
	
	static Multiplayer()
	{
		if (!Active)
			return;
		
		MP.RegisterAll(typeof(Multiplayer).Assembly);
	}
}