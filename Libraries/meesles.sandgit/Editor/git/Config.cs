using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Sandbox.git;
using Sandbox.git.models;

namespace Sandbox.SandGit.git;

/// <summary>
/// Git config helpers, loosely mirroring Desktop's config.ts.
/// </summary>
public static class Config {
	const string OperationGetConfigValueInPath = "getConfigValueInPath";
	const string OperationGetGlobalConfigPath = "getGlobalConfigPath";
	const string OperationAddGlobalConfigValue = "addGlobalConfigValue";
	const string OperationSetConfigValueInPath = "setConfigValueInPath";
	const string OperationRemoveConfigValueInPath = "removeConfigValueInPath";

	/// <summary>
	/// Canonicalization type for git config values, see <c>git config --type</c>.
	/// </summary>
	public enum ConfigValueType {
		Bool,
		Int,
		BoolOrInt,
		Path,
		ExpiryDate,
		Color
	}

	// ─── Public API: get ─────────────────────────────────────────────────────

	/// <summary>
	/// Look up a config value by name in the repository.
	/// </summary>
	/// <param name="repository">The repository whose configuration to query.</param>
	/// <param name="name">The config key (e.g. "user.name").</param>
	/// <param name="onlyLocal">
	/// Whether the value should be resolved only from the local repository
	/// configuration (equivalent to <c>git config --local</c>).
	/// </param>
	public static Task<string> GetConfigValueAsync(
		Repository repository,
		string name,
		bool onlyLocal = false
	) {
		if ( repository == null )
			throw new ArgumentNullException(nameof(repository));
		if ( string.IsNullOrWhiteSpace(name) )
			throw new ArgumentException("Config name is required.", nameof(name));

		return GetConfigValueInPathAsync(name, repository.Path, onlyLocal);
	}

	/// <summary>
	/// Look up a global config value by name.
	/// </summary>
	public static Task<string> GetGlobalConfigValueAsync(string name) {
		if ( string.IsNullOrWhiteSpace(name) )
			throw new ArgumentException("Config name is required.", nameof(name));

		return GetConfigValueInPathAsync(name, path: null, onlyLocal: false);
	}

	/// <summary>
	/// Look up a boolean config value by name in the repository.
	/// </summary>
	/// <remarks>
	/// Uses git's own boolean parsing semantics via <c>git config --type bool</c>.
	/// Returns null if the value is not set.
	/// </remarks>
	public static async Task<bool> GetBooleanConfigValueAsync(
		Repository repository,
		string name,
		bool onlyLocal = false
	) {
		if ( repository == null )
			throw new ArgumentNullException(nameof(repository));

		var value = await GetConfigValueInPathAsync(
			name,
			repository.Path,
			onlyLocal,
			ConfigValueType.Bool
		).ConfigureAwait(false);

		return value != "false";
	}

	/// <summary>
	/// Look up a global boolean config value by name.
	/// </summary>
	public static async Task<bool> GetGlobalBooleanConfigValueAsync(string name) {
		if ( string.IsNullOrWhiteSpace(name) )
			throw new ArgumentException("Config name is required.", nameof(name));

		var value = await GetConfigValueInPathAsync(
			name,
			path: null,
			onlyLocal: false,
			type: ConfigValueType.Bool
		).ConfigureAwait(false);

		return value != "false";
	}

	/// <summary>
	/// Get the path to the global git config file.
	/// </summary>
	/// <remarks>
	/// Implemented via <c>git config --global --list --show-origin</c> to avoid
	/// launching an editor. Parses the first origin line and normalizes the path.
	/// </remarks>
	public static async Task<string> GetGlobalConfigPathAsync() {
		var args = new[] { "config", "--global", "--list", "--show-origin" };

		var result = await Core.GitAsync(
			args,
			Environment.CurrentDirectory,
			OperationGetGlobalConfigPath
		).ConfigureAwait(false);

		var firstLine = result.Stdout
			.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
			.FirstOrDefault();

		if ( string.IsNullOrWhiteSpace(firstLine) )
			return string.Empty;

		const string prefix = "file:";
		var prefixIndex = firstLine.IndexOf(prefix, StringComparison.OrdinalIgnoreCase);
		if ( prefixIndex < 0 )
			return string.Empty;

		var start = prefixIndex + prefix.Length;
		var end = firstLine.IndexOf('\t', start);
		if ( end < 0 )
			end = firstLine.Length;

		var path = firstLine.Substring(start, end - start).Trim();
		if ( string.IsNullOrEmpty(path) )
			return string.Empty;

		return Path.GetFullPath(path);
	}

	// ─── Public API: set/add/remove ──────────────────────────────────────────

	/// <summary>
	/// Set a local config value by name.
	/// </summary>
	public static Task SetConfigValueAsync(
		Repository repository,
		string name,
		string value
	) {
		if ( repository == null )
			throw new ArgumentNullException(nameof(repository));

		return SetConfigValueInPathAsync(name, value, repository.Path);
	}

	/// <summary>
	/// Set a global config value by name.
	/// </summary>
	public static Task SetGlobalConfigValueAsync(
		string name,
		string value
	) {
		return SetConfigValueInPathAsync(name, value, path: null);
	}

