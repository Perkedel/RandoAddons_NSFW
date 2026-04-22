namespace Editor.ShaderGraph.Nodes;

[Title( "SGE - Constant Bias Scale" ), Category( "Shader Graph Extras - Universal" ), Icon( "add_circle_outline" )]
public sealed class SGEConstantBiasScaleNode : ShaderNode
{
	[Hide]
	public string SGEConstantBiasScale => @"
		float SGEConstantBiasScale(float input, float bias, float scale)
		{
			return float(input + bias) * scale;
		}
		";

	[Input( typeof( float ) )]
	[Hide]
	public NodeInput Input { get; set; }

	[Input( typeof( float ) )]
	[Hide]
	public NodeInput Bias { get; set; }
	
	[Input( typeof( float ) )]
	[Hide]
	public NodeInput Scale { get; set; }

	[InputDefault( nameof( Bias ) )]
	public float DefaultBias { get; set; } = 0f;
	
	[InputDefault (nameof( Scale ))]
	public float DefaultScale {get; set;} = 1f;

	[Output( typeof( float ) )] 
	[Hide]
	public NodeResult.Func Output => ( GraphCompiler compiler ) =>
	{
		var input = compiler.Result( Input);
		var bias = compiler.ResultOrDefault( Bias, DefaultBias ).Cast( 1 );
		var scale = compiler.ResultOrDefault( Scale, DefaultScale ).Cast( 1 );

		return new NodeResult(NodeResultType.Float, compiler.ResultFunction( compiler.RegisterFunction( SGEConstantBiasScale ), $"{input}, {bias}, {scale}"));
	};
}