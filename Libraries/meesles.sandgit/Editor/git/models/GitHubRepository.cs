#nullable enable
namespace Sandbox.git.models;

/// <summary>
/// A GitHub (or GitHub Enterprise) repository.
/// </summary>
public class GitHubRepository {
	public string Name { get; }
	public string FullName => $"{Owner.Login}/{Name}";
	public string HtmlUrl { get; }
	public int? DbId { get; }
	public string Hash { get; }

	/// <summary>Parent repository when this is a fork; null otherwise.</summary>
	public GitHubRepository? Parent { get; }

	public GitHubOwner Owner { get; }

	public GitHubRepository(
		string name,
		GitHubOwner owner,
		string htmlUrl,
		int? dbId,
		GitHubRepository? parent = null,
		string? hash = null) {
		Name = name;
		Owner = owner;
		HtmlUrl = htmlUrl;
		DbId = dbId;
		Parent = parent;
		Hash = hash ?? $"{owner.Login}/{name}";
	}
}

public class GitHubOwner {
	public string Login { get; }

	public GitHubOwner(string login) {
		Login = login;
	}
}
