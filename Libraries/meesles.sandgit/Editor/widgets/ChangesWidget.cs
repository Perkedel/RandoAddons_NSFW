#nullable enable
using System;
using Editor;
using Sandbox.Diagnostics;
using Sandbox.git;
using Sandbox.git.models;

namespace Sandbox.widgets;

public class ChangesWidget : Widget {
	const float MinContentHeight = 120f;
	const string? NoRepoLabel = null;

	private readonly GitStore _store;
	private readonly ScrollArea _scroller;
	private readonly Label _statusLabel;
	private static readonly Logger Logger = new Logger("SandGit[ChangesWidget]");

	public ChangesWidget(Widget parent, GitStore store) : base(parent) {
		_store = store ?? throw new ArgumentNullException(nameof(store));

		Layout = Layout.Column();
		Layout.Spacing = 4f;

		var topRow = new Widget(this) { Layout = Layout.Row() };
		topRow.Layout.Spacing = 2f;
		_statusLabel = new Label("", topRow) { MinimumWidth = 0 };
		topRow.Layout.Add(_statusLabel);
		topRow.Layout.AddStretchCell();

		Layout.Add(topRow);

		_scroller = new ScrollArea(this);
		_scroller.MinimumHeight = MinContentHeight;
		Layout.Add(_scroller, 1); // stretch to fill remaining area

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

	void UpdateFromStore() {
		if ( !IsValid )
			return;
		if ( _store.IsLoading || _store.RepositoryType is not RegularRepositoryType ) {
			_statusLabel.Text = _store.IsLoading ? "Loading…" : NoRepoLabel;
			RebuildChangeRows(null);
			return;
		}

		var fullStatus = _store.FullStatus;
		if ( fullStatus == null ) {
			_statusLabel.Text = NoRepoLabel;
			RebuildChangeRows(null);
			return;
		}

		_statusLabel.Text = FormatStatusLine(fullStatus);
		RebuildChangeRows(fullStatus);
	}

	void RebuildChangeRows(FullStatusResult? fullStatus) {
		var canvas = new Widget(_scroller);
		canvas.Layout = Layout.Column();

		if ( fullStatus != null ) {
			var files = fullStatus.WorkingDirectory.Files;
			for ( var i = 0; i < files.Count; i++ ) {
				var f = files[i];
				var pathDisplay = f.OldPath != null ? f.OldPath + " → " + f.Path : f.Path;
				var statusStr = f.Kind.ToString().ToLowerInvariant();
				var row = new ChangeRow(pathDisplay, statusStr) { Index = i };
				canvas.Layout.Add(row);
			}
		}

		canvas.Layout.AddStretchCell();
		_scroller.Canvas = canvas;
	}

	static string FormatStatusLine(FullStatusResult fullStatus) {
		var files = fullStatus.WorkingDirectory.Files;
		return files.Count == 0 ? "No pending changes." : "Changes: (" + files.Count + ")";
	}
}

public class ChangeRow : Frame {
	readonly string _path;
	readonly string _status;
	public int Index { get; set; }

	public ChangeRow(string path, string status) : base(null) {
		_path = path ?? "";
		_status = status ?? "";
		MinimumSize = 24;
		Cursor = CursorShape.Finger;
	}

	protected override void OnPaint() {
		var r = new Rect(0, Size);
		var c = _status switch {
			"modified" => Theme.Yellow,
			"added" => Theme.Green,
			"deleted" => Theme.Red,
			_ => Theme.Blue,
		};
		if ( IsUnderMouse )
			c = c.Lighten(0.3f);

		Paint.ClearPen();
		Paint.SetBrush(c.Darken(0.8f + (Index & 1) * 0.02f).Desaturate(0.3f));
		Paint.DrawRect(r);

		r = r.Grow(-8f, 0);
		Paint.SetPen(c);
		var textSize = Paint.DrawText(r, _path, TextFlag.LeftCenter);
		r.Top = textSize.Bottom - 4;
		r = r.Shrink(0, 0, 0, 8);
		Paint.SetPen(c.Darken(0.3f));
		Paint.DrawText(r, "[" + _status + "]", TextFlag.LeftBottom);
	}
}
