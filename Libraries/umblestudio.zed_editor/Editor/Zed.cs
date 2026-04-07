using System;
using System.IO;
using System.Text.RegularExpressions;
using Editor;
using Microsoft.Win32;

namespace Sandbox;

[Title( "Zed" )]
public sealed class Zed : ICodeEditor
{
	private static string? _location;

	public void OpenFile( string path, int? line = null, int? column = null )
	{
		Launch( path, line, column );
	}

	public void OpenSolution()
	{
		var path = Environment.CurrentDirectory;
		Launch( path );
	}

	public void OpenAddon( Project addon )
	{
		var projectPath = addon.GetRootPath();
		Launch( $"\"{projectPath}\"" );
	}

	public bool IsInstalled() => !string.IsNullOrEmpty( GetLocation() );

	private static void Launch( string path, int? line = null, int? column = null )
	{
		var location = GetLocation();

		if ( string.IsNullOrEmpty( location ) )
			return;

		var parentDir = Directory.GetParent( location );

		var startInfo = new System.Diagnostics.ProcessStartInfo
		{
			FileName = @$"{parentDir}\bin\zed.exe",
			Arguments = $"-- \"{path}:{line}:{column}\"",
			CreateNoWindow = true,
			UseShellExecute = false,
			RedirectStandardError = true,
			RedirectStandardOutput = true
		};

		try
		{
			System.Diagnostics.Process.Start( startInfo );
		}
		catch ( Exception e )
		{
			Log.Error( $"Failed to launch Zed: {e.Message}" );
		}
	}

	[System.Diagnostics.CodeAnalysis.SuppressMessage( "Interoperability", "CA1416:Validate platform compatibility",
		Justification = "<Pending>" )]
	private static string? GetLocation()
	{
		if (_location is not null)
		    return _location;

		string? value;

		using ( var key = Registry.ClassesRoot.OpenSubKey( @"Applications\\Zed.exe\\shell\\open\\command" ) )
			value = key?.GetValue( "" )?.ToString() ?? null;

		if ( value is null )
			return null;

		var rgx = new Regex( "\"(.*)\" \".*\"", RegexOptions.IgnoreCase );
		var matches = rgx.Matches( value );

		if ( matches.Count is 0 || matches[0].Groups.Count < 2 )
			return null;

		_location = matches[0].Groups[1].Value;
		return _location;
	}
}
