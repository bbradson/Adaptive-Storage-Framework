// Written in 2024 by bradson
// To the extent possible under law, the author(s) have dedicated all
// copyright and related and neighboring rights to this software to the
// public domain worldwide. This software is distributed without any
// warranty. You should have received a copy of the CC0 Public Domain
// Dedication along with this software. If not, see
// http://creativecommons.org/publicdomain/zero/1.0/.

using HarmonyLib;
using JetBrains.Annotations;
using Verse;

namespace SaveGameCompatibility;

[PublicAPI]
public class SaveGameCompatibilityMod : Mod
{
	public static Harmony? Harmony { get; private set; }
	
	public const string HarmonyID = "bs.savegamecompatibilityoperation";

	public SaveGameCompatibilityMod(ModContentPack content) : base(content)
		=> (Harmony = new(HarmonyID)).PatchAll(typeof(SaveGameCompatibilityMod).Assembly);
}