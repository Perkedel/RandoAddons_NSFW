namespace Editor.ShaderGraphExtras.Nodes;

[Title( "SGE - Gerstner Waves" ), Category( "Shader Graph Extras - Universal" ), Icon( "water" )]
public sealed class SGEGerstnerWavesNode : ShaderNode
{
	public enum SGEGerstnerWavesMode
	{
		Linear,
		Circular
	}

	public SGEGerstnerWavesMode Mode { get; set; } = SGEGerstnerWavesMode.Linear;

	[Hide]
	private bool IsLinearMode => Mode == SGEGerstnerWavesMode.Linear;

	[Hide]
	private bool IsCircularMode => Mode == SGEGerstnerWavesMode.Circular;

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
				var propertyInfo = typeof( SGEGerstnerWavesNode ).GetProperty( property.Name );
				if ( propertyInfo is null ) continue;
				var info = new PlugInfo( propertyInfo );
				var displayInfo = info.DisplayInfo;
				displayInfo.Name = property.DisplayName;
				info.DisplayInfo = displayInfo;

				// Try to find existing plug to preserve connections
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

	[Input( typeof( Vector3 ) )]
	[Hide]
	public NodeInput Coordinates { get; set; }

	[Input( typeof( float ) )]
	[Hide]
	public NodeInput Time { get; set; }

	[Input( typeof( Vector2 ) )]
	[Hide]
	[ShowIf( nameof( IsLinearMode ), true )]
	public NodeInput Direction { get; set; }

	[Input(typeof(Vector3))]
	[Hide]
	[ShowIf( nameof( IsCircularMode ), true )]
	public NodeInput Center { get; set; }
	
	[Input(typeof(float))]
	[Hide]
	public NodeInput Amplitude { get; set; }

	[Input( typeof( float ) )]
	[Hide]
	public NodeInput Length { get; set; }

	[Input( typeof( float ) )]
	[Hide]
	public NodeInput Gravity { get; set; }

	[Input(typeof(float))]
	[Hide]
	public NodeInput Steepness { get; set; }

	[InputDefault(nameof( Direction ))]
	[ShowIf( nameof( IsLinearMode ), true )]
	public Vector2 DefaultDirection { get; set; } = new Vector2( 0, 0 );
	
	[InputDefault( nameof( Center ) )]
	[ShowIf( nameof( IsCircularMode ), true )]
	public Vector3 DefaultCenter { get; set; } = new Vector3( 0, 0, 0 );

	[InputDefault( nameof( Amplitude ) )]
	public float DefaultAmplitude { get; set; } = 2;

	[InputDefault( nameof( Length ) )]
	public float DefaultLength { get; set; } = 128f;

	[InputDefault( nameof( Gravity ) )]
	public float DefaultGravity { get; set; } = 9.8f;

	[InputDefault( nameof( Steepness ) )]
	public float DefaultSteepness { get; set; } = 0f;

	[Output( typeof( Vector3 ) )]
	[Hide]
	public NodeResult.Func Normal => ( GraphCompiler compiler ) =>
	{
		var coordinates = compiler.Result( Coordinates );
		var time = compiler.Result( Time ).Cast( 1 );
		var amplitude = compiler.ResultOrDefault( Amplitude, DefaultAmplitude ).Cast( 1 );
		var length = compiler.ResultOrDefault( Length, DefaultLength ).Cast( 1 );
		var gravity = compiler.ResultOrDefault( Gravity, DefaultGravity ).Cast( 1 );
		var steepness = compiler.ResultOrDefault( Steepness, DefaultSteepness ).Cast( 1 );

		compiler.RegisterInclude( "shaders/hlsl/functions/func-gerstner_waves.hlsl" );

		NodeResult nodeResult = new NodeResult();

		switch ( Mode )
		{
			case SGEGerstnerWavesMode.Linear:

				var direction = compiler.ResultOrDefault( Direction, DefaultDirection );
				nodeResult = new NodeResult( NodeResultType.Vector3, $"SGEGerstnerWavesLinearNormal({coordinates}, {time}, {direction}, {amplitude}, {length}, {gravity}, {steepness})");
				break;

			case SGEGerstnerWavesMode.Circular:
				
				var center = compiler.ResultOrDefault( Center, DefaultCenter );
				nodeResult = new NodeResult( NodeResultType.Vector3, $"SGEGerstnerWavesCircularNormal({coordinates}, {time}, {center}, {amplitude}, {length}, {gravity}, {steepness})");
				break;
		}
		return nodeResult;
	};

	[Output( typeof( Vector3 ) )]
	[Hide]
	public NodeResult.Func Displacement => ( GraphCompiler compiler ) =>
	{
		var coordinates = compiler.Result( Coordinates );
		var time = compiler.Result( Time ).Cast( 1 );
		var amplitude = compiler.ResultOrDefault( Amplitude, DefaultAmplitude ).Cast( 1 );
		var length = compiler.ResultOrDefault( Length, DefaultLength ).Cast( 1 );
		var gravity = compiler.ResultOrDefault( Gravity, DefaultGravity ).Cast( 1 );
		var steepness = compiler.ResultOrDefault( Steepness, DefaultSteepness ).Cast( 1 );

		compiler.RegisterInclude( "shaders/hlsl/functions/func-gerstner_waves.hlsl" );

		NodeResult nodeResult = new NodeResult();

		switch ( Mode )
		{
			case SGEGerstnerWavesMode.Linear:

				var direction = compiler.ResultOrDefault( Direction, DefaultDirection );
				nodeResult = new NodeResult( NodeResultType.Vector3, $"SGEGerstnerWavesLinearDisplacement({coordinates}, {time}, {direction}, {amplitude}, {length}, {gravity}, {steepness})");
				break;
				
			case SGEGerstnerWavesMode.Circular:
				
				var center = compiler.ResultOrDefault( Center, DefaultCenter );
				nodeResult = new NodeResult( NodeResultType.Vector3, $"SGEGerstnerWavesCircularDisplacement({coordinates}, {time}, {center}, {amplitude}, {length}, {gravity}, {steepness})");
				break;
		}
		return nodeResult;
	};
}
