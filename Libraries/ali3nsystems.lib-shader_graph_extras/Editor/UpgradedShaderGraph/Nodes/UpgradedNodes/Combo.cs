namespace Editor.ShaderGraphExtras.Nodes;

[Title( "SGE - Combo" ), Category( "Shader Graph Extras - Upgraded" ), Icon( "compare_arrows" )]
public sealed class SGEComboNode : ShaderNode
{
	//Inputs 

	[Input( typeof( Vector4 ) )]
	[Hide]
	[ShowIf( nameof( IsBoolMode ), true )]
	[Title( "True" )]
	public NodeInput InputTrue { get; set; }

	[Input( typeof( Vector4 ) )]
	[Hide]
	[ShowIf( nameof( IsBoolMode ), true )]
	[Title( "False" )]
	public NodeInput InputFalse { get; set; }

	[Input( typeof( Vector4 ) )]
	[Hide]
	[ShowIf( nameof( IsEnumModeState1 ), true )]
	[Title( "State 1" )]
	public NodeInput InputState1 { get; set; }

	[Input( typeof( Vector4 ) )]
	[Hide]
	[ShowIf( nameof( IsEnumModeState2 ), true )]
	[Title( "State 2" )]
	public NodeInput InputState2 { get; set; }

	[Input( typeof( Vector4 ) )]
	[Hide]
	[ShowIf( nameof( IsEnumModeState3 ), true )]
	[Title( "State 3" )]
	public NodeInput InputState3 { get; set; }

	[Input( typeof( Vector4 ) )]
	[Hide]
	[ShowIf( nameof( IsEnumModeState4 ), true )]
	[Title( "State 4" )]
	public NodeInput InputState4 { get; set; }

	[Input( typeof( Vector4 ) )]
	[Hide]
	[ShowIf( nameof( IsEnumModeState5 ), true )]
	[Title( "State 5" )]
	public NodeInput InputState5 { get; set; }

	[Input( typeof( Vector4 ) )]
	[Hide]
	[ShowIf( nameof( IsEnumModeState6 ), true )]
	[Title( "State 6" )]
	public NodeInput InputState6 { get; set; }

	[Input( typeof( Vector4 ) )]
	[Hide]
	[ShowIf( nameof( IsEnumModeState7 ), true )]
	[Title( "State 7" )]
	public NodeInput InputState7 { get; set; }

	[Input( typeof( Vector4 ) )]
	[Hide]
	[ShowIf( nameof( IsEnumModeState8 ), true )]
	[Title( "State 8" )]
	public NodeInput InputState8 { get; set; }

	[Title( "Value" )]
	[Input( typeof( int ) )]
	[Hide]
	public NodeInput Value { get; set; }

	//Inspector

	[Title( "Mode" ),Group("Properties")]
	public ComboMode Mode { get; set; } = ComboMode.Static;

	[Title( "Type" ),Group("Properties")]
	public ComboType Type { get; set; } = ComboType.Bool;

	[Title("Value"),Group("Properties")]
	[InputDefault( nameof( Value ) )]
	public int DefaultValue
	{
		get => _defaultValue;
		set => _defaultValue = Math.Clamp( value, 1, IsEnumMode ? StateCount : 2 );
	}

	[Hide, JsonIgnore]
	private int _defaultValue = 1;

	[Title( "State Count" ),Group("Properties")]
	[ShowIf( nameof( IsEnumMode ), true )]
	public int StateCount
	{
		get => _stateCount;
		set => _stateCount = Math.Clamp( value, 2, 8 );
	}
	
	[Hide, JsonIgnore]
	private int _stateCount = 2;

	[Title( "Name" ),Group("UI")]
	public ComboName Name { get; set; } = "Combo Name";

	[Title( "Group" ),Group("UI")]
	public ComboGroup Group { get; set; } = "Combo Group";

	[Title( "State 1" ),Group("UI")]
	[ShowIf( nameof( IsEnumMode ), true )]
	public string State1 { get; set; } = "State 1";

	[Title( "State 2" ),Group("UI")]
	[ShowIf( nameof( IsEnumMode ), true )]
	public string State2 { get; set; } = "State 2";

	[Title( "State 3" ),Group("UI")]
	[ShowIf( nameof( HasState3 ), true )]
	public string State3 { get; set; } = "State 3";

