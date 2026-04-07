namespace Sandbox.git.models;

/// <summary>
/// Helper functions for Repository and GitHub repository operations.
/// </summary>
public static class RepositoryHelpers {
	/// <summary>
	/// Returns whether the repository has a GitHub repository.
	/// </summary>
	public static bool IsRepositoryWithGitHubRepository(this Repository repository) {
		return repository?.GitHubRepository != null;
	}

	/// <summary>
	/// Asserts that the repository has a GitHub repository. Throws otherwise.
	/// </summary>
	public static void AssertIsRepositoryWithGitHubRepository(this Repository repository) {
		if ( repository == null || repository.GitHubRepository == null )
			FatalError.Throw("Repository must be GitHub repository");
	}

	/// <summary>
	/// Returns whether the repository is a GitHub fork (has a non-null parent).
	/// </summary>
	public static bool IsRepositoryWithForkedGitHubRepository(this Repository repository) {
		return IsRepositoryWithGitHubRepository(repository)
		       && repository.GitHubRepository.Parent != null;
	}

	/// <summary>
	/// Returns the owner/name alias if associated with a GitHub repository,
	/// otherwise the folder name that contains the repository.
	/// </summary>
	public static string NameOf(this Repository repository) {
		if ( repository == null )
			return string.Empty;
		return repository.GitHubRepository != null
			? repository.GitHubRepository.FullName
			: repository.Name;
	}

	/// <summary>
	/// Gets the GitHub HTML URL for the repository, if it has one.
	/// Returns the parent GitHub repository's URL when contributing to parent. Otherwise null.
	/// </summary>
	public static string GetGitHubHtmlUrl(this Repository repository) {
		if ( !repository.IsRepositoryWithGitHubRepository() )
			return null;
		return GetNonForkGitHubRepository(repository).HtmlUrl;
	}

	/// <summary>
	/// Honors the repository's workflow preference for contributions.
	/// Returns the GitHubRepository for non-forks; for forks returns self or parent per preference.
	/// </summary>
	public static GitHubRepository GetNonForkGitHubRepository(this Repository repository) {
		repository.AssertIsRepositoryWithGitHubRepository();

		if ( !repository.IsRepositoryWithForkedGitHubRepository() )
			return repository.GitHubRepository;

		var target = repository.GetForkContributionTarget();
		switch ( target ) {
			case ForkContributionTarget.Self:
				return repository.GitHubRepository;
			case ForkContributionTarget.Parent:
				return repository.GitHubRepository.Parent;
			default:
				FatalError.AssertNever(target, "Invalid fork contribution target");
				throw new System.Exception("Unreachable");
		}
	}

	/// <summary>
	/// Returns the fork contribution target for the repository (defaults to Parent).
	/// </summary>
	public static ForkContributionTarget GetForkContributionTarget(this Repository repository) {
		if ( repository?.WorkflowPreferences?.ForkContributionTargetValue != null )
			return repository.WorkflowPreferences.ForkContributionTargetValue.Value;
		return ForkContributionTarget.Parent;
	}

	/// <summary>
	/// Returns whether the fork is contributing to the parent.
	/// </summary>
	public static bool IsForkedRepositoryContributingToParent(this Repository repository) {
		return repository.IsRepositoryWithForkedGitHubRepository()
		       && repository.GetForkContributionTarget() == ForkContributionTarget.Parent;
	}
}
