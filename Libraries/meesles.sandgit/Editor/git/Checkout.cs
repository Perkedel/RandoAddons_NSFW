#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Sandbox.git.models;

namespace Sandbox.git;

/// <summary>
/// Checkout operations: branch, commit, paths at HEAD, and conflicted file resolution.
/// Mirrors Desktop's checkout.ts (branch, commit, paths, conflicted file).
/// </summary>
public static class Checkout {
	const string OperationCheckoutBranch = "checkoutBranch";
	const string OperationCheckoutCommit = "checkoutCommit";
	const string OperationCheckoutPaths = "checkoutPaths";
	const string OperationCheckoutConflictedFile = "checkoutConflictedFile";

	// ─── Public API ─────────────────────────────────────────────────────────

	/// <summary>
	/// Check out the given branch. For a remote branch, creates a local branch with the same short name (-b).
	/// No-op if already on the given local branch.
	/// </summary>
	/// <param name="repository">The repository in which to perform the checkout.</param>
	/// <param name="branch">The branch to check out (local or remote).</param>
	public static async Task CheckoutBranchAsync(Repository repository, models.Branch branch) {
		if ( repository == null )
			throw new ArgumentNullException(nameof(repository));
		if ( branch == null )
			throw new ArgumentNullException(nameof(branch));

		var args = GetBranchCheckoutArgs(branch);
		await Core.GitAsync(args, repository.Path, OperationCheckoutBranch).ConfigureAwait(false);
	}

	/// <summary>
	/// Check out the given commit (detached HEAD). Literally runs git checkout &lt;sha&gt;.
	/// </summary>
	/// <param name="repository">The repository in which to perform the checkout.</param>
	/// <param name="commit">The commit to check out (use commit.Sha).</param>
	public static async Task CheckoutCommitAsync(Repository repository, CommitOneLine commit) {
		if ( repository == null )
			throw new ArgumentNullException(nameof(repository));
		if ( commit == null )
			throw new ArgumentNullException(nameof(commit));
		if ( string.IsNullOrEmpty(commit.Sha) )
			throw new ArgumentException("Commit SHA is required.", nameof(commit));

		var args = GetCheckoutCommitArgs(commit.Sha);
		await Core.GitAsync(args, repository.Path, OperationCheckoutCommit).ConfigureAwait(false);
	}

	/// <summary>
	/// Check out the given paths at HEAD. Reverts working tree (and index) for those paths to HEAD.
	/// </summary>
	/// <param name="repository">The repository.</param>
	/// <param name="paths">Paths to revert (relative to repo root). Empty list is a no-op.</param>
	public static async Task CheckoutPathsAsync(Repository repository, IReadOnlyList<string> paths) {
		if ( repository == null )
			throw new ArgumentNullException(nameof(repository));
		if ( paths == null )
			throw new ArgumentNullException(nameof(paths));

		if ( paths.Count == 0 )
			return;

		var args = GetCheckoutPathsArgs(paths);
		await Core.GitAsync(args, repository.Path, OperationCheckoutPaths).ConfigureAwait(false);
	}

	/// <summary>
	/// Check out either "ours" (stage #2) or "theirs" (stage #3) for a conflicted file.
	/// </summary>
	/// <param name="repository">The repository.</param>
	/// <param name="file">The conflicted file (path).</param>
	/// <param name="resolution">Ours or theirs.</param>
	public static async Task CheckoutConflictedFileAsync(
		Repository repository,
		GitWorkingDirectoryFileChange file,
		ManualConflictResolution resolution) {
		if ( repository == null )
			throw new ArgumentNullException(nameof(repository));
		if ( file == null )
			throw new ArgumentNullException(nameof(file));
		if ( string.IsNullOrEmpty(file.Path) )
			throw new ArgumentException("File path is required.", nameof(file));

		var args = GetCheckoutConflictedFileArgs(file.Path, resolution);
		await Core.GitAsync(args, repository.Path, OperationCheckoutConflictedFile).ConfigureAwait(false);
	}

	// ─── Args (exposed for testing) ──────────────────────────────────────────

	/// <summary>Builds git arguments for checking out a branch. Exposed for testing.</summary>
	public static string[] GetBranchCheckoutArgs(models.Branch branch) {
		if ( branch == null )
			throw new ArgumentNullException(nameof(branch));
		if ( branch.Type == BranchType.Remote )
			return new[] { "checkout", "-b", branch.NameWithoutRemote, branch.Name, "--" };
		return new[] { "checkout", branch.Name, "--" };
	}

	/// <summary>Builds git arguments for checking out a commit. Exposed for testing.</summary>
	public static string[] GetCheckoutCommitArgs(string sha) {
		if ( string.IsNullOrEmpty(sha) )
			throw new ArgumentException("Commit SHA is required.", nameof(sha));
		return new[] { "checkout", sha };
	}

	/// <summary>Builds git arguments for checking out paths at HEAD. Exposed for testing.</summary>
	public static string[] GetCheckoutPathsArgs(IReadOnlyList<string> paths) {
		if ( paths == null )
			throw new ArgumentNullException(nameof(paths));
		if ( paths.Count == 0 )
			return Array.Empty<string>();
		return new[] { "checkout", "HEAD", "--" }.Concat(paths).ToArray();
	}

	/// <summary>Builds git arguments for checking out ours/theirs on a conflicted file. Exposed for testing.</summary>
	public static string[] GetCheckoutConflictedFileArgs(string filePath, ManualConflictResolution resolution) {
		if ( string.IsNullOrEmpty(filePath) )
			throw new ArgumentException("File path is required.", nameof(filePath));
		var flag = resolution == ManualConflictResolution.Ours ? "--ours" : "--theirs";
		return new[] { "checkout", flag, "--", filePath };
	}
}
