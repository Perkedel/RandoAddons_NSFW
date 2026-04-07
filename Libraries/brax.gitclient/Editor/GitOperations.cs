using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace Editor;

/// <summary>
/// Represents a single changed file from git status.
/// </summary>
public class GitFileEntry
{
	/// <summary>Relative file path from repo root.</summary>
	public string FilePath { get; init; }

	/// <summary>Staged status character (X in XY porcelain format).</summary>
	public char StagedStatus { get; init; }

	/// <summary>Unstaged status character (Y in XY porcelain format).</summary>
	public char UnstagedStatus { get; init; }

	/// <summary>Whether this entry represents staged changes.</summary>
	public bool IsStaged { get; init; }

	/// <summary>Short display label for the change type.</summary>
	public string StatusLabel => IsStaged
		? StagedStatus switch
		{
			'A' => "A",
			'M' => "M",
			'D' => "D",
			'R' => "R",
			'C' => "C",
			_ => "?"
		}
		: UnstagedStatus switch
		{
			'M' => "M",
			'D' => "D",
			'?' => "?",
			_ => "~"
		};

	/// <summary>Color associated with the status type.</summary>
	public Color StatusColor => IsStaged
		? StagedStatus switch
		{
			'A' => Color.Parse( "#98c379" ) ?? Color.Green,
			'M' => Color.Parse( "#e5c07b" ) ?? Color.Yellow,
			'D' => Color.Parse( "#e06c75" ) ?? Color.Red,
			'R' => Color.Parse( "#56b6c2" ) ?? Color.Cyan,
			_ => Color.Gray
		}
		: UnstagedStatus switch
		{
			'M' => Color.Parse( "#e5c07b" ) ?? Color.Yellow,
			'D' => Color.Parse( "#e06c75" ) ?? Color.Red,
			'?' => Color.Parse( "#abb2bf" ) ?? Color.Gray,
			_ => Color.Gray
		};
}

/// <summary>
/// Utility class for running git commands in a subprocess.
/// </summary>
public static class GitOperations
{
	static string _gitExe;

	/// <summary>
	/// Finds the git executable. The S&amp;box editor process may not inherit the user's
	/// shell PATH, so we probe common installation locations before falling back to "git".
	/// </summary>
	static string FindGitExe()
	{
		if ( _gitExe != null ) return _gitExe;

		// Common Windows Git installation paths
		var candidates = new[]
		{
			@"C:\Program Files\Git\cmd\git.exe",
			@"C:\Program Files\Git\bin\git.exe",
			@"C:\Program Files (x86)\Git\cmd\git.exe",
			@"C:\Program Files (x86)\Git\bin\git.exe",
			Path.Combine( Environment.GetFolderPath( Environment.SpecialFolder.LocalApplicationData ),
				@"Programs\Git\cmd\git.exe" ),
		};

		foreach ( var path in candidates )
		{
			if ( File.Exists( path ) )
				return _gitExe = path;
		}

		// Ask where.exe (full path to avoid editor PATH restrictions)
		try
		{
			var psi = new ProcessStartInfo
			{
				FileName = Path.Combine( Environment.GetFolderPath( Environment.SpecialFolder.System ), "where.exe" ),
				Arguments = "git",
				UseShellExecute = false,
				RedirectStandardOutput = true,
				CreateNoWindow = true,
			};
			using var proc = Process.Start( psi );
			var line = proc.StandardOutput.ReadLine()?.Trim();
			proc.WaitForExit();

			if ( !string.IsNullOrEmpty( line ) && File.Exists( line ) )
				return _gitExe = line;
		}
		catch { }

		return _gitExe = "git"; // Last resort
	}

	/// <summary>Runs a git command and returns (exitCode, stdout, stderr).</summary>
	public static (int ExitCode, string Output, string Error) RunGit( string workingDir, string arguments )
	{
		try
		{
			var psi = new ProcessStartInfo
			{
				FileName = FindGitExe(),
				Arguments = arguments,
				WorkingDirectory = workingDir,
				UseShellExecute = false,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				CreateNoWindow = true,
				StandardOutputEncoding = Encoding.UTF8,
				StandardErrorEncoding = Encoding.UTF8,
			};

			using var process = Process.Start( psi );
			var output = process.StandardOutput.ReadToEnd();
			var error = process.StandardError.ReadToEnd();
			process.WaitForExit();

			return (process.ExitCode, output.TrimEnd(), error.Trim());
		}
		catch ( Exception ex )
		{
			return (-1, string.Empty, ex.Message);
		}
	}

	/// <summary>
	/// Walks up from <paramref name="startPath"/> to find the git repository root (.git directory).
	/// Returns null if no repo is found.
	/// </summary>
	public static string FindRepoRoot( string startPath )
	{
		var dir = startPath;
		while ( dir != null )
		{
			if ( Directory.Exists( Path.Combine( dir, ".git" ) ) )
				return dir;

			dir = Directory.GetParent( dir )?.FullName;
		}

		return null;
	}

