using System.Collections.Generic;

namespace Sandbox.git.models;

/// <summary>
/// The result of comparing two refs in a repository.
/// </summary>
public interface ICompareResult : IAheadBehind {
	IReadOnlyList<Commit> Commits { get; }
}
