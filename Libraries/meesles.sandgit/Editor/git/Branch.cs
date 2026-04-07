#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Sandbox.git.models;

namespace Sandbox.git;

/// <summary>
/// Branch-related git operations (create, list, rename, delete, query).
/// </summary>
public static class Branch {
	/// <summary>
	/// Create a new branch from the given start point.
	/// </summary>
	/// <param name="repository">The repository in which to create the new branch.</param>
	/// <param name="name">The name of the new branch.</param>
	/// <param name="startPoint">A committish string that the new branch should be based on, or null if the branch should be created from the current HEAD.</param>
	/// <param name="noTrack">If true, do not set up tracking (e.g. when branching from a remote branch).</param>
	public static async Task CreateBranchAsync(
		Repository repository,
		string name,
		string? startPoint,
		bool noTrack = false) {
		if ( repository == null )
			throw new ArgumentNullException(nameof(repository));
		if ( string.IsNullOrEmpty(name) )
			throw new ArgumentException("Branch name is required.", nameof(name));

		var args = GetCreateBranchArgs(name, startPoint, noTrack);
		await Core.GitAsync(args, repository.Path, "createBranch").ConfigureAwait(false);
	}

	/// <summary>
	/// Gets the short names of all local branches.
	/// </summary>
	public static async Task<string[]> GetBranchNamesAsync(Repository repository) {
		if ( repository == null )
			throw new ArgumentNullException(nameof(repository));

		var result = await Core.GitAsync(
			GetBranchNamesArgs(),
			repository.Path,
			"getBranchNames").ConfigureAwait(false);

		return ParseBranchLines(result.Stdout)
			.Select(line => line.Trim())
			.Where(s => s.Length > 0)
			.ToArray();
	}

	/// <summary>
	/// Rename the given branch to a new name.
	/// </summary>
	/// <param name="repository">The repository.</param>
	/// <param name="branch">The branch to rename.</param>
	/// <param name="newName">The new name.</param>
	/// <param name="force">If true, use -M to force rename (e.g. case-only renames on case-insensitive filesystems).</param>
	public static async Task RenameBranchAsync(
		Repository repository,
		models.Branch branch,
		string newName,
		bool? force = null) {
		if ( repository == null )
			throw new ArgumentNullException(nameof(repository));
		if ( branch == null )
			throw new ArgumentNullException(nameof(branch));
		if ( string.IsNullOrEmpty(newName) )
			throw new ArgumentException("New branch name is required.", nameof(newName));

		try {
			await Core.GitAsync(
				GetRenameBranchArgs(branch.NameWithoutRemote, newName, force),
				repository.Path,
				"renameBranch").ConfigureAwait(false);
		} catch ( GitException ex ) {
			// If we failed and the branch name only differs by case, retry with -M (see Desktop #21320).
			if ( force != null )
				throw;

			if ( !IsBranchAlreadyExistsError(ex.Result) )
				throw;

			var m = Regex.Match(ex.Result.Stderr, @"fatal: a branch named '(.+?)' already exists");
			if ( !m.Success || !string.Equals(m.Groups[1].Value, newName, StringComparison.OrdinalIgnoreCase) )
				throw;

			var names = await GetBranchNamesAsync(repository).ConfigureAwait(false);
			if ( names.Contains(newName) )
				throw;

			await RenameBranchAsync(repository, branch, newName, true).ConfigureAwait(false);
		}
	}

	/// <summary>
	/// Delete the branch locally (force delete with -D).
	/// </summary>
	public static async Task DeleteLocalBranchAsync(Repository repository, string branchName) {
		if ( repository == null )
			throw new ArgumentNullException(nameof(repository));
		if ( string.IsNullOrEmpty(branchName) )
			throw new ArgumentException("Branch name is required.", nameof(branchName));

		await Core.GitAsync(
			GetDeleteLocalBranchArgs(branchName),
			repository.Path,
			"deleteLocalBranch").ConfigureAwait(false);
	}

