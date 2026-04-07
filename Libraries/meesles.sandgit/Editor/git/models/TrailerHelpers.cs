using System;
using System.Collections.Generic;

namespace Sandbox.git.models;

/// <summary>
/// A parsed trailer from a commit message (e.g. Co-Authored-By: Name &lt;email&gt;).
/// </summary>
public interface ITrailer {
	string Token { get; }
	string Value { get; }
}

/// <summary>
/// Helpers for interpreting commit trailers.
/// </summary>
public static class TrailerHelpers {
	public const string CoAuthoredByToken = "Co-Authored-By";

	public static bool IsCoAuthoredByTrailer(ITrailer trailer) {
		return trailer != null &&
		       string.Equals(trailer.Token, CoAuthoredByToken, StringComparison.OrdinalIgnoreCase);
	}

	/// <summary>
	/// Parses unfolded trailer lines (e.g. from %(trailers:unfold,only)) with the given key-value separator.
	/// Each line is "Token: Value" or "Token separator Value".
	/// </summary>
	public static IReadOnlyList<ITrailer> ParseRawUnfoldedTrailers(string text, char keyValueSeparator = ':') {
		if ( string.IsNullOrWhiteSpace(text) )
			return Array.Empty<ITrailer>();

		var list = new List<ITrailer>();
		foreach ( var line in text.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries) ) {
			var trimmed = line.Trim();
			if ( trimmed.Length == 0 )
				continue;

			var sepIndex = trimmed.IndexOf(keyValueSeparator);
			if ( sepIndex <= 0 )
				continue;

			var token = trimmed.Substring(0, sepIndex).Trim();
			var value = trimmed.Substring(sepIndex + 1).Trim();
			if ( token.Length > 0 )
				list.Add(new Trailer(token, value));
		}

		return list;
	}
}
