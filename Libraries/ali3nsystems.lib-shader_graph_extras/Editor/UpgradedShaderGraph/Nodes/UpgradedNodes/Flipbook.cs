namespace Editor.ShaderGraphExtras.Nodes;

[Title( "SGE - Flipbook" ), Category( "Shader Graph Extras - Upgraded" ), Icon( "menu_book" )]

public sealed class SGEFlipbookNode : ShaderNode
{
	public enum SGEFlipbookMode
	{
		Interpolated,
		Standard
	}

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
	public NodeInput Width  { get; set; }

	[Input( typeof( float ) )]
	[Hide]
	public NodeInput Height { get; set; }

	[Input( typeof( float ) )]
	[Hide]
	public NodeInput Tile { get; set; }

	[Input( typeof( Vector2 ) )]
	[Hide]
	public NodeInput Invert { get; set; }

	[Input( typeof( float ) )]
	[Hide]
	public NodeInput Loop { get; set; }

	[InputDefault( nameof( Width ) )]
	public float DefaultWidth { get; set; } = 8f;

	[InputDefault( nameof( Height ) )]
	public float DefaultHeight { get; set; } = 8f;

	[InputDefault( nameof( Tile ) )]
	public float DefaultTile { get; set; } = 1f;

	[InputDefault( nameof( Invert ) )]
	public Vector2 DefaultInvert { get; set; } = new Vector2( 0f, 0f );

	[InputDefault( nameof( Loop ) )]
	public float DefaultLoop { get; set; } = 1f;

	public SGEFlipbookMode Mode {get; set;} = SGEFlipbookMode.Standard;

	[Output( typeof( Color ) )]
	[Hide]
	public NodeResult.Func Output => ( GraphCompiler compiler ) =>
	{
		var coordinates = compiler.Result(Coordinates);
		var texture = compiler.Result(Texture);
		var sampler = compiler.Result(Sampler);
		var width = compiler.ResultOrDefault(Width,DefaultWidth);
		var height = compiler.ResultOrDefault(Height,DefaultHeight);
		var tile = compiler.ResultOrDefault( Tile, DefaultTile );
		var invert = compiler.ResultOrDefault( Invert, DefaultInvert );
		var loop = compiler.ResultOrDefault( Loop, DefaultLoop );

		compiler.RegisterInclude( "shaders/HLSL/Functions/FUNC-flipbook.hlsl" );

		NodeResult nodeResult = new NodeResult();

		switch ( Mode )
		{
			case SGEFlipbookMode.Interpolated:
				nodeResult = new NodeResult( NodeResultType.Color, $"SGEFlipbookInterpolated({coordinates}, {texture}, {sampler}, {width}, {height}, {tile}, {invert}, {loop})" );
				break;

			case SGEFlipbookMode.Standard:
				nodeResult = new NodeResult( NodeResultType.Color, $"SGEFlipbookStandard({coordinates}, {texture}, {sampler}, {width}, {height}, {tile}, {invert}, {loop})" );
				break;
		}

		return nodeResult;
	};
}