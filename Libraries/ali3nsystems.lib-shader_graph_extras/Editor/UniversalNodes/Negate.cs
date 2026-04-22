namespace Editor.ShaderGraph.Nodes;

[Title( "SGE - Negate" ), Category( "Shader Graph Extras - Universal" ), Icon( "exposure_neg_1" )]
public sealed class SGENegateNode : ShaderNode
{
	[Hide]
	public static string SGENegate => @"
		float3 SGENegate(float3 input)
		{
			return -1 * input;
		}
		";

	[Input( typeof( Vector3 ) )] 
	[Hide]
	public NodeInput Input { get; set; }

	[Output( typeof( Vector3 ) )] 
	[Hide]
	public NodeResult.Func Output => ( GraphCompiler compiler ) =>
	{
		var input = compiler.Result(Input);

		return new NodeResult(NodeResultType.Vector3, compiler.ResultFunction( compiler.RegisterFunction( SGENegate ), $"{input}"));
	};
}