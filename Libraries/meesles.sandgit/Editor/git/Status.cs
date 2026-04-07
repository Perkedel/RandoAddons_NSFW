#nullable enable
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Sandbox.git.models;

namespace Sandbox.git;

/// <summary>
/// Repository status (branch info and ahead/behind from git status --porcelain=2 --branch).
/// </summary>
public static class Status {
	const string OperationGetStatus = "getStatus";
	const string OperationGetFullStatus = "getFullStatus";

	static readonly IReadOnlySet<int> SuccessExitCodesStatus = new HashSet<int> { 0, 128 };

	// Conflict status codes (index + work tree) from Desktop status-parser
	static readonly HashSet<string> ConflictStatusCodes = new HashSet<string>(StringComparer.Ordinal) {
		"DD",
		"AU",
		"UD",
		"UA",
		"DU",
		"AA",
		"UU"
	};

	// branch.oid (initial) or branch.oid <sha>
	static readonly Regex BranchOidRe = new Regex(@"^#\s+branch\.oid\s+(.+)$", RegexOptions.Compiled);

	// branch.head <name> or (detached)
	static readonly Regex BranchHeadRe = new Regex(@"^#\s+branch\.head\s+(.+)$", RegexOptions.Compiled);

	// branch.upstream <name>
	static readonly Regex BranchUpstreamRe = new Regex(@"^#\s+branch\.upstream\s+(.+)$", RegexOptions.Compiled);

	// branch.ab +<ahead> -<behind>
	static readonly Regex BranchAbRe = new Regex(@"^#\s+branch\.ab\s+\+(\d+)\s+-(\d+)$", RegexOptions.Compiled);

	/// <summary>
	/// Load status for the repository. Returns branch name, tip SHA, upstream, and ahead/behind.
	/// Returns null if the path is not a repository (exit 128).
	/// </summary>
	public static async Task<StatusResult?> GetStatusAsync(Repository repository) {
		if ( repository == null )
			throw new ArgumentNullException(nameof(repository));

		var result = await Core.GitAsync(
			GetStatusArgs(),
			repository.Path,
			OperationGetStatus,
			SuccessExitCodesStatus
		).ConfigureAwait(false);

		if ( result.ExitCode == 128 )
			return null;

		return ParseStatusOutput(result.Stdout);
	}

	/// <summary>
	/// Load status using repository path (e.g. from RevParse). Returns null if not a repo.
	/// </summary>
	public static async Task<StatusResult?> GetStatusAsync(string path) {
		if ( string.IsNullOrEmpty(path) )
			return null;

		var result = await Core.GitAsync(
			GetStatusArgs(),
			path,
			OperationGetStatus,
			SuccessExitCodesStatus
		).ConfigureAwait(false);

		if ( result.ExitCode == 128 )
			return null;

		return ParseStatusOutput(result.Stdout);
	}

	/// <summary>
	/// Load full status (branch info + working directory file changes). Uses porcelain=2 -z and includes untracked files.
	/// Returns null if the path is not a repository (exit 128).
	/// </summary>
	public static async Task<FullStatusResult?> GetFullStatusAsync(string path) {
		if ( string.IsNullOrEmpty(path) )
			return null;

		var result = await Core.GitAsync(
			GetFullStatusArgs(),
			path,
			OperationGetFullStatus,
			SuccessExitCodesStatus
		).ConfigureAwait(false);

		if ( result.ExitCode == 128 )
			return null;

		return ParseFullStatusOutput(result.Stdout);
	}

	/// <summary>Builds git arguments for status (branch + porcelain=2). Exposed for testing.</summary>
	public static string[] GetStatusArgs() {
		return new[] { "status", "--branch", "--porcelain=2" };
	}

	/// <summary>Builds git arguments for full status (with untracked, -z). Exposed for testing.</summary>
	public static string[] GetFullStatusArgs() {
		return new[] { "--no-optional-locks", "status", "--untracked-files=all", "--branch", "--porcelain=2", "-z" };
	}

