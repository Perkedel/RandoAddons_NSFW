namespace Sandbox.git.models;

/// <summary>
/// Basic data about a branch and the branch it's tracking.
/// </summary>
public interface ITrackingBranch {
	string Ref { get; }
	string Sha { get; }
	string UpstreamRef { get; }
	string UpstreamSha { get; }
}
