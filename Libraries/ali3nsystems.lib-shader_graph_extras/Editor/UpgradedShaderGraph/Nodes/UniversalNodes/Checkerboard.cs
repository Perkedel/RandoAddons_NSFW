namespace Editor.ShaderGraphExtras.Nodes;

[Title( "SGE - Checkerboard" ), Category( "Shader Graph Extras - Universal" ), Icon( "grid_on" )]
public sealed class SGECheckerboardNode : ShaderNode
{
	[Hide]
	public static string SGECheckerboard => @"
		float SGECheckerboard(float2 coordinates)
		{
			coordinates *= 0.5f;
			float2 derivative = fwidth(coordinates);
			float2 derivativeScale = 0.35f / derivative;
			float2 distanceToEdge = 4.0f * abs(frac(coordinates + 0.25f) - 0.5f) - 1.0f;
			float2 checkerAlpha = clamp(distanceToEdge * derivativeScale, -1.0f, 1.0f);
			return saturate(0.5f + 0.5f * checkerAlpha.x * checkerAlpha.y);
		}
		";

	[Input( typeof( Vector2 ) )]
	[Hide]
	public NodeInput Coordinates { get; set; }

	[Output( typeof( float ) )]
	[Hide]
	public NodeResult.Func Output => ( GraphCompiler compiler ) =>
	{
		var coordinates = compiler.Result(Coordinates).Cast(2);

		return new NodeResult(NodeResultType.Float, compiler.ResultFunction( compiler.RegisterFunction( SGECheckerboard ), $"{coordinates}"));
	};
}