namespace Sandbox.git.models;

/// <summary>
/// Throws errors for unreachable code or assertion failures.
/// </summary>
public static class FatalError {
	/// <summary>
	/// Throws an error that is not meant to be caught.
	/// </summary>
	public static void Throw(string message) {
		throw new System.Exception(message);
	}

	/// <summary>
	/// Exhaustiveness check for discriminated unions. Call in the default branch of a switch.
	/// </summary>
	public static void AssertNever<T>(T value, string message = null) {
		throw new System.Exception(message ?? $"Unexpected value: {value}");
	}
}
