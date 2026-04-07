#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Sandbox.Diagnostics;
using Sandbox.git.models;

namespace Sandbox.git;

/// <summary>
/// Commit operations: regular and merge commits.
/// Loosely mirrors Desktop's commit.ts while fitting SandGit's Core wrapper.
/// </summary>
public static class Commit {
	const string OperationCreateCommit = "createCommit";
	const string OperationCreateMergeCommit = "createMergeCommit";
	const string OperationUnstageAll = "unstageAll";
	const string OperationStageFiles = "stageFiles";
	const string OperationGetHeadSha = "getHeadSha";
	const string OperationStageManualResolution = "stageManualResolution";

	static readonly Logger Logger = new Logger("SandGit[Commit]");

	// ─── Public API ─────────────────────────────────────────────────────────

	/// <summary>
	/// Creates a commit for the given working directory changes.
	///
	/// Clears the index, stages the provided <paramref name="files"/>, and then
	/// runs <c>git commit -F -</c> with the supplied message.
	/// </summary>
	/// <param name="repository">The repository to commit in.</param>
	/// <param name="message">The commit message text.</param>
	/// <param name="files">
	/// The working directory changes to include. If null or empty, stages all
	/// changes in the working directory (equivalent to <c>git add -A</c>).
	/// </param>
	/// <param name="amend">If true, passes <c>--amend</c> to git commit.</param>
	/// <param name="noVerify">If true, passes <c>--no-verify</c> to git commit.</param>
	/// <returns>The SHA of the created (or amended) commit.</returns>
	public static async Task<string> CreateCommitAsync(
		Repository repository,
		string message,
		IReadOnlyList<GitWorkingDirectoryFileChange>? files,
		bool amend = false,
		bool noVerify = false
	) {
		if ( repository == null )
			throw new ArgumentNullException(nameof(repository));
		if ( string.IsNullOrWhiteSpace(message) )
			throw new ArgumentException("Commit message is required.", nameof(message));

		await UnstageAllAsync(repository).ConfigureAwait(false);
		await StageFilesAsync(repository, files).ConfigureAwait(false);

		var args = new List<string> { "commit", "-F", "-" };

		if ( amend )
			args.Add("--amend");

		if ( noVerify )
			args.Add("--no-verify");

		_ = await Core.GitAsync(
			args.ToArray(),
			repository.Path,
			OperationCreateCommit,
			stdin: message
		).ConfigureAwait(false);

		return await GetHeadShaAsync(repository).ConfigureAwait(false);
	}

	/// <summary>
	/// Creates a commit to finish an in-progress merge.
	///
	/// Applies any manual conflict resolutions, stages remaining <paramref name="files"/>,
	/// and then runs <c>git commit --no-edit --cleanup=strip</c>.
	/// </summary>
	/// <param name="repository">The repository to commit in.</param>
	/// <param name="files">Files participating in the merge.</param>
	/// <param name="manualResolutions">
	/// Optional map from file path to manual resolution side (ours/theirs).
	/// Paths must be relative to the repository root (same as status paths).
	/// </param>
	/// <returns>The SHA of the created merge commit.</returns>
	public static async Task<string> CreateMergeCommitAsync(
		Repository repository,
		IReadOnlyList<GitWorkingDirectoryFileChange>? files,
		IReadOnlyDictionary<string, ManualConflictResolution>? manualResolutions = null
	) {
		if ( repository == null )
			throw new ArgumentNullException(nameof(repository));

		var allFiles = files ?? Array.Empty<GitWorkingDirectoryFileChange>();
		var resolutions = manualResolutions ?? new Dictionary<string, ManualConflictResolution>();

		if ( resolutions.Count > 0 && allFiles.Count > 0 ) {
			foreach ( var kvp in resolutions ) {
				var path = kvp.Key;
				var resolution = kvp.Value;

				var file = allFiles.FirstOrDefault(f => string.Equals(f.Path, path, StringComparison.Ordinal));

				if ( file == null ) {
					Logger.Trace(
						$"[Commit] Manual resolution requested for '{path}' but no matching file was found.");
					continue;
				}

				await StageManualConflictResolutionAsync(repository, file, resolution).ConfigureAwait(false);
			}
		}

		var otherFiles = allFiles
			.Where(f => !resolutions.ContainsKey(f.Path))
			.ToList();

		if ( otherFiles.Count > 0 )
			await StageFilesAsync(repository, otherFiles).ConfigureAwait(false);

		var args = new[] { "commit", "--no-edit", "--cleanup=strip" };

		_ = await Core.GitAsync(
			args,
			repository.Path,
			OperationCreateMergeCommit
		).ConfigureAwait(false);

		return await GetHeadShaAsync(repository).ConfigureAwait(false);
	}

	// ─── Private helpers ────────────────────────────────────────────────────

	static async Task UnstageAllAsync(Repository repository) {
		if ( repository == null )
			throw new ArgumentNullException(nameof(repository));

		// Mirrors Desktop's intent of clearing the index before staging the
		// files we care about. Equivalent to: git reset HEAD -- .
		var args = new[] { "reset", "HEAD", "--", "." };
		_ = await Core.GitAsync(
			args,
			repository.Path,
			OperationUnstageAll
		).ConfigureAwait(false);
	}

	static async Task StageFilesAsync(
		Repository repository,
		IReadOnlyList<GitWorkingDirectoryFileChange>? files
	) {
		if ( repository == null )
			throw new ArgumentNullException(nameof(repository));

		// If no specific files are provided, stage everything (commit all).
		if ( files == null || files.Count == 0 ) {
			var addAllArgs = new[] { "add", "-A" };
			_ = await Core.GitAsync(
				addAllArgs,
				repository.Path,
				OperationStageFiles
			).ConfigureAwait(false);
			return;
		}

		var paths = files
			.Select(f => f.Path)
			.Where(p => !string.IsNullOrEmpty(p))
			.Distinct(StringComparer.Ordinal)
			.ToList();

		if ( paths.Count == 0 )
			return;

		var args = new List<string> { "add", "--" };
		args.AddRange(paths);

		_ = await Core.GitAsync(
			args.ToArray(),
			repository.Path,
			OperationStageFiles
		).ConfigureAwait(false);
	}

	static async Task StageManualConflictResolutionAsync(
		Repository repository,
		GitWorkingDirectoryFileChange file,
		ManualConflictResolution resolution
	) {
		if ( repository == null )
			throw new ArgumentNullException(nameof(repository));
		if ( file == null )
			throw new ArgumentNullException(nameof(file));
		if ( string.IsNullOrEmpty(file.Path) )
			throw new ArgumentException("File path is required.", nameof(file));

		// Reuse Checkout's semantics for applying "ours" or "theirs" and then stage the result.
		await Checkout.CheckoutConflictedFileAsync(repository, file, resolution).ConfigureAwait(false);

		var args = new[] { "add", "--", file.Path };
		_ = await Core.GitAsync(
			args,
			repository.Path,
			OperationStageManualResolution
		).ConfigureAwait(false);
	}

	static async Task<string> GetHeadShaAsync(Repository repository) {
		if ( repository == null )
			throw new ArgumentNullException(nameof(repository));

		var result = await Core.GitAsync(
			new[] { "rev-parse", "HEAD" },
			repository.Path,
			OperationGetHeadSha
		).ConfigureAwait(false);

		return result.Stdout.Trim();
	}
}
