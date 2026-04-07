using System.Collections.Generic;

namespace Sandbox.git.models;

/// <summary>
/// Helpers for commit SHA and trailer handling.
/// </summary>
public static class CommitHelpers {
	/// <summary>Shortens a given SHA to 7 characters.</summary>
	public static string ShortenSha(string sha) {
		if ( string.IsNullOrEmpty(sha) )
			return sha;
		return sha.Length <= 7 ? sha : sha.Substring(0, 7);
	}

	/// <summary>
	/// Extracts Co-Authored-By trailers from an array of trailers.
	/// </summary>
	public static IReadOnlyList<GitAuthor> ExtractCoAuthors(IReadOnlyList<ITrailer> trailers) {
		if ( trailers == null || trailers.Count == 0 )
			return System.Array.Empty<GitAuthor>();

		var list = new List<GitAuthor>();
		foreach ( var trailer in trailers ) {
			if ( !TrailerHelpers.IsCoAuthoredByTrailer(trailer) )
				continue;
			var author = GitAuthor.Parse(trailer.Value);
			if ( author != null )
				list.Add(author);
		}

		return list;
	}

	/// <summary>
	/// Removes Co-Authored-By trailer lines from the body text.
	/// </summary>
	public static string TrimCoAuthorsTrailers(IReadOnlyList<ITrailer> trailers, string body) {
		if ( string.IsNullOrEmpty(body) )
			return body;
		if ( trailers == null || trailers.Count == 0 )
			return body;

		var result = body;
		foreach ( var trailer in trailers ) {
			if ( !TrailerHelpers.IsCoAuthoredByTrailer(trailer) )
				continue;
			var line = $"{trailer.Token}: {trailer.Value}";
			result = result.Replace(line, "");
		}

		return result;
	}
}
