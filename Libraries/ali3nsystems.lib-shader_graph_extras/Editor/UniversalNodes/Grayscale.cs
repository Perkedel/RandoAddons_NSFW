namespace Editor.ShaderGraph.Nodes;

[Title( "SGE - Grayscale" ), Category( "Shader Graph Extras - Universal" ), Icon( "filter_b_and_w" )]
public sealed class SGEGrayscaleNode : ShaderNode
{
	[Hide]
	public static string SGEGrayscale => @"
		float SGEGrayscale(float3 input)
		{
			return float(dot(input, float3(0.299, 0.587, 0.114)));
		}
		";

	[Input( typeof( Vector3 ) )]
	[Hide]
	public NodeInput Input { get; set; }

	[Output( typeof( float ) )] 
	[Hide]
	public NodeResult.Func Output => ( GraphCompiler compiler ) =>
	{
		var input = compiler.Result(Input);

		return new NodeResult(NodeResultType.Float, compiler.ResultFunction( compiler.RegisterFunction( SGEGrayscale ), $"{input}"));
	};
}