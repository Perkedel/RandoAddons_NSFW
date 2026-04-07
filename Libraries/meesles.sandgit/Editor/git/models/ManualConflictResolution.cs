namespace Sandbox.git.models;

/// <summary>
/// Resolution side for a conflicted file: checkout stage #2 (ours) or #3 (theirs).
/// Used with git checkout --ours / --theirs.
/// </summary>
public enum ManualConflictResolution {
	/// <summary>Check out "ours" version (stage #2).</summary>
	Ours,

	/// <summary>Check out "theirs" version (stage #3).</summary>
	Theirs
}
