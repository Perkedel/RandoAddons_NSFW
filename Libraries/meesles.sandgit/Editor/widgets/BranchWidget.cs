#nullable enable
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Editor;
using Sandbox.Diagnostics;
using Sandbox.git;
using Sandbox.git.models;

namespace Sandbox.widgets;

public class BranchWidget : Widget {
	const float LabelWidth = 48f;

	private readonly GitStore _store;
	private readonly ComboBox _branchList;
	private readonly SynchronizationContext? _uiContext;
	private bool _syncingDropdown;

	private string? _lastAbortedCheckoutBranch;

	private static readonly Logger Logger = new Logger("SandGit[BranchWidget]");

	public BranchWidget(Widget parent, GitStore store) : base(parent) {
		_store = store ?? throw new ArgumentNullException(nameof(store));
		_uiContext = SynchronizationContext.Current;

		Layout = Layout.Column();
		Layout.Spacing = 6f;

		var topRow = new Widget(this) { Layout = Layout.Row() };
		topRow.Layout.Spacing = 4f;

		var branchLabel = new Label("Branch", topRow) { MinimumWidth = LabelWidth };
		_branchList = new ComboBox(topRow) { MinimumWidth = 0 };

		var dropdownIndicator = new BranchDropdownIndicator(topRow) { Icon = null };
		dropdownIndicator.Clicked += OnDropdownClicked;

		topRow.Layout.Add(branchLabel);
		topRow.Layout.Add(_branchList, 1);
		topRow.Layout.Add(dropdownIndicator);

		Layout.Add(topRow);

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
		if ( _syncingDropdown )
			return;

		_syncingDropdown = true;
		try {
			_branchList.Clear();

			if ( _store.IsLoading || _store.RepositoryType is not RegularRepositoryType ) {
				_branchList.AddItem("—", null, () => { }, null, true);
				return;
			}

			var branches = _store.Branches;
			var fullStatus = _store.FullStatus;
			if ( branches == null ) {
				_branchList.AddItem("—", null, () => { }, null, true);
				return;
			}

			var localBranches = branches.Where(b => b.Type == BranchType.Local).ToList();
			var currentName = fullStatus?.CurrentBranch;
			if ( currentName == _lastAbortedCheckoutBranch )
				_lastAbortedCheckoutBranch = null;
			var ab = fullStatus?.BranchAheadBehind;

			// New repo: HEAD points to default branch (e.g. main) but refs/heads/main doesn't exist yet, so for-each-ref returns nothing. Show current branch from status.
			var hasCurrentInList = currentName != null && localBranches.Any(b => b.Name == currentName);
			if ( currentName != null && !hasCurrentInList ) {
				var displayName = ab != null && (ab.Ahead > 0 || ab.Behind > 0)
					? $"{currentName} ↑{ab.Ahead} ↓{ab.Behind}"
					: currentName;
				_branchList.AddItem(displayName, null, () => OnBranchSelected(currentName), currentName, true);
				_branchList.TrySelectNamed(displayName);
			}

			foreach ( var branch in localBranches ) {
				var name = branch.Name;
				var isCurrent = name == currentName;
				var displayName = isCurrent && ab != null && (ab.Ahead > 0 || ab.Behind > 0)
					? $"{name} ↑{ab.Ahead} ↓{ab.Behind}"
					: name;
				var summary = string.IsNullOrEmpty(branch.Upstream) ? name : $"{name} → {branch.Upstream}";
				_branchList.AddItem(displayName, null, () => OnBranchSelected(name), summary, isCurrent);
				if ( isCurrent )
					_branchList.TrySelectNamed(displayName);
			}
		} finally {
			_syncingDropdown = false;
		}
	}

	void OnBranchSelected(string branchName) {
		if ( _syncingDropdown )
			return;

		var currentBranch = _store.FullStatus?.CurrentBranch;
		if ( _lastAbortedCheckoutBranch == branchName && currentBranch != branchName ) {
			return;
		}

		_lastAbortedCheckoutBranch = null;
		_ = CheckoutBranchAsync(branchName);
	}

