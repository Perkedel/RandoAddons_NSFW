namespace Sandbox.git.models;

/// <summary>
/// The status of a submodule.
/// </summary>
public class SubmoduleStatus {
	/// <summary>Whether the submodule is pointing to a different commit.</summary>
	public bool CommitChanged { get; init; }

	/// <summary>Whether the submodule has modified changes not yet committed.</summary>
	public bool ModifiedChanges { get; init; }

	/// <summary>Whether the submodule has untracked changes not yet committed.</summary>
	public bool UntrackedChanges { get; init; }
}
