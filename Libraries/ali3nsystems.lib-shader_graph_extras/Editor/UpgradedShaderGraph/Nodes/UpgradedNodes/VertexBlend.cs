namespace Editor.ShaderGraphExtras.Nodes;

[Title("SGE - Vertex Blend"), Category( "Shader Graph Extras - Upgraded" ), Icon("brush")]
public sealed class SGEVertexBlendNode : ShaderNode
{
	[Output( typeof( float ) )]
	[Hide]
	public static NodeResult.Func R => ( GraphCompiler compiler ) => new( NodeResultType.Float, "i.vBlendValues.x" );

	[Output( typeof( float ) )]
	[Hide]
	public static NodeResult.Func G => ( GraphCompiler compiler ) => new( NodeResultType.Float, "i.vBlendValues.y" );

	[Output( typeof( float ) )]
	[Hide]
	public static NodeResult.Func B => ( GraphCompiler compiler ) => new( NodeResultType.Float, "i.vBlendValues.z" );

	[Output( typeof( float ) )]
	[Hide]
	public static NodeResult.Func A => ( GraphCompiler compiler ) => new( NodeResultType.Float, "i.vBlendValues.w" );
}