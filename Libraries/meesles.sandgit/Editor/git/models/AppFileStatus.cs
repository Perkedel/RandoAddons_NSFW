#nullable enable
namespace Sandbox.git.models;

/// <summary>
/// Base for the union of potential states for a file change in the app.
/// </summary>
public abstract class AppFileStatus {
	public abstract AppFileStatusKind Kind { get; }
	public SubmoduleStatus? SubmoduleStatus { get; protected set; }
}

/// <summary>
/// Normal changes (new, modified, deleted).
/// </summary>
public class PlainFileStatus : AppFileStatus {
	public override AppFileStatusKind Kind { get; }

	public PlainFileStatus(AppFileStatusKind kind, SubmoduleStatus? submoduleStatus = null) {
		Kind = kind;
		SubmoduleStatus = submoduleStatus;
	}
}

/// <summary>
/// Copied or renamed file: has source and destination. Old path of rename is missing from working dir.
/// </summary>
public class CopiedOrRenamedFileStatus : AppFileStatus {
	public override AppFileStatusKind Kind { get; }
	public string OldPath { get; }
	public bool RenameIncludesModifications { get; }

	public CopiedOrRenamedFileStatus(
		AppFileStatusKind kind,
		string oldPath,
		bool renameIncludesModifications,
		SubmoduleStatus? submoduleStatus = null) {
		Kind = kind;
		OldPath = oldPath ?? string.Empty;
		RenameIncludesModifications = renameIncludesModifications;
		SubmoduleStatus = submoduleStatus;
	}
}

/// <summary>
/// Base for conflicted file status (with markers or manual).
/// </summary>
public abstract class ConflictedFileStatus : AppFileStatus {
	public override AppFileStatusKind Kind => AppFileStatusKind.Conflicted;
}

/// <summary>
/// File conflicted with detectable conflict markers.
/// </summary>
public class ConflictsWithMarkers : ConflictedFileStatus {
	public TextConflictEntry Entry { get; }
	public int ConflictMarkerCount { get; }

	public ConflictsWithMarkers(TextConflictEntry entry, int conflictMarkerCount,
		SubmoduleStatus? submoduleStatus = null) {
		Entry = entry;
		ConflictMarkerCount = conflictMarkerCount;
		SubmoduleStatus = submoduleStatus;
	}
}

/// <summary>
/// File conflicted, must be resolved manually (choose us or them).
/// </summary>
public class ManualConflict : ConflictedFileStatus {
	public ManualConflictEntry Entry { get; }

	public ManualConflict(ManualConflictEntry entry, SubmoduleStatus? submoduleStatus = null) {
		Entry = entry;
		SubmoduleStatus = submoduleStatus;
	}
}

/// <summary>
/// Untracked file in the working directory.
/// </summary>
public class UntrackedFileStatus : AppFileStatus {
	public override AppFileStatusKind Kind => AppFileStatusKind.Untracked;

	public UntrackedFileStatus(SubmoduleStatus? submoduleStatus = null) {
		SubmoduleStatus = submoduleStatus;
	}
}
