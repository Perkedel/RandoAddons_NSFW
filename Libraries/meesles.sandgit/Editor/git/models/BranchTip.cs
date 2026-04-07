namespace Sandbox.git.models;

/// <summary>
/// Basic data about the latest commit on the branch.
/// </summary>
public class BranchTip : IBranchTip {
	public string Sha { get; }

	public BranchTip(string sha) {
		Sha = sha ?? string.Empty;
	}
}
