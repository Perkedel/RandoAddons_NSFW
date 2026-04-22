
namespace Editor.ShaderGraphExtras.Nodes;
/// <summary>
/// Returns the tangent view vector, which is the direction from the camera to the position in tangent space.
/// </summary>
[Title( "Tangent View Vector" ), Category( "Variables" ), Icon( "visibility" )]
public sealed class TangentViewVector : ShaderNode
{
	[Hide]
	public static string GetTangentViewVector => @"
float3 GetTangentViewVector( float3 vPosition, float3 vNormalWs, float3 vTangentUWs, float3 vTangentVWs )
{
    float3 vCameraToPositionDirWs = CalculateCameraToPositionDirWs( vPosition.xyz );
    vNormalWs = normalize( vNormalWs.xyz );
    float3 vTangentViewVector = Vec3WsToTs( vCameraToPositionDirWs.xyz, vNormalWs.xyz, vTangentUWs.xyz, vTangentVWs.xyz );
	
    // Result
    return vTangentViewVector.xyz;
}
";

	[Title( "Position" ), Hide]
	[Input( typeof( Vector3 ) )]
	public NodeInput WorldPosition { get; set; }

	[Title( "Normal" ), Hide]
	[Input( typeof( Vector3 ) )]
	public NodeInput WorldNormal { get; set; }

	[Title( "World Tangent U" ), Hide]
	[Input( typeof( Vector3 ) )]
	public NodeInput TangentUWs { get; set; }

	[Title( "World Tangent V" ), Hide]
	[Input( typeof( Vector3 ) )]
	public NodeInput TangentVWs { get; set; }

	public TangentViewVector()
	{
		ExpandSize = new Vector2( 32, 0 );
	}

	[Hide]
	[Output( typeof( Vector3 ) )]
	public NodeResult.Func Result => ( GraphCompiler compiler ) =>
	{
		if ( compiler.Graph.Domain is ShaderDomain.PostProcess )
		{
			return NodeResult.Error( $"You cannot use `{DisplayInfo.Name}` nodes with post-processing shaders." );
		}

		var worldPosition = compiler.Result( WorldPosition );
		var worldNormal = compiler.Result( WorldNormal );
		var tangentUws = compiler.Result( TangentUWs );
		var tangentVws = compiler.Result( TangentVWs );

		string func = compiler.RegisterFunction( GetTangentViewVector );
		string funcResult = compiler.ResultFunction( func,
			$"{(worldPosition.IsValid ? worldPosition : "i.vPositionWithOffsetWs.xyz + g_vHighPrecisionLightingOffsetWs.xyz")}",
			$"{(worldNormal.IsValid ? worldNormal : "i.vNormalWs")}",
			$"{(tangentUws.IsValid ? tangentUws : "i.vTangentUWs")}",
			$"{(tangentVws.IsValid ? tangentVws : "i.vTangentVWs")}"
		);

		return new NodeResult( NodeResultType.Vector3, $"{funcResult}" );
	};
}
