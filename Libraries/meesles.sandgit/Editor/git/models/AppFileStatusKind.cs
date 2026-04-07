namespace Sandbox.git.models;

/// <summary>
/// The enum representation of a Git file change in the app.
/// </summary>
public enum AppFileStatusKind {
	New,
	Modified,
	Deleted,
	Copied,
	Renamed,
	Conflicted,
	Untracked
}
