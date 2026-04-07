using System;
using System.IO;
using System.Linq;
using System.Threading;

namespace Editor;

/// <summary>
/// Embedded Git client editor panel. Provides commit, file staging, push, and branch switching.
/// Dock it via View > Git Client, or open it via the Editor menu.
/// </summary>
[Dock( "Editor", "Git Client", "merge" )]
public class GitClientWidget : Widget
{
	// UI controls
	ComboBox _branchCombo;
	ScrollArea _scrollArea;
	Widget _fileListContainer;
	TextEdit _commitMessageEdit;
	Label _statusLabel;

	// State
	string _repoPath;
	bool _suppressBranchChange;

	// File watching
	FileSystemWatcher _fsWatcher;
	Timer _debounceTimer;
	SynchronizationContext _syncContext;

	public GitClientWidget( Widget parent ) : base( parent )
	{
		_syncContext = SynchronizationContext.Current;
		_repoPath = ResolveRepoPath();

		BuildUI();
		Refresh();
		StartWatching();
	}

	// ──────────────────────────────────────────────────────────────────────────
	// File watching
	// ──────────────────────────────────────────────────────────────────────────

	void StartWatching()
	{
		if ( string.IsNullOrEmpty( _repoPath ) || !Directory.Exists( _repoPath ) )
			return;

		_debounceTimer = new Timer( OnDebounceElapsed, null, Timeout.Infinite, Timeout.Infinite );

		try
		{
			_fsWatcher = new FileSystemWatcher( _repoPath )
			{
				NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName,
				IncludeSubdirectories = true,
				EnableRaisingEvents = true,
			};

			_fsWatcher.Changed += OnFileSystemChanged;
			_fsWatcher.Created += OnFileSystemChanged;
			_fsWatcher.Deleted += OnFileSystemChanged;
			_fsWatcher.Renamed += OnFileSystemRenamed;
		}
		catch
		{
			// If watching fails (e.g. path not found), just continue without auto-refresh
		}
	}

	void OnFileSystemChanged( object sender, FileSystemEventArgs e )
	{
		// Ignore git's internal object store and logs — they produce massive noise
		// during any git operation (add, commit, fetch, etc.)
		var p = e.FullPath;
		if ( IsGitInternalPath( p ) ) return;

		ScheduleRefresh();
	}

	void OnFileSystemRenamed( object sender, RenamedEventArgs e )
	{
		if ( IsGitInternalPath( e.FullPath ) && IsGitInternalPath( e.OldFullPath ) ) return;

		ScheduleRefresh();
	}

	static bool IsGitInternalPath( string fullPath )
	{
		// Normalise to forward slashes for consistent matching
		var p = fullPath.Replace( '\\', '/' );
		return p.Contains( "/.git/objects/" )
			|| p.Contains( "/.git/logs/" )
			|| p.Contains( "/.git/rr-cache/" );
	}

	void ScheduleRefresh()
	{
		// Restart the debounce window; the refresh fires 1 s after the last change
		_debounceTimer?.Change( 1000, Timeout.Infinite );
	}

	void OnDebounceElapsed( object state )
	{
		// This callback runs on a thread-pool thread — post back to the UI thread
		_syncContext?.Post( _ =>
		{
			if ( IsValid )
				RefreshFileList();
		}, null );
	}

	public override void OnDestroyed()
	{
		_debounceTimer?.Dispose();
		_debounceTimer = null;

		_fsWatcher?.Dispose();
		_fsWatcher = null;

		base.OnDestroyed();
	}

	// ──────────────────────────────────────────────────────────────────────────
	// Repo path resolution
	// ──────────────────────────────────────────────────────────────────────────

	string ResolveRepoPath()
	{
		// Prefer the S&box project directory — git will find the repo root itself
		try
		{
			var dir = Project.Current?.RootDirectory?.FullName;
			if ( !string.IsNullOrEmpty( dir ) && Directory.Exists( dir ) )
				return dir;
		}
		catch { }

		try
		{
			var path = Project.Current?.GetRootPath();
			if ( !string.IsNullOrEmpty( path ) && Directory.Exists( path ) )
				return path;
		}
		catch { }

		// Last resort: walk up from cwd (unlikely to be right in editor context)
		return GitOperations.FindRepoRoot( Directory.GetCurrentDirectory() )
			?? Directory.GetCurrentDirectory();
	}

