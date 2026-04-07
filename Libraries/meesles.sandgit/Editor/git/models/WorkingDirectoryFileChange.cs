#nullable enable
namespace Sandbox.git.models;

/// <summary>
/// A single file change in the working directory. Mirrors Desktop's WorkingDirectoryFileChange.
/// </summary>
public sealed class GitWorkingDirectoryFileChange {
	public string Path { get; }
	public FileChangeKind Kind { get; }
	/// <summary>For Renamed/Copied, the previous path.</summary>
	public string? OldPath { get; }

	public GitWorkingDirectoryFileChange(string path, FileChangeKind kind, string? oldPath = null) {
		Path = path ?? string.Empty;
		Kind = kind;
		OldPath = oldPath;
	}
}
