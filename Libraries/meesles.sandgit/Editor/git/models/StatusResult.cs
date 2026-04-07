#nullable enable
namespace Sandbox.git.models;

/// <summary>
/// Result of git status (branch and ahead/behind only; no working directory file list).
/// Mirrors Desktop's IStatusResult branch-related fields.
/// </summary>
public sealed class StatusResult {
	/// <summary>Current branch name, or null if detached / unborn.</summary>
	public string? CurrentBranch { get; }

	/// <summary>Tip commit SHA of the current branch, or null if unborn.</summary>
	public string? CurrentTip { get; }

	/// <summary>Upstream branch name (e.g. origin/main), or null if not set.</summary>
	public string? CurrentUpstreamBranch { get; }

	/// <summary>Ahead/behind relative to upstream, or null if no upstream.</summary>
	public IAheadBehind? BranchAheadBehind { get; }

	public StatusResult(
		string? currentBranch,
		string? currentTip,
		string? currentUpstreamBranch,
		IAheadBehind? branchAheadBehind
	) {
		CurrentBranch = currentBranch;
		CurrentTip = currentTip;
		CurrentUpstreamBranch = currentUpstreamBranch;
		BranchAheadBehind = branchAheadBehind;
	}
}
