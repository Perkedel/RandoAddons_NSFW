namespace Editor.ShaderGraphExtras.Nodes;

[Title( "SGE - Vector Transform" ), Category( "Shader Graph Extras - Universal" ), Icon( "transform" )]
public sealed class SGEVectorTransformNode : ShaderNode
{
	public enum SGEVectorTransformInputSpace
	{
		Tangent,
		Object,
		World
	}

	public enum SGEVectorTransformOutputSpace
	{
		Tangent,
		Object,
		World,
		View
	}

	public enum SGEVectorTransformMode
	{
		Position,
		Normal,
		Direction
	}

	[Hide, JsonIgnore]
	private SGEVectorTransformMode _lastMode;

	public override void OnFrame()
	{
		base.OnFrame();

		// Auto-correct OutputSpace when Mode changes
		if ( _lastMode != Mode )
		{
			_lastMode = Mode;

			if ( Mode == SGEVectorTransformMode.Direction )
			{
				// Direction mode only supports View output
				OutputSpace = SGEVectorTransformOutputSpace.View;
			}
			else if ( OutputSpace == SGEVectorTransformOutputSpace.View )
			{
				// Position/Normal modes don't support View, switch to World
				OutputSpace = SGEVectorTransformOutputSpace.World;
			}
		}
	}

	[Input( typeof( Vector3 ) )]
	[Hide]
	public NodeInput Input { get; set; }

	public SGEVectorTransformInputSpace InputSpace { get; set; } = SGEVectorTransformInputSpace.Tangent;
	public SGEVectorTransformOutputSpace OutputSpace { get; set; } = SGEVectorTransformOutputSpace.World;
	public SGEVectorTransformMode Mode { get; set; } = SGEVectorTransformMode.Normal;

