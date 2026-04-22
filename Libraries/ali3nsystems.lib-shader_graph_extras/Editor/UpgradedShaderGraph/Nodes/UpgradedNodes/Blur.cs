namespace Editor.ShaderGraphExtras.Nodes;

[Title( "SGE - Blur" ), Category( "Shader Graph Extras - Upgraded" ), Icon( "blur_on" )]
public sealed class SGEBlurNode : ShaderNode
{
	[Hide]
	public string SGEBlur => @"
		float3 SGEBlur( float2 coordinates, Texture2D texture, SamplerState sampler, float value, float directions, float quality, float taps )
		{
			float twoPi = 6.28318530718f;

			float3 color = texture.Sample( sampler, coordinates ).rgb;

			[unroll]
			for( float d=0.0; d<twoPi; d+=twoPi/directions)
			{
				[unroll]
				for(float j=1.0/quality; j<=1.0; j+=1.0/quality)
				{
					taps += 1;
					color += texture.Sample( sampler, coordinates + float2( cos(d), sin(d) ) * value * 0.1f * j ).rgb;
				}
			}
			return color / taps;
		}
		";

	[Input( typeof( Vector2 ) )] 
	[Hide]
	public NodeInput Coordinates { get; set; }
	
	[Input(typeof(Texture2DObject))]
	[Hide]
	public NodeInput Texture { get; set; }
	
	[Input(typeof(Sampler))]
	[Hide]
	public NodeInput Sampler { get; set; }

	[Input( typeof( float ) )] 
	[Hide]
	public NodeInput Value { get; set; }

	[Input( typeof( float ) )] 
	[Hide]
	public NodeInput Directions { get; set; }
	
	[Input( typeof( float ) )] 
	[Hide]
	public NodeInput Quality { get; set; }
	
	[Input( typeof( float ) )] 
	[Hide]
	public NodeInput Taps { get; set; }

	[InputDefault( nameof( Value ) )]
	public float DefaultValue { get; set; } = 1f;

	[InputDefault( nameof( Directions ) )]
	public float DefaultDirections { get; set; } = 16f;
	
	[InputDefault( nameof( Quality ) )]
	public float DefaultQuality { get; set; } = 4f;
	
	[InputDefault( nameof( Taps ) )]
	public float DefaultTaps { get; set; } = 1f;

	[Output( typeof( Vector3 ) )] 
	[Hide]
	public NodeResult.Func Output => ( GraphCompiler compiler ) =>
	{
		var coordinates = compiler.Result(Coordinates);
		var texture = compiler.Result(Texture);
		var sampler = compiler.Result(Sampler);
		var value = compiler.ResultOrDefault(Value, DefaultValue);
		var directions = compiler.ResultOrDefault(Directions, DefaultDirections);
		var quality = compiler.ResultOrDefault(Quality, DefaultQuality);
		var taps = compiler.ResultOrDefault(Taps, DefaultTaps);

		return new NodeResult(NodeResultType.Vector3, compiler.ResultFunction(compiler.RegisterFunction(SGEBlur), $"{coordinates}, {texture}, {sampler}, {value}, {directions}, {quality}, {taps}"));
	};
}