	// ──────────────────────────────────────────────────────────────────────────
	// UI construction
	// ──────────────────────────────────────────────────────────────────────────

	/// <summary>Creates a 1px horizontal divider line for use between vertical sections.</summary>
	static void AddHRule( Layout layout )
	{
		var sep = new Separator( 1 ) { Color = Color.Parse( "#2a2a2a" ) ?? Color.Black };
		layout.Add( sep );
	}

	void BuildUI()
	{
		Layout = Layout.Column();

		BuildToolbar();
		AddHRule( Layout );

		// Main splitter: file panel left, commit panel right
		var splitter = new Splitter( this );
		splitter.IsHorizontal = true;
		Layout.Add( splitter, 1 );

		splitter.AddWidget( BuildFilePanel( this ) );
		splitter.AddWidget( BuildCommitPanel( this ) );

		splitter.SetStretch( 0, 6 );
		splitter.SetStretch( 1, 4 );

		AddHRule( Layout );
		Layout.Add( BuildStatusBar() );
	}

	void BuildToolbar()
	{
		var toolbar = new Widget( this );
		toolbar.Layout = Layout.Row();
		toolbar.Layout.Margin = new Margin( 8, 6, 8, 6 );
		toolbar.Layout.Spacing = 6;

		// Branch label + combo
		var branchLabel = new Label( "Branch:", toolbar );
		branchLabel.SetStyles( "color: #999; font-size: 11px;" );
		toolbar.Layout.Add( branchLabel );

		_branchCombo = new ComboBox( toolbar );
		_branchCombo.MinimumWidth = 160;
		_branchCombo.MaximumWidth = 280;
		_branchCombo.ToolTip = "Switch branch (checkout)";
		_branchCombo.ItemChanged += OnBranchComboChanged;
		toolbar.Layout.Add( _branchCombo );

		var newBranchBtn = new Button( "", "add", toolbar );
		newBranchBtn.ToolTip = "Create new branch";
		newBranchBtn.FixedWidth = 28;
		newBranchBtn.Clicked = OnNewBranchClicked;
		toolbar.Layout.Add( newBranchBtn );

		// Thin vertical divider between branch section and remote section
		toolbar.Layout.AddSpacingCell( 4 );
		var divider = new Widget( toolbar ) { FixedWidth = 1 };
		divider.SetStyles( "background-color: #3a3a3a;" );
		toolbar.Layout.Add( divider );
		toolbar.Layout.AddSpacingCell( 4 );

		// Remote operations
		var fetchBtn = new Button( "Fetch", "cloud_download", toolbar );
		fetchBtn.ToolTip = "Fetch from remote";
		fetchBtn.Clicked = OnFetchClicked;
		toolbar.Layout.Add( fetchBtn );

		var pullBtn = new Button( "Pull", "download", toolbar );
		pullBtn.ToolTip = "Pull from remote";
		pullBtn.Clicked = OnPullClicked;
		toolbar.Layout.Add( pullBtn );

		toolbar.Layout.AddStretchCell( 1 );

		var refreshBtn = new Button( "Refresh", "refresh", toolbar );
		refreshBtn.ToolTip = "Refresh file status and branches (F5)";
		refreshBtn.Clicked = Refresh;
		toolbar.Layout.Add( refreshBtn );

		Layout.Add( toolbar );
	}

