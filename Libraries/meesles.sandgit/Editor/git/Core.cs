using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Sandbox.Diagnostics;

namespace Sandbox.git;

public readonly struct GitResult {
	public int ExitCode { get; }
	public string Stdout { get; }
	public string Stderr { get; }

	public GitResult(int exitCode, string stdout, string stderr) {
		ExitCode = exitCode;
		Stdout = stdout ?? string.Empty;
		Stderr = stderr ?? string.Empty;
	}
}

public static class Core {
	private static readonly Logger Logger = new Logger("SandGit[Git]");
	private static string _gitExe;

	/// <summary>Returns the git executable path currently in use (for display in settings). Resolves and caches if needed.</summary>
	public static string GetCurrentGitPath() => GetGitExe();

	/// <summary>Sets a user override for the git executable path. Use null or empty to clear and fall back to detection.</summary>
	public static void SetGitPathOverride(string path) {
		_gitExe = null;
		try {
			var file = GetGitOverrideFile();
			var dir = Path.GetDirectoryName(file);
			if ( !string.IsNullOrEmpty(dir) )
				Directory.CreateDirectory(dir);
			File.WriteAllText(file, path ?? string.Empty);
		} catch {
			// Intentionally ignore.
		}
	}

	/// <summary>
	/// Runs a git operation with the given args, repository path, and operation name.
	/// </summary>
	public static void Git(string[] args, string path, string operationName) {
		_ = GitAsync(args, path, operationName).GetAwaiter().GetResult();
	}

	/// <summary>
	/// Runs a git operation asynchronously. Non-blocking.
	/// </summary>
	/// <param name="args">Git arguments (e.g. ["rev-parse", "--is-bare-repository"]).</param>
	/// <param name="path">Working directory (repository path).</param>
	/// <param name="operationName">Name used for logging.</param>
	/// <param name="successExitCodes">If set, exit codes in this set are treated as success; otherwise only 0 is success and other codes throw.</param>
	/// <param name="stdin">Optional input to send on stdin (e.g. null-separated paths for checkout-index --stdin -z).</param>
	/// <returns>Exit code, stdout, and stderr.</returns>
	public static async Task<GitResult> GitAsync(
		string[] args,
		string path,
		string operationName,
		IReadOnlySet<int> successExitCodes = null,
		string stdin = null
	) {
		using var process = new Process();
		process.StartInfo.FileName = GetGitExe();
		process.StartInfo.Arguments = BuildArguments(args);
		process.StartInfo.WorkingDirectory = path;
		process.StartInfo.RedirectStandardOutput = true;
		process.StartInfo.RedirectStandardError = true;
		process.StartInfo.UseShellExecute = false;
		process.StartInfo.CreateNoWindow = true;
		process.StartInfo.StandardOutputEncoding = Encoding.UTF8;
		process.StartInfo.StandardErrorEncoding = Encoding.UTF8;
		if ( stdin != null ) {
			process.StartInfo.RedirectStandardInput = true;
			process.StartInfo.StandardInputEncoding = Encoding.UTF8;
		}

		var gitCommand = path + ": " + process.StartInfo.FileName + " " + string.Join(" ", args);
		Logger.Trace(gitCommand);

		process.Start();
		if ( stdin != null ) {
			process.StandardInput.Write(stdin);
			process.StandardInput.Close();
		}

		var outTask = process.StandardOutput.ReadToEndAsync();
		var errTask = process.StandardError.ReadToEndAsync();
		await process.WaitForExitAsync().ConfigureAwait(false);
		var stdout = await outTask.ConfigureAwait(false);
		var stderr = await errTask.ConfigureAwait(false);

		Logger.Trace(gitCommand + " - " + process.ExitCode + " - " + stdout + " - " + stderr);

		var result = new GitResult(process.ExitCode, stdout, stderr);

		if ( successExitCodes != null && !successExitCodes.Contains(result.ExitCode) ) {
			throw new GitException(result, operationName);
		}

		if ( successExitCodes == null && result.ExitCode != 0 ) {
			throw new GitException(result, operationName);
		}

		return result;
	}

