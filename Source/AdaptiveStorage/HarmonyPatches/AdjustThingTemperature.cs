// Copyright (c) 2024 bradson
// This Source Code Form is subject to the terms of the MIT license.
// If a copy of the license was not distributed with this file,
// You can obtain one at https://opensource.org/licenses/MIT/.

using HarmonyLib;

namespace AdaptiveStorage.HarmonyPatches;

[HarmonyPatch(typeof(Thing), nameof(Thing.AmbientTemperature), MethodType.Getter)]
public static class AdjustThingTemperature
{
	[HarmonyPrepare]
	public static bool HarmonyPrepare()
		=> DefDatabase<ThingDef>.AllDefsListForReading.Exists(static def
			=> def.GetModExtension<Extension>() is
			{
				temperature: { heatingOffset: not 0f } or { coolingOffset: not 0f }
			});
	
	[HarmonyPostfix]
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void Postfix(Thing __instance, ref float __result)
	{
		if (__instance.StoringAdaptiveStorage() is
		{
			Extension.temperature: { } temperatureProperties,
			CompPowerTrader: not { PowerOn: false }
		})
		{
			__result = GetNewTemperature(__result, temperatureProperties);
		}
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static float GetNewTemperature(float temperature, Extension.TemperatureProperties temperatureProperties)
	{
		float offset;
		float limit;

		if (((offset = temperatureProperties.heatingOffset) != 0f)
			& (temperature < (limit = temperatureProperties.heatingMax)))
		{
			temperature = (temperature += offset) < limit ? temperature : limit;
		}

		return ((offset = temperatureProperties.coolingOffset) != 0f)
			& (temperature > (limit = temperatureProperties.coolingMin))
				? (temperature -= offset) > limit ? temperature : limit
				: temperature;
	}
}