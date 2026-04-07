#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Sandbox.git.models;

namespace Sandbox.git;

/// <summary>
/// Result of querying files changed in a commit (e.g. from getChangedFiles).
/// </summary>
public sealed class ChangesetData {
	public IReadOnlyList<CommittedFileChange> Files { get; }
	public int LinesAdded { get; }
	public int LinesDeleted { get; }

	public ChangesetData(
		IReadOnlyList<CommittedFileChange> files,
		int linesAdded,
		int linesDeleted) {
		Files = files;
		LinesAdded = linesAdded;
		LinesDeleted = linesDeleted;
	}
}

/// <summary>
/// Log operations: get commits, single commit, and changed files.
/// Loosely mirrors Desktop's log.ts while fitting SandGit's Core wrapper.
/// </summary>
public static class Log {
	const string OperationGetCommits = "getCommits";
	const string OperationGetChangedFiles = "getChangedFiles";

	// File mode 160000 is used by git for submodules.
	const string SubmoduleFileMode = "160000";

	// Max length for summary/body when parsing (match Desktop).
	const int MaxSummaryBodyBytes = 100 * 1024;

	static readonly IReadOnlySet<int> SuccessExitCodesLog = new HashSet<int> { 0, 128 };

	// ─── Public API ─────────────────────────────────────────────────────────

	/// <summary>
	/// Gets the repository's commits for the given revision range, limited and optionally skipped.
	/// </summary>
	/// <param name="repository">The repository.</param>
	/// <param name="revisionRange">Optional revision range (e.g. branch name, HEAD~5..HEAD).</param>
	/// <param name="limit">Max number of commits to return.</param>
	/// <param name="skip">Number of commits to skip (pagination).</param>
	/// <param name="additionalArgs">Extra args to pass to git log.</param>
	/// <returns>List of commits, or empty if unborn HEAD (exit 128).</returns>
	public static async Task<IReadOnlyList<models.Commit>> GetCommitsAsync(
		Repository repository,
		string? revisionRange = null,
		int? limit = null,
		int? skip = null,
		IReadOnlyList<string>? additionalArgs = null
	) {
		if ( repository == null )
			throw new ArgumentNullException(nameof(repository));

		var args = new List<string> { "log", "--date=raw", "--no-show-signature", "--no-color" };

		if ( !string.IsNullOrEmpty(revisionRange) )
			args.Add(revisionRange);

		if ( limit.HasValue )
			args.Add($"--max-count={limit.Value}");

		if ( skip.HasValue )
			args.Add($"--skip={skip.Value}");

		// Match Desktop git-delimiter-parser: single delimiter between fields. Trailing %x01 after %D so refs and next commit's SHA are separated (git appends next %H after %D with no delimiter).
		// Order: sha, shortSha, summary, body, author, committer, parents, refs. Use 0x01 to avoid null stripping on Windows.
		args.Add(
			"--format=format:%H%x01%h%x01%s%x01%b%x01%an <%ae> %ad%x01%cn <%ce> %cd%x01%P%x01%D%x01");

		if ( additionalArgs != null && additionalArgs.Count > 0 )
			args.AddRange(additionalArgs);

		args.Add("--");

		var result = await Core.GitAsync(
			args.ToArray(),
			repository.Path,
			OperationGetCommits,
			SuccessExitCodesLog
		).ConfigureAwait(false);

		if ( result.ExitCode == 128 )
			return Array.Empty<models.Commit>();

		return ParseLogOutput(result.Stdout);
	}

	/// <summary>
	/// Gets the single commit for the given ref (e.g. HEAD, branch name, SHA).
	/// </summary>
	public static async Task<models.Commit?> GetCommitAsync(Repository repository, string refName) {
		if ( repository == null )
			throw new ArgumentNullException(nameof(repository));
		if ( string.IsNullOrWhiteSpace(refName) )
			throw new ArgumentException("Ref is required.", nameof(refName));

		var commits = await GetCommitsAsync(repository, revisionRange: refName, limit: 1).ConfigureAwait(false);
		return commits.Count > 0 ? commits[0] : null;
	}