	[Title( "State 4" ),Group("UI")]
	[ShowIf( nameof( HasState4 ), true )]
	public string State4 { get; set; } = "State 4";

	[Title( "State 5" ),Group("UI")]
	[ShowIf( nameof( HasState5 ), true )]
	public string State5 { get; set; } = "State 5";

	[Title( "State 6" ),Group("UI")]
	[ShowIf( nameof( HasState6 ), true )]
	public string State6 { get; set; } = "State 6";

	[Title( "State 7" ),Group("UI")]
	[ShowIf( nameof( HasState7 ), true )]
	public string State7 { get; set; } = "State 7";

	[Title( "State 8" ),Group("UI")]
	[ShowIf( nameof( HasState8 ), true )]
	public string State8 { get; set; } = "State 8";

	[Hide]
	private bool IsEnumMode => Type == ComboType.Enum;

	[Hide]
	private bool IsBoolMode => Type == ComboType.Bool;

	[Hide]
	private bool HasState3 => IsEnumMode && StateCount >= 3;

	[Hide]
	private bool HasState4 => IsEnumMode && StateCount >= 4;

	[Hide]
	private bool HasState5 => IsEnumMode && StateCount >= 5;

	[Hide]
	private bool HasState6 => IsEnumMode && StateCount >= 6;

	[Hide]
	private bool HasState7 => IsEnumMode && StateCount >= 7;

	[Hide]
	private bool HasState8 => IsEnumMode && StateCount >= 8;

	[Hide, JsonIgnore]
	private static Dictionary<string, (string nodeId, int hashCode)> _lastModifiedByIdentity = new();
	
	[Hide, JsonIgnore]
	private string ComboIdentity => Name.Name;

	[Hide]
	private bool IsEnumModeState1 => IsEnumMode;

	[Hide]
	private bool IsEnumModeState2 => IsEnumMode;

	[Hide]
	private bool IsEnumModeState3 => IsEnumMode && StateCount >= 3;

	[Hide]
	private bool IsEnumModeState4 => IsEnumMode && StateCount >= 4;

	[Hide]
	private bool IsEnumModeState5 => IsEnumMode && StateCount >= 5;

	[Hide]
	private bool IsEnumModeState6 => IsEnumMode && StateCount >= 6;

	[Hide]
	private bool IsEnumModeState7 => IsEnumMode && StateCount >= 7;

	[Hide]
	private bool IsEnumModeState8 => IsEnumMode && StateCount >= 8;

	[Hide, JsonIgnore]
	int _lastHashCode = 0;

