namespace Editor.ShaderGraphExtras.Nodes;

[Title( "SGE - Decal Projection" ), Category( "Shader Graph Extras - Upgraded" ), Icon( "connected_tv" )]
public sealed class SGEDecalProjectionNode : ShaderNode
{
[Output( typeof( Vector3 ) )]
[Hide]
public static NodeResult.Func Forward => ( GraphCompiler compiler ) => new( NodeResultType.Vector3, "i.vDecalForward" );

[Output( typeof( Vector3 ) )]
[Hide]
public static NodeResult.Func Right => ( GraphCompiler compiler ) => new( NodeResultType.Vector3, "i.vDecalRight" );

[Output( typeof( Vector3 ) )]
[Hide]
public static NodeResult.Func Up => ( GraphCompiler compiler ) => new( NodeResultType.Vector3, "i.vDecalUp" );

}
