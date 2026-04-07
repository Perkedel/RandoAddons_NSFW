#nullable enable
using System;

namespace Sandbox.git.models;

/// <summary>
/// Helpers for GitHub account comparison and endpoint detection.
/// </summary>
public static class AccountHelpers {
	/// <summary>
	/// The GitHub.com API endpoint.
	/// </summary>
	public const string DotComAPIEndpoint = "https://api.github.com";

	/// <summary>
	/// Returns a value indicating whether two account instances can be considered equal.
	/// Equality is determined by comparing the two instances' endpoints and user id.
	/// </summary>
	public static bool AccountEquals(Account? x, Account? y) {
		if ( x == y )
			return true;
		if ( x is null || y is null )
			return false;
		return x.Endpoint == y.Endpoint && x.Id == y.Id;
	}

	/// <summary>
	/// Whether the given account is a GitHub.com account (as opposed to GitHub Enterprise).
	/// </summary>
	public static bool IsDotComAccount(Account account) {
		if ( account == null )
			return false;
		return string.Equals(account.Endpoint, DotComAPIEndpoint, StringComparison.OrdinalIgnoreCase);
	}

	/// <summary>
	/// Whether the given account is a GitHub Enterprise account (as opposed to GitHub.com).
	/// </summary>
	public static bool IsEnterpriseAccount(Account account) {
		return account != null && !IsDotComAccount(account);
	}

	/// <summary>
	/// Gets a human-friendly description of the account endpoint.
	/// GitHub.com returns "GitHub.com"; Enterprise returns the hostname without protocol/path.
	/// </summary>
	internal static string GetFriendlyEndpoint(Account account) {
		if ( account == null )
			return "";
		if ( IsDotComAccount(account) )
			return "GitHub.com";
		try {
			var uri = new Uri(account.Endpoint);
			return uri.Host;
		}
		catch {
			return account.Endpoint;
		}
	}
}
