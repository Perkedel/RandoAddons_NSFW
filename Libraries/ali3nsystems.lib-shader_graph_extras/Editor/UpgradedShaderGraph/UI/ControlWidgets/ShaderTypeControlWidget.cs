namespace Editor.ShaderGraphExtras;

/// <summary>
/// Dropdown selection for font names
/// </summary>
[CustomEditor( typeof( string ), NamedEditor = "shadertype" )]
sealed class ShaderTypeControlWidget : DropdownControlWidget<string>
{
	public ShaderTypeControlWidget( SerializedProperty property ) : base( property )
	{
	}

	protected override IEnumerable<object> GetDropdownValues()
	{
		List<object> list = new();
		foreach ( var type in GraphCompiler.ValueTypes )
		{
			if ( type.Key == typeof( float ) ) list.Add( "float" );
			else if ( type.Key == typeof( int ) ) list.Add( "int" );
			else if ( type.Key == typeof( bool ) ) list.Add( "bool" );
			else list.Add( type.Key );
		}
		return list;
	}
}
