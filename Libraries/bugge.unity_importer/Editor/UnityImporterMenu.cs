using System.IO;
using System.Linq;
using Editor;
using Sandbox;

namespace Bugge.UnityImporter;

public static class UnityImporterMenu
{
	private const string UNITY_PACKAGE_EXTENSION = "unitypackage";

	[Menu( "Editor", $"Unity Importer/Import Package (.{UNITY_PACKAGE_EXTENSION})" )]
	public static void ImportPackageMenu()
	{
		string projectDir = Project.Current.RootDirectory.FullName;
		string assetsDir = Path.Combine( projectDir, "assets/" );
		string targetPath = EditorUtility.OpenFileDialog( "Select Unity Package..", UNITY_PACKAGE_EXTENSION, assetsDir );

		if ( targetPath is null )
			return;

		UnityPackageExtractor.Extract( targetPath, projectDir );
	}

	[Event( "asset.contextmenu", Priority = 50 )]
	private static void OnUnityPackageFileAssetContext( AssetContextMenu e )
	{
		string projectDir = Project.Current.RootDirectory.FullName;
		var packagePath = e.SelectedList
			.Where( x => Path.GetExtension( x.AbsolutePath ) == $".{UNITY_PACKAGE_EXTENSION}" )
			.Select( x => x.AbsolutePath )
			.FirstOrDefault();

		if ( string.IsNullOrEmpty( packagePath ) ) return;

		e.Menu.AddOption( "Extract..", "unarchive", () => UnityPackageExtractor.Extract( packagePath, projectDir ) );
	}
}
