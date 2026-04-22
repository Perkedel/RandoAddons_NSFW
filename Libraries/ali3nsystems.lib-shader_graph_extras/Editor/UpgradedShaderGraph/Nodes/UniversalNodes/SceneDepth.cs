namespace Editor.ShaderGraphExtras.Nodes;

[Title( "SGE - Scene Depth" ), Category( "Shader Graph Extras - Universal" ), Icon( "gradient" )]
public sealed class SGESceneDepthNode : ShaderNode
{
	public enum SGESceneDepthMode
	{
		Raw,
		Linear,
		Eye
	}

	[Input( typeof( Vector2 ) )]
	[Hide]
	public NodeInput Coordinates { get; set; }

	public SGESceneDepthMode Mode { get; set; } = SGESceneDepthMode.Raw;

	[Output( typeof( float ) )]
	[Hide]
	public NodeResult.Func Output => ( GraphCompiler compiler ) =>
	{
		var coordinates = compiler.Result( Coordinates ).Cast( 2 );

		NodeResult nodeResult = new NodeResult();

		switch ( Mode )
		{
			case SGESceneDepthMode.Raw:
				nodeResult = new NodeResult ( NodeResultType.Float, $"Depth::Get( {coordinates} )");
				break;
			
			case SGESceneDepthMode.Linear:
				nodeResult = new NodeResult ( NodeResultType.Float, $"Depth::GetNormalized( {coordinates} )");
				break;

			case SGESceneDepthMode.Eye:
				nodeResult = new NodeResult ( NodeResultType.Float, $"Depth::GetLinear( {coordinates} )");
				break;
		}

		return nodeResult;
	};
}