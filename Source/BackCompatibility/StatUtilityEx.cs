// Copyright (c) 2023 bradson
// This Source Code Form is subject to the terms of the MIT license.
// If a copy of the license was not distributed with this file,
// You can obtain one at https://opensource.org/licenses/MIT/.

#if V1_4
global using AdaptiveStorage.BackCompatibility;

namespace AdaptiveStorage.BackCompatibility;

public static class StatUtilityEx
{
	public static float GetStatFactorFromList(this List<StatModifierQuality> modList, StatDef stat, QualityCategory qc)
		=> modList.GetStatValueFromList(stat, qc, 1f);

	public static float GetStatOffsetFromList(this List<StatModifierQuality> modList, StatDef stat, QualityCategory qc)
		=> modList.GetStatValueFromList(stat, qc, 0f);

	public static float GetStatValueFromList(this List<StatModifierQuality>? modList, StatDef stat, QualityCategory qc,
		float defaultValue)
	{
		if (modList != null)
		{
			for (var i = 0; i < modList.Count; i++)
			{
				var mod = modList[i];
				if (mod.stat == stat)
					return mod.GetValue(qc);
			}
		}

		return defaultValue;
	}
}
#endif