	static FullStatusResult ParseFullStatusOutput(string stdout) {
		// Strip BOM if present (can cause header lines to be missed and branch to show as detached)
		if ( stdout.Length > 0 && stdout[0] == '\uFEFF' )
			stdout = stdout.Substring(1);

		// In -z mode, NUL terminates each LINE (record), not each field. Split once to get records (Desktop: splitBuffer(output, '\0')).
		var records = stdout.Split('\0');
		string? currentBranch = null;
		string? currentTip = null;
		string? currentUpstream = null;
		IAheadBehind? aheadBehind = null;
		var files = new List<GitWorkingDirectoryFileChange>();

		for ( var i = 0; i < records.Length; i++ ) {
			var record = records[i];
			if ( string.IsNullOrEmpty(record) )
				continue;

			// Header lines: "# branch.oid xxx", "# branch.head main", etc. (Desktop: field.startsWith('# ') && field.length > 2)
			if ( record.StartsWith("# ", StringComparison.Ordinal) && record.Length > 2 ) {
				var oidMatch = BranchOidRe.Match(record);
				if ( oidMatch.Success ) {
					var oid = oidMatch.Groups[1].Value.Trim();
					if ( oid != "(initial)" )
						currentTip = oid;
					continue;
				}

				var headMatch = BranchHeadRe.Match(record);
				if ( headMatch.Success ) {
					var head = headMatch.Groups[1].Value.Trim();
					if ( head != "(detached)" )
						currentBranch = head;
					continue;
				}

				var upstreamMatch = BranchUpstreamRe.Match(record);
				if ( upstreamMatch.Success ) {
					currentUpstream = upstreamMatch.Groups[1].Value.Trim();
					continue;
				}

				var abMatch = BranchAbRe.Match(record);
				if ( abMatch.Success
				     && int.TryParse(abMatch.Groups[1].Value, out var ahead)
				     && int.TryParse(abMatch.Groups[2].Value, out var behind) ) {
					aheadBehind = new AheadBehind(ahead, behind);
				}

				continue;
			}

			var entryKind = record.Length > 0 ? record.Substring(0, 1) : "";

			if ( entryKind == "1" ) {
				// 1 <XY> <sub> <mH> <mI> <mW> <hH> <hI> <path> — space-separated, path is rest after 8th field (Desktop: parseChangedEntry)
				var parsed = ParseType1Record(record);
				if ( parsed != null && !ShouldSkipEntry(parsed.Value.XY) )
					files.Add(
						new GitWorkingDirectoryFileChange(parsed.Value.Path, MapStatusCode(parsed.Value.XY), null));
				continue;
			}

			if ( entryKind == "2" ) {
				// 2 <XY> ... <path> then next record is origPath (Desktop: parsedRenamedOrCopiedEntry(field, tokens[++i]))
				var parsed = ParseType2Record(record);
				if ( parsed != null && i + 1 < records.Length ) {
					var origPath = records[++i].TrimEnd('\r');
					if ( !ShouldSkipEntry(parsed.Value.XY) )
						files.Add(new GitWorkingDirectoryFileChange(parsed.Value.Path, MapStatusCode(parsed.Value.XY),
							origPath));
				}

				continue;
			}

			if ( entryKind == "?" ) {
				// ? <path> — path is from position 2 (Desktop: parseUntrackedEntry, path = field.substring(2))
				var path = record.Length > 2 ? record.Substring(2).TrimEnd('\r') : "";
				if ( path.Length > 0 )
					files.Add(new GitWorkingDirectoryFileChange(path, FileChangeKind.Untracked, null));
				continue;
			}

			if ( entryKind == "!" ) {
				// Ignored — skip (Desktop: "we don't care about these for now")
			}
		}

		var workingDirectory = new GitWorkingDirectoryStatus(files);
		return new FullStatusResult(
			currentBranch,
			currentTip,
			currentUpstream,
			aheadBehind,
			workingDirectory
		);
	}

