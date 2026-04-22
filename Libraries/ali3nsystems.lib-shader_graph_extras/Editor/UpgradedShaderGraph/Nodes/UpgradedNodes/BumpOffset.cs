namespace Editor.ShaderGraphExtras.Nodes;

[Title( "SGE - Bump Offset" ), Category( "Shader Graph Extras - Upgraded" ), Icon( "airline_stops" )]
public sealed class SGEBumpOffsetNode : ShaderNode
{
	public enum SGEBumpOffsetMode
	{
		Iterative,
		Standard
	}
	public enum SGEBumpOffsetChannel
	{
		Red,
		Green,
		Blue,
		Alpha
	}

	[Hide]
	private bool IsIterativeMode => Mode == SGEBumpOffsetMode.Iterative;

	[Hide, JsonIgnore]
	int _lastHashCode = 0;

	public override void OnFrame()
	{
		base.OnFrame();

		var hashCode = new HashCode();
		hashCode.Add( Mode );
		var hc = hashCode.ToHashCode();
		if ( hc != _lastHashCode )
		{
			_lastHashCode = hc;
			CreateInputs();
			Update();
		}
	}

	private void CreateInputs()
	{
		var plugs = new List<IPlugIn>();
		var serialized = this.GetSerialized();
		foreach ( var property in serialized )
		{
			if ( property.TryGetAttribute<InputAttribute>( out var inputAttr ) )
			{
				if ( property.TryGetAttribute<ConditionalVisibilityAttribute>( out var conditionalVisibilityAttr ) )
				{
					if ( conditionalVisibilityAttr.TestCondition( this.GetSerialized() ) )
					{
						continue;
					}
				}
				var propertyInfo = typeof( SGEBumpOffsetNode ).GetProperty( property.Name );
				if ( propertyInfo is null ) continue;
				var info = new PlugInfo( propertyInfo );
				var displayInfo = info.DisplayInfo;
				displayInfo.Name = property.DisplayName;
				info.DisplayInfo = displayInfo;

				var oldPlug = Inputs.FirstOrDefault( x => x is BasePlugIn plugIn && plugIn.Info.Name == property.Name ) as BasePlugIn;
				if ( oldPlug is not null )
				{
					oldPlug.Info.Name = info.Name;
					oldPlug.Info.Type = info.Type;
					oldPlug.Info.DisplayInfo = info.DisplayInfo;
					plugs.Add( oldPlug );
				}
				else
				{
					var plug = new BasePlugIn( this, info, info.Type );
					plugs.Add( plug );
				}
			}
		}
		Inputs = plugs;
	}

	[Input( typeof( Vector2 ) )]
	[Hide]
	public NodeInput Coordinates { get; set; }

	[Input( typeof( Texture2DObject ) )]
	[Hide]
	public NodeInput Texture { get; set; }

	[Input( typeof( Sampler ) )]
	[Hide]
	public NodeInput Sampler { get; set; }

	[Title( "Tangent Space View Direction" )]
	[Input( typeof( Vector3 ) )]
	[Hide]
	public NodeInput TangentSpaceViewDirection { get; set; }

	[Input( typeof( float ) )]
	[Hide]
	public NodeInput Amplitude { get; set; }

	[Title( "Minimum Iterations" )]
	[Input( typeof( float ) )]
	[Hide]
	[ShowIf( nameof( IsIterativeMode ), true )]
	public NodeInput MinimumIterations { get; set; }

	[Title( "Maximum Iterations" )]
	[Input( typeof( float ) )]
	[Hide]
	[ShowIf( nameof( IsIterativeMode ), true )]
	public NodeInput MaximumIterations { get; set; }

	[Title( "Level Of Detail" )]
	[Input( typeof( float ) )]
	[Hide]
	public NodeInput LevelOfDetail { get; set; }

	[Input( typeof( float ) )]
	[Hide]
	public NodeInput Offset { get; set; }

	public SGEBumpOffsetNode()
	{
		ExpandSize = new Vector2( 32, 0 );
	}

	public SGEBumpOffsetMode Mode { get; set; } = SGEBumpOffsetMode.Standard;

	public SGEBumpOffsetChannel Channel { get; set; } = SGEBumpOffsetChannel.Alpha;

	[InputDefault( nameof( Amplitude ) )]
	public float DefaultAmplitude { get; set; } = 0.1f;

	[InputDefault( nameof( MinimumIterations ) )]
	public float DefaultMinimumIterations { get; set; } = 0;

	[InputDefault( nameof( MaximumIterations ) )]
	public float DefaultMaximumIterations { get; set; } = 4;

	[InputDefault( nameof( LevelOfDetail ) )]
	public float DefaultLevelOfDetail { get; set; } = 1f;

	[InputDefault( nameof( Offset ) )]
	public float DefaultOffset { get; set; } = 0.3f;

	[Output( typeof( Vector2 ) )]
	[Hide]
	public NodeResult.Func Output => ( GraphCompiler compiler ) =>
	{
		var coordinates = compiler.Result( Coordinates );
		var texture = compiler.Result( Texture );
		var sampler = compiler.Result( Sampler );
		var tangentSpaceViewDirection = compiler.Result( TangentSpaceViewDirection );
		var amplitude = compiler.ResultOrDefault( Amplitude, DefaultAmplitude );
		var minimumIterations = compiler.ResultOrDefault( MinimumIterations, DefaultMinimumIterations );
		var maximumIterations = compiler.ResultOrDefault( MaximumIterations, DefaultMaximumIterations );
		var levelOfDetail = compiler.ResultOrDefault( LevelOfDetail, DefaultLevelOfDetail );
		var offset = compiler.ResultOrDefault( Offset, DefaultOffset );
		int channel = (int)Channel;

		compiler.RegisterInclude( "shaders/hlsl/functions/func-bump_offset.hlsl" );

		NodeResult nodeResult = new NodeResult();

		switch (Mode)
		{
			case SGEBumpOffsetMode.Iterative:
				nodeResult = new NodeResult(NodeResultType.Vector2, $"SGEBumpOffsetIterative({coordinates}, {texture}, {sampler}, {tangentSpaceViewDirection}, {amplitude}, {minimumIterations},{maximumIterations}, {levelOfDetail}, {offset}, {channel})");
				break;


			case SGEBumpOffsetMode.Standard:
				nodeResult = new NodeResult(NodeResultType.Vector2, $"SGEBumpOffsetStandard({coordinates}, {texture}, {sampler}, {tangentSpaceViewDirection}, {amplitude}, {levelOfDetail}, {offset}, {channel})");
				break;
		}
		return nodeResult;
	};
}