	async Task CheckoutBranchAsync(string branchName) {
		var repo = _store.CurrentRepository;
		if ( repo == null ) {
			Logger.Trace("Checkout skipped: no repository");
			SyncDropdownToStoreOnUiThread();
			return;
		}

		var branches = _store.Branches;
		if ( branches == null ) {
			Logger.Trace("Checkout skipped: branches not loaded");
			SyncDropdownToStoreOnUiThread();
			return;
		}

		var fullStatus = _store.FullStatus;
		if ( fullStatus?.CurrentBranch == branchName ) {
			return;
		}

		var hasUncommittedChanges = fullStatus != null && fullStatus.WorkingDirectory.Files.Count > 0;
		if ( hasUncommittedChanges ) {
			var fileCount = fullStatus!.WorkingDirectory.Files.Count;
			Logger.Warning("Cannot change branch. " + fileCount + " file(s) have uncommitted changes");
			_lastAbortedCheckoutBranch = branchName;
			SyncDropdownToStoreOnUiThread();
			return;
		}

		var branch = branches.FirstOrDefault(b => b.Type == BranchType.Local && b.Name == branchName);
		if ( branch == null ) {
			Logger.Trace("Checkout skipped: branch not found: " + branchName);
			SyncDropdownToStoreOnUiThread();
			return;
		}

		try {
			await Checkout.CheckoutBranchAsync(repo, branch).ConfigureAwait(false);
			_store.RequestDebouncedRefresh("branch checkout");
		} catch ( Exception ex ) {
			Logger.Warning("Checkout failed: " + ex.Message);
			SyncDropdownToStoreOnUiThread();
		}
	}

	void SyncDropdownToStore() {
		UpdateFromStore();
	}

	void SyncDropdownToStoreOnUiThread() {
		if ( _uiContext != null )
			_uiContext.Post(_ => SyncDropdownToStore(), null);
		else
			SyncDropdownToStore();
	}

	void OnDropdownClicked() {
		var menu = new ContextMenu(null);
		menu.AddOption("Create new branch", "add", OnCreateBranchClicked);
		menu.OpenAtCursor();
	}

	void OnCreateBranchClicked() {
		var repo = _store.CurrentRepository;
		if ( repo == null ) {
			Logger.Error("Create branch skipped: no repository");
			return;
		}

		if ( _store.RepositoryType is not RegularRepositoryType ) {
			Logger.Trace("Create branch skipped: not a regular repository");
			return;
		}

		Dialog.AskString(OnCreateBranchNameEntered, "Enter the name for the new branch:", "Create",
			title: "Create Branch", minLength: 1);
	}

	void OnCreateBranchNameEntered(string branchName) {
		var name = branchName.Trim();
		if ( string.IsNullOrEmpty(name) )
			return;
		_ = CreateBranchAsync(name);
	}

	async Task CreateBranchAsync(string branchName) {
		var repo = _store.CurrentRepository;
		if ( repo == null ) {
			Logger.Error("Create branch failed: no repository");
			return;
		}

		try {
			await git.Branch.CreateBranchAsync(repo, branchName, startPoint: null).ConfigureAwait(false);
			var newBranch = new git.models.Branch(
				branchName,
				upstream: "",
				new BranchTip(""),
				BranchType.Local,
				git.Branch.FormatAsLocalRef(branchName));
			await Checkout.CheckoutBranchAsync(repo, newBranch).ConfigureAwait(false);
			_store.RequestDebouncedRefresh("create branch");
		} catch ( Exception ex ) {
			Logger.Error("Create branch failed: " + ex.Message);
		}
	}
}

sealed class BranchDropdownIndicator : Button {
	public BranchDropdownIndicator(Widget parent) : base(parent) {
		FixedWidth = 12f;
		MinimumHeight = 24f;
		ToolTip = "Branch options";
	}

	protected override void OnPaint() {
		WidgetPaintUtils.DrawDropdownChevron(new Rect(0, Size));
	}
}
