namespace Sandbox.git.models;

/// <summary>
/// Encapsulates changes to a file associated with a commit.
/// </summary>
public class FileChange {
	public string Id { get; }
	public string Path { get; }
	public AppFileStatus Status { get; }

	public FileChange(string path, AppFileStatus status) {
		Path = path ?? string.Empty;
		Status = status;

		if ( status is CopiedOrRenamedFileStatus cr )
			Id = $"{status.Kind}+{path}+{cr.OldPath}";
		else
			Id = $"{status.Kind}+{path}";
	}

	public bool IsDeleted() => Status.Kind == AppFileStatusKind.Deleted;
	public bool IsNew() => Status.Kind == AppFileStatusKind.New;
	public bool IsModified() => Status.Kind == AppFileStatusKind.Modified;
	public bool IsUntracked() => Status.Kind == AppFileStatusKind.Untracked;
}
