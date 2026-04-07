using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sandbox.git;

/// <summary>
/// Initialize a new git repository.
/// </summary>
public static class InitRepository {
	const string OperationInitGitRepository = "initGitRepository";
	const string OperationGetDefaultBranch = "getDefaultBranch";

	static readonly IReadOnlySet<int> SuccessExitCodesConfig = new HashSet<int> { 0, 1 };

	/// <summary>
	/// Initializes a new git repository in the given path, using the configured or default initial branch name.
	/// </summary>
	public static async Task InitGitRepositoryAsync(string path) {
		var defaultBranch = await GetDefaultBranchAsync().ConfigureAwait(false);
		await Core.GitAsync(
			GetInitArgs(defaultBranch),
			path,
			OperationInitGitRepository
		).ConfigureAwait(false);
	}

	/// <summary>Builds git arguments for init with a given default branch. Exposed for testing.</summary>
	public static string[] GetInitArgs(string defaultBranch) {
		if ( string.IsNullOrEmpty(defaultBranch) )
			throw new ArgumentException("Default branch is required.", nameof(defaultBranch));
		return new[] { "-c", $"init.defaultBranch={defaultBranch}", "init" };
	}

	/// <summary>
	/// Returns the configured default branch for new repositories (e.g. from init.defaultBranch), or "main" if unset.
	/// </summary>
	public static async Task<string> GetDefaultBranchAsync() {
		var result = await Core.GitAsync(
			GetDefaultBranchConfigArgs(),
			Environment.CurrentDirectory,
			OperationGetDefaultBranch,
			SuccessExitCodesConfig
		).ConfigureAwait(false);

		if ( result.ExitCode == 0 ) {
			var branch = result.Stdout.Trim();
			if ( !string.IsNullOrEmpty(branch) ) return branch;
		}

		return "main";
	}

	/// <summary>Builds git arguments for reading init.defaultBranch config. Exposed for testing.</summary>
	public static string[] GetDefaultBranchConfigArgs() {
		return new[] { "config", "--global", "init.defaultBranch" };
	}
}
