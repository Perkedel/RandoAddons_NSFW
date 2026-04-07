#nullable enable
using System;
using System.Threading.Tasks;
using Sandbox.git;
using Sandbox.git.models;

namespace Sandbox.SandGit.git;

/// <summary>
/// Add working directory files to the index. Mirrors Desktop's add.ts.
/// </summary>
public static class Add {
	const string OperationAddConflictedFile = "addConflictedFile";

	/// <summary>
	/// Add a conflicted file to the index.
	/// Typically done after having resolved conflicts either manually or through checkout --theirs/--ours.
	/// </summary>
	/// <param name="repository">The repository.</param>
	/// <param name="file">The file to add (path relative to repo root).</param>
	public static async Task AddConflictedFileAsync(
		Repository repository,
		GitWorkingDirectoryFileChange file
	) {
		if ( repository == null )
			throw new ArgumentNullException(nameof(repository));
		if ( file == null )
			throw new ArgumentNullException(nameof(file));
		if ( string.IsNullOrEmpty(file.Path) )
			throw new ArgumentException("File path is required.", nameof(file));

		var args = GetAddConflictedFileArgs(file.Path);
		_ = await Core.GitAsync(args, repository.Path, OperationAddConflictedFile).ConfigureAwait(false);
	}

	/// <summary>
	/// Builds the git arguments for adding a conflicted file. Exposed for testing to lock down the command.
	/// </summary>
	public static string[] GetAddConflictedFileArgs(string filePath) {
		if ( string.IsNullOrEmpty(filePath) )
			throw new ArgumentException("File path is required.", nameof(filePath));
		return new[] { "add", "--", filePath };
	}
}
