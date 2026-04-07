namespace Sandbox.git.models;

/// <summary>
/// Result of repository type detection (bare, regular, missing, or unsafe).
/// </summary>
public abstract class RepositoryType {
	public abstract string Kind { get; }
}

/// <summary>Repository is a bare clone.</summary>
public sealed class BareRepositoryType : RepositoryType {
	public override string Kind => "bare";
}

/// <summary>Repository is a regular (non-bare) clone with a working directory.</summary>
public sealed class RegularRepositoryType : RepositoryType {
	public override string Kind => "regular";
	public string TopLevelWorkingDirectory { get; }

	public RegularRepositoryType(string topLevelWorkingDirectory) {
		TopLevelWorkingDirectory = topLevelWorkingDirectory ?? string.Empty;
	}
}

/// <summary>Path is not a repository or directory not found.</summary>
public sealed class MissingRepositoryType : RepositoryType {
	public override string Kind => "missing";
}

/// <summary>Repository exists but has dubious ownership (Git refuses to use it).</summary>
public sealed class UnsafeRepositoryType : RepositoryType {
	public override string Kind => "unsafe";
	public string RepositoryPath { get; }

	public UnsafeRepositoryType(string repositoryPath) {
		RepositoryPath = repositoryPath ?? string.Empty;
	}
}
