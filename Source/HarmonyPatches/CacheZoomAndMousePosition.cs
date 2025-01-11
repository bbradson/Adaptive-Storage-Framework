// Copyright (c) 2023 bradson
// This Source Code Form is subject to the terms of the MIT license.
// If a copy of the license was not distributed with this file,
// You can obtain one at https://opensource.org/licenses/MIT/.

using System.Linq;
using HarmonyLib;

namespace AdaptiveStorage.HarmonyPatches;

[HarmonyPatch(typeof(ThingOverlays), nameof(ThingOverlays.ThingOverlaysOnGUI))]
public static class CacheZoomAndMousePosition
{
	[HarmonyTranspiler]
	public static CodeInstructions Transpiler(CodeInstructions instructions)
	{
		var codes = instructions.ToList();
		var cameraDriverMethod
			= AccessTools.DeclaredPropertyGetter(typeof(CameraDriver), nameof(CameraDriver.CurrentViewRect));

		var targetIndex = codes.FindIndex(code => code.Calls(cameraDriverMethod));
		if (targetIndex > 0)
		{
			targetIndex++;
		}
		else
		{
			Log.Error($"'AdaptiveStorage.HarmonyPatches.CacheZoomAndMousePosition.Transpiler' failed to find its "
				+ $"'CameraDriver.CurrentViewRect' target method call. Current instructions:\n{
					string.Join("\n", codes.Select(static code => $"{code.opcode} {code.operand} {
						code.labels?.ToStringSafeEnumerable()} {code.blocks?.ToStringSafeEnumerable()}"))}");
			return codes;
		}
		
		codes.Insert(targetIndex, Code.Call[((Delegate)Update).Method]);
		
		return codes;
	}

	public static void Update()
	{
		var cameraDriver = Current.CameraDriver;
		ZoomValue = cameraDriver.ZoomRootSize;
		ZoomRange = cameraDriver.CurrentZoom;
		Position = UI.MouseCell().ToIntVec2;
		LabelHidingZoom = AdaptiveStorageFrameworkSettings.LabelHidingMaxZoomValue;
	}
	
	public static IntVec2 Position { get; private set; }
	
	public static CameraZoomRange ZoomRange { get; private set; }

	public static double ZoomValue { get; private set; }
	
	public static double LabelHidingZoom { get; private set; }
}