	/// <summary>
	/// Deletes a remote branch (push :ref to remote). If the remote ref was already deleted, removes the local remote-tracking ref.
	/// </summary>
	/// <param name="repository">The repository.</param>
	/// <param name="remote">The remote (e.g. origin).</param>
	/// <param name="remoteBranchName">The name of the branch on the remote.</param>
	public static async Task DeleteRemoteBranchAsync(
		Repository repository,
		IRemote remote,
		string remoteBranchName) {
		if ( repository == null )
			throw new ArgumentNullException(nameof(repository));
		if ( remote == null )
			throw new ArgumentNullException(nameof(remote));
		if ( string.IsNullOrEmpty(remoteBranchName) )
			throw new ArgumentException("Remote branch name is required.", nameof(remoteBranchName));

		var args = GetDeleteRemoteBranchArgs(remote.Name, remoteBranchName);
		var result = await Core.GitAsync(
			args,
			repository.Path,
			"deleteRemoteBranch",
			successExitCodes: new HashSet<int> { 0, 1 }).ConfigureAwait(false);

		// If push failed (e.g. ref already deleted on remote), remove our local remote-tracking ref.
		if ( result.ExitCode != 0 ) {
			var refName = $"refs/remotes/{remote.Name}/{remoteBranchName}";
			await DeleteRefAsync(repository, refName).ConfigureAwait(false);
		}
	}

	/// <summary>
	/// Finds branches whose tip equals the given committish (sha, HEAD, etc.).
	/// </summary>
	/// <returns>List of branch short names, or null if the committish could not be resolved or was malformed.</returns>
	public static async Task<string[]?> GetBranchesPointedAtAsync(Repository repository, string commitish) {
		if ( repository == null )
			throw new ArgumentNullException(nameof(repository));
		if ( string.IsNullOrEmpty(commitish) )
			throw new ArgumentException("Committish is required.", nameof(commitish));

		var result = await Core.GitAsync(
			GetBranchesPointedAtArgs(commitish),
			repository.Path,
			"branchPointedAt",
			successExitCodes: new HashSet<int> { 0, 1, 129 }).ConfigureAwait(false);

		if ( result.ExitCode == 1 || result.ExitCode == 129 )
			return null;

		var lines = result.Stdout.Split('\n');
		return lines.Length > 0 && string.IsNullOrEmpty(lines[lines.Length - 1])
			? lines.Take(lines.Length - 1).ToArray()
			: lines;
	}

	/// <summary>
	/// Gets all branches that have been merged into the given branch.
	/// </summary>
	/// <param name="repository">The repository.</param>
	/// <param name="branchName">The base branch name (e.g. main).</param>
	/// <returns>Map of branch canonical ref to its tip sha (excluding the base branch itself).</returns>
	public static async Task<Dictionary<string, string>> GetMergedBranchesAsync(
		Repository repository,
		string branchName) {
		if ( repository == null )
			throw new ArgumentNullException(nameof(repository));
		if ( string.IsNullOrEmpty(branchName) )
			throw new ArgumentException("Branch name is required.", nameof(branchName));

		var canonicalBranchRef = FormatAsLocalRef(branchName);
		var result = await Core.GitAsync(
			GetMergedBranchesArgs(branchName),
			repository.Path,
			"mergedBranches").ConfigureAwait(false);

		var merged = new Dictionary<string, string>(StringComparer.Ordinal);
		foreach ( var line in ParseBranchLines(result.Stdout) ) {
			var trimmed = line.Trim();
			if ( trimmed.Length == 0 )
				continue;

			var firstSpace = trimmed.IndexOf(' ');
			if ( firstSpace <= 0 )
				continue;

			var sha = trimmed.Substring(0, firstSpace);
			var canonicalRef = trimmed.Substring(firstSpace + 1).Trim();
			if ( canonicalRef == canonicalBranchRef )
				continue;

			merged[canonicalRef] = sha;
		}

		return merged;
	}

	// --- Helpers ---

	/// <summary>
	/// Canonical local branch ref (e.g. refs/heads/main).
	/// </summary>
	public static string FormatAsLocalRef(string branchName) {
		if ( string.IsNullOrEmpty(branchName) )
			throw new ArgumentException("Branch name is required.", nameof(branchName));
		return "refs/heads/" + branchName;
	}

