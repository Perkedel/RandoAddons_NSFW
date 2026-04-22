namespace Editor.ShaderGraphExtras;

/// <summary>
/// Final result
/// </summary>
[Title( "Material" ), Icon( "tonality" )]
public sealed class Result : BaseResult
{
	[Hide]
	private bool SupportsAlbedo => GetFeature( "SupportsAlbedo", true );

	[Hide]
	private bool SupportsEmission => GetFeature( "SupportsEmission", true );

	[Hide]
	private bool SupportsOpacity => GetFeature( "SupportsOpacity", true );

	[Hide]
	private bool SupportsNormal => GetFeature( "SupportsNormal", true );

	[Hide]
	private bool SupportsRoughness => GetFeature( "SupportsRoughness", true );

	[Hide]
	private bool SupportsMetalness => GetFeature( "SupportsMetalness", true );

	[Hide]
	private bool SupportsAmbientOcclusion => GetFeature( "SupportsAmbientOcclusion", true );

	[Hide]
	private bool SupportsPositionOffset => GetFeature( "SupportsPositionOffset", true );

	[Hide]
	private bool SupportsPixelDepthOffset => GetFeature( "SupportsPixelDepthOffset", true );

	/// <summary>
	/// Get a feature value from both template and shading model.
	/// For input visibility features: both must be true for the feature to be enabled.
	/// For other features: template takes precedence.
	/// </summary>
	private bool GetFeature( string featureName, bool defaultValue )
	{
		if ( Graph is not ShaderGraph shaderGraph )
			return defaultValue;

		// Get template path based on domain
		string templatePath = shaderGraph.Domain switch
		{
			ShaderDomain.Surface => "Compiler/Templates/Surface.cs",
			ShaderDomain.PostProcess => "Compiler/Templates/PostProcess.cs",
			ShaderDomain.Custom => shaderGraph.TemplatePath,
			_ => null
		};

		// Get shading model path based on selection
		string shadingModelPath = shaderGraph.ShadingModel switch
		{
			ShadingModel.Lit => "Compiler/ShadingModels/Lit.cs",
			ShadingModel.Unlit => "Compiler/ShadingModels/Unlit.cs",
			ShadingModel.Custom => shaderGraph.ShadingModelPath,
			_ => null
		};

		// For input support features, both template and shading model must allow it
		if ( featureName.StartsWith( "Supports" ) && !featureName.Contains( "BlendMode" ) && !featureName.Contains( "ShadingModel" ) )
		{
			bool templateSupports = defaultValue;
			bool shadingModelSupports = defaultValue;

			// Check template features
			if ( !string.IsNullOrWhiteSpace( templatePath ) )
			{
				var templateFeatures = ShaderTemplate.GetTemplateFeatures( templatePath );
				if ( templateFeatures.TryGetValue( featureName, out var value ) )
					templateSupports = value;
			}

			// Check shading model features
			if ( !string.IsNullOrWhiteSpace( shadingModelPath ) )
			{
				var shadingModelFeatures = ShaderTemplate.GetShadingModelFeatures( shadingModelPath );
				if ( shadingModelFeatures.TryGetValue( featureName, out var value ) )
					shadingModelSupports = value;
			}

			// Both must be true
			return templateSupports && shadingModelSupports;
		}
		else
		{
			// For other features (blend modes, shading model support), check template first
			if ( !string.IsNullOrWhiteSpace( templatePath ) )
			{
				var templateFeatures = ShaderTemplate.GetTemplateFeatures( templatePath );
				if ( templateFeatures.TryGetValue( featureName, out var value ) )
					return value;
			}

			if ( !string.IsNullOrWhiteSpace( shadingModelPath ) )
			{
				var shadingModelFeatures = ShaderTemplate.GetShadingModelFeatures( shadingModelPath );
				if ( shadingModelFeatures.TryGetValue( featureName, out var value ) )
					return value;
			}
		}

		return defaultValue;
	}

	[Hide]
	[Input( typeof( Vector3 ) )]
	[ShowIf( nameof( SupportsAlbedo ), true )]
	public NodeInput Albedo { get; set; }

	[Hide]
	[Input( typeof( Vector3 ) )]
	[ShowIf( nameof( SupportsEmission ), true )]
	public NodeInput Emission { get; set; }

	[Hide, Editor( nameof( DefaultOpacity ) )]
	[Input( typeof( float ) )]
	[ShowIf( nameof( SupportsOpacity ), true )]
	public NodeInput Opacity { get; set; }

	[Hide]
	[Input( typeof( Vector3 ) )]
	[ShowIf( nameof( SupportsNormal ), true )]
	public NodeInput Normal { get; set; }

	[Hide, Editor( nameof( DefaultRoughness ) )]
	[Input( typeof( float ) )]
	[ShowIf( nameof( SupportsRoughness ), true )]
	public NodeInput Roughness { get; set; }