	/// <summary>
	/// Gets the author identity for the given SHAs (mirrors Desktop getAuthors).
	/// Uses git log --no-walk=unsorted --format=... -z --stdin. Duplicate SHAs may produce fewer results.
	/// </summary>
	public static async Task<IReadOnlyList<CommitIdentity>> GetAuthorsAsync(Repository repository,
		IReadOnlyList<string> shas) {
		if ( repository == null )
			throw new ArgumentNullException(nameof(repository));
		if ( shas.Count == 0 )
			return Array.Empty<CommitIdentity>();

		var stdin = string.Join("\n", shas);
		var args = new[] {
			"log", "--format=format:%an <%ae> %ad", "--no-walk=unsorted", "--date=raw", "-z", "--stdin"
		};

		var result = await Core.GitAsync(
			args,
			repository.Path,
			"getAuthors",
			stdin: stdin
		).ConfigureAwait(false);

		var parts = result.Stdout.Split('\0');
		var list = new List<CommitIdentity>();
		foreach ( var part in parts ) {
			if ( string.IsNullOrWhiteSpace(part) )
				continue;
			list.Add(CommitIdentity.Parse(part));
		}

		return list;
	}

	/// <summary>
	/// Gets the files changed in the given commit and line stats.
	/// Uses -C -M for copy/rename detection.
	/// </summary>
	public static async Task<ChangesetData> GetChangedFilesAsync(Repository repository, string sha) {
		if ( repository == null )
			throw new ArgumentNullException(nameof(repository));
		if ( string.IsNullOrWhiteSpace(sha) )
			throw new ArgumentException("SHA is required.", nameof(sha));

		var args = new[] {
			"log", sha, "-C", "-M", "-m", "-1", "--no-show-signature", "--first-parent", "--raw", "--format=format:",
			"--numstat", "-z", "--"
		};

		var result = await Core.GitAsync(
			args,
			repository.Path,
			OperationGetChangedFiles
		).ConfigureAwait(false);

		var parentCommitish = $"{sha}^";
		return ParseRawLogWithNumstat(result.Stdout, sha, parentCommitish);
	}

	// ─── Private: parse log output ───────────────────────────────────────────
	// Matches Desktop createLogParser: single delimiter between fields, no record separator.
	// Parse as flat list: split by delimiter, then take every LogFormatFieldCount consecutive segments as one commit.
	const int LogFormatFieldCount = 8;

	static IReadOnlyList<models.Commit> ParseLogOutput(string stdout) {
		if ( string.IsNullOrEmpty(stdout) )
			return Array.Empty<models.Commit>();

		var commits = new List<models.Commit>();
		var seenShas = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		var parts = stdout.Split('\x01');

		// Desktop: take every 8 consecutive segments as one commit. When a field contains 0x01 we get extra segments and desync;
		// on validation failure, advance by 1 to resync instead of 8, and skip duplicate SHAs.
		for ( var i = 0; i + LogFormatFieldCount <= parts.Length; ) {
			var sha = parts[i].Trim();
			var shortSha = parts[i + 1].Trim();
			if ( sha.Length != 40 || !System.Text.RegularExpressions.Regex.IsMatch(sha, @"^[0-9a-fA-F]{40}$") ) {
				i++;
				continue;
			}

			if ( shortSha.Length == 0 )
				shortSha = sha.Substring(0, 7);
			else if ( shortSha.Length > 7 )
				shortSha = shortSha.Substring(0, 7);

			// Reject misaligned rows: %h is abbreviation of %H, so full sha must start with short sha (avoids "just now" when delimiter in body shifted fields)
			if ( !sha.StartsWith(shortSha, StringComparison.OrdinalIgnoreCase) ) {
				i++;
				continue;
			}

			var summary = TruncateToMaxUtf8Bytes(parts[i + 2], MaxSummaryBodyBytes);
			var body = TruncateToMaxUtf8Bytes(parts[i + 3], MaxSummaryBodyBytes);
			var authorStr = parts[i + 4];
			var committerStr = parts[i + 5];
			var parentsStr = parts[i + 6];
			var refsStr = parts[i + 7];

			// Author must look like "Name <email> timestamp timezone"; otherwise we're likely showing summary/body as author (→ UtcNow = "just now")
			if ( !LooksLikeAuthorIdentity(authorStr) ) {
				i++;
				continue;
			}

			if ( seenShas.Add(sha) ) {
				var author = CommitIdentity.Parse(authorStr);
				var committer = CommitIdentity.Parse(committerStr);
				var parentShAs = string.IsNullOrWhiteSpace(parentsStr)
					? Array.Empty<string>()
					: parentsStr.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
				var trailers = Array.Empty<ITrailer>();
				var tags = ParseTagsFromRefs(refsStr);
				commits.Add(new models.Commit(sha, shortSha, summary, body, author, committer, parentShAs, trailers,
					tags));
			}

			i += LogFormatFieldCount;
		}

		return commits;
	}

