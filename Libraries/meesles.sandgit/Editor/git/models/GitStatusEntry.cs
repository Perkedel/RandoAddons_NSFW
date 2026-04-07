namespace Sandbox.git.models;

/// <summary>
/// The status entry code as reported by Git.
/// </summary>
public enum GitStatusEntry {
	Modified = 77, // 'M'
	Added = 65, // 'A'
	Deleted = 68, // 'D'
	Renamed = 82, // 'R'
	Copied = 67, // 'C'
	Unchanged = 46, // '.'
	Untracked = 63, // '?'
	Ignored = 33, // '!'
	UpdatedButUnmerged = 85 // 'U'
}
