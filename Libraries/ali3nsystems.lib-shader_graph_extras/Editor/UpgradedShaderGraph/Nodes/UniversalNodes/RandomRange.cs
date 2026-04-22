namespace Editor.ShaderGraphExtras.Nodes;

[Title( "SGE - Random Range" ), Category( "Shader Graph Extras - Universal" ), Icon("shuffle")]
public sealed class SGERandomRangeNode : ShaderNode
{
	[Hide]
	public static string SGERandomRange => @"
		float SGERandomRange(float2 input, float minimum, float maximum)
		{
			return lerp(minimum, maximum, frac(sin(dot(input, float2(12.9898, 78.233)))*43758.5453));
		}
		";

	[Input( typeof( Vector2 ) )]
	[Hide]
	public NodeInput Input {get; set;}
	
	[Input( typeof( float ) )]
	[Hide]
	public NodeInput Minimum {get; set;}
	
	[Input( typeof( float ) )]
	[Hide]
	public NodeInput Maximum {get; set;}

	[InputDefault (nameof( Minimum ))]
	public float DefaultMinimum {get; set;} = 0f;

	[InputDefault (nameof( Maximum ))]
	public float DefaultMaximum {get; set;} = 1f;
	
	[Output( typeof( float ) )]
	[Hide]
	public NodeResult.Func Output => ( GraphCompiler compiler ) =>
	{
		var input = compiler.Result(Input).Cast(2);
		var minimum = compiler.ResultOrDefault(Minimum, DefaultMinimum);
		var maximum = compiler.ResultOrDefault(Maximum, DefaultMaximum);
		
		return new NodeResult( NodeResultType.Float, compiler.ResultFunction( compiler.RegisterFunction( SGERandomRange ), $"{input}, {minimum}, {maximum}" ) );
	};
}