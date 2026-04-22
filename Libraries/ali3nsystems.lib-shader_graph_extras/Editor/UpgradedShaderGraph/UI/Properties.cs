
namespace Editor.ShaderGraphExtras;

public class Properties : Widget
{
	private ScrollArea scroller;
	private ControlSheet sheet;
	private string filterText;

	private object _target;
	private List<BaseNode> _multiEditTargets;

	public List<BaseNode> MultiEditTargets => _multiEditTargets;

	public object Target
	{
		get => _target;
		set
		{
			if ( value == _target )
				return;

			_target = value;
			_multiEditTargets = null;

			Editor.Clear( true );

			if ( value is null )
				return;

			var so = value.GetSerialized();
			so.OnPropertyChanged += x =>
			{
				PropertyUpdated?.Invoke();
			};

			sheet = new ControlSheet();
			sheet.AddObject( so, PropertyFilter );

			scroller = new ScrollArea( this );
			scroller.Canvas = new Widget();
			scroller.Canvas.Layout = Layout.Column();
			scroller.Canvas.VerticalSizeMode = SizeMode.CanGrow;
			scroller.Canvas.HorizontalSizeMode = SizeMode.Flexible;
			scroller.Canvas.Layout.Add( sheet );
			scroller.Canvas.Layout.AddStretchCell();

			Editor.Add( scroller );
		}
	}

	private readonly Layout Editor;

	public Action PropertyUpdated { get; set; }

	public Properties( Widget parent ) : base( parent )
	{
		Name = "Properties";
		WindowTitle = "Properties";
		SetWindowIcon( "edit" );

		Layout = Layout.Column();

		var toolbar = new ToolBar( this );
		var filter = new LineEdit( toolbar ) { PlaceholderText = "⌕  Filter Properties.." };
		filter.TextEdited += OnFilterEdited;
		toolbar.AddWidget( filter );
		Layout.Add( toolbar );
		Layout.AddSeparator();

		Editor = Layout.AddRow( 1 );
		Layout.AddStretchCell();
	}

	public void SetMultiEditTargets( List<BaseNode> nodes )
	{
		_target = null;
		_multiEditTargets = nodes;

		Editor.Clear( true );

		if ( nodes == null || nodes.Count == 0 )
			return;

		var multiPanel = new MultiEditPanel( this, nodes );
		multiPanel.PropertyUpdated += () => PropertyUpdated?.Invoke();

		scroller = new ScrollArea( this );
		scroller.Canvas = new Widget();
		scroller.Canvas.Layout = Layout.Column();
		scroller.Canvas.VerticalSizeMode = SizeMode.CanGrow;
		scroller.Canvas.HorizontalSizeMode = SizeMode.Flexible;
		scroller.Canvas.Layout.Add( multiPanel );
		scroller.Canvas.Layout.AddStretchCell();

		Editor.Add( scroller );
	}

	private void OnFilterEdited( string filter )
	{
		filterText = filter;
		sheet.Clear( true );
		sheet.AddObject( _target.GetSerialized(), PropertyFilter );
		scroller.Update();
	}

	bool PropertyFilter( SerializedProperty property )
	{
		if ( property.HasAttribute<HideAttribute>() ) return false;
		if ( string.IsNullOrEmpty( filterText ) ) return true;
		if ( property.Name.ToLower().Contains( filterText.ToLower() ) ) return true;
		if ( property.DisplayName.ToLower().Contains( filterText.ToLower() ) ) return true;
		if ( property.TryGetAsObject( out var obj ) )
		{
			if ( property.TryGetAttribute<ConditionalVisibilityAttribute>( out var conditional ) )
			{
				if ( conditional.TestCondition( obj ) ) return false;
			}
			foreach ( var childProp in obj )
			{
				if ( childProp.HasAttribute<HideAttribute>() ) continue;
				if ( childProp.Name.ToLower().Contains( filterText.ToLower() ) || childProp.DisplayName.ToLower().Contains( filterText.ToLower() ) )
				{
					sheet.AddRow( childProp );
				}
			}
		}
		return false;
	}
}