	static string GetGitExe() {
		// 0) User override from settings
		var ov = GetOverridePath();
		if ( !string.IsNullOrWhiteSpace(ov) )
			return _gitExe = ov;

		if ( _gitExe != null )
			return _gitExe;

		// 1) Saved path from previous successful launch
		var saved = GetSavedGitPath();
		if ( !string.IsNullOrEmpty(saved) && TryGitAtPath(saved) )
			return _gitExe = SaveAndReturn(saved);

		// 2) PATH
		if ( TryGitAtPath("git") )
			return _gitExe = "git";

		// 3) Common Windows installation paths (one at a time)
		var commonPaths = new[] {
			@"C:\Program Files\Git\cmd\git.exe", @"C:\Program Files\Git\bin\git.exe",
			@"C:\Program Files (x86)\Git\cmd\git.exe", @"C:\Program Files (x86)\Git\bin\git.exe", Path.Combine(
				Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
				@"Programs\Git\cmd\git.exe"),
		};
		foreach ( var candidate in commonPaths ) {
			if ( !File.Exists(candidate) )
				continue;
			if ( TryGitAtPath(candidate) )
				return _gitExe = SaveAndReturn(candidate);
		}

		// 4) where.exe to discover git
		var wherePath = TryWhereGit();
		if ( !string.IsNullOrEmpty(wherePath) && TryGitAtPath(wherePath) )
			return _gitExe = SaveAndReturn(wherePath);

		// 5) Last resort
		return _gitExe = "git";
	}

	/// <summary>Runs git --version at the given path; returns true if exit code 0.</summary>
	static bool TryGitAtPath(string exePath) {
		try {
			using var p = new Process();
			p.StartInfo.FileName = exePath;
			p.StartInfo.Arguments = "--version";
			p.StartInfo.UseShellExecute = false;
			p.StartInfo.RedirectStandardOutput = true;
			p.StartInfo.RedirectStandardError = true;
			p.StartInfo.CreateNoWindow = true;
			p.Start();
			p.WaitForExit(5000);
			return p.ExitCode == 0;
		} catch {
			// Intentionally ignore; try next candidate.
			return false;
		}
	}

	static string SaveAndReturn(string path) {
		if ( path != null && (path.Contains(Path.DirectorySeparatorChar) || path.Contains('/')) )
			SaveGitPath(path);
		return path;
	}

	static string GetSavedGitPath() {
		try {
			var file = GetGitPathCacheFile();
			if ( File.Exists(file) )
				return File.ReadAllText(file).Trim();
		} catch {
			// Intentionally ignore; fall back to detection.
		}

		return null;
	}

	static void SaveGitPath(string path) {
		try {
			var file = GetGitPathCacheFile();
			var dir = Path.GetDirectoryName(file);
			if ( !string.IsNullOrEmpty(dir) )
				Directory.CreateDirectory(dir);
			File.WriteAllText(file, path);
		} catch {
			// Intentionally ignore; in-memory cache still used.
		}
	}

	static string GetGitPathCacheFile() =>
		Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SandGit",
			"git-exe.txt");

	static string GetGitOverrideFile() =>
		Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SandGit",
			"git-override.txt");

	static string GetOverridePath() {
		try {
			var file = GetGitOverrideFile();
			if ( File.Exists(file) ) {
				var s = File.ReadAllText(file).Trim();
				if ( !string.IsNullOrEmpty(s) )
					return s;
			}
		} catch {
			// Intentionally ignore.
		}

		return null;
	}

	static string TryWhereGit() {
		try {
			var whereExe = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "where.exe");
			if ( !File.Exists(whereExe) )
				return null;
			using var p = new Process();
			p.StartInfo.FileName = whereExe;
			p.StartInfo.Arguments = "git";
			p.StartInfo.UseShellExecute = false;
			p.StartInfo.RedirectStandardOutput = true;
			p.StartInfo.CreateNoWindow = true;
			p.Start();
			var line = p.StandardOutput.ReadLine()?.Trim();
			p.WaitForExit(5000);
			if ( !string.IsNullOrEmpty(line) && File.Exists(line) )
				return line;
		} catch {
			// Intentionally ignore; fall back to "git".
		}

		return null;
	}

	static string BuildArguments(string[] args) {
		if ( args == null || args.Length == 0 ) return string.Empty;
		var sb = new StringBuilder();
		foreach ( var a in args ) {
			if ( sb.Length > 0 ) sb.Append(' ');
			if ( a.Contains(" ") ) {
				sb.Append('"').Append(a.Replace("\"", "\\\"")).Append('"');
			} else {
				sb.Append(a);
			}
		}

		return sb.ToString();
	}
}

public class GitException : Exception {
	public GitResult Result { get; }

	public GitException(GitResult result, string operationName)
		: base($"Git {operationName} failed with exit code {result.ExitCode}. stderr: {result.Stderr}") {
		Result = result;
	}
}
