namespace Editor.ShaderGraphExtras.Nodes;

[Title( "SGE - Normal Blend" ), Category( "Shader Graph Extras - Universal" ), Icon( "blender" )]

public sealed class SGENormalBlendNode : ShaderNode
{
	public enum SGENormalBlendMode
	{
		Reoriented,
		Whiteout
	}
	
	[Hide]
	public static string SGENormalBlendReoriented => @"					
		float3 SGENormalBlendReoriented(float3 inputA, float3 inputB)
		{
			float3 t = inputA.rgb + float3(0.0, 0.0, 1.0);
			float3 u = inputB.rgb * float3(-1.0, -1.0, 1.0);
			return float3((t / t.b) * dot(t, u) - u);
		}
		";

	[Hide]
	public static string SGENormalBlendWhiteout => @"
		float3 SGENormalBlendWhiteout(float3 inputA, float3 inputB)
		{
			return normalize(float3(inputA.rg + inputB.rg, inputA.b * inputB.b));
		}
		";
	
	[Title("Input A")]
	[Input( typeof( Vector3 ) )]
	[Hide]
	public NodeInput InputA { get; set; }
	
	[Title("Input B")]
	[Input( typeof( Vector3 ) )]
	[Hide]
	public NodeInput InputB { get; set; }

	public SGENormalBlendMode Mode { get; set; } = SGENormalBlendMode.Reoriented;

	[Output( typeof( Vector3 ))]
	[Hide]
	public NodeResult.Func Output => ( GraphCompiler compiler ) =>
	{
		var inputA = compiler.ResultOrDefault(InputA, Color.Blue).Cast(3);
		var inputB = compiler.ResultOrDefault(InputB, Color.Blue).Cast(3);

		NodeResult nodeResult = new NodeResult();
		
		switch (Mode)
		{
			case SGENormalBlendMode.Reoriented:
				nodeResult = new NodeResult(NodeResultType.Vector3 , compiler.ResultFunction(compiler.RegisterFunction( SGENormalBlendReoriented ), $"{inputA}, {inputB}"));
				break;

			case SGENormalBlendMode.Whiteout:
				nodeResult = new NodeResult(NodeResultType.Vector3 , compiler.ResultFunction(compiler.RegisterFunction( SGENormalBlendWhiteout ), $"{inputA}, {inputB}"));
				break;
		}

		return nodeResult;
	};
}