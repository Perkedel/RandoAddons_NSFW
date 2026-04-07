namespace Sandbox.git.models;

/// <summary>
/// A local repository. Supporting model for git operations.
/// </summary>
public class Repository {
	readonly WorkingTree _mainWorkTree;

	public string Name { get; }
	public int Id { get; }
	public GitHubRepository GitHubRepository { get; }
	public bool Missing { get; }
	public string Alias { get; }
	public WorkflowPreferences WorkflowPreferences { get; }

	/// <summary>
	/// True if the repository is a tutorial repository (e.g. from onboarding).
	/// </summary>
	public bool IsTutorialRepository { get; }

	/// <summary>
	/// A hash of the properties. Objects with the same hash are structurally equal.
	/// </summary>
	public string Hash { get; }

	/// <summary>
	/// The working directory of this repository.
	/// </summary>
	public string Path => _mainWorkTree.Path;

	/// <param name="path">The working directory of this repository.</param>
	/// <param name="id">Repository identifier.</param>
	/// <param name="gitHubRepository">Associated GitHub repository, if any.</param>
	/// <param name="missing">Was the repository missing on disk last we checked?</param>
	/// <param name="alias">Display alias for the repository.</param>
	/// <param name="workflowPreferences">Workflow preferences; defaults to <see cref="WorkflowPreferences.Default"/> if null.</param>
	/// <param name="isTutorialRepository">True if this is a tutorial repository (e.g. from onboarding).</param>
	public Repository(
		string path,
		int id,
		GitHubRepository gitHubRepository,
		bool missing,
		string alias = null,
		WorkflowPreferences workflowPreferences = null,
		bool isTutorialRepository = false) {
		_mainWorkTree = new WorkingTree(path ?? string.Empty);
		Id = id;
		GitHubRepository = gitHubRepository;
		Missing = missing;
		Alias = alias;
		WorkflowPreferences = workflowPreferences ?? WorkflowPreferences.Default;
		IsTutorialRepository = isTutorialRepository;

		Name = (GitHubRepository != null && !string.IsNullOrEmpty(GitHubRepository.Name))
			? GitHubRepository.Name
			: GetBaseName(path);

		Hash = EqualityHash.Create(
			path,
			Id,
			GitHubRepository?.Hash,
			Missing,
			Alias,
			WorkflowPreferences.ForkContributionTargetValue,
			IsTutorialRepository);
	}

	static string GetBaseName(string path) {
		if ( string.IsNullOrEmpty(path) )
			return path;

		var baseName = System.IO.Path.GetFileName(path);
		if ( baseName.Length == 0 )
			return path; // repository at root of drive
		return baseName;
	}
}
