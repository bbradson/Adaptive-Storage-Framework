// Copyright (c) 2023 bradson
// This Source Code Form is subject to the terms of the MIT license.
// If a copy of the license was not distributed with this file,
// You can obtain one at https://opensource.org/licenses/MIT/.

using Verse.Sound;

namespace AdaptiveStorage;

// more or less just MaxSearchTextLength exposed, because it was a hardcoded const with a value of 15
[PublicAPI]
public class BetterQuickSearchWidget
{
	public const float DEFAULT_WIDGET_HEIGHT = 24f;

	public const float ICON_SIZE = 18f;

	public const float ICON_MARGIN = 4f;

	public int MaxSearchTextLength { get; set; } = 15;

	private static int _instanceCounter;

	public QuickSearchFilter Filter { get; } = new();

	public bool NoResultsMatched { get; set; }

	public Color InactiveTextColor { get; set; } = Color.white;

	private readonly string _controlName;

	public BetterQuickSearchWidget() => _controlName = $"BetterQuickSearchWidget_{_instanceCounter++}";

	public void OnGUI(Rect rect, Action? onFilterChange = null)
	{
		if (CurrentlyFocused() && Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape)
		{
			Unfocus();
			Event.current.Use();
		}
		
		if (OriginalEventUtility.EventType == EventType.MouseDown && !rect.Contains(Event.current.mousePosition))
			Unfocus();
		
		var previousGUIColor = GUI.color;
		
		GUI.color = Color.white;
		
		var searchIconHeight = Mathf.Min(ICON_SIZE, rect.height);
		var searchIconAreaWidth = searchIconHeight + (2f * ICON_MARGIN);
		var y = rect.y + ((rect.height - searchIconAreaWidth) / 2f) + ICON_MARGIN;
		var searchIconRect = new Rect(rect.x + ICON_MARGIN, y, searchIconHeight, searchIconHeight);
		
		GUI.DrawTexture(searchIconRect, TexButton.Search);
		GUI.SetNextControlName(_controlName);

		var textBoxRect = new Rect(searchIconRect.xMax + ICON_MARGIN, rect.y, rect.width - (2f * searchIconAreaWidth),
			rect.height);

		if (NoResultsMatched && Filter.Active)
			GUI.color = ColorLibrary.RedReadable;
		else if (!Filter.Active && !CurrentlyFocused())
			GUI.color = InactiveTextColor;

		var text = Widgets.TextField(textBoxRect, Filter.Text, MaxSearchTextLength);
		GUI.color = Color.white;

		if (text != Filter.Text)
		{
			Filter.Text = text;
			onFilterChange?.Invoke();
		}

		if (Filter.Active
			&& Widgets.ButtonImage(new(textBoxRect.xMax + 4f, y, searchIconHeight, searchIconHeight),
				TexButton.CloseXSmall))
		{
			Filter.Text = "";
			SoundDefOf.CancelMode.PlayOneShotOnCamera();
			onFilterChange?.Invoke();
		}
		
		GUI.color = previousGUIColor;
	}

	public void Unfocus()
	{
		if (CurrentlyFocused())
			UI.UnfocusCurrentControl();
	}

	public void Focus() => GUI.FocusControl(_controlName);

	public bool CurrentlyFocused() => GUI.GetNameOfFocusedControl() == _controlName;

	public void Reset()
	{
		Filter.Text = "";
		NoResultsMatched = false;
	}
}