// Copyright (c) 2025 bradson
// This Source Code Form is subject to the terms of the MIT license.
// If a copy of the license was not distributed with this file,
// You can obtain one at https://opensource.org/licenses/MIT/.

using HarmonyLib;
using Multiplayer.API;

namespace AdaptiveStorage.ModCompatibility;

/// <summary>
/// Implements https://github.com/rwmt/Multiplayer-Compatibility/commit/23be30e73c1e0025846ee968cc75f69ae66c9ed7
/// by Sokyran
/// </summary>
[StaticConstructorOnStartup]
public static class Multiplayer
{
	public static bool Active { get; private set; }

	static Multiplayer()
	{
		// This class body must never contain direct MP.* references: JITting such a token throws
		// TypeLoadException when no (or an incompatible) 0MultiplayerAPI assembly is loaded. Keep
		// all MP access inside Register, which only runs after the type is confirmed resolvable.
		if (AccessTools.TypeByName("Multiplayer.API.MP") is null)
			return;

		Register();
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static void Register()
	{
		Active = MP.enabled;
		if (Active)
			MP.RegisterAll(typeof(Multiplayer).Assembly);
	}
}