	static async Task DeleteRefAsync(Repository repository, string refName) {
		await Core.GitAsync(
			GetDeleteRefArgs(refName),
			repository.Path,
			"deleteRef",
			successExitCodes: new HashSet<int> { 0, 1 }).ConfigureAwait(false);
	}

	// --- Args (exposed for testing) ---

	/// <summary>Builds git arguments for creating a branch. Exposed for testing.</summary>
	public static string[] GetCreateBranchArgs(string name, string? startPoint, bool noTrack = false) {
		if ( string.IsNullOrEmpty(name) )
			throw new ArgumentException("Branch name is required.", nameof(name));
		var args = startPoint != null
			? new[] { "branch", name, startPoint }
			: new[] { "branch", name };
		if ( noTrack )
			args = args.Concat(new[] { "--no-track" }).ToArray();
		return args;
	}

	/// <summary>Builds git arguments for listing branch names. Exposed for testing.</summary>
	public static string[] GetBranchNamesArgs() {
		return new[] { "branch", "--format=%(refname:short)" };
	}

	/// <summary>Builds git arguments for renaming a branch. Exposed for testing.</summary>
	public static string[] GetRenameBranchArgs(string nameWithoutRemote, string newName, bool? force = null) {
		if ( string.IsNullOrEmpty(nameWithoutRemote) )
			throw new ArgumentException("Branch name is required.", nameof(nameWithoutRemote));
		if ( string.IsNullOrEmpty(newName) )
			throw new ArgumentException("New branch name is required.", nameof(newName));
		return new[] { "branch", force == true ? "-M" : "-m", nameWithoutRemote, newName };
	}

	/// <summary>Builds git arguments for deleting a local branch. Exposed for testing.</summary>
	public static string[] GetDeleteLocalBranchArgs(string branchName) {
		if ( string.IsNullOrEmpty(branchName) )
			throw new ArgumentException("Branch name is required.", nameof(branchName));
		return new[] { "branch", "-D", branchName };
	}

	/// <summary>Builds git arguments for deleting a remote branch (push :ref). Exposed for testing.</summary>
	public static string[] GetDeleteRemoteBranchArgs(string remoteName, string remoteBranchName) {
		if ( string.IsNullOrEmpty(remoteName) )
			throw new ArgumentException("Remote name is required.", nameof(remoteName));
		if ( string.IsNullOrEmpty(remoteBranchName) )
			throw new ArgumentException("Remote branch name is required.", nameof(remoteBranchName));
		return new[] { "push", remoteName, $":{remoteBranchName}" };
	}

	/// <summary>Builds git arguments for listing branches pointed at a committish. Exposed for testing.</summary>
	public static string[] GetBranchesPointedAtArgs(string commitish) {
		if ( string.IsNullOrEmpty(commitish) )
			throw new ArgumentException("Committish is required.", nameof(commitish));
		return new[] { "branch", $"--points-at={commitish}", "--format=%(refname:short)" };
	}

	/// <summary>Builds git arguments for listing merged branches. Exposed for testing.</summary>
	public static string[] GetMergedBranchesArgs(string branchName) {
		if ( string.IsNullOrEmpty(branchName) )
			throw new ArgumentException("Branch name is required.", nameof(branchName));
		return new[] { "branch", "--format=%(objectname) %(refname)", "--merged", branchName };
	}

	/// <summary>Builds git arguments for deleting a ref. Exposed for testing.</summary>
	public static string[] GetDeleteRefArgs(string refName) {
		if ( string.IsNullOrEmpty(refName) )
			throw new ArgumentException("Ref name is required.", nameof(refName));
		return new[] { "update-ref", "-d", refName };
	}

	static bool IsBranchAlreadyExistsError(GitResult result) {
		return result.ExitCode != 0
		       && (result.Stderr?.Contains("a branch named ", StringComparison.OrdinalIgnoreCase) == true
		           && result.Stderr?.Contains(" already exists", StringComparison.OrdinalIgnoreCase) == true);
	}

	static IEnumerable<string> ParseBranchLines(string stdout) {
		if ( string.IsNullOrEmpty(stdout) )
			yield break;
		foreach ( var line in stdout.Split('\n') )
			yield return line;
	}
}
