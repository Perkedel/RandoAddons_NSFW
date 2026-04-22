namespace Editor.ShaderGraphExtras;

internal static class ComboControlWidgetHelper
{
	public static ShaderNode GetShaderNode( SerializedProperty property )
	{
		if ( property is null )
			return null;

		if ( property.Parent.Targets.First() is ShaderNode shaderNode )
			return shaderNode;

		return GetShaderNode( property.Parent?.ParentProperty );
	}

	public static void SetupComboBox<T>(
		ComboBox comboBox,
		SerializedProperty property,
		string currentValue,
		Func<SGEComboNode, string> getValue,
		Func<string, T> createValue )
	{
		List<string> namesSoFar = [currentValue];

		comboBox.AddItem( "" );
		if ( !string.IsNullOrEmpty( currentValue ) )
		{
			comboBox.AddItem( currentValue );
			comboBox.CurrentIndex = 1;
		}

		var parentNode = GetShaderNode( property );
		if ( parentNode is not null )
		{
			foreach ( var node in parentNode.Graph.Nodes )
			{
				if ( node is SGEComboNode comboNode )
				{
					string value = getValue( comboNode );
					if ( !string.IsNullOrEmpty( value ) && !namesSoFar.Contains( value ) )
					{
						comboBox.AddItem( value );
						namesSoFar.Add( value );
					}
				}
			}
		}

		comboBox.Editable = true;
		comboBox.Insertion = ComboBox.InsertMode.Skip;

		comboBox.TextChanged += () =>
		{
			property.SetValue( createValue( comboBox.CurrentText ) );
		};
	}
}

[CustomEditor( typeof( ComboName ) )]
internal class ComboNameControlWidget : ControlWidget
{
	public override bool SupportsMultiEdit => false;

	public ComboNameControlWidget( SerializedProperty property ) : base( property )
	{
		Layout = Layout.Row();
		var comboBox = Layout.Add( new ComboBox( this ) );
		var currentValue = SerializedProperty.GetValue<ComboName>().Name;

		ComboControlWidgetHelper.SetupComboBox(
			comboBox, property, currentValue,
			node => node.Name,
			text => new ComboName { Name = text } );
	}
}

[CustomEditor( typeof( ComboGroup ) )]
internal class ComboGroupControlWidget : ControlWidget
{
	public override bool SupportsMultiEdit => false;

	public ComboGroupControlWidget( SerializedProperty property ) : base( property )
	{
		Layout = Layout.Row();
		var comboBox = Layout.Add( new ComboBox( this ) );
		var currentValue = SerializedProperty.GetValue<ComboGroup>().Group;

		ComboControlWidgetHelper.SetupComboBox(
			comboBox, property, currentValue,
			node => node.Group,
			text => new ComboGroup { Group = text } );
	}
}
