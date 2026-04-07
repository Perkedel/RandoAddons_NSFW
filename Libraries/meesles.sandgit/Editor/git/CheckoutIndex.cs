#nullable enable
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Sandbox.git.models;

namespace Sandbox.git;

/// <summary>
/// Updates the working directory from the index for a given set of files (git checkout-index).
/// </summary>
public static class CheckoutIndex {
	const string OperationCheckoutIndex = "checkoutIndex";

	static readonly HashSet<int> SuccessExitCodes = new() { 0, 1 };

	/// <summary>
	/// Forcefully updates the working directory with information from the index
	/// for a given set of files.
	/// </summary>
	/// <remarks>
	/// This is essentially the same as running <c>git checkout -- files</c>
	/// except by using <c>checkout-index</c> we pass the files on stdin, avoiding
	/// issues with too long command lines.
	/// Does not throw for paths that do not exist in the index (-q).
	/// </remarks>
	/// <param name="repository">The repository in which to update the working directory from the index.</param>
	/// <param name="paths">Relative paths in the working directory to update from the index.</param>
	public static async Task CheckoutIndexAsync(Repository repository, IReadOnlyList<string> paths) {
		if ( repository == null )
			throw new ArgumentNullException(nameof(repository));
		if ( paths == null )
			throw new ArgumentNullException(nameof(paths));

		if ( paths.Count == 0 )
			return;

		var stdin = BuildCheckoutIndexStdin(paths);
		var args = GetCheckoutIndexArgs();

		await Core.GitAsync(
			args,
			repository.Path,
			OperationCheckoutIndex,
			SuccessExitCodes,
			stdin
		).ConfigureAwait(false);
	}

	/// <summary>Builds git arguments for checkout-index. Exposed for testing.</summary>
	public static string[] GetCheckoutIndexArgs() {
		return new[] { "checkout-index", "-f", "-u", "-q", "--stdin", "-z" };
	}

	/// <summary>Builds stdin for checkout-index (null-separated paths). Exposed for testing.</summary>
	public static string BuildCheckoutIndexStdin(IReadOnlyList<string> paths) {
		if ( paths == null || paths.Count == 0 )
			return string.Empty;
		return string.Join("\0", paths);
	}
}
