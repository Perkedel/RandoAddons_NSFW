namespace Editor.ShaderGraph.Nodes;

[Title( "SGE - Clamp" ), Category( "Shader Graph Extras - Universal" ), Icon( "plumbing" )]
public sealed class SGEClampNode : ShaderNode
{
	[Hide]
	public string SGEClamp => @"
		float SGEClamp( float input, float minimum, float maximum )
		{
			return clamp(input, minimum, maximum);
		}
		";

	[Input (typeof( float ))]
	[Hide]
	public NodeInput Input {get; set;}
	
	[Input (typeof( float ))]
	[Hide]
	public NodeInput Minimum {get; set;}
	
	[Input (typeof( float ))]
	[Hide]
	public NodeInput Maximum {get; set;}
	
	[InputDefault( nameof( Minimum ) )]
	public float DefaultMinimum {get; set;} = 0f;
	
    [InputDefault( nameof( Maximum ) )]
	public float DefaultMaximum {get; set;} = 1f;

	[Output( typeof( float ) )]
	[Hide]
	public NodeResult.Func Output => ( GraphCompiler compiler ) =>
	{
		var input = compiler.Result(Input);
		var minimum = compiler.ResultOrDefault(Minimum, DefaultMinimum);
		var maximum = compiler.ResultOrDefault(Maximum, DefaultMaximum);
		
		return new NodeResult( NodeResultType.Float, compiler.ResultFunction( compiler.RegisterFunction ( SGEClamp ), $"{input}, {minimum}, {maximum}" ) );
	};
}