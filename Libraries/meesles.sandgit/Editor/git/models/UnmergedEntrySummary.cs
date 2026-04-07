namespace Sandbox.git.models;

/// <summary>
/// Summary of how an unmerged entry arose (us/them).
/// </summary>
public enum UnmergedEntrySummary {
	AddedByUs,
	DeletedByUs,
	AddedByThem,
	DeletedByThem,
	BothDeleted,
	BothAdded,
	BothModified
}
