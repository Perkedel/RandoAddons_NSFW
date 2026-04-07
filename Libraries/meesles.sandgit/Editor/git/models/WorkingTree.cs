namespace Sandbox.git.models;

/// <summary>
/// Base type for a directory you can run git commands in.
/// </summary>
public class WorkingTree {
	public string Path { get; }

	public WorkingTree(string path) {
		Path = path ?? string.Empty;
	}
}
