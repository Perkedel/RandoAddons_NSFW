namespace Sandbox.git.models;

/// <summary>
/// Type guards and helpers for status types.
/// </summary>
public static class StatusHelpers {
	public static bool IsConflictedFileStatus(AppFileStatus appFileStatus) {
		return appFileStatus?.Kind == AppFileStatusKind.Conflicted;
	}

	public static bool IsConflictWithMarkers(ConflictedFileStatus conflictedFileStatus) {
		return conflictedFileStatus is ConflictsWithMarkers;
	}

	public static bool IsManualConflict(ConflictedFileStatus conflictedFileStatus) {
		return conflictedFileStatus is ManualConflict;
	}
}
