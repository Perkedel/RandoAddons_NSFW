namespace Sandbox.git.models;

/// <summary>
/// Basic data about the latest commit on the branch.
/// </summary>
public interface IBranchTip {
	string Sha { get; }
}
