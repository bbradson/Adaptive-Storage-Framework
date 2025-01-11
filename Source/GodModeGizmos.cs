// Copyright (c) 2023 bradson
// This Source Code Form is subject to the terms of the MIT license.
// If a copy of the license was not distributed with this file,
// You can obtain one at https://opensource.org/licenses/MIT/.

using System.Linq;

namespace AdaptiveStorage;

public class GodModeGizmos(ThingClass parent)
{
	public readonly Command_AddStack AddStack = new(parent)
	{
		defaultLabel = "Add stack",
		action = () => GenSpawn.Spawn(ThingMakerUtility.Make(parent.GetStoreSettings().filter.AnyAllowedDef
				?? parent.GetParentStoreSettings().filter.AnyAllowedDef).TryMakeMinified(),
			parent.FreeMapCells.First(), parent.Map)
	};

	public readonly CommandWithFloatMenu EditGraphics = new()
	{
		defaultLabel = "Edit graphics",
		action = () => Find.WindowStack.Add(new DefEditorWindow(parent.Renderer!.CurrentGraphicVariation
			?? parent.Renderer.AllGraphics!.First())),
		floatMenuOptionsInitializer = () => parent.Renderer!.AllGraphics!.Select(static graphic
				=> new FloatMenuOption(graphic.defName, () => Find.WindowStack.Add(new DefEditorWindow(graphic))))
			.ToArray()
	};

	public readonly Command_Action UpdateGraphics = new()
	{
		defaultLabel = "Update graphics",
		action = () =>
		{
			var renderer = parent.Renderer!;
			foreach (var graphic in renderer.AllGraphics!)
			{
				foreach (var storageGraphic in graphic.graphics)
				{
					foreach (var graphicData in storageGraphic.graphicDatas)
						graphicData.Init();
				}
			}

			parent.def.graphicData.Init();

			renderer.InitializeStoredThingGraphics(parent.CurrentSectionLayer);
			renderer.NotifyCurrentGraphicChanged();
			LongEventHandler.ExecuteWhenFinished(() => parent.DirtyMapMesh(parent.Map));
		}
	};

	public class Command_AddStack(ThingClass parent) : Command_Action
	{
		private (ThingDef def, FloatMenuOption option)[]? _floatMenuOptionsByDef;
		private IEnumerable<FloatMenuOption>? _filteredFloatMenuOptions, _unfilteredFloatMenuOptions;

		private (ThingDef def, FloatMenuOption option)[] FloatMenuOptionsByDef
			=> _floatMenuOptionsByDef
				??= parent.GetParentStoreSettings().filter.AllowedThingDefs
					.Select(def => (def,
						new FloatMenuOption(def.label, () => GenSpawn.Spawn(ThingMakerUtility.Make(def)
							.TryMakeMinified(), parent.FreeMapCells.First(), parent.Map))))
					.ToArray();

		private IEnumerable<FloatMenuOption> FilteredFloatMenuOptions
			=> _filteredFloatMenuOptions ??= InitializeFilteredFloatMenuOptions();

		private IEnumerable<FloatMenuOption> InitializeFilteredFloatMenuOptions()
		{
			var filter = parent.GetStoreSettings().filter;

			return FloatMenuOptionsByDef
				.Where(tuple => filter.Allows(tuple.def))
				.Select(static tuple => tuple.option);
		}

		private IEnumerable<FloatMenuOption> UnfilteredFloatMenuOptions
			=> _unfilteredFloatMenuOptions ??= FloatMenuOptionsByDef.Select(static tuple => tuple.option);

		public override IEnumerable<FloatMenuOption> RightClickFloatMenuOptions
			=> !parent.AnyFreeSlots
				? base.RightClickFloatMenuOptions
				: parent.GetStoreSettings().filter.AnyAllowedDef != null
					? FilteredFloatMenuOptions
					: UnfilteredFloatMenuOptions;
	}
}