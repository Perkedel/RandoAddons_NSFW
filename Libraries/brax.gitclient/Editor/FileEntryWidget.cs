using System;
using System.IO;

namespace Editor;

/// <summary>
/// A single row widget representing one changed file in the git status list.
/// Shows a stage/unstage checkbox, a status badge, and the file path.
/// </summary>
public class FileEntryWidget : Widget
{
	/// <summary>Invoked when the staging checkbox is toggled. Argument is the new staged state.</summary>
	public Action<bool> OnStageToggled;

	readonly GitFileEntry _entry;
	readonly Checkbox _checkbox;
	readonly Label _statusBadge;
	readonly Label _pathLabel;

	static readonly Color HoverColor = Color.White.WithAlpha( 0.05f );
	static readonly Color StagedRowTint = new Color( 0.15f, 0.25f, 0.15f, 0.3f );

	public FileEntryWidget( GitFileEntry entry, Widget parent ) : base( parent )
	{
		_entry = entry;

		Layout = Layout.Row();
		Layout.Margin = new Margin( 6, 3, 6, 3 );
		Layout.Spacing = 6;

		// Staging checkbox
		_checkbox = new Checkbox( this );
		_checkbox.Value = entry.IsStaged;
		_checkbox.ToolTip = entry.IsStaged ? "Click to unstage" : "Click to stage";
		_checkbox.StateChanged += OnCheckboxStateChanged;
		Layout.Add( _checkbox );

		// Status badge (M / A / D / R / ?)
		_statusBadge = new Label( entry.StatusLabel, this );
		_statusBadge.FixedWidth = 18;
		_statusBadge.SetStyles( $"font-weight: bold; font-size: 11px; color: {entry.StatusColor.Hex};" );
		_statusBadge.ToolTip = GetStatusTooltip( entry );
		Layout.Add( _statusBadge );

		// File path — show only the filename prominently, full path as tooltip
		var filename = Path.GetFileName( entry.FilePath );
		var directory = Path.GetDirectoryName( entry.FilePath );

		var pathCol = Layout.AddColumn();

		_pathLabel = new Label( filename, this );
		_pathLabel.SetStyles( "font-size: 11px;" );
		pathCol.Add( _pathLabel );

		if ( !string.IsNullOrEmpty( directory ) )
		{
			var dirLabel = new Label( directory, this );
			dirLabel.SetStyles( "font-size: 9px; color: #777;" );
			pathCol.Add( dirLabel );
		}

		pathCol.AddStretchCell( 1 );

		FixedHeight = string.IsNullOrEmpty( directory ) ? 26 : 38;
		ToolTip = entry.FilePath;
		MouseTracking = true;
	}

	void OnCheckboxStateChanged( CheckState state )
	{
		OnStageToggled?.Invoke( state == CheckState.On );
	}

	protected override void OnPaint()
	{
		if ( _entry.IsStaged )
		{
			Paint.ClearPen();
			Paint.SetBrush( StagedRowTint );
			Paint.DrawRect( LocalRect );
		}

		if ( IsUnderMouse )
		{
			Paint.ClearPen();
			Paint.SetBrush( HoverColor );
			Paint.DrawRect( LocalRect );
		}
	}

	static string GetStatusTooltip( GitFileEntry entry )
	{
		var status = entry.IsStaged ? entry.StagedStatus : entry.UnstagedStatus;
		return status switch
		{
			'M' => "Modified",
			'A' => "Added",
			'D' => "Deleted",
			'R' => "Renamed",
			'C' => "Copied",
			'U' => "Unmerged",
			'?' => "Untracked",
			_ => "Changed"
		};
	}
}