	/// <summary>True if string looks like "Name &lt;email&gt; unixTimestamp timezone" (author/committer from --date=raw).</summary>
	static bool LooksLikeAuthorIdentity(string? s) {
		if ( string.IsNullOrWhiteSpace(s) ) return false;
		var idx = s.IndexOf('>');
		if ( idx < 0 || s.IndexOf('<') < 0 ) return false;
		var after = s.Substring(idx + 1).Trim();
		// Require at least a timestamp (digit sequence) after "Name <email>" to avoid treating summary/body as author
		return after.Length > 0 && after[0] >= '0' && after[0] <= '9';
	}

	/// <summary>Extract tag names from %D refs (e.g. "HEAD -> main, tag: v1.0"). Split on ", "; tags with commas in name may be clipped (Desktop same).</summary>
	static IReadOnlyList<string> ParseTagsFromRefs(string refs) {
		if ( string.IsNullOrWhiteSpace(refs) )
			return Array.Empty<string>();

		return refs
			.Split(new[] { ", " }, StringSplitOptions.RemoveEmptyEntries)
			.Where(r => r.StartsWith("tag: ", StringComparison.Ordinal))
			.Select(r => r.Substring(5))
			.ToList();
	}

	static string TruncateToMaxUtf8Bytes(string value, int maxBytes) {
		if ( string.IsNullOrEmpty(value) || maxBytes <= 0 )
			return string.Empty;

		var enc = System.Text.Encoding.UTF8;
		if ( enc.GetByteCount(value) <= maxBytes )
			return value;

		for ( var n = value.Length; n > 0; n-- ) {
			var s = value.Substring(0, n);
			if ( enc.GetByteCount(s) <= maxBytes )
				return s;
		}

		return string.Empty;
	}

	// ─── Private: changed files and numstat ───────────────────────────────────

