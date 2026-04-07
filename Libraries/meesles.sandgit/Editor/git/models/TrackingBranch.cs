namespace Sandbox.git.models;

/// <summary>
/// A local branch and its upstream tracking info (when they differ).
/// </summary>
public class TrackingBranch : ITrackingBranch {
	public string Ref { get; }
	public string Sha { get; }
	public string UpstreamRef { get; }
	public string UpstreamSha { get; }

	public TrackingBranch(string refName, string sha, string upstreamRef, string upstreamSha) {
		Ref = refName ?? string.Empty;
		Sha = sha ?? string.Empty;
		UpstreamRef = upstreamRef ?? string.Empty;
		UpstreamSha = upstreamSha ?? string.Empty;
	}
}
