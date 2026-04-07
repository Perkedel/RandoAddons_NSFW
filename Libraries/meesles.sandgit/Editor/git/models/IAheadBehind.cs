namespace Sandbox.git.models;

/// <summary>
/// Ahead/behind count for a branch relative to its tracking branch.
/// </summary>
public interface IAheadBehind {
	int Ahead { get; }
	int Behind { get; }
}
