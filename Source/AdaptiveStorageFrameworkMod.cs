// Copyright (c) 2023 bradson
// This Source Code Form is subject to the terms of the MIT license.
// If a copy of the license was not distributed with this file,
// You can obtain one at https://opensource.org/licenses/MIT/.

using AdaptiveStorage.ModCompatibility;
using HarmonyLib;

namespace AdaptiveStorage;

[PublicAPI]
public class AdaptiveStorageFrameworkMod : Mod
{
	public const string
		NAME = "Adaptive Storage Framework",
		PACKAGE_ID = PackageIDs.ADAPTIVE_STORAGE_FRAMEWORK;
	
	public static Harmony Harmony { get; } = new(PACKAGE_ID);
	public static AdaptiveStorageFrameworkSettings Settings { get; private set; } = null!;
	public static AdaptiveStorageFrameworkMod Mod { get; private set; } = null!;
	
	public AdaptiveStorageFrameworkMod(ModContentPack content) : base(content)
	{
		Mod = this;
		Settings = GetSettings<AdaptiveStorageFrameworkSettings>();
	}

	public override string SettingsCategory() => NAME;

	public override void DoSettingsWindowContents(Rect inRect)
		=> AdaptiveStorageFrameworkSettings.DoSettingsWindowContents(inRect);
}