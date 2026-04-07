namespace Sandbox.git.models;

/// <summary>
/// Kind of change for a working-directory file. Mirrors Desktop's AppFileStatusKind.
/// </summary>
public enum FileChangeKind {
	New,
	Modified,
	Deleted,
	Untracked,
	Conflicted,
	Renamed,
	Copied,
}