	public override void OnFrame()
	{
		base.OnFrame();

		SyncWithMatchingCombos();

		var hashCode = new HashCode();
		hashCode.Add( Type );
		hashCode.Add( StateCount );
		hashCode.Add( State1 );
		hashCode.Add( State2 );
		hashCode.Add( State3 );
		hashCode.Add( State4 );
		hashCode.Add( State5 );
		hashCode.Add( State6 );
		hashCode.Add( State7 );
		hashCode.Add( State8 );
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
				if ( property.TryGetAttribute<ConditionalVisibilityAttribute>( out var conditionalVisibilityAttribute ) )
				{
					if ( conditionalVisibilityAttribute.TestCondition( this.GetSerialized() ) )
					{
						continue;
					}
				}
				var propertyInfo = typeof( SGEComboNode ).GetProperty( property.Name );
				if ( propertyInfo is null ) continue;
				var info = new PlugInfo( propertyInfo );
				var displayInfo = info.DisplayInfo;

				displayInfo.Name = property.Name switch
				{
					nameof( InputTrue ) => "True",
					nameof( InputFalse ) => "False",
					nameof( InputState1 ) => State1,
					nameof( InputState2 ) => State2,
					nameof( InputState3 ) => State3,
					nameof( InputState4 ) => State4,
					nameof( InputState5 ) => State5,
					nameof( InputState6 ) => State6,
					nameof( InputState7 ) => State7,
					nameof( InputState8 ) => State8,
					nameof( DefaultValue ) => "Default Value",
					_ => property.DisplayName
				};

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

	private int GetSyncablePropertiesHash()
	{
		var hash = new HashCode();
		hash.Add( Type );
		hash.Add( Mode );
		hash.Add( Group.Group );
		hash.Add( StateCount );
		hash.Add( DefaultValue );
		hash.Add( State1 );
		hash.Add( State2 );
		hash.Add( State3 );
		hash.Add( State4 );
		hash.Add( State5 );
		hash.Add( State6 );
		hash.Add( State7 );
		hash.Add( State8 );
		return hash.ToHashCode();
	}
	private void SyncWithMatchingCombos()
	{
		if ( Graph == null ) return;

		var identity = ComboIdentity;
		var currentHash = GetSyncablePropertiesHash();

		var myPreviousHash = GetStoredHashForNode( identity, Identifier );
		var isNewToIdentity = myPreviousHash == 0;

		if ( _lastModifiedByIdentity.TryGetValue( identity, out var last ) )
		{
			if ( isNewToIdentity )
			{
				var master = Graph.Nodes.OfType<SGEComboNode>()
					.FirstOrDefault( n => n.Identifier == last.nodeId && n.ComboIdentity == identity );
				if ( master != null && master != this )
				{
					PullSyncFrom( master );
					StoreHashForNode( identity, Identifier, GetSyncablePropertiesHash() );
				}
				else
				{
					_lastModifiedByIdentity[identity] = (Identifier, currentHash);
					StoreHashForNode( identity, Identifier, currentHash );
				}
				return;
			}

			if ( last.nodeId == Identifier )
			{
				if ( last.hashCode != currentHash )
				{
					PushSyncToOthers( identity );
					_lastModifiedByIdentity[identity] = (Identifier, currentHash);
				}
				return;
			}
			else
			{
				if ( myPreviousHash != currentHash )
				{
					PushSyncToOthers( identity );
					_lastModifiedByIdentity[identity] = (Identifier, currentHash);
					return;
				}

				var master = Graph.Nodes.OfType<SGEComboNode>()
					.FirstOrDefault( n => n.Identifier == last.nodeId && n.ComboIdentity == identity );
				if ( master != null && master != this )
				{
					PullSyncFrom( master );
				}
			}
		}
		else
		{
			var existingNode = Graph.Nodes.OfType<SGEComboNode>()
				.FirstOrDefault( n => n != this && n.ComboIdentity == identity );

			if ( existingNode != null )
			{
				PullSyncFrom( existingNode );
				_lastModifiedByIdentity[identity] = (existingNode.Identifier, existingNode.GetSyncablePropertiesHash());
				StoreHashForNode( identity, Identifier, GetSyncablePropertiesHash() );
			}
			else
			{
				_lastModifiedByIdentity[identity] = (Identifier, currentHash);
				StoreHashForNode( identity, Identifier, currentHash );
			}
		}
	}

	[Hide, JsonIgnore]
	private static Dictionary<string, Dictionary<string, int>> _nodeHashCache = new();

	private int GetStoredHashForNode( string identity, string nodeId )
	{
		if ( _nodeHashCache.TryGetValue( identity, out var nodeHashes ) )
		{
			if ( nodeHashes.TryGetValue( nodeId, out var hash ) )
				return hash;
		}
		return 0;
	}

	private void StoreHashForNode( string identity, string nodeId, int hash )
	{
		if ( !_nodeHashCache.ContainsKey( identity ) )
			_nodeHashCache[identity] = new Dictionary<string, int>();
		_nodeHashCache[identity][nodeId] = hash;
	}

	private void PushSyncToOthers( string identity )
	{
		if ( Graph == null ) return;

		var others = Graph.Nodes.OfType<SGEComboNode>()
			.Where( n => n != this && n.ComboIdentity == identity );

		foreach ( var other in others )
		{
			other.Type = this.Type;
			other.Mode = this.Mode;
			other.Group = this.Group;
			other.StateCount = this.StateCount;
			other.DefaultValue = this.DefaultValue;
			other.State1 = this.State1;
			other.State2 = this.State2;
			other.State3 = this.State3;
			other.State4 = this.State4;
			other.State5 = this.State5;
			other.State6 = this.State6;
			other.State7 = this.State7;
			other.State8 = this.State8;

			StoreHashForNode( identity, other.Identifier, GetSyncablePropertiesHash() );

			other.IsDirty = true;
			other.Update();
		}
		StoreHashForNode( identity, Identifier, GetSyncablePropertiesHash() );
	}
	private void PullSyncFrom( SGEComboNode master )
	{
		Type = master.Type;
		Mode = master.Mode;
		Group = master.Group;
		StateCount = master.StateCount;
		DefaultValue = master.DefaultValue;
		State1 = master.State1;
		State2 = master.State2;
		State3 = master.State3;
		State4 = master.State4;
		State5 = master.State5;
		State6 = master.State6;
		State7 = master.State7;
		State8 = master.State8;

		StoreHashForNode( ComboIdentity, Identifier, GetSyncablePropertiesHash() );

		IsDirty = true;
		Update();
	}

	[Output( typeof( Vector4 ) )]
	[Hide]
	public NodeResult.Func Output => ( GraphCompiler compiler ) =>
	{
		string hlslName = Name.Name.Replace( ' ', '_' );

		var combo = new ComboDeclaration
		{
			HLSLName = hlslName,
			DisplayName = Name.Name,
			Group = Group.Group,
			Type = Type,
			Mode = Mode,
			Value = DefaultValue - 1
		};
		if ( Type == ComboType.Bool )
		{
			combo.Range = 1; 
			combo.Labels = new[] { State1, State2 };
		}
		else 
		{
			int actualStateCount = System.Math.Max( 2, System.Math.Min( 8, StateCount ) );
			combo.Range = actualStateCount - 1; 

			var stateNames = new[]
			{
				State1, State2, State3, State4,
				State5, State6, State7, State8
			};
			combo.Labels = stateNames.Take( actualStateCount ).ToArray();
		}

		if ( !combo.IsValid() )
		{
			return NodeResult.Error( $"Invalid combo configuration: {Name.Name}" );
		}

		compiler.RegisterCombo( combo );

		string comboVariable = combo.GetComboVariableName();

		NodeResult[] results;
		int stateCount;

		if ( Type == ComboType.Bool )
		{
			
			var trueResult = compiler.Result( InputTrue );
			var falseResult = compiler.Result( InputFalse );
			
			if ( !trueResult.IsValid )
				return NodeResult.Error( "True input must be connected" );

			if ( !falseResult.IsValid )
				return NodeResult.Error( "False input must be connected" );

			results = new[] { falseResult, trueResult };
			stateCount = 2;
		}
		else
		{
			int actualStateCount = System.Math.Max( 2, System.Math.Min( 8, StateCount ) );
			var inputs = new[]
			{
				InputState1, InputState2, InputState3, InputState4,
				InputState5, InputState6, InputState7, InputState8
			};

			results = new NodeResult[actualStateCount];
			for ( int i = 0; i < actualStateCount; i++ )
			{
				var result = compiler.Result( inputs[i] );
				if ( !result.IsValid )
					return NodeResult.Error( $"State {i + 1} input must be connected" );
				results[i] = result;
			}
			stateCount = actualStateCount;
		}

		int maxComponents = results.Max( r => r.Components );

		var inputValueResult = compiler.Result( Value );

		if ( compiler.IsPreview )
		{
			int valueIndex = DefaultValue - 1;
			valueIndex = System.Math.Max( 0, System.Math.Min( valueIndex, stateCount - 1 ) );
			string cast = CastToComponents( results[valueIndex], maxComponents );
			return new NodeResult( maxComponents, cast );
		}

		string switchVariable = inputValueResult.IsValid
			? $"(int)({inputValueResult.Code} - 1)"
			: comboVariable;

		var sb = new StringBuilder();
		sb.Append( "(" );

		for ( int i = 0; i < stateCount; i++ )
		{
			string cast = CastToComponents( results[i], maxComponents );

			if ( i < stateCount - 1 )
			{
				sb.Append( $"{switchVariable} == {i} ? {cast} : " );
			}
			else
			{
				sb.Append( cast );
			}
		}

		sb.Append( ")" );

		return new NodeResult( maxComponents, sb.ToString() );
	};

	private string CastToComponents( NodeResult result, int targetComponents )
	{
		if ( result.Components == targetComponents )
			return result.Code;

		if ( targetComponents == 1 )
			return result.Code;
		else if ( targetComponents == 2 )
			return $"float2({result.Code})";
		else if ( targetComponents == 3 )
			return $"float3({result.Code})";
		else
			return $"float4({result.Code})";
	}
}
