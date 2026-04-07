using System;
using System.Text.RegularExpressions;

namespace Sandbox.git.models;

/// <summary>
/// Identity (name, email, date) for a commit author or committer.
/// </summary>
public class CommitIdentity {
	public string Name { get; }
	public string Email { get; }
	public DateTimeOffset Date { get; }

	public CommitIdentity(string name, string email, DateTimeOffset date) {
		Name = name ?? string.Empty;
		Email = email ?? string.Empty;
		Date = date;
	}

	/// <summary>
	/// Parses an identity string in GIT_AUTHOR_IDENT format: "Name &lt;email&gt; unixTimestamp timezone"
	/// (e.g. from git log --format=%an &lt;%ae&gt; %ad --date=raw).
	/// </summary>
	public static CommitIdentity Parse(string identityString) {
		if ( string.IsNullOrWhiteSpace(identityString) )
			return new CommitIdentity(string.Empty, string.Empty, DateTimeOffset.UtcNow);

		var parts = identityString.TrimEnd().Split(' ');
		if ( parts.Length < 4 ) {
			// No date; try name <email> only
			var ident = identityString.Trim();
			var nameEmail = ParseNameAndEmail(ident);
			return new CommitIdentity(
				nameEmail.name,
				nameEmail.email,
				DateTimeOffset.UtcNow
			);
		}

		var timestampStr = parts[parts.Length - 2];
		var timezoneStr = parts[parts.Length - 1];
		var nameAndEmail = string.Join(" ", parts, 0, parts.Length - 2);
		var (name, email) = ParseNameAndEmail(nameAndEmail);

		if ( !long.TryParse(timestampStr, out var unixSeconds) )
			return new CommitIdentity(name, email, DateTimeOffset.UtcNow);

		var offset = ParseTimezoneOffset(timezoneStr);
		var date = DateTimeOffset.FromUnixTimeSeconds(unixSeconds).ToOffset(offset);
		return new CommitIdentity(name, email, date);
	}

	static (string name, string email) ParseNameAndEmail(string value) {
		if ( string.IsNullOrWhiteSpace(value) )
			return (string.Empty, string.Empty);

		var match = Regex.Match(value.Trim(), @"^(.+?)\s*<([^>]+)>\s*$");
		if ( !match.Success || match.Groups.Count < 3 )
			return (value.Trim(), string.Empty);

		var name = match.Groups[1].Value.Trim();
		var email = match.Groups[2].Value.Trim();
		return (name, email);
	}

	static TimeSpan ParseTimezoneOffset(string timezoneStr) {
		if ( string.IsNullOrEmpty(timezoneStr) || timezoneStr.Length < 2 )
			return TimeSpan.Zero;

		var sign = timezoneStr[0] == '-' ? -1 : 1;
		if ( timezoneStr[0] != '+' && timezoneStr[0] != '-' )
			return TimeSpan.Zero;

		if ( !int.TryParse(timezoneStr.Substring(1), System.Globalization.NumberStyles.None, null, out var hhmm) )
			return TimeSpan.Zero;

		var hours = hhmm / 100;
		var minutes = hhmm % 100;
		return new TimeSpan(0, sign * (hours * 60 + minutes), 0);
	}
}
