namespace Sandbox.git.models;

/// <summary>
/// Ahead/behind count for a branch relative to its tracking branch.
/// </summary>
public sealed class AheadBehind : IAheadBehind {
	public int Ahead { get; }
	public int Behind { get; }

	public AheadBehind(int ahead, int behind) {
		Ahead = ahead;
		Behind = behind;
	}
}
