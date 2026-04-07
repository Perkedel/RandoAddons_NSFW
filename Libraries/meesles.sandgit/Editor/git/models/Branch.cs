using System.Text.RegularExpressions;

namespace Sandbox.git.models;

/// <summary>
/// A branch as loaded from Git.
/// </summary>
public class Branch {
	public string Name { get; }
	public string Upstream { get; }
	public IBranchTip Tip { get; }
	public BranchType Type { get; }
	public string Ref { get; }

	/// <summary>
	/// A branch as loaded from Git.
	/// </summary>
	/// <param name="name">The short name of the branch, e.g. main.</param>
	/// <param name="upstream">The remote-prefixed upstream name, e.g. origin/main.</param>
	/// <param name="tip">Basic information (sha) of the latest commit on the branch.</param>
	/// <param name="type">The type of branch, local or remote.</param>
	/// <param name="refName">The canonical ref of the branch.</param>
	public Branch(
		string name,
		string upstream,
		IBranchTip tip,
		BranchType type,
		string refName) {
		Name = name ?? string.Empty;
		Upstream = upstream;
		Tip = tip;
		Type = type;
		Ref = refName ?? string.Empty;
	}

	/// <summary>
	/// The name of the upstream's remote.
	/// </summary>
	public string UpstreamRemoteName {
		get {
			if ( string.IsNullOrEmpty(Upstream) )
				return null;

			var match = Regex.Match(Upstream, @"(.*?)/.*");
			if ( !match.Success || match.Groups.Count < 2 )
				return null;

			return match.Groups[1].Value;
		}
	}

	/// <summary>
	/// The name of remote for a remote branch. If local, returns null.
	/// </summary>
	public string RemoteName {
		get {
			if ( Type == BranchType.Local )
				return null;

			var match = Regex.Match(Ref, @"^refs/remotes/(.*?)/.*");
			if ( !match.Success || match.Groups.Count != 2 )
				throw new System.Exception($"Remote branch ref has unexpected format: {Ref}");

			return match.Groups[1].Value;
		}
	}

	/// <summary>
	/// The name of the branch's upstream without the remote prefix.
	/// </summary>
	public string UpstreamWithoutRemote {
		get {
			if ( string.IsNullOrEmpty(Upstream) )
				return null;
			return RemoteHelpers.RemoveRemotePrefix(Upstream);
		}
	}

	/// <summary>
	/// The name of the branch without the remote prefix. For local branches, same as Name.
	/// </summary>
	public string NameWithoutRemote {
		get {
			if ( Type == BranchType.Local )
				return Name;

			var withoutRemote = RemoteHelpers.RemoveRemotePrefix(Name);
			return !string.IsNullOrEmpty(withoutRemote) ? withoutRemote : Name;
		}
	}

	/// <summary>
	/// True if this is a remote branch from one of Desktop's fork remotes (github-desktop-*).
	/// These are hidden in the UI as plumbing.
	/// </summary>
	public bool IsDesktopForkRemoteBranch =>
		Type == BranchType.Remote && Name.StartsWith(RemoteHelpers.ForkedRemotePrefix);
}
