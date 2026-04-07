using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Sandbox.git.models;

namespace Sandbox.git;

/// <summary>
/// Reflog-based operations: recent branches and branch checkouts.
/// </summary>
public static class Reflog {
	const string OperationGetRecentBranches = "getRecentBranches";
	const string OperationGetBranchCheckouts = "getBranchCheckouts";

	static readonly IReadOnlySet<int> SuccessExitCodesReflog = new HashSet<int> { 0, 128 };

	// .*? (renamed|checkout)(?:: moving from|\s*) (?:refs/heads/|\s*)(.*?) to (?:refs/heads/|\s*)(.*?)$
	static readonly Regex RecentBranchesRe = new Regex(
		@".*? (renamed|checkout)(?:: moving from|\s*) (?:refs/heads/|\s*)(.*?) to (?:refs/heads/|\s*)(.*?)$",
		RegexOptions.IgnoreCase
	);

	// ^[a-z0-9]{40}\sHEAD@{(.*)}\scheckout: moving from\s.*\sto\s(.*)$
	static readonly Regex BranchCheckoutLineRe = new Regex(
		@"^[a-z0-9]{40}\sHEAD@{(.*)}\scheckout: moving from\s.*\sto\s(.*)$"
	);

	static readonly Regex NoCommitsOnBranchRe = new Regex(
		@"fatal: your current branch '.*' does not have any commits yet"
	);

	/// <summary>
	/// Gets the <paramref name="limit"/> most recently checked out branches.
	/// Uses git log -g to avoid unbounded output on large reflogs.
	/// </summary>
	public static async Task<IReadOnlyList<string>> GetRecentBranchesAsync(
		Repository repository,
		int limit
	) {
		if ( repository == null )
			return Array.Empty<string>();

		var result = await Core.GitAsync(
			GetRecentBranchesArgs(),
			repository.Path,
			OperationGetRecentBranches,
			SuccessExitCodesReflog
		).ConfigureAwait(false);

		if ( result.ExitCode == 128 )
			return Array.Empty<string>();

		var names = new List<string>();
		var excludedNames = new HashSet<string>();

		foreach ( var line in result.Stdout.Split('\n') ) {
			var match = RecentBranchesRe.Match(line);
			if ( !match.Success || match.Groups.Count < 4 )
				continue;

			var operationType = match.Groups[1].Value;
			var excludeBranchName = match.Groups[2].Value;
			var branchName = match.Groups[3].Value;

			if ( string.Equals(operationType, "renamed", StringComparison.OrdinalIgnoreCase) )
				excludedNames.Add(excludeBranchName);

			if ( !excludedNames.Contains(branchName) && !names.Contains(branchName) )
				names.Add(branchName);

			if ( names.Count >= limit )
				break;
		}

		return names;
	}

	/// <summary>
	/// Gets the distinct list of branches that have been checked out on or after <paramref name="afterDate"/>.
	/// Returns a map of branch name to (first) checkout date.
	/// </summary>
	public static async Task<IReadOnlyDictionary<string, DateTime>> GetBranchCheckoutsAsync(
		Repository repository,
		DateTime afterDate
	) {
		if ( repository == null )
			return new Dictionary<string, DateTime>();

		var result = await Core.GitAsync(
			GetBranchCheckoutsArgs(afterDate),
			repository.Path,
			OperationGetBranchCheckouts,
			SuccessExitCodesReflog
		).ConfigureAwait(false);

		var checkouts = new Dictionary<string, DateTime>();

		if ( result.ExitCode == 128 && NoCommitsOnBranchRe.IsMatch(result.Stderr) )
			return checkouts;

		if ( result.ExitCode != 0 )
			return checkouts;

		foreach ( var line in result.Stdout.Split('\n') ) {
			var match = BranchCheckoutLineRe.Match(line);
			if ( !match.Success || match.Groups.Count < 3 )
				continue;

			var timestampStr = match.Groups[1].Value;
			var branchName = match.Groups[2].Value;

			if ( checkouts.ContainsKey(branchName) )
				continue;

			if ( DateTime.TryParse(timestampStr, null, System.Globalization.DateTimeStyles.RoundtripKind, out var dt) )
				checkouts[branchName] = dt;
		}

		return checkouts;
	}

	/// <summary>Builds git arguments for get recent branches (log -g). Exposed for testing.</summary>
	public static string[] GetRecentBranchesArgs() {
		return new[] { "log", "-g", "--no-abbrev-commit", "--pretty=oneline", "HEAD", "-n", "2500", "--" };
	}

	/// <summary>Builds git arguments for get branch checkouts (reflog). Exposed for testing.</summary>
	public static string[] GetBranchCheckoutsArgs(DateTime afterDate) {
		return new[] {
			"reflog", "--date=iso", $"--after=\"{afterDate:O}\"", "--pretty=%H %gd %gs",
			"--grep-reflog=checkout: moving from .* to .*$", "--"
		};
	}
}
