// Copyright (c) 2023 bradson
// This Source Code Form is subject to the terms of the MIT license.
// If a copy of the license was not distributed with this file,
// You can obtain one at https://opensource.org/licenses/MIT/.

namespace AdaptiveStorage;

[UsedImplicitly(ImplicitUseTargetFlags.Members)]
public class Extension : DefModExtension
{
	public List<GraphicsDef>? graphics;

	public bool lockStorageSettingsToStuff;

	public LabelFormat labelFormat;

	public List<StatModifier>? itemStatFactors, itemStatOffsets;
	
	public List<StatModifierQuality>? itemStatFactorsByQuality, itemStatOffsetsByQuality;

	public ValuesByQuality? maxItemsPerCellByQuality;

	public CellTable<ValuesByQuality>? maxItemsByCell;

	public TemperatureProperties? temperature;

	public void Initialize(ThingDef parent)
	{
		if (graphics is null)
			return;

		for (var i = graphics.Count; i-- > 0;)
			graphics[i].targetDefs.AddDistinct(parent);
		
		maxItemsByCell?.Initialize([parent]);

		if (temperature is { coolingOffset: 0f, heatingOffset: 0f })
			temperature = null;
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