	[Output( typeof( Vector4 ) )]
	[Hide]
	public NodeResult.Func Output => ( GraphCompiler compiler ) =>
	{
		var input = compiler.Result( Input );

		var worldSpacePosition = compiler.IsVs ? "i.vPositionWs" : "i.vPositionWithOffsetWs.xyz + g_vHighPrecisionLightingOffsetWs.xyz";
		var worldSpaceNormal = "i.vNormalWs";
		var worldSpaceTangentU = "i.vTangentUWs";
		var worldSpaceTangentV = "i.vTangentVWs";

		var objectSpacePosition = "i.vPositionOs";
		var objectSpaceNormal = "i.vNormalOs.xyz";
		var objectSpaceTangentU = "i.vTangentUOs_flTangentVSign.xyz";
		var objectSpaceTangentV = "cross( i.vNormalOs.xyz, i.vTangentUOs_flTangentVSign.xyz ) * i.vTangentUOs_flTangentVSign.w";

		compiler.RegisterInclude( "shaders/HLSL/Functions/FUNC-vector_transform.hlsl" );

		NodeResult nodeResult = new NodeResult();

		switch ( Mode )
		{
			case SGEVectorTransformMode.Position:
				switch ( InputSpace )
				{
					case SGEVectorTransformInputSpace.Tangent:
						switch ( OutputSpace )
						{
							case SGEVectorTransformOutputSpace.Tangent:
								nodeResult = new NodeResult( NodeResultType.Vector3, $"SGETangentToTangentPosition( {input} )" );
								break;
							case SGEVectorTransformOutputSpace.Object:
								nodeResult = new NodeResult( NodeResultType.Vector3, $"SGETangentToObjectPosition( {input}, {objectSpaceNormal}, {objectSpaceTangentU}, {objectSpaceTangentV} )" );
								break;
							case SGEVectorTransformOutputSpace.World:
								nodeResult = new NodeResult( NodeResultType.Vector3, $"SGETangentToWorldPosition( {input}, {worldSpaceNormal}, {worldSpaceTangentU}, {worldSpaceTangentV} )" );
								break;
						}
						break;
					case SGEVectorTransformInputSpace.Object:
						switch ( OutputSpace )
						{
							case SGEVectorTransformOutputSpace.Tangent:
								nodeResult = new NodeResult( NodeResultType.Vector3, $"SGEObjectToTangentPosition( {input}, {objectSpaceNormal}, {objectSpaceTangentU}, {objectSpaceTangentV} )" );
								break;
							case SGEVectorTransformOutputSpace.Object:
								nodeResult = new NodeResult( NodeResultType.Vector3, $"SGEObjectToObjectPosition( {input} )" );
								break;
							case SGEVectorTransformOutputSpace.World:
								nodeResult = new NodeResult( NodeResultType.Vector3, $"SGEObjectToWorldPosition( {input}, {objectSpacePosition}, {worldSpacePosition} )" );
								break;
						}
						break;
					case SGEVectorTransformInputSpace.World:
						switch ( OutputSpace )
						{
							case SGEVectorTransformOutputSpace.Tangent:
								nodeResult = new NodeResult( NodeResultType.Vector3, $"SGEWorldToTangentPosition( {input}, {worldSpaceNormal}, {worldSpaceTangentU}, {worldSpaceTangentV} )" );
								break;
							case SGEVectorTransformOutputSpace.Object:
								nodeResult = new NodeResult( NodeResultType.Vector3, $"SGEWorldToObjectPosition( {input}, {worldSpacePosition}, {objectSpacePosition} )" );
								break;
							case SGEVectorTransformOutputSpace.World:
								nodeResult = new NodeResult( NodeResultType.Vector3, $"SGEWorldToWorldPosition( {input} )" );
								break;
						}
						break;
				}
				break;

			case SGEVectorTransformMode.Normal:
				switch ( InputSpace )
				{
					case SGEVectorTransformInputSpace.Tangent:
						switch ( OutputSpace )
						{
							case SGEVectorTransformOutputSpace.Tangent:
								nodeResult = new NodeResult( NodeResultType.Vector3, $"SGETangentToTangentNormal( {input} )" );
								break;
							case SGEVectorTransformOutputSpace.Object:
								nodeResult = new NodeResult( NodeResultType.Vector3, $"SGETangentToObjectNormal( {input}, {objectSpaceNormal}, {objectSpaceTangentU}, {objectSpaceTangentV} )" );
								break;
							case SGEVectorTransformOutputSpace.World:
								nodeResult = new NodeResult( NodeResultType.Vector3, $"SGETangentToWorldNormal( {input}, {worldSpaceNormal}, {worldSpaceTangentU}, {worldSpaceTangentV} )" );
								break;
						}
						break;
					case SGEVectorTransformInputSpace.Object:
						switch ( OutputSpace )
						{
							case SGEVectorTransformOutputSpace.Tangent:
								nodeResult = new NodeResult( NodeResultType.Vector3, $"SGEObjectToTangentNormal( {input}, {objectSpaceNormal}, {objectSpaceTangentU}, {objectSpaceTangentV} )" );
								break;
							case SGEVectorTransformOutputSpace.Object:
								nodeResult = new NodeResult( NodeResultType.Vector3, $"SGEObjectToObjectNormal( {input} )" );
								break;
							case SGEVectorTransformOutputSpace.World:
								nodeResult = new NodeResult( NodeResultType.Vector3, $"SGEObjectToWorldNormal( {input}, {worldSpaceNormal}, {worldSpaceTangentU}, {worldSpaceTangentV}, {objectSpaceNormal}, {objectSpaceTangentU}, {objectSpaceTangentV} )" );
								break;
						}
						break;
					case SGEVectorTransformInputSpace.World:
						switch ( OutputSpace )
						{
							case SGEVectorTransformOutputSpace.Tangent:
								nodeResult = new NodeResult( NodeResultType.Vector3, $"SGEWorldToTangentNormal( {input}, {worldSpaceNormal}, {worldSpaceTangentU}, {worldSpaceTangentV} )" );
								break;
							case SGEVectorTransformOutputSpace.Object:
								nodeResult = new NodeResult( NodeResultType.Vector3, $"SGEWorldToObjectNormal( {input}, {worldSpaceNormal}, {worldSpaceTangentU}, {worldSpaceTangentV}, {objectSpaceNormal}, {objectSpaceTangentU}, {objectSpaceTangentV} )" );
								break;
							case SGEVectorTransformOutputSpace.World:
								nodeResult = new NodeResult( NodeResultType.Vector3, $"SGEWorldToWorldNormal( {input} )" );
								break;
						}
						break;
				}
				break;

			case SGEVectorTransformMode.Direction:
				switch ( InputSpace )
				{
					case SGEVectorTransformInputSpace.Tangent:
						nodeResult = new NodeResult( NodeResultType.Color, $"SGETangentToViewDirection( {input}, {worldSpaceNormal}, {worldSpaceTangentU}, {worldSpaceTangentV} )" );
						break;
					case SGEVectorTransformInputSpace.Object:
						nodeResult = new NodeResult( NodeResultType.Color, $"SGEObjectToViewDirection( {input}, {objectSpacePosition}, {worldSpacePosition} )" );
						break;
					case SGEVectorTransformInputSpace.World:
						nodeResult = new NodeResult( NodeResultType.Color, $"SGEWorldToViewDirection( {input} )" );
						break;
				}
				break;
		}

		return nodeResult;
	};
}

/// <summary>
/// Custom enum editor for SGEVectorTransformOutputSpace that shows View only when Mode is Direction
/// </summary>
[CustomEditor( typeof( SGEVectorTransformNode.SGEVectorTransformOutputSpace ) )]
public class SGEVectorTransformOutputSpaceControlWidget : ControlWidget
{
	SGEVectorTransformNode node;
	EnumDescription enumDesc;
	PopupWidget _menu;

	public override bool IsControlActive => base.IsControlActive || _menu.IsValid();
	public override bool IsControlButton => true;
	public override bool IsControlHovered => base.IsControlHovered || _menu.IsValid();

	public SGEVectorTransformOutputSpaceControlWidget( SerializedProperty property ) : base( property )
	{
		node = property.Parent?.Targets?.FirstOrDefault() as SGEVectorTransformNode;

		var propertyType = property.PropertyType;
		Cursor = CursorShape.Finger;
		Layout = Layout.Row();
		Layout.Spacing = 2;
		enumDesc = EditorTypeLibrary.GetEnumDescription( propertyType );
	}

