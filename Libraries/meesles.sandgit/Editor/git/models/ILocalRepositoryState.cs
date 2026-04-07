namespace Sandbox.git.models;

/// <summary>
/// A snapshot of the local state for a given repository.
/// </summary>
public interface ILocalRepositoryState {
	/// <summary>
	/// The ahead/behind count for the current branch, or null if no tracking branch.
	/// </summary>
	IAheadBehind AheadBehind { get; }

	/// <summary>
	/// The number of uncommitted changes currently in the repository.
	/// </summary>
	int ChangedFilesCount { get; }
}
