namespace Editor.ShaderGraphExtras;

/// <summary>
/// Custom enum editor for BlendMode that filters options based on template/shading model features
/// </summary>
[CustomEditor( typeof( BlendMode ) )]
public class BlendModeControlWidget : ControlWidget
{
	ShaderGraph shaderGraph;
	EnumDescription enumDesc;
	PopupWidget _menu;

	public override bool IsControlActive => base.IsControlActive || _menu.IsValid();
	public override bool IsControlButton => true;
	public override bool IsControlHovered => base.IsControlHovered || _menu.IsValid();

	public BlendModeControlWidget( SerializedProperty property ) : base( property )
	{
		shaderGraph = property.Parent?.Targets?.FirstOrDefault() as ShaderGraph;

		var propertyType = property.PropertyType;
		Cursor = CursorShape.Finger;
		Layout = Layout.Row();
		Layout.Spacing = 2;
		enumDesc = EditorTypeLibrary.GetEnumDescription( propertyType );
	}

	private bool IsEntrySupported( EnumDescription.Entry entry )
	{
		if ( shaderGraph is null )
			return entry.Browsable;

		if ( !entry.Browsable )
			return false;

		return entry.Name switch
		{
			nameof( BlendMode.Opaque ) => shaderGraph.SupportsOpaqueBlendMode,
			nameof( BlendMode.Masked ) => shaderGraph.SupportsMaskedBlendMode,
			nameof( BlendMode.Translucent ) => shaderGraph.SupportsTranslucentBlendMode,
			nameof( BlendMode.Dynamic ) => shaderGraph.SupportsDynamicBlendMode,
			_ => true
		};
	}

	protected override void PaintControl()
	{
		if ( enumDesc is null )
			return;

		// Auto-correct if current value is not supported
		shaderGraph?.ValidateSettings();

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

		// Auto-correct if current value is not supported
		shaderGraph?.ValidateSettings();

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

			var b = scroller.Canvas.Layout.Add( new BlendModeMenuOption( o, SerializedProperty ) );
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

file class BlendModeMenuOption : Widget
{
	EnumDescription.Entry info;
	SerializedProperty property;

	public BlendModeMenuOption( EnumDescription.Entry e, SerializedProperty p ) : base( null )
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

/// <summary>
/// Custom enum editor for ShadingModel that filters options based on template features
/// </summary>
[CustomEditor( typeof( ShadingModel ) )]
public class ShadingModelControlWidget : ControlWidget
{
	ShaderGraph shaderGraph;
	EnumDescription enumDesc;
	PopupWidget _menu;

	public override bool IsControlActive => base.IsControlActive || _menu.IsValid();
	public override bool IsControlButton => true;
	public override bool IsControlHovered => base.IsControlHovered || _menu.IsValid();

	public ShadingModelControlWidget( SerializedProperty property ) : base( property )
	{
		shaderGraph = property.Parent?.Targets?.FirstOrDefault() as ShaderGraph;

		var propertyType = property.PropertyType;
		Cursor = CursorShape.Finger;
		Layout = Layout.Row();
		Layout.Spacing = 2;
		enumDesc = EditorTypeLibrary.GetEnumDescription( propertyType );
	}

	private bool IsEntrySupported( EnumDescription.Entry entry )
	{
		if ( shaderGraph is null )
			return entry.Browsable;

		if ( !entry.Browsable )
			return false;

		return entry.Name switch
		{
			nameof( ShadingModel.Lit ) => shaderGraph.SupportsLitShadingModel,
			nameof( ShadingModel.Unlit ) => shaderGraph.SupportsUnlitShadingModel,
			nameof( ShadingModel.Custom ) => shaderGraph.SupportsCustomShadingModel,
			_ => true
		};
	}

	protected override void PaintControl()
	{
		if ( enumDesc is null )
			return;

		// Auto-correct if current value is not supported
		shaderGraph?.ValidateSettings();

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

		// Auto-correct if current value is not supported
		shaderGraph?.ValidateSettings();

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

			var b = scroller.Canvas.Layout.Add( new ShadingModelMenuOption( o, SerializedProperty ) );
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

file class ShadingModelMenuOption : Widget
{
	EnumDescription.Entry info;
	SerializedProperty property;

	public ShadingModelMenuOption( EnumDescription.Entry e, SerializedProperty p ) : base( null )
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
