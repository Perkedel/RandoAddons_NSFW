#nullable enable
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Sandbox.git.models;

namespace Sandbox.git;

/// <summary>
/// List branches and refs using git for-each-ref.
/// </summary>
public static class ForEachRef {
	const string OperationGetBranches = "getBranches";
	const string OperationGetBranchesDifferingFromUpstream = "getBranchesDifferingFromUpstream";

	/// <summary>Git exit code when not a git repository or similar fatal error.</summary>
	static readonly IReadOnlySet<int> SuccessExitCodesWithFatal = new HashSet<int> { 0, 128 };

	/// <summary>
	/// Get all the branches (local and remote).
	/// </summary>
	/// <param name="repository">The repository.</param>
	/// <param name="prefixes">Ref prefixes to list (e.g. refs/heads, refs/remotes). If null or empty, uses refs/heads and refs/remotes.</param>
	public static async Task<IReadOnlyList<models.Branch>> GetBranchesAsync(
		Repository repository,
		params string[]? prefixes
	) {
		if ( repository == null )
			throw new ArgumentNullException(nameof(repository));

		if ( prefixes == null || prefixes.Length == 0 )
			prefixes = new[] { "refs/heads", "refs/remotes" };

		// Format: fullName, shortName, upstreamShortName, sha, symRef (null-byte separated; one line per ref)
		var format = "%(refname)%00%(refname:short)%00%(upstream:short)%00%(objectname)%00%(symref)";
		var args = GetForEachRefArgs(format, prefixes);

		var result = await Core.GitAsync(
			args,
			repository.Path,
			OperationGetBranches,
			SuccessExitCodesWithFatal
		).ConfigureAwait(false);

		if ( result.ExitCode != 0 )
			return Array.Empty<models.Branch>();

		var branches = new List<models.Branch>();
		foreach ( var refEntry in ParseRefLines(result.Stdout, 5) ) {
			var fullName = refEntry[0];
			var shortName = refEntry[1];
			var upstreamShortName = refEntry[2];
			var sha = refEntry[3];
			var symRef = refEntry[4];

			// Exclude symbolic refs from the branch list
			if ( symRef.Length > 0 )
				continue;

			var tip = new BranchTip(sha);
			var type = fullName.StartsWith("refs/heads", StringComparison.Ordinal)
				? BranchType.Local
				: BranchType.Remote;
			var upstream = upstreamShortName.Length > 0 ? upstreamShortName : null;

			branches.Add(new models.Branch(shortName, upstream, tip, type, fullName));
		}

		return branches;
	}

	/// <summary>
	/// Gets all branches that differ from their upstream (ahead, behind, or both), excluding the current branch.
	/// Useful to narrow down branches that could potentially be fast-forwarded.
	/// </summary>
	public static async Task<IReadOnlyList<ITrackingBranch>> GetBranchesDifferingFromUpstreamAsync(
		Repository repository
	) {
		if ( repository == null )
			throw new ArgumentNullException(nameof(repository));

		// Format: fullName, sha, upstream, symref, head
		var format = "%(refname)%00%(objectname)%00%(upstream)%00%(symref)%00%(HEAD)";
		var prefixes = new[] { "refs/heads", "refs/remotes" };
		var args = GetForEachRefArgs(format, prefixes);

		var result = await Core.GitAsync(
			args,
			repository.Path,
			OperationGetBranchesDifferingFromUpstream,
			SuccessExitCodesWithFatal
		).ConfigureAwait(false);

		if ( result.ExitCode != 0 )
			return Array.Empty<ITrackingBranch>();

		var localBranches = new List<(string Ref, string Sha, string Upstream)>();
		var remoteBranchShas = new Dictionary<string, string>(StringComparer.Ordinal);

		foreach ( var refEntry in ParseRefLines(result.Stdout, 5) ) {
			var fullName = refEntry[0];
			var sha = refEntry[1];
			var upstream = refEntry[2];
			var symRef = refEntry[3];
			var head = refEntry[4];

			// Exclude symbolic refs and the current branch
			if ( symRef.Length > 0 || head == "*" )
				continue;

			if ( fullName.StartsWith("refs/heads", StringComparison.Ordinal) ) {
				if ( upstream.Length == 0 )
					continue;
				localBranches.Add((fullName, sha, upstream));
			} else {
				remoteBranchShas[fullName] = sha;
			}
		}

		var eligible = new List<ITrackingBranch>();
		foreach ( var branch in localBranches ) {
			if ( !remoteBranchShas.TryGetValue(branch.Upstream, out var remoteSha) )
				continue;
			if ( remoteSha == branch.Sha )
				continue;
			eligible.Add(new TrackingBranch(
				branch.Ref,
				branch.Sha,
				branch.Upstream,
				remoteSha
			));
		}

		return eligible;
	}

	/// <summary>Builds git arguments for for-each-ref. Exposed for testing.</summary>
	public static string[] GetForEachRefArgs(string format, string[] prefixes) {
		if ( format == null )
			throw new ArgumentNullException(nameof(format));
		if ( prefixes == null || prefixes.Length == 0 )
			throw new ArgumentException("At least one prefix is required.", nameof(prefixes));
		var list = new List<string> { "for-each-ref", $"--format={format}" };
		list.AddRange(prefixes);
		return list.ToArray();
	}

	/// <summary>
	/// Parses git for-each-ref output: one line per ref, fields separated by null byte.
	/// </summary>
	static IEnumerable<string[]> ParseRefLines(string stdout, int expectedFields) {
		if ( string.IsNullOrEmpty(stdout) )
			yield break;

		foreach ( var line in stdout.Split('\n') ) {
			var trimmed = line.TrimEnd('\r');
			if ( trimmed.Length == 0 )
				continue;

			var fields = trimmed.Split('\0');
			if ( fields.Length < expectedFields )
				continue;

			yield return fields;
		}
	}
}
