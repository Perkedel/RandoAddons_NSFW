namespace Editor.ShaderGraphExtras;

public enum BlendMode
{
	[Icon( "circle" )]
	Opaque,
	[Icon( "radio_button_unchecked" )]
	Masked,
	[Icon( "blur_on" )]
	Translucent,
	[Icon( "tune" )]
	Dynamic,
	[Icon( "build" )]
	Custom,
}

public enum ShadingModel
{
	[Icon( "tungsten" )]
	Lit,
	[Icon( "brightness_3" )]
	Unlit,
	[Icon( "palette" )]
	Custom,
}

public enum ShaderDomain
{
	[Icon( "view_in_ar" )]
	Surface,
	[Icon( "desktop_windows" )]
	PostProcess,
	[Icon( "gradient" )]
	Custom,
}

public class PreviewSettings
{
	public bool RenderBackfaces { get; set; } = false;
	public bool EnableShadows { get; set; } = true;
	public bool ShowGround { get; set; } = false;
	public bool ShowSkybox { get; set; } = true;
	public Color BackgroundColor { get; set; } = Color.Black;
	public Color Tint { get; set; } = Color.White;
}

public sealed partial class ShaderGraph : IGraph
{
	[Hide, JsonIgnore]
	public IEnumerable<BaseNode> Nodes => _nodes.Values;

	[Hide, JsonIgnore]
	private readonly Dictionary<string, BaseNode> _nodes = new();

	[Hide, JsonIgnore]
	IEnumerable<INode> IGraph.Nodes => Nodes;

	[Hide]
	public bool IsSubgraph { get; set; }

	[Hide]
	public string Path { get; set; }

	[Hide]
	public string Model { get; set; }

	/// <summary>
	/// The name of the Node when used in ShaderGraph
	/// </summary>
	[ShowIf( nameof( IsSubgraph ), true )]
	public string Title { get; set; }

	public string Description { get; set; }

	/// <summary>
	/// The category of the Node when browsing the Node Library (optional)
	/// </summary>
	[ShowIf( nameof( AddToNodeLibrary ), true )]
	public string Category { get; set; }

	[IconName, ShowIf( nameof( IsSubgraph ), true )]
	public string Icon { get; set; }

	/// <summary>
	/// Whether or not this Node should appear when browsing the Node Library.
	/// Otherwise can only be referenced by dragging the Subgraph asset into the graph.
	/// </summary>
	[ShowIf( nameof( IsSubgraph ), true )]
	public bool AddToNodeLibrary { get; set; }

	public BlendMode BlendMode { get; set; }

	public ShadingModel ShadingModel { get; set; }

	[ShowIf( nameof( ShadingModel ), ShadingModel.Custom )]
	public string ShadingModelPath { get; set; }

	public ShaderDomain Domain { get; set; }

	[ShowIf( nameof( Domain ), ShaderDomain.Custom )]
	public string TemplatePath { get; set; }

	// Feature helpers for shading model support (used by custom editors)
	[Hide]
	public bool SupportsLitShadingModel => GetFeature( "SupportsLitShadingModel", true );
	[Hide]
	public bool SupportsUnlitShadingModel => GetFeature( "SupportsUnlitShadingModel", true );
	[Hide]
	public bool SupportsCustomShadingModel => GetFeature( "SupportsCustomShadingModel", true );

	// Feature helpers for blend mode support (used by custom editors)
	[Hide]
	public bool SupportsOpaqueBlendMode => GetFeature( "SupportsOpaqueBlendMode", true );
	[Hide]
	public bool SupportsMaskedBlendMode => GetFeature( "SupportsMaskedBlendMode", true );
	[Hide]
	public bool SupportsTranslucentBlendMode => GetFeature( "SupportsTranslucentBlendMode", true );
	[Hide]
	public bool SupportsDynamicBlendMode => GetFeature( "SupportsDynamicBlendMode", true );
	[Hide]
	public bool SupportsCustomBlendMode => GetFeature( "SupportsCustomBlendMode", true );