	[Hide, Editor( nameof( DefaultMetalness ) )]
	[Input( typeof( float ) )]
	[ShowIf( nameof( SupportsMetalness ), true )]
	public NodeInput Metalness { get; set; }

	[Hide, Editor( nameof( DefaultAmbientOcclusion ) )]
	[Input( typeof( float ) )]
	[ShowIf( nameof( SupportsAmbientOcclusion ), true )]
	public NodeInput AmbientOcclusion { get; set; }

	[Hide]
	[Input( typeof( Vector3 ) )]
	[ShowIf( nameof( SupportsPositionOffset ), true )]
	public NodeInput PositionOffset { get; set; }

	[Hide]
	[Input( typeof( float ) )]
	[ShowIf( nameof( SupportsPixelDepthOffset ), true )]
	public NodeInput PixelDepthOffset { get; set; }

	//Default Values
	
	[InputDefault( nameof( Opacity ) )]
	public float DefaultOpacity { get; set; } = 1.0f;

	[InputDefault( nameof( Roughness ) )]
	public float DefaultRoughness { get; set; } = 1.0f;
	[InputDefault( nameof( Metalness ) )]
	public float DefaultMetalness { get; set; } = 0.0f;
	[InputDefault( nameof( AmbientOcclusion ) )]
	public float DefaultAmbientOcclusion { get; set; } = 1.0f;

	[Hide, JsonIgnore]
	int _lastHashCode = 0;

	public override void OnFrame()
	{
		var hashCode = new HashCode();
		if ( Graph is ShaderGraph shaderGraph )
		{
			hashCode.Add( shaderGraph.ShadingModel );
			hashCode.Add( shaderGraph.Domain );
			hashCode.Add( shaderGraph.ShadingModelPath );
			hashCode.Add( shaderGraph.TemplatePath );
		}
		var hc = hashCode.ToHashCode();
		if ( hc != _lastHashCode )
		{
			_lastHashCode = hc;
			CreateInputs();
			Update();
		}
	}

	[JsonIgnore, Hide]
	public override Color PrimaryColor => Color.Lerp( Theme.Blue, Color.White, 0.25f );

	public override NodeInput GetAlbedo() => Albedo;
	public override NodeInput GetEmission() => Emission;
	public override NodeInput GetOpacity() => Opacity;
	public override NodeInput GetNormal() => Normal;
	public override NodeInput GetRoughness() => Roughness;
	public override NodeInput GetMetalness() => Metalness;
	public override NodeInput GetAmbientOcclusion() => AmbientOcclusion;
	public override NodeInput GetPositionOffset() => PositionOffset;
	public override NodeInput GetPixelDepthOffset() => PixelDepthOffset;

	public override float GetDefaultOpacity() => DefaultOpacity;

	/// <summary>
	/// Returns true if the input should be visible based on feature support
	/// </summary>
	private bool IsInputSupported( string propertyName )
	{
		return propertyName switch
		{
			nameof( Albedo ) => SupportsAlbedo,
			nameof( Emission ) => SupportsEmission,
			nameof( Opacity ) => SupportsOpacity,
			nameof( Normal ) => SupportsNormal,
			nameof( Roughness ) => SupportsRoughness,
			nameof( Metalness ) => SupportsMetalness,
			nameof( AmbientOcclusion ) => SupportsAmbientOcclusion,
			nameof( PositionOffset ) => SupportsPositionOffset,
			nameof( PixelDepthOffset ) => SupportsPixelDepthOffset,
			_ => true
		};
	}

	private void CreateInputs()
	{
		var plugs = new List<IPlugIn>();
		var serialized = this.GetSerialized();
		foreach ( var property in serialized )
		{
			if ( property.TryGetAttribute<InputAttribute>( out var inputAttr ) )
			{
				// Check if this input is supported by the current shading model/template
				if ( !IsInputSupported( property.Name ) )
				{
					continue;
				}

				var propertyInfo = typeof( Result ).GetProperty( property.Name );
				if ( propertyInfo is null ) continue;
				var info = new PlugInfo( propertyInfo );
				var displayInfo = info.DisplayInfo;
				displayInfo.Name = property.DisplayName;
				info.DisplayInfo = displayInfo;
				var plug = new BasePlugIn( this, info, info.Type );
				var oldPlug = Inputs.FirstOrDefault( x => x is BasePlugIn plugIn && plugIn.Info.DisplayInfo.Name == property.Name ) as BasePlugIn;
				if ( oldPlug is not null )
				{
					oldPlug.Info.Name = info.Name;
					oldPlug.Info.Type = info.Type;
					oldPlug.Info.DisplayInfo = info.DisplayInfo;
					var nodeInput = property.GetValue<NodeInput>();
					if ( nodeInput.IsValid && plug is IPlugIn plugIn )
					{
						var connectedNode = Graph.Nodes.FirstOrDefault( x => x is BaseNode node && node.Identifier == nodeInput.Identifier ) as BaseNode;
						plugIn.ConnectedOutput = connectedNode.Outputs.FirstOrDefault( x => x.Identifier == nodeInput.Output );
					}
					plugs.Add( oldPlug );
				}
				else
				{
					var nodeInput = property.GetValue<NodeInput>();
					if ( nodeInput.IsValid && plug is IPlugIn plugIn )
					{
						var connectedNode = Graph.Nodes.FirstOrDefault( x => x is BaseNode node && node.Identifier == nodeInput.Identifier ) as BaseNode;
						plugIn.ConnectedOutput = connectedNode.Outputs.FirstOrDefault( x => x.Identifier == nodeInput.Output );
					}
					plugs.Add( plug );
				}
			}
		}
		Inputs = plugs;
	}
}