	/// <summary>Parsed type-1 (ordinary) entry: 1 XY sub mH mI mW hH hI path (space-separated, path may contain spaces).</summary>
	static (string XY, string Path)? ParseType1Record(string record) {
		// Desktop: /^1 ([MADRCUTX?!.]{2}) (N\.\.\.|S[C.][M.][U.]) (\d+) (\d+) (\d+) ([a-f0-9]+) ([a-f0-9]+) ([\s\S]*?)$/
		var parts = record.Split(new[] { ' ' }, 9,
			StringSplitOptions.None); // at most 9 parts, last is path (may contain spaces)
		if ( parts.Length < 9 )
			return null;
		var xy = parts[1].Length >= 2 ? parts[1] : parts[1].PadRight(2, ' ');
		return (xy, parts[8]);
	}

	/// <summary>Parsed type-2 (rename/copy) entry: 2 XY sub mH mI mW hH hI R100 path (origPath is next record).</summary>
	static (string XY, string Path)? ParseType2Record(string record) {
		var parts = record.Split(new[] { ' ' }, 10, StringSplitOptions.None); // 9 fields + path
		if ( parts.Length < 10 )
			return null;
		var xy = parts[1].Length >= 2 ? parts[1] : parts[1].PadRight(2, ' ');
		return (xy, parts[9]);
	}

	/// <summary>Desktop: when added in index but deleted in work tree, skip (file won't be in commit).</summary>
	static bool ShouldSkipEntry(string xy) {
		if ( xy.Length < 2 ) return false;
		var x = xy[0];
		var y = xy.Length > 1 ? xy[1] : ' ';
		return x == 'A' && y == 'D';
	}

	/// <summary>Map two-char XY (index + work tree) to FileChangeKind. Mirrors Desktop mapStatus / convertToAppStatus.</summary>
	static FileChangeKind MapStatusCode(string xy) {
		if ( string.IsNullOrEmpty(xy) || xy.Length < 2 )
			return FileChangeKind.Modified;

		var normalized = xy.PadRight(2, ' ');
		if ( ConflictStatusCodes.Contains(normalized) )
			return FileChangeKind.Conflicted;

		var y = normalized[1]; // work tree
		if ( y == '?' )
			return FileChangeKind.Untracked;

		var x = normalized[0]; // index
		if ( x == 'R' || y == 'R' )
			return FileChangeKind.Renamed;
		if ( x == 'C' || y == 'C' )
			return FileChangeKind.Copied;
		if ( x == 'A' || y == 'A' )
			return FileChangeKind.New;
		if ( x == 'M' || y == 'M' )
			return FileChangeKind.Modified;
		if ( x == 'D' || y == 'D' )
			return FileChangeKind.Deleted;

		return FileChangeKind.Modified;
	}

	static StatusResult ParseStatusOutput(string stdout) {
		string? currentBranch = null;
		string? currentTip = null;
		string? currentUpstream = null;
		IAheadBehind? aheadBehind = null;

		foreach ( var line in ReadHeaderLines(stdout) ) {
			var oidMatch = BranchOidRe.Match(line);
			if ( oidMatch.Success ) {
				var oid = oidMatch.Groups[1].Value.Trim();
				if ( oid != "(initial)" )
					currentTip = oid;
				continue;
			}

			var headMatch = BranchHeadRe.Match(line);
			if ( headMatch.Success ) {
				var head = headMatch.Groups[1].Value.Trim();
				if ( head != "(detached)" )
					currentBranch = head;
				continue;
			}

			var upstreamMatch = BranchUpstreamRe.Match(line);
			if ( upstreamMatch.Success ) {
				currentUpstream = upstreamMatch.Groups[1].Value.Trim();
				continue;
			}

			var abMatch = BranchAbRe.Match(line);
			if ( abMatch.Success
			     && int.TryParse(abMatch.Groups[1].Value, out var ahead)
			     && int.TryParse(abMatch.Groups[2].Value, out var behind) ) {
				aheadBehind = new AheadBehind(ahead, behind);
			}
		}

		return new StatusResult(currentBranch, currentTip, currentUpstream, aheadBehind);
	}

	static IEnumerable<string> ReadHeaderLines(string stdout) {
		if ( string.IsNullOrEmpty(stdout) )
			yield break;

		foreach ( var line in stdout.Split('\n') ) {
			var trimmed = line.TrimEnd('\r');
			if ( !trimmed.StartsWith("# ", StringComparison.Ordinal) )
				break;
			yield return trimmed;
		}
	}
}
