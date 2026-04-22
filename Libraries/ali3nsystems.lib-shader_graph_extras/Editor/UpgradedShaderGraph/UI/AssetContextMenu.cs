using Editor.ShaderGraphExtras;

namespace ShaderGraphExtras;

public static class ShaderGraphToggle
{
	[Event( "asset.contextmenu", Priority = 1 )]
	public static void OnShaderGraphAssetContext( AssetContextMenu e )
	{
		if ( !e.SelectedList.All( x => x.Asset?.Path?.EndsWith( ".shdrgrph" ) == true
			|| x.Asset?.Path?.EndsWith( ".shdrfunc" ) == true ) )
			return;

		e.Menu.AddOption( "Open with Upgraded Shader Graph", "gradient", action: () =>
		{
			foreach ( var entry in e.SelectedList )
			{
				var asset = entry.Asset;
				if ( asset == null ) continue;

				var isFunc = asset.Path.EndsWith( ".shdrfunc" );
				var window = isFunc
					? (MainWindow)new MainWindowFunc()
					: (MainWindow)new MainWindowShader();

				window.AssetOpen( asset );
			}
		} );
	}
}