	private bool GetFeature( string featureName, bool defaultValue )
	{
		// Get template path based on domain
		string templatePath = Domain switch
		{
			ShaderDomain.Surface => "Compiler/Templates/Surface.cs",
			ShaderDomain.PostProcess => "Compiler/Templates/PostProcess.cs",
			ShaderDomain.Custom => TemplatePath,
			_ => null
		};

		// Get shading model path based on selection
		string shadingModelPath = ShadingModel switch
		{
			ShadingModel.Lit => "Compiler/ShadingModels/Lit.cs",
			ShadingModel.Unlit => "Compiler/ShadingModels/Unlit.cs",
			ShadingModel.Custom => ShadingModelPath,
			_ => null
		};

		bool templateValue = defaultValue;
		bool shadingModelValue = defaultValue;

		// Check template features
		if ( !string.IsNullOrWhiteSpace( templatePath ) )
		{
			var templateFeatures = ShaderTemplate.GetTemplateFeatures( templatePath );
			if ( templateFeatures.TryGetValue( featureName, out var value ) )
				templateValue = value;
		}

		// Check shading model features
		if ( !string.IsNullOrWhiteSpace( shadingModelPath ) )
		{
			var shadingModelFeatures = ShaderTemplate.GetShadingModelFeatures( shadingModelPath );
			if ( shadingModelFeatures.TryGetValue( featureName, out var value ) )
				shadingModelValue = value;
		}

		// Both template and shading model must support the feature
		return templateValue && shadingModelValue;
	}

	/// <summary>
	/// Validates and auto-corrects BlendMode and ShadingModel if current values are not supported
	/// </summary>
	public void ValidateSettings()
	{
		// Auto-correct BlendMode if current is not supported
		bool currentBlendModeSupported = BlendMode switch
		{
			BlendMode.Opaque => SupportsOpaqueBlendMode,
			BlendMode.Masked => SupportsMaskedBlendMode,
			BlendMode.Translucent => SupportsTranslucentBlendMode,
			BlendMode.Dynamic => SupportsDynamicBlendMode,
			BlendMode.Custom => SupportsCustomBlendMode,
			_ => false
		};

		if ( !currentBlendModeSupported )
		{
			// Find the first supported blend mode
			if ( SupportsOpaqueBlendMode ) BlendMode = BlendMode.Opaque;
			else if ( SupportsMaskedBlendMode ) BlendMode = BlendMode.Masked;
			else if ( SupportsTranslucentBlendMode ) BlendMode = BlendMode.Translucent;
			else if ( SupportsDynamicBlendMode ) BlendMode = BlendMode.Dynamic;
			else if ( SupportsCustomBlendMode ) BlendMode = BlendMode.Custom;
		}

		// Auto-correct ShadingModel if current is not supported
		bool currentShadingModelSupported = ShadingModel switch
		{
			ShadingModel.Lit => SupportsLitShadingModel,
			ShadingModel.Unlit => SupportsUnlitShadingModel,
			ShadingModel.Custom => SupportsCustomShadingModel,
			_ => false
		};

		if ( !currentShadingModelSupported )
		{
			// Find the first supported shading model
			if ( SupportsLitShadingModel ) ShadingModel = ShadingModel.Lit;
			else if ( SupportsUnlitShadingModel ) ShadingModel = ShadingModel.Unlit;
			else if ( SupportsCustomShadingModel ) ShadingModel = ShadingModel.Custom;
		}
	}

	[Hide]
	public PreviewSettings PreviewSettings { get; set; } = new();

	[Hide]
	public int Version { get; set; } = 1;

	public ShaderGraph()
	{
	}

	public void AddNode( BaseNode node )
	{
		node.Graph = this;
		_nodes.Add( node.Identifier, node );
	}

	public void RemoveNode( BaseNode node )
	{
		if ( node.Graph != this )
			return;

		_nodes.Remove( node.Identifier );
	}

	public BaseNode FindNode( string name )
	{
		_nodes.TryGetValue( name, out var node );
		return node;
	}

	public void ClearNodes()
	{
		_nodes.Clear();
	}

	string IGraph.SerializeNodes( IEnumerable<INode> nodes )
	{
		return SerializeNodes( nodes.Cast<BaseNode>() );
	}

	IEnumerable<INode> IGraph.DeserializeNodes( string serialized )
	{
		return DeserializeNodes( serialized );
	}

	void IGraph.AddNode( INode node )
	{
		AddNode( (BaseNode)node );
	}

	void IGraph.RemoveNode( INode node )
	{
		RemoveNode( (BaseNode)node );
	}
}
