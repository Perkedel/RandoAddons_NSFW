#nullable enable
using System.Text.RegularExpressions;

namespace Sandbox.git.models;

/// <summary>
/// A remote as defined in Git.
/// </summary>
public interface IRemote {
	string Name { get; }
	string Url { get; }
}

/// <summary>
/// Remote ref prefix used by Desktop for fork remotes. Branches from these are hidden in the UI.
/// This is the magic remote name prefix for when we add a remote on behalf of the user.
/// </summary>
public static class RemoteHelpers {
	public const string ForkedRemotePrefix = "github-desktop-";

	/// <summary>
	/// Returns the remote name used for a fork pull request (ForkedRemotePrefix + remoteName).
	/// </summary>
	public static string ForkPullRequestRemoteName(string remoteName) {
		return $"{ForkedRemotePrefix}{remoteName}";
	}

	/// <summary>
	/// Gets a value indicating whether two remotes can be considered structurally equivalent.
	/// </summary>
	public static bool RemoteEquals(IRemote? x, IRemote? y) {
		if ( x == y )
			return true;
		if ( x is null || y is null )
			return false;
		return x.Name == y.Name && x.Url == y.Url;
	}

	/// <summary>
	/// Removes the remote prefix (e.g. "origin/") from a ref name.
	/// </summary>
	public static string RemoveRemotePrefix(string refName) {
		if ( string.IsNullOrEmpty(refName) )
			return refName;

		var match = Regex.Match(refName, @"(.*?)/.*");
		if ( !match.Success || match.Groups.Count < 2 )
			return refName;

		var remoteName = match.Groups[1].Value;
		if ( string.IsNullOrEmpty(remoteName) || refName.Length <= remoteName.Length + 1 )
			return refName;

		return refName.Substring(remoteName.Length + 1);
	}
}