	Widget BuildFilePanel( Widget parent )
	{
		var panel = new Widget( parent );
		panel.Layout = Layout.Column();
		panel.Layout.Margin = new Margin( 4, 4, 2, 4 );
		panel.Layout.Spacing = 4;
		panel.MinimumWidth = 260;

		// Header row: title + stage-all / unstage-all buttons
		var headerRow = panel.Layout.AddRow();
		headerRow.Margin = new Margin( 4, 0, 4, 2 );

		var filesLabel = new Label( "Changed Files", panel );
		filesLabel.SetStyles( "font-size: 12px; font-weight: bold;" );
		headerRow.Add( filesLabel );
		headerRow.AddStretchCell( 1 );

		var stageAllBtn = new Button( "", "check_box", panel );
		stageAllBtn.ToolTip = "Stage all changes";
		stageAllBtn.FixedWidth = 28;
		stageAllBtn.Clicked = OnStageAllClicked;
		headerRow.Add( stageAllBtn );

		var unstageAllBtn = new Button( "", "check_box_outline_blank", panel );
		unstageAllBtn.ToolTip = "Unstage all changes";
		unstageAllBtn.FixedWidth = 28;
		unstageAllBtn.Clicked = OnUnstageAllClicked;
		headerRow.Add( unstageAllBtn );

		// Scrollable file list
		_scrollArea = new ScrollArea( panel );
		_scrollArea.HorizontalScrollbarMode = ScrollbarMode.Off;
		_fileListContainer = CreateFreshCanvas();

		panel.Layout.Add( _scrollArea, 1 );

		return panel;
	}

	Widget BuildCommitPanel( Widget parent )
	{
		var panel = new Widget( parent );
		panel.Layout = Layout.Column();
		panel.Layout.Margin = new Margin( 2, 4, 4, 4 );
		panel.Layout.Spacing = 6;
		panel.MinimumWidth = 220;

		var commitLabel = new Label( "Commit Message", panel );
		commitLabel.SetStyles( "font-size: 12px; font-weight: bold;" );
		panel.Layout.Add( commitLabel );

		_commitMessageEdit = new TextEdit( panel );
		_commitMessageEdit.PlaceholderText = "Summary (required)\n\nDescription (optional)";
		panel.Layout.Add( _commitMessageEdit, 1 );

		panel.Layout.AddSpacingCell( 4 );

		var btnRow = panel.Layout.AddRow();
		btnRow.Spacing = 4;

		var commitBtn = new Button( "Commit", "commit", panel );
		commitBtn.ToolTip = "Commit staged changes";
		commitBtn.Clicked = OnCommitClicked;
		btnRow.Add( commitBtn, 1 );

		var commitPushBtn = new Button( "Commit & Push", "upload", panel );
		commitPushBtn.ToolTip = "Commit staged changes then push to remote";
		commitPushBtn.Clicked = OnCommitAndPushClicked;
		btnRow.Add( commitPushBtn, 1 );

		return panel;
	}

	Widget BuildStatusBar()
	{
		var row = new Widget( this );
		row.Layout = Layout.Row();
		row.Layout.Margin = new Margin( 8, 4, 8, 4 );

		_statusLabel = new Label( "Ready", row );
		_statusLabel.SetStyles( "font-size: 11px; color: #aaa;" );
		row.Layout.Add( _statusLabel, 1 );

		// Show trimmed repo path on the right
		var display = _repoPath.Length > 60 ? "..." + _repoPath[^57..] : _repoPath;
		var repoLabel = new Label( display, row );
		repoLabel.SetStyles( "font-size: 10px; color: #555;" );
		repoLabel.ToolTip = _repoPath;
		row.Layout.Add( repoLabel );

		return row;
	}

	// ──────────────────────────────────────────────────────────────────────────
	// Data refresh
	// ──────────────────────────────────────────────────────────────────────────

	void Refresh()
	{
		SetStatus( "Refreshing..." );
		RefreshBranches();
		RefreshFileList();
		SetStatus( "Ready" );
	}

	void RefreshBranches()
	{
		var (branches, current) = GitOperations.GetBranches( _repoPath );

		_suppressBranchChange = true;
		_branchCombo.Clear();

		if ( branches.Count == 0 )
		{
			_branchCombo.AddItem( "(no branches)" );
		}
		else
		{
			foreach ( var b in branches )
				_branchCombo.AddItem( b, "merge" );

			if ( !string.IsNullOrEmpty( current ) )
				_branchCombo.TrySelectNamed( current );
		}

		_suppressBranchChange = false;
	}

