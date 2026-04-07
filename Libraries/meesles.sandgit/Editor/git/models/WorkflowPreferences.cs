#nullable enable
namespace Sandbox.git.models;

/// <summary>
/// Per-repository workflow preferences.
/// </summary>
public class WorkflowPreferences {
	public ForkContributionTarget? ForkContributionTargetValue { get; init; }

	public static WorkflowPreferences Default { get; } = new();
}