	/// <summary>
	/// Parses `git status --porcelain` output into a list of file entries.
	/// Files with both staged and unstaged changes appear twice.
	/// Returns (entries, errorMessage) — errorMessage is null on success.
	/// </summary>
	public static (List<GitFileEntry> Entries, string Error) GetStatus( string repoPath )
	{
		var entries = new List<GitFileEntry>();
		var (code, output, error) = RunGit( repoPath, "status --porcelain -u" );

		if ( code != 0 )
		{
			var msg = string.IsNullOrWhiteSpace( error ) ? $"git exited with code {code}" : error;
			return (entries, msg);
		}

		if ( string.IsNullOrWhiteSpace( output ) )
			return (entries, null); // Clean working tree — not an error

		foreach ( var line in output.Split( '\n', StringSplitOptions.RemoveEmptyEntries ) )
		{
			// Each porcelain line is: XY<space>path  (minimum 4 chars)
			var trimmed = line.TrimEnd( '\r' );
			if ( trimmed.Length < 4 )
				continue;

			var x = trimmed[0]; // staged status
			var y = trimmed[1]; // unstaged status
			var path = trimmed[3..].Trim();

			// Handle renames: "old -> new"
			if ( path.Contains( " -> " ) )
				path = path[(path.IndexOf( " -> " ) + 4)..];

			// Staged entry (X is not space and not untracked marker)
			if ( x != ' ' && x != '?' )
			{
				entries.Add( new GitFileEntry
				{
					FilePath = path,
					StagedStatus = x,
					UnstagedStatus = y,
					IsStaged = true,
				} );
			}

			// Unstaged / untracked entry
			if ( y != ' ' || x == '?' )
			{
				entries.Add( new GitFileEntry
				{
					FilePath = path,
					StagedStatus = x,
					UnstagedStatus = y == ' ' ? '?' : y,
					IsStaged = false,
				} );
			}
		}

		return (entries, null);
	}

	/// <summary>Returns (branchNames, currentBranch).</summary>
	public static (List<string> Branches, string Current) GetBranches( string repoPath )
	{
		var branches = new List<string>();
		string current = string.Empty;

		var (_, output, _) = RunGit( repoPath, "branch --format=%(refname:short)" );
		foreach ( var line in output.Split( '\n', StringSplitOptions.RemoveEmptyEntries ) )
		{
			var name = line.Trim();
			if ( !string.IsNullOrEmpty( name ) )
				branches.Add( name );
		}

		var (_, head, _) = RunGit( repoPath, "rev-parse --abbrev-ref HEAD" );
		current = head.Trim();

		return (branches, current);
	}

	/// <summary>Stages a single file.</summary>
	public static bool Stage( string repoPath, string filePath )
	{
		var (code, _, _) = RunGit( repoPath, $"add -- \"{filePath}\"" );
		return code == 0;
	}

	/// <summary>Unstages a single file. Uses reset HEAD for compatibility with all Git versions.</summary>
	public static bool Unstage( string repoPath, string filePath )
	{
		var (code, _, _) = RunGit( repoPath, $"reset HEAD -- \"{filePath}\"" );
		return code == 0;
	}

	/// <summary>Stages all changed files.</summary>
	public static bool StageAll( string repoPath )
	{
		var (code, _, _) = RunGit( repoPath, "add -A" );
		return code == 0;
	}

	/// <summary>Unstages all staged files.</summary>
	public static bool UnstageAll( string repoPath )
	{
		var (code, _, _) = RunGit( repoPath, "reset HEAD" );
		return code == 0;
	}

	/// <summary>Creates a commit with the given message. Returns (success, output).</summary>
	public static (bool Success, string Output) Commit( string repoPath, string message )
	{
		// Pass the message via stdin (-F -) to avoid all command-line quoting issues
		// on Windows (special chars, backslashes, newlines in multi-line messages).
		try
		{
			var psi = new ProcessStartInfo
			{
				FileName = FindGitExe(),
				Arguments = "commit -F -",
				WorkingDirectory = repoPath,
				UseShellExecute = false,
				RedirectStandardInput = true,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				CreateNoWindow = true,
				StandardOutputEncoding = Encoding.UTF8,
				StandardErrorEncoding = Encoding.UTF8,
			};

			using var process = Process.Start( psi );
			process.StandardInput.Write( message );
			process.StandardInput.Close();

			var output = process.StandardOutput.ReadToEnd().Trim();
			var error = process.StandardError.ReadToEnd().Trim();
			process.WaitForExit();

			return (process.ExitCode == 0, process.ExitCode == 0 ? output : error);
		}
		catch ( Exception ex )
		{
			return (false, ex.Message);
		}
	}

	/// <summary>Pushes the current branch to origin. Returns (success, output).</summary>
	public static (bool Success, string Output) Push( string repoPath )
	{
		var (code, output, error) = RunGit( repoPath, "push" );
		return (code == 0, code == 0 ? output : error);
	}

	/// <summary>Fetches from origin. Returns true on success.</summary>
	public static bool Fetch( string repoPath )
	{
		var (code, _, _) = RunGit( repoPath, "fetch" );
		return code == 0;
	}

	/// <summary>Pulls from origin. Returns (success, output).</summary>
	public static (bool Success, string Output) Pull( string repoPath )
	{
		var (code, output, error) = RunGit( repoPath, "pull" );
		return (code == 0, code == 0 ? output : error);
	}

	/// <summary>Switches to the given branch. Returns true on success.</summary>
	public static bool SwitchBranch( string repoPath, string branch )
	{
		var (code, _, _) = RunGit( repoPath, $"checkout \"{branch}\"" );
		return code == 0;
	}

	/// <summary>Creates and switches to a new branch. Returns (success, error).</summary>
	public static (bool Success, string Error) CreateBranch( string repoPath, string branch )
	{
		var (code, _, error) = RunGit( repoPath, $"checkout -b \"{branch}\"" );
		return (code == 0, error);
	}
}
