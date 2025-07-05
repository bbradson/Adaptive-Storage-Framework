// Copyright (c) 2025 bradson
// This Source Code Form is subject to the terms of the MIT license.
// If a copy of the license was not distributed with this file,
// You can obtain one at https://opensource.org/licenses/MIT/.

namespace AdaptiveStorage;

public class Frame : RimWorld.Frame, IStorageGroupMember, IStoreSettingsParent
{
	public Extension? Extension { get; private set; }

	public override string LabelNoCount => Extension.TryGenerateCustomLabel(this) ?? base.LabelNoCount;

	private StorageSettings? _fixedStorageSettings;

	StorageSettings IStorageGroupMember.ParentStoreSettings => GetParentStoreSettings();

	public new StorageSettings GetParentStoreSettings() => _fixedStorageSettings ??= PrepareFixedStorageSettings();

	private StorageSettings PrepareFixedStorageSettings()
		=> BuildDef?.GetModExtension<Extension>()?.TryCreateStuffLockedStorageSettings(this)
			?? base.GetParentStoreSettings();

	public override void DrawExtraSelectionOverlays()
	{
		base.DrawExtraSelectionOverlays();
		Extension?.TryHighlightRoomWhenSelected(this);
	}

	protected virtual void PostInitialize()
	{
		try
		{
			Extension = BuildDef?.GetModExtension<Extension>();
		}
		catch (Exception ex)
		{
			Log.Error(ex.ToString());
		}
	}

	public override void PostMake()
	{
		base.PostMake();
		PostInitialize();
	}

	public override void ExposeData()
	{
		base.ExposeData();

		if (Scribe.mode == LoadSaveMode.LoadingVars)
			PostInitialize();
	}
}
