namespace Editor.ShaderGraphExtras.Nodes;

[Title( "SGE - Rotate UV" ), Category( "Shader Graph Extras - Universal" ), Icon( "360" )]
public sealed class SGERotateUVNode : ShaderNode
{
	public enum SGERotateUVMode
	{
		Degrees,
		Radians
	}
	
	[Hide]
	public static string SGERotateUVDegrees => @"
		float2 SGERotateUVDegrees( float2 coordinates, float2 center, float rotation )
		{
			rotation *= 3.1415926f/180.0f;
			coordinates -= center;
			float s = sin(rotation);
			float c = cos(rotation);
			float2x2 rotationMatrix = float2x2(c, -s, s, c);
			rotationMatrix *= 0.5;
			rotationMatrix += 0.5;
			rotationMatrix = rotationMatrix * 2 - 1;
			coordinates.xy = mul(coordinates.xy, rotationMatrix);
			coordinates += center;
			return coordinates;
		}
		";

	[Hide]
	public static string SGERotateUVRadians => @"
		float2 SGERotateUVRadians( float2 coordinates, float2 center, float rotation )
		{
			coordinates -= center;
			float s = sin(rotation);
			float c = cos(rotation);
			float2x2 rotationMatrix = float2x2(c, -s, s, c);
			rotationMatrix *= 0.5;
			rotationMatrix += 0.5;
			rotationMatrix = rotationMatrix * 2 - 1;
			coordinates.xy = mul(coordinates.xy, rotationMatrix);
			coordinates += center;
			return coordinates;
		}
		";

	[Input (typeof( Vector2 ))]
	[Hide]
	public NodeInput Coordinates {get; set;}
	
	[Input (typeof( Vector2 ))]
	[Hide]
	public NodeInput Center {get; set;}
	
	[Input (typeof( float ))]
	[Hide]
	public NodeInput Rotation {get; set;}
	
	[InputDefault( nameof( Center ) )]
	public Vector2 DefaultCenter {get; set;} = new Vector2(0.5f, 0.5f);

	[InputDefault( nameof( Rotation ) )]
	public float DefaultRotation {get; set;} = 0f;
	public SGERotateUVMode Mode {get; set;} = SGERotateUVMode.Degrees;

	[Output( typeof( Vector2 ) )]
	[Hide]
	public NodeResult.Func Output => ( GraphCompiler compiler ) =>
	{
		var coordinates = compiler.Result(Coordinates).Cast(2);
		var center = compiler.ResultOrDefault(Center, DefaultCenter).Cast(2);
		var rotation = compiler.ResultOrDefault(Rotation, DefaultRotation).Cast(1);
		
		NodeResult nodeResult = new NodeResult();

		switch (Mode)
		{
			case SGERotateUVMode.Degrees:
				nodeResult = new NodeResult( NodeResultType.Vector2 , compiler.ResultFunction( compiler.RegisterFunction(SGERotateUVDegrees), $"{coordinates}, {center}, {rotation}" ) );
				break;

			case SGERotateUVMode.Radians:
				nodeResult = new NodeResult( NodeResultType.Vector2 , compiler.ResultFunction( compiler.RegisterFunction(SGERotateUVRadians), $"{coordinates}, {center}, {rotation}" ) );
				break;
		}
		
		return nodeResult;
	};
}