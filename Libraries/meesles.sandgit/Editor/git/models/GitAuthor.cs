using System.Text.RegularExpressions;

namespace Sandbox.git.models;

/// <summary>
/// Parsed author from a trailer value (e.g. "Name &lt;email@example.com&gt;").
/// </summary>
public class GitAuthor {
	public string Name { get; }
	public string Email { get; }

	public GitAuthor(string name, string email) {
		Name = name ?? string.Empty;
		Email = email ?? string.Empty;
	}

	/// <summary>
	/// Parses a trailer value like "Name &lt;email@example.com&gt;" into a GitAuthor, or null if invalid.
	/// </summary>
	public static GitAuthor Parse(string value) {
		if ( string.IsNullOrWhiteSpace(value) )
			return null;

		var match = Regex.Match(value.Trim(), @"^(.+?)\s*<([^>]+)>$");
		if ( !match.Success || match.Groups.Count < 3 )
			return null;

		var name = match.Groups[1].Value.Trim();
		var email = match.Groups[2].Value.Trim();
		if ( string.IsNullOrEmpty(name) || string.IsNullOrEmpty(email) )
			return null;

		return new GitAuthor(name, email);
	}
}
