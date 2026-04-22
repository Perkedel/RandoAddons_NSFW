namespace Editor.ShaderGraph.Nodes;

[Title( "SGE - Height To Normal" ), Category( "Shader Graph Extras - Universal" ), Icon( "alt_route" )]
public sealed class SGEHeightToNormalNode : ShaderNode
{
	public enum SGEHeightToNormalMode
	{
		Tangent,
		World
	}

	[Input( typeof( float ) )]
	[Hide]
	public NodeInput Input { get; set; }

	[Input( typeof( float ) )]
	[Hide]
	public NodeInput Strength { get; set; }

	[InputDefault( nameof( Strength ) )]
	public float DefaultStrength { get; set; } = 1f;

	public SGEHeightToNormalMode Mode { get; set; } = SGEHeightToNormalMode.Tangent;

	[Output( typeof( Vector3 ) )]
	[Hide]
	public NodeResult.Func Output => ( GraphCompiler compiler ) =>
	{
		var input = compiler.Result( Input );
		var strength = compiler.ResultOrDefault( Strength, DefaultStrength );
		var worldSpacePosition = "i.vPositionWithOffsetWs.xyz + g_vHighPrecisionLightingOffsetWs.xyz";
		var worldSpaceNormal = "i.vNormalWs";

		compiler.RegisterInclude( "shaders/hlsl/functions/func-height_to_normal.hlsl" );

		NodeResult nodeResult = new NodeResult();

		switch ( Mode )
		{
			case SGEHeightToNormalMode.Tangent:
				
				var worldSpaceTangentU = "i.vTangentUWs";
				var worldSpaceTangentV = "i.vTangentVWs";
				
				nodeResult = new NodeResult( NodeResultType.Vector3, $"Vec3WsToTs(SGEHeightToNormal ({input}, {strength}, {worldSpacePosition}, {worldSpaceNormal}),{worldSpaceNormal},{worldSpaceTangentU},{worldSpaceTangentV})" );
				break;

			case SGEHeightToNormalMode.World:
				nodeResult = new NodeResult( NodeResultType.Vector3, $"SGEHeightToNormal ({input}, {strength}, {worldSpacePosition}, {worldSpaceNormal})" );
				break;
		}

		return nodeResult;
	};
}