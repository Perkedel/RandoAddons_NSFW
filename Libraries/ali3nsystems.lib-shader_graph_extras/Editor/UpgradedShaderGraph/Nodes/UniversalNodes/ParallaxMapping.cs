namespace Editor.ShaderGraphExtras.Nodes;

[Title( "SGE - Parallax Mapping" ), Category( "Shader Graph Extras - Universal" ), Icon( "layers" )]
public sealed class SGEParallaxMappingNode : ShaderNode
{
	[Hide]
	public static string SGEParallaxMapping => @"
		float3 SGEParallaxMapping(float3 coordinates, float3 viewDirection, float amplitude)
		{
			return coordinates - viewDirection * float3( amplitude / viewDirection.z, amplitude / viewDirection.z, amplitude / viewDirection.z );
		}
		";

	[Input( typeof( Vector3 ) )]
	[Hide]
	public NodeInput Coordinates { get; set; }

	[Title("View Direction")]
	[Input( typeof( float ) )]
	[Hide]
	public NodeInput ViewDirection { get; set; }

	[Input( typeof( float ) )]
	[Hide]
	public NodeInput Amplitude { get; set; }

	[InputDefault( nameof( Amplitude ) )]
	public float DefaultAmplitude { get; set; } = 0f;

	[Output( typeof( Vector3 ) )] 
	[Hide]
	public NodeResult.Func Output => ( GraphCompiler compiler ) =>
	{
		var coordinates = compiler.Result(Coordinates).Cast(3);
		var viewDirection = compiler.Result( ViewDirection );
		var amplitude = compiler.ResultOrDefault( Amplitude, DefaultAmplitude );

		return new NodeResult( NodeResultType.Vector3, compiler.ResultFunction( compiler.RegisterFunction( SGEParallaxMapping ), $"{coordinates}, {viewDirection}, {amplitude}" ) );
	};
}