	void RefreshFileList()
	{
		// Recreate the scroll canvas so no layout spacers accumulate across refreshes
		_fileListContainer = CreateFreshCanvas();

		var (files, gitError) = GitOperations.GetStatus( _repoPath );

		if ( gitError != null )
		{
			SetStatus( $"git: {gitError}" );
			AddListPlaceholder( $"git error — {gitError}" );
			return;
		}

		var staged = files.Where( f => f.IsStaged ).ToList();
		var unstaged = files.Where( f => !f.IsStaged ).ToList();

		if ( staged.Count == 0 && unstaged.Count == 0 )
		{
			AddListPlaceholder( "No changes — working tree is clean" );
			return;
		}

		if ( staged.Count > 0 )
		{
			AddSectionHeader( $"Staged Changes  ({staged.Count})", Color.Parse( "#98c379" ) ?? Color.Green );
			foreach ( var entry in staged )
				AddFileEntry( entry );
		}

		if ( unstaged.Count > 0 )
		{
			if ( staged.Count > 0 )
			{
				var spacer = new Widget( _fileListContainer ) { FixedHeight = 8 };
				_fileListContainer.Layout.Add( spacer );
			}

			AddSectionHeader( $"Changes  ({unstaged.Count})", Color.Parse( "#e5c07b" ) ?? Color.Yellow );
			foreach ( var entry in unstaged )
				AddFileEntry( entry );
		}

		// Stretch widget to push entries to the top
		var stretch = new Widget( _fileListContainer );
		_fileListContainer.Layout.Add( stretch, 1 );
	}

	// ──────────────────────────────────────────────────────────────────────────
	// File list helpers
	// ──────────────────────────────────────────────────────────────────────────

	/// <summary>
	/// Destroys the existing file list canvas and replaces it with a fresh empty one.
	/// This ensures layout spacer/stretch items from the previous render don't accumulate.
	/// </summary>
	Widget CreateFreshCanvas()
	{
		_fileListContainer?.Destroy();
		var canvas = new Widget( _scrollArea );
		canvas.Layout = Layout.Column();
		canvas.Layout.Spacing = 0;
		_scrollArea.Canvas = canvas;
		return canvas;
	}

	void AddSectionHeader( string title, Color color )
	{
		var header = new Widget( _fileListContainer );
		header.Layout = Layout.Row();
		header.Layout.Margin = new Margin( 8, 6, 6, 2 );

		var lbl = new Label( title, header );
		lbl.SetStyles( $"font-size: 10px; font-weight: bold; color: {color.Hex};" );
		header.Layout.Add( lbl );
		header.Layout.AddStretchCell( 1 );

		_fileListContainer.Layout.Add( header );
	}

	void AddFileEntry( GitFileEntry entry )
	{
		var widget = new FileEntryWidget( entry, _fileListContainer );
		widget.OnStageToggled = staged => OnFileStageToggled( entry, staged );
		_fileListContainer.Layout.Add( widget );
	}

	void AddListPlaceholder( string message )
	{
		var lbl = new Label( message, _fileListContainer );
		lbl.SetStyles( "padding: 16px; color: #555; font-style: italic;" );
		_fileListContainer.Layout.Add( lbl );
		_fileListContainer.Layout.AddStretchCell( 1 );
	}

	// ──────────────────────────────────────────────────────────────────────────
	// Status
	// ──────────────────────────────────────────────────────────────────────────

	void SetStatus( string message )
	{
		if ( _statusLabel.IsValid() )
			_statusLabel.Text = message;
	}

	// ──────────────────────────────────────────────────────────────────────────
	// Event handlers
	// ──────────────────────────────────────────────────────────────────────────

	void OnFileStageToggled( GitFileEntry entry, bool stage )
	{
		if ( stage )
			GitOperations.Stage( _repoPath, entry.FilePath );
		else
			GitOperations.Unstage( _repoPath, entry.FilePath );

		RefreshFileList();
	}

	void OnStageAllClicked()
	{
		GitOperations.StageAll( _repoPath );
		RefreshFileList();
		SetStatus( "All changes staged" );
	}

	void OnUnstageAllClicked()
	{
		GitOperations.UnstageAll( _repoPath );
		RefreshFileList();
		SetStatus( "All changes unstaged" );
	}

