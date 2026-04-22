namespace Editor.ShaderGraph.Nodes;

[Title( "SGE - Sphere Mask" ), Category( "Shader Graph Extras - Universal" ), Icon( "circle" )]
public sealed class SGESphereMaskNode : ShaderNode
{
	[Hide]
	public static string SGESphereMask => @"
		float3 SGESphereMask(float3 coordinates, float3 center, float radius, float hardness)
		{
			return 1 - saturate((distance(coordinates,center) - radius) / hardness);
		}
		";

	[Input( typeof( Vector3 ) )] 
	[Hide]
	public NodeInput Coordinates { get; set; }
	
	[Input( typeof( Vector3 ) )] 
	[Hide]
	public NodeInput Center { get; set; }
	
	[Input( typeof( float ) )] 
	[Hide]
	public NodeInput Radius { get; set; }
	
	[Input( typeof( float ) )]
	[Hide]
	public NodeInput Hardness { get; set; }
	
	[InputDefault( nameof( Center ) )]
	public Vector3 DefaultCenter { get; set; } = new Vector3( 0f, 0f, 0f );

	[InputDefault( nameof( Radius ) )]
	public float DefaultRadius { get; set; } = 32f;
	
	[InputDefault( nameof( Hardness ) )]
	public float DefaultHardness { get; set; } = 16f;

	[Output( typeof( Vector3 ) )] 
	[Hide]
	public NodeResult.Func Output => ( GraphCompiler compiler ) =>
	{
		var coordinates = compiler.Result(Coordinates).Cast(3);
		var center = compiler.ResultOrDefault(Center, DefaultCenter);
		var radius = compiler.ResultOrDefault(Radius, DefaultRadius);
		var hardness = compiler.ResultOrDefault(Hardness, DefaultHardness);

		return new NodeResult(NodeResultType.Vector3, compiler.ResultFunction( compiler.RegisterFunction( SGESphereMask ), $"{coordinates}, {center}, {radius}, {hardness}"));
	};
}