namespace Sandbox.git.models;

/// <summary>
/// Simple implementation of a commit message trailer.
/// </summary>
public class Trailer : ITrailer {
	public string Token { get; }
	public string Value { get; }

	public Trailer(string token, string value) {
		Token = token ?? string.Empty;
		Value = value ?? string.Empty;
	}
}
