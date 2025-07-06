// Copyright (c) 2023 bradson
// This Source Code Form is subject to the terms of the MIT license.
// If a copy of the license was not distributed with this file,
// You can obtain one at https://opensource.org/licenses/MIT/.

namespace AdaptiveStorage;

[UsedImplicitly(ImplicitUseTargetFlags.Members)]
public class Extension : DefModExtension
{
	public List<GraphicsDef>? graphics;

	public bool lockStorageSettingsToStuff, highlightRoomWhenSelected;

	public LabelFormat labelFormat;

	public List<StatModifier>? itemStatFactors, itemStatOffsets;
	
	public List<StatModifierQuality>? itemStatFactorsByQuality, itemStatOffsetsByQuality;

	public ValuesByQuality? maxItemsPerCellByQuality;

	public CellTable<ValuesByQuality>? maxItemsByCell;

	public TemperatureProperties? temperature;
	
	public Type frameClass = typeof(Frame);

	public void Initialize(ThingDef parent)
	{
		if (graphics != null)
		{
			for (var i = graphics.Count; --i >= 0;)
				graphics[i].targetDefs.AddDistinct(parent);
		}
		
		maxItemsByCell?.Initialize([parent]);

		if (temperature is { coolingOffset: 0f, heatingOffset: 0f })
			temperature = null;

		if (parent.blueprintDef is { } blueprintDef)
		{
			ref var blueprintClass = ref blueprintDef.thingClass;

#if !V1_4 && !V1_5
			if (typeof(Blueprint_StorageWithRoomHighlight).IsAssignableFrom(blueprintClass))
				highlightRoomWhenSelected = true;
#endif
		
			if (!typeof(Blueprint).IsAssignableFrom(blueprintClass))
			{
				blueprintClass = typeof(Blueprint);
				
				if (parent.building is { } buildingProperties)
					buildingProperties.blueprintClass = blueprintClass;
			}
		}

		// ReSharper disable once NullCoalescingConditionIsAlwaysNotNullAccordingToAPIContract
		if (parent.frameDef is { } frameDef)
			frameDef.thingClass = frameClass ?? typeof(Frame);
	}

	public StorageSettings? TryCreateStuffLockedStorageSettings(Thing thing)
		=> lockStorageSettingsToStuff && thing.GetStuffToUse() is { } stuff
			? CreateStuffLockedStorageSettings(stuff)
			: null;

	public static StorageSettings CreateStuffLockedStorageSettings(ThingDef stuff)
	{
		var fixedStorageSettings = new StorageSettings();
		var filter = fixedStorageSettings.filter = ThingFilter.CreateOnlyEverStorableThingFilter();
		filter.SetDisallowAll();
		filter.SetAllow(stuff, true);
		return fixedStorageSettings;
	}

	public void TryHighlightRoomWhenSelected(Thing thing)
	{
		if (!highlightRoomWhenSelected || Find.Selector.SingleSelectedThing != thing)
			return;

		var room = thing.GetRoom();
		if (room is { ProperRoom: true, PsychologicallyOutdoors: false })
			room.DrawFieldEdges();
	}

	[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
	public class TemperatureProperties
	{
		public float
			coolingOffset,
			heatingOffset,
			coolingMin = float.NegativeInfinity,
			heatingMax = float.PositiveInfinity;

		public bool
			requiresPower = true,
			requiresSwitchOn = true,
			requiresFuel = true;
	}
}
