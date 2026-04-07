using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Sandbox.git.models;

namespace Sandbox.git;

/// <summary>
/// Rev-parse and repository-type detection. Uses a single git process where possible to avoid blocking the editor.
/// </summary>
public static class RevParse {
	const string OperationGetRepositoryType = "getRepositoryType";
	const string OperationGetUpstreamRefForRef = "getUpstreamRefForRef";

	static readonly IReadOnlySet<int> SuccessExitCodesRevParse = new HashSet<int> { 0, 128 };

	/// <summary>
	/// Attempts to fulfill the work of isGitRepository and isBareRepository while requiring only one Git process.
	/// Returns 'bare', 'regular', 'missing', or 'unsafe' (dubious ownership).
	/// </summary>
	public static async Task<RepositoryType> GetRepositoryTypeAsync(string path) {
		if ( string.IsNullOrEmpty(path) || !Directory.Exists(path) ) {
			return new MissingRepositoryType();
		}

		try {
			var result = await Core.GitAsync(
				GetRepositoryTypeArgs(),
				path,
				OperationGetRepositoryType,
				SuccessExitCodesRevParse
			).ConfigureAwait(false);

			if ( result.ExitCode == 0 ) {
				var parts = result.Stdout.Split('\n', 2);
				var isBare = parts.Length > 0 ? parts[0].Trim() : string.Empty;
				var cdup = parts.Length > 1 ? parts[1].Trim() : string.Empty;
				if ( isBare == "true" ) {
					return new BareRepositoryType();
				}

				var topLevel = Path.GetFullPath(Path.Combine(path, cdup));
				return new RegularRepositoryType(topLevel);
			}

			var unsafeMatch = Regex.Match(
				result.Stderr,
				@"fatal: detected dubious ownership in repository at '(.+)'"
			);
			if ( unsafeMatch.Success ) {
				return new UnsafeRepositoryType(unsafeMatch.Groups[1].Value);
			}

			return new MissingRepositoryType();
		} catch ( Exception ex ) {
			// ENOENT-style: path or git not found; treat as missing so we don't block the editor.
			if ( ex is FileNotFoundException or DirectoryNotFoundException ) {
				return new MissingRepositoryType();
			}

			throw;
		}
	}

	/// <summary>
	/// Resolves the upstream ref for the given ref (e.g. "refs/remotes/origin/main"). Returns null if no upstream.
	/// </summary>
	public static async Task<string> GetUpstreamRefForRefAsync(string path, string refName = null) {
		var args = GetUpstreamRefForRefArgs(refName);
		var result = await Core.GitAsync(
			args,
			path,
			OperationGetUpstreamRefForRef,
			SuccessExitCodesRevParse
		).ConfigureAwait(false);
		return result.ExitCode == 0 ? result.Stdout.Trim() : null;
	}

	/// <summary>
	/// Returns the remote name for the given ref's upstream (e.g. "origin"). Returns null if no upstream.
	/// </summary>
	public static async Task<string> GetUpstreamRemoteNameForRefAsync(string path, string refName = null) {
		var remoteRef = await GetUpstreamRefForRefAsync(path, refName).ConfigureAwait(false);
		if ( string.IsNullOrEmpty(remoteRef) ) return null;
		var match = Regex.Match(remoteRef, @"^refs/remotes/([^/]+)/");
		return match.Success ? match.Groups[1].Value : null;
	}

	/// <summary>
	/// Upstream ref for the current HEAD.
	/// </summary>
	public static Task<string> GetCurrentUpstreamRefAsync(string path) {
		return GetUpstreamRefForRefAsync(path);
	}

	/// <summary>
	/// Upstream remote name for the current HEAD.
	/// </summary>
	public static Task<string> GetCurrentUpstreamRemoteNameAsync(string path) {
		return GetUpstreamRemoteNameForRefAsync(path);
	}

	/// <summary>
	/// Current branch name (e.g. "main"), or "HEAD" if detached. Returns null if not a git repo or on failure.
	/// </summary>
	public static async Task<string> GetCurrentBranchNameAsync(string path) {
		if ( string.IsNullOrEmpty(path) )
			return null;

		var result = await Core.GitAsync(
			GetCurrentBranchNameArgs(),
			path,
			"getCurrentBranch",
			SuccessExitCodesRevParse
		).ConfigureAwait(false);

		if ( result.ExitCode != 0 )
			return null;

		var name = result.Stdout.Trim();
		return string.IsNullOrEmpty(name) ? null : name;
	}

	/// <summary>Builds git arguments for repository type detection. Exposed for testing.</summary>
	public static string[] GetRepositoryTypeArgs() {
		return new[] { "rev-parse", "--is-bare-repository", "--show-cdup" };
	}

	/// <summary>Builds git arguments for resolving upstream ref. Exposed for testing.</summary>
	public static string[] GetUpstreamRefForRefArgs(string refName = null) {
		var rev = (refName ?? string.Empty) + "@{upstream}";
		return new[] { "rev-parse", "--symbolic-full-name", rev };
	}

	/// <summary>Builds git arguments for current branch name. Exposed for testing.</summary>
	public static string[] GetCurrentBranchNameArgs() {
		return new[] { "rev-parse", "--abbrev-ref", "HEAD" };
	}
}
