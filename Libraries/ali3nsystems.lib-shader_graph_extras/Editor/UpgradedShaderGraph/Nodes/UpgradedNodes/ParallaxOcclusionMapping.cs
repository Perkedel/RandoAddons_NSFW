namespace Editor.ShaderGraphExtras.Nodes;

[Title("SGE - Parallax Occlusion Mapping"), Category( "Shader Graph Extras - Upgraded" ), Icon("auto_awesome_motion")]
public sealed class SGEParallaxOcclusionMappingNode : ShaderNode
{

	public enum SGEParallaxOcclusionMappingMode
	{
		Standard,
		Steep
	}

	public enum SGEParallaxOcclusionMappingChannel
	{
		Red,
		Green,
		Blue,
		Alpha
	}

	[Input(typeof(Vector2))]
	[Hide]
	public NodeInput Coordinates { get; set; }

	[Input(typeof(Texture2DObject))]
	[Hide]
	public NodeInput Texture { get; set; }

	[Input(typeof(Sampler))]
	[Hide]
	public NodeInput Sampler { get; set; }

	[Title("Tangent Space View Direction")]
	[Input(typeof(Vector3))]
	[Hide]
	public NodeInput TangentSpaceViewDirection { get; set; }

	[Input(typeof(float))]
	[Hide]
	public NodeInput Amplitude { get; set; }

	[Title("Minimum Iterations")]
	[Input(typeof(int))]
	[Hide]
	public NodeInput MinimumIterations { get; set; }

	[Title("Maximum Iterations")]
	[Input(typeof(int))]
	[Hide]
	public NodeInput MaximumIterations { get; set; }

	[Title ("Level Of Detail")]
	[Input(typeof(float))]
	[Hide]
	public NodeInput LevelOfDetail { get; set; }

	[Input(typeof(float))]
	[Hide]
	public NodeInput Offset { get; set; }

	public SGEParallaxOcclusionMappingNode()
	{
		ExpandSize = new Vector2( 32, 0 );
	}

	public SGEParallaxOcclusionMappingMode Mode { get; set; } = SGEParallaxOcclusionMappingMode.Standard;
	public SGEParallaxOcclusionMappingChannel Channel { get; set; } = SGEParallaxOcclusionMappingChannel.Alpha;

	[InputDefault( nameof( Amplitude ) )]
	public float DefaultAmplitude { get; set; } = 0.1f;

	[InputDefault(nameof(MinimumIterations))]
	public float DefaultMinimumIterations { get; set; } = 8;
	
	[InputDefault( nameof( MaximumIterations ) )]
	public float DefaultMaximumIterations { get; set; } = 32;

	[InputDefault( nameof( LevelOfDetail ) )]
	public float DefaultLevelOfDetail { get; set; } = 1f;
	
	[InputDefault( nameof( Offset ) )]
	public float DefaultOffset { get; set; } = 0.3f;

	[Output(typeof(Vector2))]
	[Hide]
	public NodeResult.Func Output => (GraphCompiler compiler) =>
    {
		var coordinates = compiler.Result(Coordinates);
		var texture = compiler.Result( Texture );
		var sampler = compiler.Result( Sampler );
		var tangentSpaceViewDirection = compiler.Result(TangentSpaceViewDirection);
		var amplitude = compiler.ResultOrDefault(Amplitude, DefaultAmplitude);
		var maximumIterations = compiler.ResultOrDefault(MaximumIterations, DefaultMaximumIterations);
		var minimumIterations = compiler.ResultOrDefault(MinimumIterations, DefaultMinimumIterations);
		var levelOfDetail = compiler.ResultOrDefault( LevelOfDetail, DefaultLevelOfDetail );
		var offset = compiler.ResultOrDefault(Offset, DefaultOffset);
		int channel = (int)Channel;

		compiler.RegisterInclude( "shaders/hlsl/functions/func-parallax_occlusion_mapping.hlsl" );

		NodeResult nodeResult = new NodeResult();

		switch (Mode)
		{
			case SGEParallaxOcclusionMappingMode.Standard:
				nodeResult = new NodeResult(NodeResultType.Vector2, $"SGEParallaxOcclusionMappingStandard({coordinates}, {texture}, {sampler}, {tangentSpaceViewDirection}, {amplitude}, {minimumIterations},{maximumIterations},  {levelOfDetail}, {offset}, {channel})");
				break;


			case SGEParallaxOcclusionMappingMode.Steep:
				nodeResult = new NodeResult(NodeResultType.Vector2, $"SGEParallaxOcclusionMappingSteep({coordinates}, {texture}, {sampler}, {tangentSpaceViewDirection}, {amplitude}, {minimumIterations},{maximumIterations},  {levelOfDetail}, {offset}, {channel})");
				break;
		}

		return nodeResult;
	};
}
