#nullable enable
using System;
using System.Threading;
using Editor;
using Sandbox.Diagnostics;
using Sandbox.git;
using Sandbox.git.models;

namespace Sandbox.widgets;

public class CommitWidget : Widget {
	const float RowHeight = 28f;

	private readonly GitStore _store;
	private readonly SynchronizationContext? _uiContext;
	private readonly CommitMessageLineEdit _messageField;
	private readonly CommitOptionsDropdown _optionsDropdown;
	private readonly Button _commitButton;
	private bool _isCommitting;
	private bool _skipCommitHooks;
	private static readonly Logger Logger = new Logger("SandGit[CommitWidget]");

	public CommitWidget(Widget parent, GitStore store) : base(parent) {
		_store = store ?? throw new ArgumentNullException(nameof(store));
		_uiContext = SynchronizationContext.Current;

		Layout = Layout.Column();
		Layout.Spacing = 4f;

		var row = new Widget(this) { Layout = Layout.Row() };
		row.Layout.Spacing = 4f;

		_messageField = new CommitMessageLineEdit(row) {
			MinimumHeight = RowHeight, MinimumWidth = 0, PlaceholderText = "Commit message"
		};
		_messageField.TextChanged += _ => UpdateCommitButtonState();
		_messageField.CommitRequested += OnCommitClicked;

		_optionsDropdown = new CommitOptionsDropdown(row);
		_optionsDropdown.Clicked += OnOptionsClicked;

		_commitButton = new Button(row) { Text = "Commit All" };
		_commitButton.Clicked += OnCommitClicked;
		_store.OnDataChanged += UpdateCommitButtonState;
		UpdateCommitButtonState();

		row.Layout.Add(_messageField, 1);
		row.Layout.Add(_commitButton);
		row.Layout.Add(_optionsDropdown);

		Layout.Add(row);
	}

	protected override void OnClosed() {
		_store.OnDataChanged -= UpdateCommitButtonState;
		base.OnClosed();
	}

	void UpdateCommitButtonState() {
		if ( !IsValid )
			return;

		var hasMessage = !string.IsNullOrWhiteSpace(_messageField.Text);
		var canCommit = CanCommit(out var reasonDisabled);
		_commitButton.Enabled = hasMessage && canCommit && !_isCommitting;
		_commitButton.Text = _isCommitting ? "Committing…" : "Commit All";
		_commitButton.ToolTip = GetCommitButtonToolTip(_isCommitting, hasMessage, canCommit, reasonDisabled);
	}

	static string GetCommitButtonToolTip(bool isCommitting, bool hasMessage, bool canCommit, string? reasonDisabled) {
		if ( isCommitting )
			return "Committing…";
		if ( !hasMessage )
			return "Commit message required";
		if ( !canCommit )
			return reasonDisabled ?? "Commit all staged and unstaged changes";
		return "Commit all staged and unstaged changes";
	}

	bool CanCommit(out string? reasonDisabled) {
		reasonDisabled = null;
		if ( _store.RepositoryType is not RegularRepositoryType ) {
			reasonDisabled = _store.IsLoading ? "Loading…" : "Not a git repository.";
			return false;
		}

		var repo = _store.CurrentRepository;
		if ( repo == null ) {
			reasonDisabled = "No repository.";
			return false;
		}

		var files = _store.FullStatus?.WorkingDirectory.Files;
		var changeCount = files?.Count ?? 0;
		if ( changeCount == 0 ) {
			reasonDisabled = "No changes to commit.";
			return false;
		}

		return true;
	}

	void OnOptionsClicked() {
		var menu = new ContextMenu();
		var label = _skipCommitHooks ? "Bypass Commit Hooks ✓" : "Bypass Commit Hooks";
		menu.AddOption(label, null, () => {
			_skipCommitHooks = !_skipCommitHooks;
		});
		menu.OpenAtCursor();
	}

	async void OnCommitClicked() {
		var message = _messageField.Text?.Trim() ?? "";
		if ( string.IsNullOrWhiteSpace(message) )
			return;
		if ( !CanCommit(out _) )
			return;

		var repo = _store.CurrentRepository!;
		_isCommitting = true;
		UpdateCommitButtonState();

		try {
			// files: null => "Commit All" (stage everything via add -A). Desktop can commit selected files only.
			_ = await Sandbox.git.Commit
				.CreateCommitAsync(repo, message, files: null, amend: false, noVerify: _skipCommitHooks)
				.ConfigureAwait(false);

			_uiContext?.Post(_ => {
				if ( !IsValid ) return;
				_messageField.Text = "";
				_isCommitting = false;
				UpdateCommitButtonState();
				_store.RequestDebouncedRefresh("commit");
			}, null);
		} catch ( Exception ex ) {
			Logger.Warning($"Commit failed: {ex.Message}");
			_uiContext?.Post(_ => {
				if ( !IsValid ) return;
				_isCommitting = false;
				UpdateCommitButtonState();
			}, null);
		}
	}

	// ─── Not implemented yet (Desktop parity) ───────────────────────────────
	// • Repo rules: branch protection, signed commits, commit message patterns; getButtonTooltip() for disabled reasons.
	// • Select files to commit: anyFilesSelected, filesToBeCommittedCount; we currently only support "Commit All".
}

/// <summary>Line edit for the commit message; hosts Cmd/Ctrl+Enter shortcut only when this field is focused.</summary>
sealed class CommitMessageLineEdit : LineEdit {
	public event Action? CommitRequested;

	public CommitMessageLineEdit(Widget parent) : base(parent) { }

	[Shortcut("sandgit.commit-submit", "CTRL+ENTER", typeof(CommitMessageLineEdit), ShortcutType.Widget)]
	private void OnCommitShortcut() {
		CommitRequested?.Invoke();
	}
}

sealed class CommitOptionsDropdown : Button {
	public CommitOptionsDropdown(Widget parent) : base(parent) {
		FixedWidth = 12f;
		MinimumHeight = 24f;
		ToolTip = "Commit options";
	}

	protected override void OnPaint() {
		WidgetPaintUtils.DrawDropdownChevron(new Rect(0, Size));
	}
}
