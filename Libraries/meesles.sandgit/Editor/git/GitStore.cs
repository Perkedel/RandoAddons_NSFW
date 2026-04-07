#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Editor;
using Sandbox.Diagnostics;
using Sandbox.git.models;

namespace Sandbox.git;

using GitBranch = models.Branch;

/// <summary>
/// Centralized git data for the current project. Owns one coordinated refresh and notifies widgets via OnDataChanged.
/// Subscribes to editor events and debounces refresh requests.
/// History loading mirrors Desktop's loadCommitBatch (getCommits + storeCommits).
/// </summary>
public class GitStore {
	const int RecentBranchesLimit = 10;
	const int DebounceDelayMs = 400;

	/// <summary>Number of commits to load per history batch (mirrors Desktop CommitBatchSize).</summary>
	public const int CommitBatchSize = 100;

	readonly string _rootPath;
	readonly SynchronizationContext _uiContext;
	readonly object _dataLock = new();
	CancellationTokenSource? _debounceCts;
	readonly object _refreshLock = new();
	bool _refreshInProgress;
	bool _refreshPending;
	private static readonly Logger Logger = new Logger("GitStore");

	RepositoryType? _repositoryType;
	FullStatusResult? _fullStatus;
	IReadOnlyList<GitBranch>? _branches;
	IReadOnlyList<string>? _recentBranchNames;
	Repository? _currentRepository;

	readonly Dictionary<string, models.Commit> _commitLookup = new();
	readonly List<string> _history = new();
	readonly object _historyLock = new();
	bool _historyLoadInProgress;
	bool _historyLoadPending;

	public GitStore(string rootPath, SynchronizationContext? uiContext = null) {
		_rootPath = rootPath ?? string.Empty;
		_uiContext = uiContext ?? SynchronizationContext.Current!;
	}

	public string RootPath => _rootPath;

	public bool IsLoading { get; private set; }

	public RepositoryType? RepositoryType {
		get {
			lock ( _dataLock ) return _repositoryType;
		}
		private set {
			lock ( _dataLock ) _repositoryType = value;
		}
	}

	public FullStatusResult? FullStatus {
		get {
			lock ( _dataLock ) return _fullStatus;
		}
		private set {
			lock ( _dataLock ) _fullStatus = value;
		}
	}

	public IReadOnlyList<GitBranch>? Branches {
		get {
			lock ( _dataLock ) return _branches;
		}
		private set {
			lock ( _dataLock ) _branches = value;
		}
	}

	public IReadOnlyList<string>? RecentBranchNames {
		get {
			lock ( _dataLock ) return _recentBranchNames;
		}
		private set {
			lock ( _dataLock ) _recentBranchNames = value;
		}
	}

	/// <summary>Current repository when RepositoryType is a regular repo; null otherwise.</summary>
	public Repository? CurrentRepository {
		get {
			lock ( _dataLock ) return _currentRepository;
		}
		private set {
			lock ( _dataLock ) _currentRepository = value;
		}
	}

	/// <summary>Commits keyed by SHA (populated when history is loaded).</summary>
	public IReadOnlyDictionary<string, models.Commit> CommitLookup {
		get {
			lock ( _historyLock ) {
				return new Dictionary<string, models.Commit>(_commitLookup);
			}
		}
	}

	/// <summary>Ordered list of commit SHAs (HEAD first). Empty until history is loaded.</summary>
	public IReadOnlyList<string> History {
		get {
			lock ( _historyLock ) {
				return _history.Count == 0 ? Array.Empty<string>() : _history.ToList();
			}
		}
	}

	/// <summary>True while a history batch load is in progress.</summary>
	public bool IsLoadingHistory {
		get {
			lock ( _historyLock ) return _historyLoadInProgress;
		}
	}

	/// <summary>Raised on the UI thread after cached data is updated.</summary>
	public event Action? OnDataChanged;

	/// <summary>
	/// Load a batch of commits from the repository (mirrors Desktop loadCommitBatch).
	/// Uses HEAD or the given commitish, with optional skip for pagination.
	/// Stores commits in CommitLookup and appends/prepends SHAs to History; raises OnDataChanged on the UI thread.
	/// </summary>
	/// <param name="commitish">Starting point (e.g. HEAD). If null, uses HEAD.</param>
	/// <param name="skip">Number of commits to skip for pagination. 0 = first batch (replaces history).</param>
	/// <returns>SHAs of the commits loaded, or null if load was skipped or failed.</returns>
	public async Task<IReadOnlyList<string>?> LoadCommitBatchAsync(string? commitish = null, int skip = 0) {
		var repo = CurrentRepository;
		if ( repo == null )
			return null;

		lock ( _historyLock ) {
			if ( _historyLoadInProgress ) {
				_historyLoadPending = true;
				return null;
			}

			_historyLoadInProgress = true;
		}

		try {
			var commits = await Log.GetCommitsAsync(repo, commitish ?? "HEAD", CommitBatchSize, skip)
				.ConfigureAwait(false);

			if ( commits.Count == 0 ) {
				_uiContext.Post(_ => {
					lock ( _historyLock ) {
						_historyLoadInProgress = false;
						if ( _historyLoadPending ) {
							_historyLoadPending = false;
							_ = LoadCommitBatchAsync(commitish, skip);
						}
					}

					OnDataChanged?.Invoke();
				}, null);
				return Array.Empty<string>();
			}

			var newShas = commits.Select(c => c.Sha).ToList();

			_uiContext.Post(_ => {
				lock ( _historyLock ) {
					foreach ( var c in commits )
						_commitLookup[c.Sha] = c;

					if ( skip == 0 )
						_history.Clear();
					_history.AddRange(newShas);

					_historyLoadInProgress = false;
					if ( _historyLoadPending ) {
						_historyLoadPending = false;
						_ = LoadCommitBatchAsync(commitish, _history.Count);
					}
				}

				OnDataChanged?.Invoke();
			}, null);

			return newShas;
		} catch ( Exception ex ) {
			Logger.Trace($"[GitStore] LoadCommitBatchAsync failed: {ex.Message}");
			_uiContext.Post(_ => {
				lock ( _historyLock ) {
					_historyLoadInProgress = false;
					_historyLoadPending = false;
				}

				OnDataChanged?.Invoke();
			}, null);
			return null;
		}
	}