	private bool IsEntrySupported( EnumDescription.Entry entry )
	{
		if ( node is null )
			return entry.Browsable;

		if ( !entry.Browsable )
			return false;

		// View is only available when Mode is Direction
		if ( entry.Name == nameof( SGEVectorTransformNode.SGEVectorTransformOutputSpace.View ) )
		{
			return node.Mode == SGEVectorTransformNode.SGEVectorTransformMode.Direction;
		}

		// Tangent, World, Object are only available when Mode is NOT Direction
		return node.Mode != SGEVectorTransformNode.SGEVectorTransformMode.Direction;
	}

	protected override void PaintControl()
	{
		if ( enumDesc is null )
			return;

		var value = SerializedProperty.GetValue<long>( 0 );
		var color = IsControlHovered ? Theme.Blue : Theme.TextControl;
		var rect = LocalRect.Shrink( 8, 0 );

		var e = enumDesc.GetEntry( value );

		if ( !string.IsNullOrEmpty( e.Icon ) )
		{
			Paint.SetPen( color.WithAlpha( 0.5f ) );
			var i = Paint.DrawIcon( rect, e.Icon, 16, TextFlag.LeftCenter );
			rect.Left += i.Width + 8;
		}

		Paint.SetPen( color );
		Paint.DrawText( rect, e.Title ?? "Unset", TextFlag.LeftCenter );
		Paint.DrawIcon( rect, "Arrow_Drop_Down", 17, TextFlag.RightCenter );
	}

	protected override void OnMouseClick( MouseEvent e )
	{
		if ( e.LeftMouseButton && !_menu.IsValid() )
		{
			OpenMenu();
		}
	}

	void ToggleValue( EnumDescription.Entry e )
	{
		SerializedProperty.SetValue( e.IntegerValue );
	}

	void OpenMenu()
	{
		if ( enumDesc is null )
			return;

		_menu = new PopupWidget( null );
		_menu.Layout = Layout.Column();
		var menuWidth = ScreenRect.Width;
		_menu.MinimumWidth = menuWidth;
		_menu.MaximumWidth = menuWidth;

		var scroller = _menu.Layout.Add( new ScrollArea( this ), 1 );
		scroller.Canvas = new Widget( scroller )
		{
			Layout = Layout.Column(),
			VerticalSizeMode = SizeMode.CanGrow | SizeMode.Expand,
			MaximumWidth = menuWidth
		};

		foreach ( var o in enumDesc )
		{
			if ( !IsEntrySupported( o ) )
				continue;

			var b = scroller.Canvas.Layout.Add( new SGEOutputSpaceMenuOption( o, SerializedProperty ) );
			b.MouseLeftPress = () =>
			{
				ToggleValue( o );
				_menu.Update();
				_menu.Close();
			};
		}

		_menu.Position = ScreenRect.BottomLeft;
		_menu.Visible = true;
		_menu.AdjustSize();
		_menu.ConstrainToScreen();
		_menu.OnPaintOverride = () =>
		{
			Paint.SetBrushAndPen( Theme.ControlBackground );
			Paint.DrawRect( Paint.LocalRect, 0 );
			return true;
		};
	}
}

file class SGEOutputSpaceMenuOption : Widget
{
	EnumDescription.Entry info;
	SerializedProperty property;

	public SGEOutputSpaceMenuOption( EnumDescription.Entry e, SerializedProperty p ) : base( null )
	{
		info = e;
		property = p;

		Layout = Layout.Row();
		Layout.Margin = 8;
		VerticalSizeMode = SizeMode.CanGrow;

		if ( !string.IsNullOrWhiteSpace( e.Icon ) )
		{
			Layout.Add( new IconButton( e.Icon ) { Background = Color.Transparent, TransparentForMouseEvents = true, IconSize = 18 } );
		}

		Layout.AddSpacingCell( 8 );
		var c = Layout.AddColumn();
		var title = c.Add( new Label( e.Title ) );
		title.SetStyles( $"font-size: 12px; font-weight: bold; font-family: {Theme.DefaultFont}; color: white;" );

		if ( !string.IsNullOrWhiteSpace( e.Description ) )
		{
			var desc = c.Add( new Label( e.Description.Trim( '\n', '\r', '\t', ' ' ) ) );
			desc.WordWrap = true;
			desc.MinimumHeight = 1;
			desc.VerticalSizeMode = SizeMode.CanGrow;
		}
	}

	bool HasValue()
	{
		var value = property.GetValue<long>( 0 );
		return value == info.IntegerValue;
	}

	protected override void OnPaint()
	{
		if ( Paint.HasMouseOver || HasValue() )
		{
			Paint.SetBrushAndPen( Theme.Blue.WithAlpha( HasValue() ? 0.3f : 0.1f ) );
			Paint.DrawRect( LocalRect.Shrink( 2 ), 2 );
		}
	}
}
