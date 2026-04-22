namespace ShaderGraphPlus;

public interface IParameterNode
{
	Guid ParameterIdentifier { get; set; }
	string Name { get; }
}

public abstract class ParameterNode<T, Y> : ShaderNodePlus, IParameterNode where Y : BlackboardParameter
{
	[Hide]
	protected bool IsSubgraph => (Graph is ShaderGraphPlus shaderGraph && shaderGraph.IsSubgraph);

	[JsonIgnore, Hide, Browsable( false )]
	public override Color NodeTitleColor => ShaderGraphPlusTheme.NodeHeaderColors.ParameterNode;

	[Hide, Browsable( false )]
	public Guid ParameterIdentifier { get; set; }

	[JsonIgnore, Hide, Browsable( false )]
	public override string Title => string.IsNullOrWhiteSpace( Name ) ?
		$"{DisplayInfo.For( this ).Name}" :
		$"{Name}";

	[Hide]
	public string Name => GetParameter().Name;

	[Hide, JsonIgnore]
	public T Value
	{
		get => (T)GetParameter().GetValue();
		set
		{
			if ( Graph is ShaderGraphPlus graph )
			{
				graph.UpdateParameterValue( ParameterIdentifier, value );

				Update();
				IsDirty = true;
			}
		}
	}

	protected Y GetParameter()
	{
		if ( Graph is ShaderGraphPlus graph )
		{
			return graph.FindParameter<Y>( ParameterIdentifier );
		}

		return null;
	}

	protected NodeResult Component( string component, float value, GraphCompiler compiler )
	{
		if ( compiler.IsPreview )
			return compiler.ResultValue( value );

		var result = compiler.Result( new NodeInput { Identifier = Identifier, Output = nameof( Result ) } );
		return new( ResultType.Float, $"{result}.{component}", true );
	}
}
