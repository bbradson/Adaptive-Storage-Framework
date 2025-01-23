// Copyright (c) 2023 bradson
// This Source Code Form is subject to the terms of the MIT license.
// If a copy of the license was not distributed with this file,
// You can obtain one at https://opensource.org/licenses/MIT/.

namespace AdaptiveStorage.Utility;

public static class ListingExtensions
{
	/// <summary>
	/// The normal button only displays mouseover highlights when given a tooltip. This always displays highlights on
	/// mouseover
	/// </summary>
	public static bool RadioButtonConsistent(this Listing_Standard listing,
		string label,
		bool active,
		float tabIn = 0.0f,
		string? tooltip = null,
		float? tooltipDelay = null)
	{
		var rect = listing.GetRect(Text.LineHeight);
		rect.xMin += tabIn;
		
		if (listing.BoundingRectCached is { } boundingRect && !rect.Overlaps(boundingRect))
			return false;
		
		if (Mouse.IsOver(rect))
			Widgets.DrawHighlight(rect);
		
		if (!tooltip.NullOrEmpty())
			TooltipHandler.TipRegion(rect, tooltipDelay is { } delay ? new TipSignal(tooltip, delay) : new(tooltip));
		
		var buttonPressed = Widgets.RadioButtonLabeled(rect, label, active);
		
		listing.Gap(listing.verticalSpacing);
		
		return buttonPressed;
	}
	
	public static void CollectionSlider<T>(this Listing_Standard ls, IList<T> values, ref int selectedIndex,
		string label, string? tooltip = null, Func<T, string>? labelSelector = null)
	{
		var maxIndex = values.Count - 1;
		selectedIndex = Math.Min(selectedIndex, maxIndex);
		
		using (new GUIScope.TextAnchor(TextAnchor.MiddleRight))
		{
			var currentValue = values[selectedIndex];
			var currentValueText
				= labelSelector != null ? labelSelector(currentValue) : currentValue?.ToString() ?? "null";
			var columnWidth = ls.ColumnWidth;
			Widgets.Label(new(ls.curX, ls.curY, columnWidth, Text.CalcHeight(currentValueText, columnWidth)),
				currentValueText);
		}

		ls.Label(label, tooltip: tooltip);
		selectedIndex = Convert.ToInt32(ls.Slider(selectedIndex, 0f, maxIndex));
	}
}