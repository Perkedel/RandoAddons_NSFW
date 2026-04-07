namespace Sandbox.git.models;

/// <summary>
/// Email address associated with an account from the API.
/// </summary>
public interface IAPIEmail {
	string Email { get; }
	bool Primary { get; }
	bool Verified { get; }
}
