using System.Collections.Generic;

namespace Sandbox.git.models;

/// <summary>
/// A git commit with full metadata.
/// </summary>
public class Commit {
	public string Sha { get; }
	public string ShortSha { get; }
	public string Summary { get; }
	public string Body { get; }
	public CommitIdentity Author { get; }
	public CommitIdentity Committer { get; }
	public IReadOnlyList<string> ParentSHAs { get; }
	public IReadOnlyList<ITrailer> Trailers { get; }
	public IReadOnlyList<string> Tags { get; }

	/// <summary>Co-authors parsed from commit message trailers.</summary>
	public IReadOnlyList<GitAuthor> CoAuthors { get; }

	/// <summary>The commit body after removing co-author trailer lines.</summary>
	public string BodyNoCoAuthors { get; }

	/// <summary>True when author and committer are the same person.</summary>
	public bool AuthoredByCommitter { get; }

	/// <summary>True when the commit has at least 2 parents (merge commit).</summary>
	public bool IsMergeCommit { get; }

	/// <summary>
	/// </summary>
	/// <param name="sha">The commit's SHA.</param>
	/// <param name="shortSha">The commit's short SHA (or null to derive from sha).</param>
	/// <param name="summary">The first line of the commit message.</param>
	/// <param name="body">The commit message without the first line and CR.</param>
	/// <param name="author">Author identity (name, email, date).</param>
	/// <param name="committer">Committer identity (name, email, date).</param>
	/// <param name="parentSHAs">SHAs of the commit's parents.</param>
	/// <param name="trailers">Parsed trailers from the body, if any.</param>
	/// <param name="tags">Tags associated with this commit.</param>
	public Commit(
		string sha,
		string shortSha,
		string summary,
		string body,
		CommitIdentity author,
		CommitIdentity committer,
		IReadOnlyList<string> parentSHAs,
		IReadOnlyList<ITrailer> trailers,
		IReadOnlyList<string> tags) {
		Sha = sha ?? string.Empty;
		ShortSha = !string.IsNullOrEmpty(shortSha) ? shortSha : CommitHelpers.ShortenSha(sha);
		Summary = summary ?? string.Empty;
		Body = body ?? string.Empty;
		Author = author;
		Committer = committer;
		ParentSHAs = parentSHAs ?? System.Array.Empty<string>();
		Trailers = trailers ?? System.Array.Empty<ITrailer>();
		Tags = tags ?? System.Array.Empty<string>();

		CoAuthors = CommitHelpers.ExtractCoAuthors(Trailers);
		AuthoredByCommitter = Author != null && Committer != null
		                                     && Author.Name == Committer.Name
		                                     && Author.Email == Committer.Email;
		BodyNoCoAuthors = CommitHelpers.TrimCoAuthorsTrailers(Trailers, Body);
		IsMergeCommit = ParentSHAs.Count > 1;
	}

	/// <summary>
	/// Minimal constructor for use when only SHA is needed (e.g. compare results).
	/// </summary>
	public Commit(string sha)
		: this(sha, null, null, null, null, null, null, null, null) {
	}
}
