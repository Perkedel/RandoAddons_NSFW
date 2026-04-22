namespace Editor.ShaderGraph.Nodes;

[Title( "SGE - Rotate About Axis" ), Category( "Shader Graph Extras - Universal" ), Icon( "360" )]
public sealed class SGERotateAboutAxisNode : ShaderNode
{
	public enum SGERotateAboutAxisMode
	{
		Degrees,
		Radians
	}

	[Hide]
	public static string SGERotateAboutAxisDegrees => @"
		float3 SGERotateAboutAxisDegrees( float3 coordinates, float3 axis, float rotation )
		{
			rotation *= 3.1415926f/180.0f;
			float s = sin(rotation);
			float c = cos(rotation);
			float inverted_c = 1.0 - c;

			axis = normalize(axis);
			float3x3 rotationMatrix =
			{   inverted_c * axis.x * axis.x + c, inverted_c * axis.x * axis.y - axis.z * s, inverted_c * axis.z * axis.x + axis.y * s,
				inverted_c * axis.x * axis.y + axis.z * s, inverted_c * axis.y * axis.y + c, inverted_c * axis.y * axis.z - axis.x * s,
				inverted_c * axis.z * axis.x - axis.y * s, inverted_c * axis.y * axis.z + axis.x * s, inverted_c * axis.z * axis.z + c
			};
			return float3(mul(rotationMatrix, coordinates));
		}
		";

	[Hide]
	public static string SGERotateAboutAxisRadians => @"
		float3 SGERotateAboutAxisRadians( float3 coordinates, float3 axis, float rotation )
		{
			float s = sin(rotation);
			float c = cos(rotation);
			float inverted_c = 1.0 - c;

			axis = normalize(axis);
			float3x3 rotationMatrix =
			{   inverted_c * axis.x * axis.x + c, inverted_c * axis.x * axis.y - axis.z * s, inverted_c * axis.z * axis.x + axis.y * s,
				inverted_c * axis.x * axis.y + axis.z * s, inverted_c * axis.y * axis.y + c, inverted_c * axis.y * axis.z - axis.x * s,
				inverted_c * axis.z * axis.x - axis.y * s, inverted_c * axis.y * axis.z + axis.x * s, inverted_c * axis.z * axis.z + c
			};
			return float3(mul(rotationMatrix, coordinates));
		}
		";

	[Input (typeof( Vector3 ))]
	[Hide]
	public NodeInput Coordinates {get; set;}
	
	[Input (typeof( Vector3 ))]
	[Hide]
	public NodeInput Axis {get; set;}
	
	[Input( typeof( float ) )]
	[Hide]
	public NodeInput Rotation { get; set; }
	
	[InputDefault( nameof( Axis ) )]
	public Vector3 DefaultAxis {get; set;} = new Vector3(0, 0, 1);

	[InputDefault( nameof( Rotation ) )]
	public float DefaultRotation {get; set;} = 0f;
	public SGERotateAboutAxisMode Mode {get; set;} = SGERotateAboutAxisMode.Degrees;

	[Output( typeof( Vector3 ) )]
	[Hide]
	public NodeResult.Func Output => ( GraphCompiler compiler ) =>
	{
		var coordinates = compiler.Result(Coordinates).Cast(3);
		var axis = compiler.ResultOrDefault(Axis, DefaultAxis).Cast(3);
		var rotation = compiler.ResultOrDefault(Rotation, DefaultRotation).Cast(1);
		
		NodeResult nodeResult = new NodeResult();

		switch (Mode)
		{
			case SGERotateAboutAxisMode.Degrees:
				nodeResult = new NodeResult( NodeResultType.Vector3 , compiler.ResultFunction( compiler.RegisterFunction(SGERotateAboutAxisDegrees), $"{coordinates}, {axis}, {rotation}" ) );
				break;

			case SGERotateAboutAxisMode.Radians:
				nodeResult = new NodeResult( NodeResultType.Vector3 , compiler.ResultFunction( compiler.RegisterFunction(SGERotateAboutAxisRadians), $"{coordinates}, {axis}, {rotation}" ) );
				break;
		}
		
		return nodeResult;
	};
}