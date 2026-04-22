namespace Editor.ShaderGraphExtras.Nodes;

[Title("SGE - Vertex Tint"), Category( "Shader Graph Extras - Upgraded" ), Icon("format_paint")]

public sealed class SGEVertexTintNode : ShaderNode
{
	[Output( typeof( Vector3 ) )]
	[Hide]
	public static NodeResult.Func RGB => ( GraphCompiler compiler ) => new( NodeResultType.Vector3, "i.vPaintValues.xyz" );

	[Output( typeof( float ) )]
	[Hide]
	public static NodeResult.Func Alpha => ( GraphCompiler compiler ) => new( NodeResultType.Float, "i.vPaintValues.w" );
}