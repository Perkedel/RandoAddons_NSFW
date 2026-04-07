#nullable enable
using System.Collections.Generic;

namespace Sandbox.git.models;

/// <summary>
/// Full status result: branch info plus working directory changes. Mirrors Desktop's IStatusResult.
/// </summary>
public sealed class FullStatusResult {
	public string? CurrentBranch { get; }
	public string? CurrentTip { get; }
	public string? CurrentUpstreamBranch { get; }
	public IAheadBehind? BranchAheadBehind { get; }
	public GitWorkingDirectoryStatus WorkingDirectory { get; }

	public FullStatusResult(
		string? currentBranch,
		string? currentTip,
		string? currentUpstreamBranch,
		IAheadBehind? branchAheadBehind,
		GitWorkingDirectoryStatus workingDirectory
	) {
		CurrentBranch = currentBranch;
		CurrentTip = currentTip;
		CurrentUpstreamBranch = currentUpstreamBranch;
		BranchAheadBehind = branchAheadBehind;
		WorkingDirectory = workingDirectory ?? new GitWorkingDirectoryStatus(new List<GitWorkingDirectoryFileChange>());
	}
}