	void OnBranchComboChanged()
	{
		if ( _suppressBranchChange ) return;

		var target = _branchCombo.CurrentText;
		if ( string.IsNullOrEmpty( target ) || target.StartsWith( '(' ) ) return;

		SetStatus( $"Switching to '{target}'..." );
		var success = GitOperations.SwitchBranch( _repoPath, target );
		SetStatus( success ? $"Switched to '{target}'" : $"Could not switch to '{target}'" );

		if ( success )
			RefreshFileList();
		else
			RefreshBranches(); // Revert combo to real current branch
	}

	void OnNewBranchClicked()
	{
		Dialog.AskString(
			OnSuccess: name =>
			{
				name = name.Trim();
				if ( string.IsNullOrEmpty( name ) ) return;

				SetStatus( $"Creating branch '{name}'..." );
				var (success, error) = GitOperations.CreateBranch( _repoPath, name );

				if ( success )
				{
					SetStatus( $"Created and switched to '{name}'" );
					Refresh();
				}
				else
				{
					SetStatus( $"Failed: {error}" );
				}
			},
			question: "Enter a name for the new branch:",
			title: "New Branch",
			okay: "Create"
		);
	}

	void OnFetchClicked()
	{
		SetStatus( "Fetching from remote..." );
		var success = GitOperations.Fetch( _repoPath );
		SetStatus( success ? "Fetch complete" : "Fetch failed — check remote connection" );
	}

	void OnPullClicked()
	{
		SetStatus( "Pulling..." );
		var (success, output) = GitOperations.Pull( _repoPath );
		SetStatus( success ? "Pull complete" : $"Pull failed: {output}" );

		if ( success )
			RefreshFileList();
	}

	void OnCommitClicked()
	{
		var message = _commitMessageEdit?.PlainText?.Trim();
		if ( string.IsNullOrWhiteSpace( message ) )
		{
			SetStatus( "Please enter a commit message" );
			return;
		}

		var (files, _) = GitOperations.GetStatus( _repoPath );
		if ( !files.Any( f => f.IsStaged ) )
		{
			SetStatus( "Nothing staged — tick files to stage them first" );
			return;
		}

		SetStatus( "Committing..." );
		var (success, output) = GitOperations.Commit( _repoPath, message );

		if ( success )
		{
			_commitMessageEdit.PlainText = string.Empty;
			SetStatus( "Commit successful" );
			RefreshFileList();
		}
		else
		{
			SetStatus( "Commit failed — see dialog" );
			EditorUtility.DisplayDialog( "Commit Failed", string.IsNullOrWhiteSpace( output )
				? "git commit returned a non-zero exit code but produced no output."
				: output );
		}
	}

	void OnCommitAndPushClicked()
	{
		var message = _commitMessageEdit?.PlainText?.Trim();
		if ( string.IsNullOrWhiteSpace( message ) )
		{
			SetStatus( "Please enter a commit message" );
			return;
		}

		var (files, _) = GitOperations.GetStatus( _repoPath );
		if ( !files.Any( f => f.IsStaged ) )
		{
			SetStatus( "Nothing staged — tick files to stage them first" );
			return;
		}

		SetStatus( "Committing..." );
		var (commitOk, commitOut) = GitOperations.Commit( _repoPath, message );

		if ( !commitOk )
		{
			SetStatus( "Commit failed — see dialog" );
			EditorUtility.DisplayDialog( "Commit Failed", string.IsNullOrWhiteSpace( commitOut )
				? "git commit returned a non-zero exit code but produced no output."
				: commitOut );
			return;
		}

		_commitMessageEdit.PlainText = string.Empty;
		RefreshFileList();

		SetStatus( "Pushing..." );
		var (pushOk, pushOut) = GitOperations.Push( _repoPath );
		SetStatus( pushOk ? "Committed and pushed successfully" : $"Commit OK — Push failed: {pushOut}" );
	}

	protected override void OnKeyPress( KeyEvent e )
	{
		if ( e.Key == KeyCode.F5 )
		{
			Refresh();
			e.Accepted = true;
			return;
		}

		base.OnKeyPress( e );
	}
}
