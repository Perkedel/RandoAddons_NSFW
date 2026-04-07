namespace Sandbox.git.models;

/// <summary>
/// Encapsulates changes to a committed file (commitish = "after", parentCommitish = "before").
/// </summary>
public class CommittedFileChange : FileChange {
	public string Commitish { get; }
	public string ParentCommitish { get; }

	public CommittedFileChange(string path, AppFileStatus status, string commitish, string parentCommitish)
		: base(path, status) {
		Commitish = commitish ?? string.Empty;
		ParentCommitish = parentCommitish ?? string.Empty;
	}
}