public abstract class BaseResult : ShaderNode
{
	[JsonIgnore, Hide, Browsable( false )]
	public override bool CanRemove => Graph.Nodes.Count( x => x is BaseResult ) > 1;

	public virtual NodeInput GetAlbedo() => new();
	public virtual NodeInput GetEmission() => new();
	public virtual NodeInput GetOpacity() => new();
	public virtual NodeInput GetNormal() => new();
	public virtual NodeInput GetRoughness() => new();
	public virtual NodeInput GetMetalness() => new();
	public virtual NodeInput GetAmbientOcclusion() => new();
	public virtual NodeInput GetPositionOffset() => new();
	public virtual NodeInput GetPixelDepthOffset() => new();

	public virtual Color GetDefaultAlbedo() => Color.White;
	public virtual Color GetDefaultEmission() => Color.Black;
	public virtual float GetDefaultOpacity() => 1.0f;
	public virtual Vector3 GetDefaultNormal() => new( 0, 0, 0 );
	public virtual float GetDefaultRoughness() => 1.0f;
	public virtual float GetDefaultMetalness() => 0.0f;
	public virtual float GetDefaultAmbientOcclusion() => 1.0f;
	public virtual Vector3 GetDefaultPositionOffset() => new( 0, 0, 0 );
	public virtual float GetDefaultPixelDepthOffset() => 0.0f;

	public NodeResult GetAlbedoResult( GraphCompiler compiler )
	{
		var albedoInput = GetAlbedo();
		if ( albedoInput.IsValid )
			return compiler.ResultValue( albedoInput );
		return compiler.ResultValue( GetDefaultAlbedo() );
	}

	public NodeResult GetEmissionResult( GraphCompiler compiler )
	{
		var emissionInput = GetEmission();
		if ( emissionInput.IsValid )
			return compiler.ResultValue( emissionInput );
		return compiler.ResultValue( GetDefaultEmission() );
	}

	public NodeResult GetOpacityResult( GraphCompiler compiler )
	{
		var opacityInput = GetOpacity();
		if ( opacityInput.IsValid )
			return compiler.ResultValue( opacityInput );
		return compiler.ResultValue( GetDefaultOpacity() );
	}

	public NodeResult GetNormalResult( GraphCompiler compiler )
	{
		var normalInput = GetNormal();
		if ( normalInput.IsValid )
			return compiler.ResultValue( normalInput );
		return compiler.ResultValue( GetDefaultNormal() );
	}

	public NodeResult GetRoughnessResult( GraphCompiler compiler )
	{
		var roughnessInput = GetRoughness();
		if ( roughnessInput.IsValid )
			return compiler.ResultValue( roughnessInput );
		return compiler.ResultValue( GetDefaultRoughness() );
	}

	public NodeResult GetMetalnessResult( GraphCompiler compiler )
	{
		var metalnessInput = GetMetalness();
		if ( metalnessInput.IsValid )
			return compiler.ResultValue( metalnessInput );
		return compiler.ResultValue( GetDefaultMetalness() );
	}

	public NodeResult GetAmbientOcclusionResult( GraphCompiler compiler )
	{
		var ambientOcclusionInput = GetAmbientOcclusion();
		if ( ambientOcclusionInput.IsValid )
			return compiler.ResultValue( ambientOcclusionInput );
		return compiler.ResultValue( GetDefaultAmbientOcclusion() );
	}

	public NodeResult GetPositionOffsetResult( GraphCompiler compiler )
	{
		var positionOffsetInput = GetPositionOffset();
		if ( positionOffsetInput.IsValid )
			return compiler.ResultValue( positionOffsetInput );
		return compiler.ResultValue( GetDefaultPositionOffset() );
	}

	public NodeResult GetPixelDepthOffsetResult( GraphCompiler compiler )
	{
		var pixelDepthOffsetInput = GetPixelDepthOffset();
		if ( pixelDepthOffsetInput.IsValid )
			return compiler.ResultValue( pixelDepthOffsetInput );
		return compiler.ResultValue( GetDefaultPixelDepthOffset() );
	}
}