	/// <summary>
	/// Add a global config value by name (does not replace existing values).
	/// </summary>
	public static async Task AddGlobalConfigValueAsync(string name, string value) {
		if ( string.IsNullOrWhiteSpace(name) )
			throw new ArgumentException("Config name is required.", nameof(name));

		var args = new[] { "config", "--global", "--add", name, value };
		_ = await Core.GitAsync(
			args,
			Environment.CurrentDirectory,
			OperationAddGlobalConfigValue
		).ConfigureAwait(false);
	}

	/// <summary>
	/// Adds a path to the <c>safe.directory</c> configuration variable if it's not already present.
	/// </summary>
	public static async Task AddSafeDirectoryAsync(string path) {
		if ( string.IsNullOrWhiteSpace(path) )
			throw new ArgumentException("Path is required.", nameof(path));

		if ( RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && path[0] == '/' ) {
			path = "%(prefix)/" + path;
		}

		await AddGlobalConfigValueIfMissingAsync("safe.directory", path).ConfigureAwait(false);
	}

	/// <summary>
	/// Add a global config value if the given value is not already present.
	/// </summary>
	public static async Task AddGlobalConfigValueIfMissingAsync(string name, string value) {
		if ( string.IsNullOrWhiteSpace(name) )
			throw new ArgumentException("Config name is required.", nameof(name));

		var args = new[] { "config", "--global", "-z", "--get-all", name, value };

		var successCodes = new HashSet<int> { 0, 1 };
		var result = await Core.GitAsync(
			args,
			Environment.CurrentDirectory,
			OperationAddGlobalConfigValue,
			successCodes
		).ConfigureAwait(false);

		var pieces = (result.Stdout ?? string.Empty).Split('\0');
		var hasValue = pieces.Any(p => string.Equals(p, value, StringComparison.Ordinal));

		if ( result.ExitCode == 1 || !hasValue ) {
			await AddGlobalConfigValueAsync(name, value).ConfigureAwait(false);
		}
	}

	/// <summary>
	/// Remove a local config value by name.
	/// </summary>
	public static Task RemoveConfigValueAsync(
		Repository repository,
		string name
	) {
		if ( repository == null )
			throw new ArgumentNullException(nameof(repository));

		return RemoveConfigValueInPathAsync(name, repository.Path);
	}

	/// <summary>
	/// Remove a global config value by name.
	/// </summary>
	public static Task RemoveGlobalConfigValueAsync(string name) {
		return RemoveConfigValueInPathAsync(name, path: null);
	}

	// ─── Private helpers ─────────────────────────────────────────────────────

	static async Task<string> GetConfigValueInPathAsync(
		string name,
		string path,
		bool onlyLocal = false,
		ConfigValueType? type = null
	) {
		if ( string.IsNullOrWhiteSpace(name) )
			throw new ArgumentException("Config name is required.", nameof(name));

		var args = new List<string> { "config", "-z" };

		if ( string.IsNullOrEmpty(path) ) {
			args.Add("--global");
		} else if ( onlyLocal ) {
			args.Add("--local");
		}

		if ( type.HasValue ) {
			args.Add("--type");
			args.Add(TypeToString(type.Value));
		}

		args.Add(name);

		var successCodes = new HashSet<int> { 0, 1 };
		var workingDir = string.IsNullOrEmpty(path) ? Environment.CurrentDirectory : path;

		var result = await Core.GitAsync(
			args.ToArray(),
			workingDir,
			OperationGetConfigValueInPath,
			successCodes
		).ConfigureAwait(false);

		if ( result.ExitCode == 1 )
			return string.Empty;

		var output = result.Stdout ?? string.Empty;
		var pieces = output.Split('\0');
		return pieces.Length > 0 ? pieces[0] : string.Empty;
	}

	static Task SetConfigValueInPathAsync(
		string name,
		string value,
		string path
	) {
		if ( string.IsNullOrWhiteSpace(name) )
			throw new ArgumentException("Config name is required.", nameof(name));

		var args = new List<string> { "config" };

		if ( string.IsNullOrEmpty(path) ) {
			args.Add("--global");
		}

		args.Add("--replace-all");
		args.Add(name);
		args.Add(value);

		var workingDir = string.IsNullOrEmpty(path) ? Environment.CurrentDirectory : path;
		return Core.GitAsync(
			args.ToArray(),
			workingDir,
			OperationSetConfigValueInPath
		);
	}

	static Task RemoveConfigValueInPathAsync(
		string name,
		string path
	) {
		if ( string.IsNullOrWhiteSpace(name) )
			throw new ArgumentException("Config name is required.", nameof(name));

		var args = new List<string> { "config" };

		if ( string.IsNullOrEmpty(path) ) {
			args.Add("--global");
		}

		args.Add("--unset-all");
		args.Add(name);

		var workingDir = string.IsNullOrEmpty(path) ? Environment.CurrentDirectory : path;
		return Core.GitAsync(
			args.ToArray(),
			workingDir,
			OperationRemoveConfigValueInPath
		);
	}

	static string TypeToString(ConfigValueType type) =>
		type switch {
			ConfigValueType.Bool => "bool",
			ConfigValueType.Int => "int",
			ConfigValueType.BoolOrInt => "bool-or-int",
			ConfigValueType.Path => "path",
			ConfigValueType.ExpiryDate => "expiry-date",
			ConfigValueType.Color => "color",
			_ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
		};
}
