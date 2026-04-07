#nullable enable
using System;
using System.Collections.Generic;
using Editor;
using Sandbox.git;
using Sandbox.git.models;
using CommitModel = Sandbox.git.models.Commit;

namespace Sandbox.widgets;

static class HistoryWidgetHelpers {
	/// <summary>Format date like Desktop: relative ("2 hours ago", "3 days ago") or absolute if older.</summary>
	public static string FormatCommitDate(DateTimeOffset date) {
		var now = DateTimeOffset.Now;
		var diff = now - date;
		if ( diff.TotalMinutes < 1 ) return "just now";
		if ( diff.TotalMinutes < 60 ) return $"{(int)diff.TotalMinutes} minutes ago";
		if ( diff.TotalHours < 24 ) return $"{(int)diff.TotalHours} hours ago";
		if ( diff.TotalDays < 7 ) return $"{(int)diff.TotalDays} days ago";
		return date.ToString("yyyy-MM-dd");
	}

	public static bool LooksLikeSha(string value) {
		if ( string.IsNullOrEmpty(value) || value.Length != 40 ) return false;
		for ( var i = 0; i < value.Length; i++ ) {
			var c = value[i];
			if ( (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F') )
				continue;
			return false;
		}

		return true;
	}
}

/// <summary>
/// Simplified git history: commit summary, date, author, and branch ↑/↓ (ahead/behind).
/// Loads history when the widget is shown (mirrors Desktop loadCommitBatch on tab switch).
/// </summary>
public class HistoryWidget : Widget {
	const float MinContentHeight = 120f;

	readonly GitStore _store;
	readonly ScrollArea _scroller;
	bool _initialLoadTriggered;

	public HistoryWidget(Widget parent, GitStore store) : base(parent) {
		_store = store ?? throw new ArgumentNullException(nameof(store));

		Layout = Layout.Column();
		Layout.Spacing = 4f;


		_scroller = new ScrollArea(this);
		_scroller.MinimumHeight = MinContentHeight;
		Layout.Add(_scroller, 1);

		_scroller.Canvas = new Widget(_scroller);
		_scroller.Canvas.Layout = Layout.Column();
		_scroller.Canvas.Layout.AddStretchCell();

		_store.OnDataChanged += UpdateFromStore;
		UpdateFromStore();
	}

	protected override void OnClosed() {
		_store.OnDataChanged -= UpdateFromStore;
		base.OnClosed();
	}

	/// <summary>
	/// Call when the History tab is shown to load history if needed (connect from tab switch).
	/// </summary>
	public void EnsureHistoryLoaded() {
		if ( !IsValid || !Visible )
			return;
		if ( _store.CurrentRepository == null )
			return;
		if ( _store.IsLoadingHistory )
			return;
		var history = _store.History;
		if ( history.Count > 0 )
			return;
		if ( _initialLoadTriggered )
			return;

		_initialLoadTriggered = true;
		_ = _store.LoadCommitBatchAsync("HEAD", 0);
	}

	void UpdateFromStore() {
		if ( !IsValid )
			return;

		if ( _store.CurrentRepository == null )
			_initialLoadTriggered = false;

		if ( Visible && _store.CurrentRepository != null && _store.History.Count == 0 && !_store.IsLoadingHistory )
			EnsureHistoryLoaded();

		if ( _store.IsLoading || _store.RepositoryType is not RegularRepositoryType ) {
			RebuildRows(null);
			return;
		}

		var fullStatus = _store.FullStatus;
		var ab = fullStatus?.BranchAheadBehind;
		var upDown = ab != null && (ab.Ahead > 0 || ab.Behind > 0)
			? $"↑{ab.Ahead} ↓{ab.Behind}"
			: "";

		RebuildRows(_store.History, _store.CommitLookup);
	}

	void RebuildRows(IReadOnlyList<string>? history, IReadOnlyDictionary<string, CommitModel>? lookup = null) {
		var canvas = new Widget(_scroller);
		canvas.Layout = Layout.Column();

		lookup ??= _store.CommitLookup;

		if ( _store.IsLoadingHistory && (history == null || history.Count == 0) ) {
			var loadingRow = new Label("Loading history…", canvas);
			canvas.Layout.Add(loadingRow);
		} else if ( history != null && history.Count > 0 ) {
			for ( var i = 0; i < history.Count; i++ ) {
				var sha = history[i];
				if ( !lookup.TryGetValue(sha, out var commit) )
					continue;
				var row = new HistoryRow(commit) { Index = i };
				canvas.Layout.Add(row);
			}

			var loadMore = new Button(canvas) { Text = "Load more…" };
			loadMore.Clicked += OnLoadMoreClicked;
			canvas.Layout.Add(loadMore);
		}

		canvas.Layout.AddStretchCell();
		_scroller.Canvas = canvas;
	}

	void OnLoadMoreClicked() {
		if ( _store.CurrentRepository == null || _store.IsLoadingHistory )
			return;
		var history = _store.History;
		_ = _store.LoadCommitBatchAsync("HEAD", history.Count);
	}
}

/// <summary>Single row: commit summary, date, author. Text is truncated to fit the row width.</summary>
public class HistoryRow : Frame {
	const float RowHeight = 36f;
	const float HorizontalPadding = 10f;
	const float VerticalPadding = 6f;
	const float LineGap = 2f;
	const int MaxSummaryChars = 32;
	const int MaxAuthorChars = 18;
	const int ShortShaDisplayLength = 7;

	readonly CommitModel _commit;

	public int Index { get; set; }

	public HistoryRow(CommitModel commit) : base(null) {
		_commit = commit;
		MinimumSize = (int)RowHeight;
		Cursor = CursorShape.Finger;
	}

	static string Truncate(string value, int maxChars, bool addEllipsis = true) {
		if ( string.IsNullOrEmpty(value) || value.Length <= maxChars )
			return value ?? "";
		return value.Substring(0, addEllipsis ? Math.Max(0, maxChars - 1) : maxChars) + (addEllipsis ? "…" : "");
	}

	protected override void OnPaint() {
		var r = new Rect(0, Size);
		var borderColor = Theme.Text.Darken(0.6f).Desaturate(0.5f);
		var textColor = Theme.Text;
		var textColorSecondary = Theme.Text.Darken(0.25f);

		Paint.ClearPen();
		Paint.SetBrush(borderColor);
		Paint.DrawRect(new Rect(r.Left, r.Bottom - 1f, r.Width, 1f));

		var contentWidth = r.Width - 2f * HorizontalPadding;
		if ( contentWidth <= 0 )
			return;

		var left = r.Left + HorizontalPadding;
		var innerHeight = r.Height - 2f * VerticalPadding;
		if ( innerHeight <= 0 )
			return;
		var lineHeight = (innerHeight - LineGap) * 0.5f;
		var line1Rect = new Rect(left, r.Top + VerticalPadding, contentWidth, lineHeight);
		var line2Rect = new Rect(left, r.Top + VerticalPadding + lineHeight + LineGap, contentWidth, lineHeight);

		Paint.SetPen(textColor);

		var shortSha = _commit.ShortSha ?? _commit.Sha ?? "";
		if ( shortSha.Length > ShortShaDisplayLength )
			shortSha = shortSha.Substring(0, ShortShaDisplayLength);

		var summary = string.IsNullOrEmpty(_commit.Summary) ? "" : _commit.Summary;
		summary = Truncate(summary, MaxSummaryChars);

		var line1 = string.IsNullOrEmpty(summary) ? shortSha : shortSha + " " + summary;
		line1 = Truncate(line1, Math.Max(1, (int)(contentWidth / 6f)));

		Paint.DrawText(line1Rect, line1, TextFlag.LeftCenter);

		var dateStr = _commit.Author?.Date != null
			? HistoryWidgetHelpers.FormatCommitDate(_commit.Author.Date)
			: "—";
		var rawAuthor = _commit.Author?.Name ?? "—";
		var authorStr = HistoryWidgetHelpers.LooksLikeSha(rawAuthor) ? "—" : Truncate(rawAuthor, MaxAuthorChars);
		var line2 = dateStr + "  " + authorStr;
		line2 = Truncate(line2, Math.Max(1, (int)(contentWidth / 6f)));

		Paint.SetPen(textColorSecondary);
		Paint.DrawText(line2Rect, line2, TextFlag.LeftCenter);
	}
}
