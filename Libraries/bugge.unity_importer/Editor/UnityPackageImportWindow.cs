using Editor;
using System;
using System.IO;

namespace Bugge.UnityImporter;

public class ImportWindow : Window
{
	public event Action OnConfirm;
	public event Action OnCancel;

	public ImportWindow( UnityPackageExtractor.Item[] items )
	{
		WindowTitle = "Import Unity Package";
		SetWindowIcon( "unarchive" );
		Size = new Vector2( 400, 800 );
		StatusBar.Visible = false;

		var canvas = new Widget( null );

		var layout = canvas.Layout = Layout.Column();
		layout.Margin = 10;
		layout.Spacing = 10;

		var scrollArea = layout.Add( new ScrollArea( null ), 1 );
		scrollArea.HorizontalScrollbarMode = ScrollbarMode.Off;

		var scroll = new Widget( null );
		var scrollLayout = scroll.Layout = Layout.Column();
		scrollArea.Canvas = scroll;

		var checkboxes = new Checkbox[items.Length];
		int baseDepth = GetPathDepth( items[0].Path );

		for ( int i = 0; i < items.Length; i++ )
		{
			var item = items[i];

			int depth = GetPathDepth( item.Path ) - baseDepth;

			var row = scrollLayout.Add( new Widget( null ) );
			var rowLayout = row.Layout = Layout.Row();
			rowLayout.Margin = new Sandbox.UI.Margin( depth * 20, 0, 0, 0 );

			var checkbox = rowLayout.Add( new Checkbox( item.Path ) { Value = item.Included } );

			checkbox.Toggled += () =>
			{
				bool isDirectory = !Path.HasExtension( item.Path );
				bool newValue = !item.Included;

				item.Included = newValue;
				if ( !isDirectory ) return;

				for ( int j = 0; j < checkboxes.Length; j++ )
				{
					var jItem = items[j];
					var jCheckbox = checkboxes[j];

					bool isInDirectory = jItem.Path.Contains( item.Path ) && jItem.Path != item.Path;
					if ( !isInDirectory ) continue;
					jCheckbox.Enabled = newValue;
					jItem.Included = newValue;
				}
			};

			checkboxes[i] = checkbox;
		}

		layout.AddSeparator( true );

		// Buttons
		var btnRow = layout.AddRow();
		btnRow.Spacing = 10;

		var confirmBtn = btnRow.Add( new Button( "Confirm", "check" ) );
		var cancelBtn = btnRow.Add( new Button( "Cancel", "cancel" ) );

		confirmBtn.Clicked = () =>
		{
			OnConfirm?.Invoke();
			Close();
		};

		cancelBtn.Clicked = () =>
		{
			OnCancel?.Invoke();
			Close();
		};

		layout.AddStretchCell();

		Canvas = canvas;
	}

	protected override void OnClosed()
	{
		base.OnClosed();
		OnCancel?.Invoke();
	}

	public static int GetPathDepth( string path )
	{
		if ( string.IsNullOrWhiteSpace( path ) ) return 0;
		var parts = path.Split( ['/', '\\'], StringSplitOptions.RemoveEmptyEntries );

		return parts.Length;
	}
}
