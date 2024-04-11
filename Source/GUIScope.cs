// Copyright (c) 2023 bradson
// This Source Code Form is subject to the terms of the MIT license.
// If a copy of the license was not distributed with this file,
// You can obtain one at https://opensource.org/licenses/MIT/.

namespace AdaptiveStorage;

public static class GUIScope
{
	public readonly record struct WidgetGroup : IDisposable
	{
		public WidgetGroup(in Rect rect) => Widgets.BeginGroup(rect);

		public void Dispose() => Widgets.EndGroup();
	}
	
	public readonly record struct ScrollView : IDisposable
	{
		private readonly ScrollViewStatus _scrollViewStatus;

		private readonly float _outRectHeight;

		public readonly Rect Rect;
		
		public ref float Height => ref _scrollViewStatus.Height;

		public ScrollView(Rect outRect, ScrollViewStatus scrollViewStatus, bool showScrollbars = true)
		{
			_scrollViewStatus = scrollViewStatus;
			_outRectHeight = outRect.height;
			Rect = new(0f, 0f, outRect.width, Math.Max(Height, _outRectHeight));
			if (Height - 0.1f >= outRect.height)
				Rect.width -= 16f;
			
			Height = 0f;
			Widgets.BeginScrollView(outRect, ref _scrollViewStatus.Position, Rect, showScrollbars);
		}

		public void Dispose() => Widgets.EndScrollView();
		
		public bool CanCull(float entryHeight, float entryY)
			=> entryY + entryHeight < _scrollViewStatus.Position.y
				|| entryY > _scrollViewStatus.Position.y + _outRectHeight;
	}

	public class ScrollViewStatus
	{
		public Vector2 Position;
		public float Height;
	}
	
	public readonly record struct TextAnchor : IDisposable
	{
		private readonly UnityEngine.TextAnchor _default;
	
		public TextAnchor(UnityEngine.TextAnchor anchor)
		{
			_default = Text.Anchor;
			Text.Anchor = anchor;
		}
	
		public void Dispose() => Text.Anchor = _default;
	}

	public readonly record struct WordWrap : IDisposable
	{
		private readonly bool _default;
	
		public WordWrap(bool wordWrap)
		{
			_default = Text.WordWrap;
			Text.WordWrap = wordWrap;
		}
	
		public void Dispose() => Text.WordWrap = _default;
	}

	public readonly record struct Color : IDisposable
	{
		private readonly UnityEngine.Color _default;
	
		public Color(UnityEngine.Color color)
		{
			_default = GUI.color;
			GUI.color = color;
		}
	
		public void Dispose() => GUI.color = _default;
	}
	
	public readonly record struct Font : IDisposable
	{
		private readonly GameFont _default;
	
		public Font(GameFont font)
		{
			_default = Text.Font;
			Text.Font = font;
		}
	
		public void Dispose() => Text.Font = _default;
	}
	
	public readonly record struct FontSize : IDisposable
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