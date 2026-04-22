namespace Editor.ShaderGraphExtras.Nodes;

[Title( "SGE - Angle Attenuation" ), Category( "Shader Graph Extras - Upgraded" ), Icon( "opacity" )]
public sealed class SGEAngleAttenuationNode : ShaderNode
{
	[Hide]
	public static string SGEAngleAttenuation => @"
		float SGEAngleAttenuation(float input, float2 screenPosition, float3 decalRight)
		{
			float3 worldPosition = Depth::GetWorldPosition( screenPosition );
			float3 ddxPosition = ddx(worldPosition);
			float3 ddyPosition = ddy(worldPosition);
			float3 surfaceNormal = normalize(cross(ddyPosition, ddxPosition));
			float angleDot = saturate( dot(surfaceNormal, -decalRight));
			return lerp(1.0, angleDot, input);
		}
		";

	[Input( typeof( float ) )]
	[Hide]
	public NodeInput Input { get; set; }

	[InputDefault( nameof( Input) )]
	public float DefaultInput { get; set; } = 0;

	[Output( typeof( float ) )]
	[Hide]
	public NodeResult.Func Output => ( GraphCompiler compiler ) =>
	{
		var input = compiler.ResultOrDefault(Input, DefaultInput);

		return new NodeResult(NodeResultType.Float, compiler.ResultFunction( compiler.RegisterFunction( SGEAngleAttenuation ), $"{input}, i.vPositionSs.xy, i.vDecalRight"));
	};
}