	/// <summary>Clear cached history and commit lookup (e.g. when repo changes).</summary>
	public void ClearHistory() {
		lock ( _historyLock ) {
			_commitLookup.Clear();
			_history.Clear();
		}

		_uiContext.Post(_ => OnDataChanged?.Invoke(), null);
	}

	public void RequestDebouncedRefresh(string? triggeredByEvent = null) {
		Logger.Trace(
			$"[GitStore] RequestDebouncedRefresh triggered by event: {triggeredByEvent ?? "(direct call)"}");
		_debounceCts?.Cancel();
		_debounceCts = new CancellationTokenSource();
		var cts = _debounceCts;
		var eventName = triggeredByEvent;
		_ = Task.Delay(DebounceDelayMs, cts.Token).ContinueWith(t => {
			if ( t.IsCanceled || cts.IsCancellationRequested )
				return;
			ScheduleRefreshAfterDebounce(eventName);
		}, TaskScheduler.Default);
	}

	public async Task RefreshAsync() {
		Logger.Trace("[GitStore] RefreshAsync started");

		try {
			var repoType = await RevParse.GetRepositoryTypeAsync(_rootPath).ConfigureAwait(false);

			if ( repoType is not RegularRepositoryType regular ) {
				ClearHistory();
				PostUpdateData(repoType, null, null, null, null);
				return;
			}

			var repoPath = regular.TopLevelWorkingDirectory;
			var repository = new Repository(repoPath, 0, null, false);

			var fullStatusTask = Status.GetFullStatusAsync(repoPath);
			var branchesTask = ForEachRef.GetBranchesAsync(repository);
			var recentTask = Reflog.GetRecentBranchesAsync(repository, RecentBranchesLimit + 1);

			await Task.WhenAll(fullStatusTask, branchesTask, recentTask).ConfigureAwait(false);

			var fullStatus = await fullStatusTask.ConfigureAwait(false);
			var branches = await branchesTask.ConfigureAwait(false);
			var recentRaw = await recentTask.ConfigureAwait(false);
			var recent = recentRaw?.Count > 0
				? new List<string>(recentRaw).GetRange(0, Math.Min(RecentBranchesLimit, recentRaw.Count))
				: (IReadOnlyList<string>?)new List<string>();

			PostUpdateData(repoType, fullStatus, branches, recent, repository);
		} catch ( Exception ex ) {
			Logger.Trace($"[GitStore] RefreshAsync failed: {ex.Message}");
			ClearHistory();
			PostUpdateData(RepositoryType, null, null, null, null);
		} finally {
			Logger.Trace("[GitStore] RefreshAsync completed");
		}
	}

	void ScheduleRefreshAfterDebounce(string? triggeredByEvent) {
		lock ( _refreshLock ) {
			if ( _refreshInProgress ) {
				_refreshPending = true;
				return;
			}

			_refreshInProgress = true;
		}

		_ = RunRefreshLoopAsync(triggeredByEvent);
	}

	async Task RunRefreshLoopAsync(string? triggeredByEvent) {
		string? eventNameForLog = triggeredByEvent;

		while ( true ) {
			Logger.Trace(
				$"[GitStore] Debounce elapsed, running RefreshAsync (was triggered by: {eventNameForLog ?? "(direct)"})");

			_uiContext.Post(_ => {
				IsLoading = true;
				OnDataChanged?.Invoke();
			}, null);

			await RefreshAsync();

			lock ( _refreshLock ) {
				if ( !_refreshPending ) {
					_refreshInProgress = false;
					return;
				}

				_refreshPending = false;
			}

			eventNameForLog = null;
		}
	}

	void PostUpdateData(
		RepositoryType? repoType,
		FullStatusResult? fullStatus,
		IReadOnlyList<GitBranch>? branches,
		IReadOnlyList<string>? recentBranchNames,
		Repository? currentRepository
	) {
		_uiContext.Post(_ => {
			IsLoading = false;
			RepositoryType = repoType;
			FullStatus = fullStatus;
			Branches = branches;
			RecentBranchNames = recentBranchNames;
			CurrentRepository = currentRepository;
			OnDataChanged?.Invoke();
		}, null);
	}

	// ─── Editor events ───────────────────────────────────────────────────────
	// Only subscribe to events that do not re-fire when our widgets update.
	// scene.session.save is NOT subscribed: it fires when session state changes and is re-raised
	// after our OnDataChanged UI updates, causing an infinite refresh loop.

	[Event("scene.saved", Priority = 100)]
	public void OnSceneSaved(Scene _) => RequestDebouncedRefresh("scene.saved");

	[Event("assetsystem.newfolder", Priority = 100)]
	public void OnAssetsystemNewfolder() => RequestDebouncedRefresh("assetsystem.newfolder");

	[Event("actiongraph.saved", Priority = 100)]
	public void OnActiongraphSaved(object _) => RequestDebouncedRefresh("actiongraph.saved");

	[Event("hotloaded", Priority = 100)]
	public void OnHotloaded() => RequestDebouncedRefresh("hotloaded");
}
