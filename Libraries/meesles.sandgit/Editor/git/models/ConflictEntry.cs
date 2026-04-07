#nullable enable
namespace Sandbox.git.models;

/// <summary>
/// Base for unmerged (conflicted) entries.
/// </summary>
public abstract class UnmergedEntry {
	public const string EntryKind = "conflicted";
	public SubmoduleStatus? SubmoduleStatus { get; init; }
}

/// <summary>
/// Text conflict with detectable markers (BothAdded or BothModified).
/// </summary>
public class TextConflictEntry : UnmergedEntry {
	public UnmergedEntrySummary Action { get; }
	public GitStatusEntry Us { get; }
	public GitStatusEntry Them { get; }

	public TextConflictEntry(UnmergedEntrySummary action, GitStatusEntry us, GitStatusEntry them,
		SubmoduleStatus? submoduleStatus = null) {
		Action = action;
		Us = us;
		Them = them;
		SubmoduleStatus = submoduleStatus;
	}
}

/// <summary>
/// Manual conflict: user must choose us or them.
/// </summary>
public class ManualConflictEntry : UnmergedEntry {
	public UnmergedEntrySummary Action { get; }
	public GitStatusEntry Us { get; }
	public GitStatusEntry Them { get; }

	public ManualConflictEntry(UnmergedEntrySummary action, GitStatusEntry us, GitStatusEntry them,
		SubmoduleStatus? submoduleStatus = null) {
		Action = action;
		Us = us;
		Them = them;
		SubmoduleStatus = submoduleStatus;
	}
}
