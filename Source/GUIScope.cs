// Copyright (c) 2023 bradson
// This Source Code Form is subject to the terms of the MIT license.
// If a copy of the license was not distributed with this file,
// You can obtain one at https://opensource.org/licenses/MIT/.

namespace AdaptiveStorage;

public static class GUIScope
{
	public readonly struct WidgetGroup : IDisposable
	{
		public WidgetGroup(in Rect rect) => Widgets.BeginGroup(rect);

		public void Dispose() => Widgets.EndGroup();
	}
	
	public readonly struct ScrollView : IDisposable
	{
		private readonly ScrollViewStatus _scrollViewStatus;

		public readonly Rect Rect;
		
		public ref float Height => ref _scrollViewStatus.Height;

		public ScrollView(Rect outRect, ScrollViewStatus scrollViewStatus, bool showScrollbars = true)
		{
			_scrollViewStatus = scrollViewStatus;
			// var viewRect = outRect with { width = outRect.width - 20f, height = _scrollViewStatus.Height };
			Rect = new(0f, 0f, outRect.width, Math.Max(_scrollViewStatus.Height, outRect.height));
			if (_scrollViewStatus.Height - 0.1f >= outRect.height)
				Rect.width -= 16f;
			
			scrollViewStatus.Height = 0f;
			Widgets.BeginScrollView(outRect, ref _scrollViewStatus.Position, Rect, showScrollbars);
		}

		public void Dispose() => Widgets.EndScrollView();
	}

	public class ScrollViewStatus
	{
		public Vector2 Position;
		public float Height;
	}
	
	public readonly struct TextAnchor : IDisposable
	{
		private readonly UnityEngine.TextAnchor _default;
	
		public TextAnchor(UnityEngine.TextAnchor anchor)
		{
			_default = Text.Anchor;
			Text.Anchor = anchor;
		}
	
		public void Dispose() => Text.Anchor = _default;
	}

	public readonly struct WordWrap : IDisposable
	{
		private readonly bool _default;
	
		public WordWrap(bool wordWrap)
		{
			_default = Text.WordWrap;
			Text.WordWrap = wordWrap;
		}
	
		public void Dispose() => Text.WordWrap = _default;
	}

	public readonly struct Color : IDisposable
	{
		private readonly UnityEngine.Color _default;
	
		public Color(UnityEngine.Color color)
		{
			_default = GUI.color;
			GUI.color = color;
		}
	
		public void Dispose() => GUI.color = _default;
	}
	
	public readonly struct Font : IDisposable
	{
		private readonly GameFont _default;
	
		public Font(GameFont font)
		{
			_default = Text.Font;
			Text.Font = font;
		}
	
		public void Dispose() => Text.Font = _default;
	}
	
	public readonly struct FontSize : IDisposable
	{
		private readonly int _default;
	
		public FontSize(int size)
		{
			var curFontStyle = Text.CurFontStyle;
			
			_default = curFontStyle.fontSize;
			curFontStyle.fontSize = size;
		}
	
		public void Dispose() => Text.CurFontStyle.fontSize = _default;
	}
}