	/// <summary>
	/// Parses output of git log with -z --raw --numstat into files and line stats.
	/// </summary>
	public static ChangesetData ParseRawLogWithNumstat(string stdout, string sha, string parentCommitish) {
		var files = new List<CommittedFileChange>();
		var linesAdded = 0;
		var linesDeleted = 0;
		var numStatCount = 0;

		var lines = stdout.Split('\0');

		for ( var i = 0; i < lines.Length - 1; i++ ) {
			var line = lines[i];
			if ( line.StartsWith(":", StringComparison.Ordinal) ) {
				var parts = line.Split(' ');
				if ( parts.Length < 4 )
					continue;

				var srcMode = parts[0].TrimStart(':');
				var dstMode = parts[1];
				var status = parts[parts.Length - 1].Trim();
				string? oldPath = null;
				if ( status.Length > 0 && (status[0] == 'R' || status[0] == 'C') ) {
					i++;
					if ( i < lines.Length )
						oldPath = lines[i];
				}

				i++;
				var path = i < lines.Length ? lines[i] : null;
				if ( string.IsNullOrEmpty(path) )
					continue;

				var appStatus = MapStatus(status, oldPath, srcMode, dstMode);
				files.Add(new CommittedFileChange(path, appStatus, sha, parentCommitish));
			} else {
				var match = System.Text.RegularExpressions.Regex.Match(line, @"^(\d+|-)\t(\d+|-)\t");
				if ( match.Success && match.Groups.Count >= 3 ) {
					var addedStr = match.Groups[1].Value;
					var deletedStr = match.Groups[2].Value;
					linesAdded += addedStr == "-"
						? 0
						: int.Parse(addedStr, System.Globalization.CultureInfo.InvariantCulture);
					linesDeleted += deletedStr == "-"
						? 0
						: int.Parse(deletedStr, System.Globalization.CultureInfo.InvariantCulture);

					if ( numStatCount < files.Count && IsCopyOrRename(files[numStatCount].Status) )
						i += 2;
					numStatCount++;
				}
			}
		}

		return new ChangesetData(files, linesAdded, linesDeleted);
	}

	static SubmoduleStatus? MapSubmoduleStatusFileModes(string status, string srcMode, string dstMode) {
		if ( srcMode == SubmoduleFileMode && dstMode == SubmoduleFileMode && status == "M" )
			return new SubmoduleStatus { CommitChanged = true, UntrackedChanges = false, ModifiedChanges = false };
		if ( (srcMode == SubmoduleFileMode && status == "D") || (dstMode == SubmoduleFileMode && status == "A") )
			return new SubmoduleStatus { CommitChanged = false, UntrackedChanges = false, ModifiedChanges = false };
		return null;
	}

	/// <summary>
	/// Maps raw status from git log --raw to app-friendly AppFileStatus (from Desktop).
	/// </summary>
	static AppFileStatus MapStatus(string rawStatus, string? oldPath, string srcMode, string dstMode) {
		var status = rawStatus.Trim();
		var submoduleStatus = MapSubmoduleStatusFileModes(status, srcMode, dstMode);

		if ( status == "M" ) return new PlainFileStatus(AppFileStatusKind.Modified, submoduleStatus);
		if ( status == "A" ) return new PlainFileStatus(AppFileStatusKind.New, submoduleStatus);
		if ( status == "?" ) return new UntrackedFileStatus(submoduleStatus);
		if ( status == "D" ) return new PlainFileStatus(AppFileStatusKind.Deleted, submoduleStatus);
		if ( status == "R" && !string.IsNullOrEmpty(oldPath) )
			return new CopiedOrRenamedFileStatus(AppFileStatusKind.Renamed, oldPath, false, submoduleStatus);
		if ( status == "C" && !string.IsNullOrEmpty(oldPath) )
			return new CopiedOrRenamedFileStatus(AppFileStatusKind.Copied, oldPath, false, submoduleStatus);

		if ( System.Text.RegularExpressions.Regex.IsMatch(status, @"^R[0-9]+") && !string.IsNullOrEmpty(oldPath) )
			return new CopiedOrRenamedFileStatus(AppFileStatusKind.Renamed, oldPath, status != "R100", submoduleStatus);
		if ( System.Text.RegularExpressions.Regex.IsMatch(status, @"^C[0-9]+") && !string.IsNullOrEmpty(oldPath) )
			return new CopiedOrRenamedFileStatus(AppFileStatusKind.Copied, oldPath, false, submoduleStatus);

		return new PlainFileStatus(AppFileStatusKind.Modified, submoduleStatus);
	}

	static bool IsCopyOrRename(AppFileStatus status) {
		return status.Kind == AppFileStatusKind.Copied || status.Kind == AppFileStatusKind.Renamed;
	}
}
