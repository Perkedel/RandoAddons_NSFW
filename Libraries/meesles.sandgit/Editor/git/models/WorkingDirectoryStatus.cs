using System.Collections.Generic;

namespace Sandbox.git.models;

/// <summary>
/// Working directory file changes from git status. Mirrors Desktop's WorkingDirectoryStatus.
/// </summary>
public sealed class GitWorkingDirectoryStatus {
	public IReadOnlyList<GitWorkingDirectoryFileChange> Files { get; }

	public GitWorkingDirectoryStatus(IReadOnlyList<GitWorkingDirectoryFileChange> files) {
		Files = files ?? new List<GitWorkingDirectoryFileChange>();
	}
}
