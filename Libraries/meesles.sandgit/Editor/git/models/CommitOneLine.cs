namespace Sandbox.git.models;

/// <summary>
/// Minimal commit data when full metadata is not needed (e.g. --oneline --no-abbrev-commit).
/// </summary>
public class CommitOneLine {
	/// <summary>The full commit id.</summary>
	public string Sha { get; }

	/// <summary>The first line of the commit message.</summary>
	public string Summary { get; }

	public CommitOneLine(string sha, string summary) {
		Sha = sha ?? string.Empty;
		Summary = summary ?? string.Empty;
	}
}
