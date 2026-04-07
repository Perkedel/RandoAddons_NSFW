#nullable enable
using System;
using Editor;

namespace Sandbox.widgets;

/// <summary>Shared painting helpers for SandGit widgets.</summary>
public static class WidgetPaintUtils {
	public const int DefaultChevronSize = 14;
	public const float DefaultFieldCornerRadius = 4f;

	/// <summary>Paints a centered dropdown chevron (expand_more) in the given bounds. Use from OnPaint.</summary>
	public static void DrawDropdownChevron(Rect bounds, int chevronSize = DefaultChevronSize) {
		var iconRect = GetCenteredIconRect(bounds, chevronSize, 2, 4, 2, 4);
		var fg = Theme.Text.Darken(0.2f);
		Paint.SetPen(in fg);
		Paint.SetBrush(in fg);
		Paint.DrawIcon(iconRect, "expand_more", chevronSize);
	}

	/// <summary>Returns a rect centered in bounds, shrunk by padding and clamped to iconSize. Use for drawing icons in buttons.</summary>
	public static Rect GetCenteredIconRect(Rect bounds, int iconSize, float paddingLeft = 2, float paddingTop = 4,
		float paddingRight = 2, float paddingBottom = 4) {
		var r = bounds.Shrink(paddingLeft, paddingTop, paddingRight, paddingBottom);
		r.Width = Math.Min(r.Width, iconSize);
		r.Height = Math.Min(r.Height, iconSize);
		r.Left = bounds.Left + (bounds.Width - r.Width) * 0.5f;
		r.Top = bounds.Top + (bounds.Height - r.Height) * 0.5f;
		return r;
	}

	/// <summary>Paints the standard text/input field background (dark rounded rect). Use from OnPaint before drawing text.</summary>
	public static void DrawTextFieldBackground(Rect bounds, float cornerRadius = DefaultFieldCornerRadius) {
		var bg = Color.White.Darken(0.85f).Desaturate(0.5f);
		Paint.ClearPen();
		Paint.SetBrush(in bg);
		Paint.DrawRect(bounds, cornerRadius);
	}
}
