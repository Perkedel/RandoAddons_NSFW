using System;
using System.IO;
using System.IO.Compression;
using System.Formats.Tar;
using Editor;
using System.Linq;

namespace Bugge.UnityImporter;

public static class UnityPackageExtractor
{
	public class Item
	{
		public string Path;
		public string TempPath;
		public bool Included;
	}

	public static void Extract( string packagePath, string outputDirectory )
	{
		string tempPath = Path.Combine( Path.GetTempPath(), "UnityUnpack_" + Guid.NewGuid() );
		Directory.CreateDirectory( tempPath );

		using ( var fs = File.OpenRead( packagePath ) )
		using ( var gzip = new GZipStream( fs, CompressionMode.Decompress ) )
			TarFile.ExtractToDirectory( gzip, tempPath, overwriteFiles: true );

		var directories = Directory.GetDirectories( tempPath );
		var items = new Item[directories.Length];

		for ( int i = 0; i < items.Length; i++ )
		{
			string dir = directories[i];
			string pathnameFile = Path.Combine( dir, "pathname" );
			string path = File.ReadAllText( pathnameFile ).Trim();

			var item = new Item()
			{
				Path = path,
				TempPath = dir,
				Included = true
			};

			items[i] = item;
		}

		items = [.. items.OrderBy( i => i.Path )];

		var window = new ImportWindow( items );
		window.Show();

		window.OnConfirm += () =>
		{
			foreach ( var item in items )
			{
				if ( !item.Included ) continue;
				string pathnameFile = Path.Combine( item.TempPath, "pathname" );
				string assetFile = Path.Combine( item.TempPath, "asset" );

				if ( File.Exists( pathnameFile ) && File.Exists( assetFile ) )
				{
					string relativePath = File.ReadAllText( pathnameFile ).Trim();
					string finalPath = Path.Combine( outputDirectory, relativePath );

					Directory.CreateDirectory( Path.GetDirectoryName( finalPath )! );

					File.Move( assetFile, finalPath, true );
				}
			}

			if ( Directory.Exists( tempPath ) )
				Directory.Delete( tempPath, true );

			EditorUtility.RestartEditorPrompt(
				"""
				Importing complete.
				A restart of the editor is needed to register the assets properly.
				"""
			);
		};
	}
}
