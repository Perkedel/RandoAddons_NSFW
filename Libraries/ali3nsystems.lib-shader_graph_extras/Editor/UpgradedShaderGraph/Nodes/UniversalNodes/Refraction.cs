namespace Editor.ShaderGraphExtras.Nodes;

[Title( "SGE - Refraction" ), Category( "Shader Graph Extras - Universal" ), Icon( "lens" )]
public sealed class SGERefractionNode : ShaderNode
{
	[Hide]
	public static string SGERefraction => @"
		float3 SGERefraction(float3 viewDirection, float3 worldNormal, float indexOfRefraction)
		{
			return refract(viewDirection, worldNormal, indexOfRefraction);
		}
		";

	[Input( typeof( Vector3 ) )]
	[Title("View Direction")]
	[Hide]
	public NodeInput ViewDirection { get; set; }

	[Input( typeof( Vector3 ) )]
	[Title("World Normal")]
	[Hide]
	public NodeInput WorldNormal { get; set; }

	[Input (typeof(float))]
	[Title("Index Of Refraction")]
	[Hide]
	public NodeInput IndexOfRefraction {get;set;}

	[InputDefault( nameof( IndexOfRefraction ) )]
	public float DefaultIndexOfRefraction {get; set;} = 0.5f;

	[Output( typeof( Vector3 ) )] 
	[Hide]
	public NodeResult.Func Output => ( GraphCompiler compiler ) =>
	{
		var viewDirection = compiler.Result(ViewDirection);
		var worldNormal = compiler.Result(WorldNormal);
		var indexOfRefraction = compiler.ResultOrDefault(IndexOfRefraction,DefaultIndexOfRefraction);

		return new NodeResult(NodeResultType.Vector3, compiler.ResultFunction( compiler.RegisterFunction( SGERefraction ), $"{viewDirection}, {worldNormal}, {indexOfRefraction}"));
	};
}