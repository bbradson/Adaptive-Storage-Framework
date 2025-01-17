// Copyright (c) 2023 bradson
// This Source Code Form is subject to the terms of the MIT license.
// If a copy of the license was not distributed with this file,
// You can obtain one at https://opensource.org/licenses/MIT/.

using AdaptiveStorage.Fishery.Pools;

namespace AdaptiveStorage;

[PublicAPI]
public abstract class ContentLabelWorker
{
	public abstract string?[]? UpdateLabels(ThingClass building, int totalThingCount, GraphicsDef? graphicsDef);

	public virtual void DrawGUIOverlayLabels(ThingClass building)
	{
		var overlayLabels = building.GUIOverlayLabels;
		if (overlayLabels is not [_,..])
			return;
		
		var labelDrawPos = LabelDrawPosFor(building.DrawPos);
		var labelYOffset = (Text.TinyFontSupported ? GenMapUI.NameBGHeight_Tiny : GenMapUI.NameBGHeight_Small) + 1f;
		
		labelDrawPos.y -= (overlayLabels.Length - 1) * labelYOffset / 2f;
		
		for (var i = 0; i < overlayLabels.Length; i++)
		{
			var overlayLabel = overlayLabels[i];
			if (!string.IsNullOrEmpty(overlayLabel))
				GenMapUI.DrawThingLabel(labelDrawPos, overlayLabel, GenMapUI.DefaultThingLabelColor);

			labelDrawPos.y += labelYOffset;
		}
	}

	public virtual void ResolveReferences()
	{
	}

	private static Vector2 LabelDrawPosFor(Vector3 drawPos)
	{
		drawPos.z += GenMapUI.LabelOffsetYStandard;
		var vector2 = (Vector2)Find.Camera.WorldToScreenPoint(drawPos) / Prefs.UIScale;
		vector2.y = UI.screenHeight - vector2.y;

		return vector2;
	}

	public class Automatic : ContentLabelWorker
	{
		public override string?[]? UpdateLabels(ThingClass building, int totalThingCount, GraphicsDef? graphicsDef)
		{
			var worker = graphicsDef?.AutomaticLabelStyle.ContentLabelWorker;
			return worker != null && worker.GetType() != typeof(Automatic)
				? worker.UpdateLabels(building, totalThingCount, graphicsDef) : null;
		}
	}

	public class NamesWithCount : Names
	{
		public override string?[]? UpdateLabels(ThingClass building, int totalThingCount, GraphicsDef? graphicsDef)
			=> totalThingCount > 0
				? building.StoredThings.ToStringsThingLabels(true, true, GetMaxLabelLines(building))
				: null;
	}
	
	public class NamesWithCountOrTotalCount : NamesOrTotalCount
	{
		public override string?[]? UpdateLabels(ThingClass building, int totalThingCount, GraphicsDef? graphicsDef)
			=> totalThingCount > 0
				? GetMaxLabelLines(building) is var maxLabelLines
				&& building.StoredThings.ToStringsThingLabels(true, true, maxLabelLines + 1) is var labels
				&& labels.Length <= maxLabelLines
					? labels
					: TotalCount.Instance.UpdateLabels(building, totalThingCount, graphicsDef)
				: null;
	}
	
	public class TotalCount : ContentLabelWorker
	{
		public override string?[]? UpdateLabels(ThingClass building, int totalThingCount, GraphicsDef? graphicsDef)
			=> totalThingCount > 0 ? [string.Concat("[ ", totalThingCount.ToStringCached(), " ]")] : null;

		public static readonly TotalCount Instance = new();
	}
	
	public class Names : ContentLabelWorker
	{
		public override string?[]? UpdateLabels(ThingClass building, int totalThingCount, GraphicsDef? graphicsDef)
			=> totalThingCount > 0
				? building.StoredThings.ToStringsThingLabels(false, true, GetMaxLabelLines(building))
				: null;

		protected virtual int GetMaxLabelLines(ThingClass building) => building.RotatedSize.z > 1 ? 4 : 3;
	}
	
	public class NamesOrTotalCount : Names
	{
		public override string?[]? UpdateLabels(ThingClass building, int totalThingCount, GraphicsDef? graphicsDef)
			=> totalThingCount > 0
				? GetMaxLabelLines(building) is var maxLabelLines
				&& building.StoredThings.ToStringsThingLabels(false, true, maxLabelLines + 1) is var labels
				&& labels.Length <= maxLabelLines
					? labels
					: TotalCount.Instance.UpdateLabels(building, totalThingCount, graphicsDef)
				: null;

		protected override int GetMaxLabelLines(ThingClass building) => 3;
	}

	public class NamesOrNameCount : NamesOrTotalCount
	{
		public override string?[]? UpdateLabels(ThingClass building, int totalThingCount, GraphicsDef? graphicsDef)
			=> totalThingCount > 0
				? building.StoredThings.ToStringsThingLabels(false, true) is var labels
				&& labels.Length <= GetMaxLabelLines(building)
					? labels
					: TotalCount.Instance.UpdateLabels(building, labels.Length, graphicsDef)
				: null;

		protected override int GetMaxLabelLines(ThingClass building) => 3;
	}
	
	public class None : ContentLabelWorker
	{
		public override string?[]? UpdateLabels(ThingClass building, int totalThingCount, GraphicsDef? graphicsDef)
			=> null;
	}

	public class Vanilla : ContentLabelWorker
	{
		public override string?[]? UpdateLabels(ThingClass building, int totalThingCount, GraphicsDef? graphicsDef)
		{
			var storedThings = building.StoredThings;
			if (storedThings.Count <= 0)
				return null;
			
			using var pooledList = new PooledList<string?>();
			var list = pooledList.List;
			
			for (var i = 0; i < storedThings.Count; i++)
			{
				var itemDef = storedThings.DefAt(i);

				list.Add(!itemDef.drawGUIOverlay
					? null
					: itemDef.stackLimit > 1
						? storedThings[i].stackCount.ToStringCached()
						: itemDef.drawGUIOverlayQuality && storedThings[i].TryGetQuality(out var quality)
							? quality.GetLabelShort()
							: null);
			}

			return list.ToArray();
		}

		public override void DrawGUIOverlayLabels(ThingClass building)
		{
			var labels = building.GUIOverlayLabels;
			if (labels is not [_,..])
				return;

			var storedThings = building.StoredThings;
			var renderer = building.Renderer;
			var drawAtDefaultPosition
				= renderer is null || !renderer.ShowContainedItems || renderer.CurrentGraphicVariation is null;
			
			var buildingDrawPos = building.DrawPos;

			for (var i = 0; i < labels.Length; i++)
			{
				var itemLabel = labels[i];
				if (itemLabel.NullOrEmpty())
					continue;

				var item = storedThings[i];
				GenMapUI.DrawThingLabel(LabelDrawPosFor(!drawAtDefaultPosition
					&& renderer!.TryGetPrintDataOf(item) is { } printData
						? printData.DrawOffset
						+ buildingDrawPos
						: item.DrawPos), itemLabel, GenMapUI.DefaultThingLabelColor);
			}
		}
	}
}