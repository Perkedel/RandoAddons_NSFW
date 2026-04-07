#nullable enable
using System.Collections.Generic;

namespace Sandbox.git.models;

/// <summary>
/// A GitHub account, representing the user found on GitHub.com or GitHub Enterprise.
/// Contains a token used for operations that require authentication.
/// </summary>
public class Account {
	public string Login { get; }
	public string Endpoint { get; }
	public string Token { get; }
	public IReadOnlyList<IAPIEmail> Emails { get; }
	public string AvatarUrl { get; }
	public long Id { get; }
	public string Name { get; }
	public string? Plan { get; }
	public string? CopilotEndpoint { get; }
	public bool? IsCopilotDesktopEnabled { get; }
	public IReadOnlyList<string>? Features { get; }

	string? _friendlyEndpoint;

	/// <summary>
	/// Create an account which can be used to perform unauthenticated API actions.
	/// </summary>
	public static Account Anonymous() {
		return new Account(
			login: "",
			endpoint: AccountHelpers.DotComAPIEndpoint,
			token: "",
			emails: [],
			avatarUrl: "",
			id: -1,
			name: "",
			plan: "free"
		);
	}

	/// <summary>
	/// Create an instance of an account.
	/// </summary>
	public Account(
		string login,
		string endpoint,
		string token,
		IReadOnlyList<IAPIEmail> emails,
		string avatarUrl,
		long id,
		string name,
		string? plan = null,
		string? copilotEndpoint = null,
		bool? isCopilotDesktopEnabled = null,
		IReadOnlyList<string>? features = null
	) {
		Login = login ?? "";
		Endpoint = endpoint ?? "";
		Token = token ?? "";
		Emails = emails ?? System.Array.Empty<IAPIEmail>();
		AvatarUrl = avatarUrl ?? "";
		Id = id;
		Name = name ?? "";
		Plan = plan;
		CopilotEndpoint = copilotEndpoint;
		IsCopilotDesktopEnabled = isCopilotDesktopEnabled;
		Features = features;
	}

	/// <summary>
	/// Returns a new account with the given token; all other properties are unchanged.
	/// </summary>
	public Account WithToken(string token) {
		return new Account(
			Login,
			Endpoint,
			token,
			Emails,
			AvatarUrl,
			Id,
			Name,
			Plan,
			CopilotEndpoint,
			IsCopilotDesktopEnabled,
			Features
		);
	}

	/// <summary>
	/// Name to display: Name if non-empty, otherwise Login.
	/// </summary>
	public string FriendlyName => string.IsNullOrEmpty(Name) ? Login : Name;

	/// <summary>
	/// Human-friendly description of the account endpoint.
	/// GitHub.com accounts return "GitHub.com"; Enterprise accounts return the hostname.
	/// </summary>
	public string FriendlyEndpoint => _friendlyEndpoint ??= AccountHelpers.GetFriendlyEndpoint(this);
}
