// Copyright (c) 2023 bradson
// This Source Code Form is subject to the terms of the MIT license.
// If a copy of the license was not distributed with this file,
// You can obtain one at https://opensource.org/licenses/MIT/.

#if !V1_4
using LudeonTK;
#endif

namespace AdaptiveStorage.Utility;

public static class UIHelpers
{
	static UIHelpers()
	{
		ButtonStyle.alignment = TextAnchor.MiddleLeft;
		ButtonStyle.wordWrap = false;
		LabelStyle.alignment = TextAnchor.UpperLeft;
		LabelStyle.wordWrap = true;
		TitleStyle.alignment = TextAnchor.LowerCenter;
		TitleStyle.wordWrap = false;
	}

	/// <summary>
	/// Displaying a piece of text
	/// </summary>
	/// <param name="rect">Size and position of the button</param>
	/// <param name="label">The text</param>
	/// <param name="style">The GUIStyle used for the text. Use GUIStyle.normal.textColor to set the color.
	///     Defaults to UIHelpers.LabelStyle</param>
	/// <returns>The height of the input rect</returns>
	public static void Label(this Rect rect, string label, GUIStyle style)
	{
		// UI scaling copied from vanilla code. Not sure if necessary.
		var num = Prefs.UIScale / 2f;
		if (Prefs.UIScale > 1f && num - Mathf.Floor(num) > float.Epsilon)
		{
#if V1_4
			rect.xMin = Widgets.AdjustCoordToUIScalingFloor(rect.xMin);
			rect.yMin = Widgets.AdjustCoordToUIScalingFloor(rect.yMin);
			rect.xMax = Widgets.AdjustCoordToUIScalingCeil(rect.xMax + 1E-05f);
			rect.yMax = Widgets.AdjustCoordToUIScalingCeil(rect.yMax + 1E-05f);
#else
			rect.xMin = UIScaling.AdjustCoordToUIScalingFloor(rect.xMin);
			rect.yMin = UIScaling.AdjustCoordToUIScalingFloor(rect.yMin);
			rect.xMax = UIScaling.AdjustCoordToUIScalingCeil(rect.xMax + 1E-05f);
			rect.yMax = UIScaling.AdjustCoordToUIScalingCeil(rect.yMax + 1E-05f);
#endif
		}

		GUI.Label(rect, label, style);
	}

	/// <summary>
	/// Determines whether a rect is not visible from a given scrollPosition and should therefore be skipped
	/// </summary>
	/// <param name="scrollViewSize">The width or height of the ScrollView's outRect</param>
	/// <param name="entrySize">The width or height of the tested rect</param>
	/// <param name="entryPosition">The x or y value of the tested rect</param>
	/// <param name="scrollPosition">The x or y value of the ScrollView's scrollPosition</param>
	/// <returns>true when out of view, otherwise false</returns>
	public static bool ShouldSkipForScrollView(this float scrollViewSize, float entrySize, float entryPosition,
		float scrollPosition)
		=> entryPosition + entrySize < scrollPosition || entryPosition > scrollPosition + scrollViewSize;

	public static Vector2 BeginScrollView(this Rect outRect, Vector2 scrollPosition, Rect viewRect,
		GUIStyle? verticalScrollbar = null, GUIStyle? horizontalScrollbar = null /*, GUIStyle background = null*/)
	{
		// no idea what that ScrollViewStack is used for tbf
		if (MouseOverScrollViewStack.Count > 0)
		{
			MouseOverScrollViewStack.Push(MouseOverScrollViewStack.Peek()
				&& outRect.Contains(Event.current.mousePosition));
		}
		else
		{
			MouseOverScrollViewStack.Push(outRect.Contains(Event.current.mousePosition));
		}

		// BeginScrollView's overload with background argument is marked internal for some reason. It's stupid, but not
		// really worth bothering with either. The other styles should be enough for most use cases.
		return GUI.BeginScrollView(outRect, scrollPosition, viewRect, false, false,
			horizontalScrollbar ?? GUI.skin.horizontalScrollbar,
			verticalScrollbar ?? GUI.skin.verticalScrollbar /*, background ?? GUI.skin.scrollView*/);
	}

	/// <summary>
	/// A single-line text field, just like the one from Verse.Widgets, but as extension and with GUIStyle argument
	/// </summary>
	public static string TextField(this Rect rect, string? text, GUIStyle? style = null)
		=> GUI.TextField(rect, text ?? string.Empty, style ?? Text.CurTextFieldStyle);

	/// <summary>
	/// A multi-line text area, just like the one from Verse.Widgets, but as extension and with GUIStyle argument
	/// </summary>
	public static string TextArea(this Rect rect, string? text, GUIStyle? style = null)
		=> GUI.TextArea(rect, text ?? string.Empty, style ?? Text.CurTextAreaStyle);

	/// <summary>
	/// Copy of Verse.Text.CalcHeight with additional GUIStyle argument
	/// </summary>
	public static float CalcHeight(this string text, float width, GUIStyle? style = null)
	{
		TmpTextGUIContent.text = text.StripTags();
		return (style ?? Text.CurFontStyle).CalcHeight(TmpTextGUIContent, width);
	}
	
	/// <summary>
	/// Label with GUIStyle argument for Verse.WidgetRow
	/// </summary>
	public static void StyledLabel(this WidgetRow widgetRow, string text, GUIStyle? guiStyle = null)
	{
		var width = Text.CalcSize(text).x;
		widgetRow.IncrementYIfWillExceedMaxWidth(width + 2f);
		widgetRow.IncrementPosition(2f);
		Rect rect = new(widgetRow.LeftX(width), widgetRow.curY, width, 24f);
		rect.Label(text, guiStyle ?? LabelStyle);
		widgetRow.IncrementPosition(2f + rect.width);
	}

	/// <summary>
	/// InfoText with GUIStyle argument for Verse.Listing_Tree
	/// </summary>
	public static void StyledInfoText(this Listing_TreeDefs listing, string text, GUIStyle? style = null)
	{
		Rect rect = new(0f, listing.CurHeight, listing.ColumnWidth, 50f) { xMin = listing.GetLabelWidth() };
		style ??= LabelStyle;
		rect.height = text.CalcHeight(rect.width, style);
		rect.Label(text, style);
		listing.curY += rect.height;
	}
	
	public static GUIContent TmpTextGUIContent { get; } = Text.tmpTextGUIContent;
	public static GUIStyle LabelStyle { get; } = new(Text.fontStyles[1]);
	public static GUIStyle TitleStyle { get; } = new(Text.fontStyles[1]);
	public static GUIStyle ButtonStyle { get; } = new(Text.fontStyles[1]);
	private static Stack<bool> MouseOverScrollViewStack => Widgets.mouseOverScrollViewStack;
}