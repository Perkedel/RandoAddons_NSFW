using Editor;
using Editor.ShaderGraph;

namespace ShaderGraphPlus.Nodes;

public abstract class Texture2DSamplerBase : ShaderNodePlus, IErroringNode
{
	[JsonIgnore, Hide, Browsable( false )]
	public override Color NodeTitleColor => ShaderGraphPlusTheme.NodeHeaderColors.FunctionNode;

	[JsonIgnore, Hide, Browsable( false )]
	public string InternalImage
	{
		get => _internalImage;
		set
		{
			_internalImage = value;
			_asset = AssetSystem.FindByPath( _internalImage );

			if ( _asset == null )
				return;

			CompileTexture();
		}
	}

	[JsonIgnore, Hide] private Asset _asset;
	[JsonIgnore, Hide] private string _texture;
	[JsonIgnore, Hide] private string _internalImage;
	[JsonIgnore, Hide] private string _resourceText;
	[JsonIgnore, Hide] private Asset Asset => _asset;
	[JsonIgnore, Hide] protected string TexturePath => _texture;

	protected void CompileTexture()
	{
		if ( _asset == null )
			return;

		if ( string.IsNullOrWhiteSpace( _internalImage ) )
			return;

		var resourceText = string.Format( ShaderTemplate.TextureDefinition,
			_internalImage,
			PreviewUI.ColorSpace,
			PreviewUI.ImageFormat,
			PreviewUI.Processor );

		if ( _resourceText == resourceText )
			return;

		_resourceText = resourceText;

		var assetPath = $"shadergraphplus/{_internalImage.Replace( ".", "_" )}_shadergraphplus.generated.vtex";
		var resourcePath = Editor.FileSystem.Root.GetFullPath( "/.source2/temp" );
		resourcePath = System.IO.Path.Combine( resourcePath, assetPath );

		if ( AssetSystem.CompileResource( resourcePath, resourceText ) )
		{
			_texture = assetPath;
		}
		else
		{
			Log.Warning( $"Failed to compile {_internalImage}" );
		}
	}

	[Hide]
	protected virtual TextureInput PreviewUI => new TextureInput
	{
		Type = TextureType.Tex2D,
		ImageFormat = TextureFormat.DXT5,
		SrgbRead = true,
		DefaultColor = Color.White,
	};

	protected Texture2DSamplerBase() : base()
	{
		InternalImage = "materials/default/default.tga";
		ExpandSize = new Vector2( 0, 8 + Inputs.Count() * 24 );
	}

	public override void OnPaint( Rect rect )
	{
		rect = rect.Align( 130, TextFlag.LeftBottom ).Shrink( 3 );

		Paint.SetBrush( "/image/transparent-small.png" );
		Paint.DrawRect( rect.Shrink( 2 ), 2 );

		Paint.SetBrush( Theme.ControlBackground.WithAlpha( 0.7f ) );
		Paint.DrawRect( rect, 2 );

		if ( Asset != null )
		{
			Paint.Draw( rect.Shrink( 2 ), Asset.GetAssetThumb( true ) );
		}
	}

	protected NodeResult Component( string component, GraphCompiler compiler )
	{
		var result = compiler.Result( new NodeInput { Identifier = Identifier, Output = nameof( Result ) } );
		return result.IsValid ? new( ResultType.Float, $"{result}.{component}", true ) : new( ResultType.Float, "0.0f", true );
	}

	protected bool ProcessTexture2DInput( GraphCompiler compiler, NodeInput texture2D, out NodeResult texture2DResult, out NodeResult errorResult )
	{
		errorResult = default;
		texture2DResult = compiler.Result( texture2D );
		if ( texture2DResult.IsValid )
		{
			if ( texture2DResult.ResultType != ResultType.Texture2D )
			{
				errorResult = NodeResult.IncorrectInputType( "Texture2D", ResultType.Texture2D );

				return false;
			}

			ClearError();

			if ( compiler.TryGetPreviewImage( texture2DResult.Code, out var imagePath ) )
			{
				InternalImage = imagePath;

				return true;
			}
			else
			{
				throw new Exception( $"Cannot find PreviewImage for texture input : {texture2DResult.Code}" );
			}
		}

		InternalImage = "materials/dev/white_color.tga";
		errorResult = NodeResult.MissingInput( "Texture2D" );

		return false;
	}

	public List<string> GetErrors()
	{
		var errors = new List<string>();
		var graph = Graph as ShaderGraphPlus;

		return errors;
	}
}

/// <summary>
/// Sample a 2D Texture
/// </summary>
[Title( "Sample Texture 2D" ), Category( "Textures" ), Icon( "colorize" )]
public sealed class SampleTexture2DNode : Texture2DSamplerBase
{
	/// <summary>
	/// Texture2D Object input
	/// </summary>
	[Title( "Texture2D" )]
	[Input( typeof( Texture ), Order = 0 )]
	[Hide]
	public NodeInput Texture2DInput { get; set; }

	/// <summary>
	/// Coordinates to sample this texture (Defaults to vertex coordinates)
	/// </summary>
	[Title( "Coordinates" )]
	[Input( typeof( Vector2 ), Order = 1 )]
	[Hide]
	public NodeInput CoordsInput { get; set; }

	/// <summary>
	/// How the texture is filtered and wrapped when sampled
	/// </summary>
	[Title( "Sampler" )]
	[Input( typeof( Sampler ), Order = 2 )]
	[Hide]
	public NodeInput SamplerInput { get; set; }

	[InlineEditor( Label = false ), Group( "Sampler" ), Order( 2 )]
	public Sampler SamplerState { get; set; } = new Sampler();

	public SampleTexture2DNode() : base()
	{
		ExpandSize = new Vector2( 0, 8 + Inputs.Count() * 24 );
	}

	/// <summary>
	/// RGBA color result
	/// </summary>
	[Hide]
	[Output( typeof( Vector4 ) ), Title( "RGBA" )]
	public NodeResult.Func Result => ( GraphCompiler compiler ) =>
	{
		if ( !ProcessTexture2DInput( compiler, Texture2DInput, out var texture2DResult, out var errorResult ) )
		{
			return errorResult;
		}

		CompileTexture();

		var texture = string.IsNullOrWhiteSpace( TexturePath ) ? null : Texture.Load( TexturePath );
		texture ??= Texture.White;

		var samplerGlobal = compiler.ResultSamplerOrDefault( SamplerInput, SamplerState );
		var textureGlobal = texture2DResult.Code;

		var attributeName = texture2DResult.Code.TrimStart( "g_t" ).ToString();
		compiler.SetAttribute( attributeName, texture );

		var coords = compiler.Result( CoordsInput );

		if ( compiler.Stage == GraphCompiler.ShaderStage.Vertex )
		{
			return new NodeResult( ResultType.Vector4, $"{textureGlobal}.SampleLevel(" +
				$" {samplerGlobal}," +
				$" {(coords.IsValid ? $"{coords.Cast( 2 )}" : "i.vTextureCoords.xy")}, 0 )" );
		}
		else
		{
			return new NodeResult( ResultType.Vector4, $"{textureGlobal}.Sample( {samplerGlobal}," +
				$"{(coords.IsValid ? $"{coords.Cast( 2 )}" : "i.vTextureCoords.xy")} )" );
		}
	};

	/// <summary>
	/// Red component of result
	/// </summary>
	[Output( typeof( float ) ), Hide, Title( "R" )]
	public NodeResult.Func R => ( GraphCompiler compiler ) => Component( "r", compiler );

	/// <summary>
	/// Green component of result
	/// </summary>
	[Output( typeof( float ) ), Hide, Title( "G" )]
	public NodeResult.Func G => ( GraphCompiler compiler ) => Component( "g", compiler );

	/// <summary>
	/// Blue component of result
	/// </summary>
	[Output( typeof( float ) ), Hide, Title( "B" )]
	public NodeResult.Func B => ( GraphCompiler compiler ) => Component( "b", compiler );

	/// <summary>
	/// Alpha (Opacity) component of result
	/// </summary>
	[Output( typeof( float ) ), Hide, Title( "A" )]
	public NodeResult.Func A => ( GraphCompiler compiler ) => Component( "a", compiler );
}

/// <summary>
/// Sample a 2D texture from 3 directions, then blend based on a normal vector.
/// </summary>
[Title( "Sample Texture 2D Triplanar" ), Category( "Textures" ), Icon( "colorize" )]
public sealed class SampleTexture2DTriplanarNode : Texture2DSamplerBase
{
	/// <summary>
	/// Texture2D Object input
	/// </summary>
	[Title( "Texture2D" )]
	[Input( typeof( Texture ), Order = 0 )]
	[Hide]
	public NodeInput Texture2DInput { get; set; }

	/// <summary>
	/// Coordinates to sample this texture (Defaults to vertex coordinates)
	/// </summary>
	[Title( "Coordinates" )]
	[Input( typeof( Vector2 ), Order = 1 )]
	[Hide]
	public NodeInput CoordsInput { get; set; }

	/// <summary>
	/// How the texture is filtered and wrapped when sampled
	/// </summary>
	[Title( "Sampler" )]
	[Input( typeof( Sampler ), Order = 2 )]
	[Hide]
	public NodeInput SamplerInput { get; set; }

	/// <summary>
	/// Normal to use when blending between each sampled direction (Defaults to vertex normal)
	/// </summary>
	[Title( "Normal" )]
	[Input( typeof( Vector3 ), Order = 3 )]
	[Hide]
	public NodeInput NormalInput { get; set; }

	/*
	/// <summary>
	/// How many times to file the coordinates.
	/// </summary>
	[Title( "Tile" )]
	[Input( typeof( float ), Order = 4 )]
	[Hide]
	public NodeInput TileInput { get; set; }
	*/

	/// <summary>
	/// Blend factor between different samples.
	/// </summary>
	[Title( "Blend Factor" )]
	[Input( typeof( float ), Order = 5 )]
	[Hide]
	public NodeInput BlendFactorInput { get; set; }

	[InlineEditor( Label = false ), Group( "Sampler" ), Order( 2 )]
	public Sampler SamplerState { get; set; } = new Sampler();

	//public float DefaultTile { get; set; } = 1.0f;
	public float DefaultBlendFactor { get; set; } = 4.0f;

	public SampleTexture2DTriplanarNode() : base()
	{
		ExpandSize = new Vector2( 0, 8 + Inputs.Count() * 24 );
	}

	[Hide]
	protected override TextureInput PreviewUI => new TextureInput
	{
		Type = TextureType.Tex2D,
		ImageFormat = TextureFormat.DXT5,
		SrgbRead = true,
		DefaultColor = Color.White,
	};

	/// <summary>
	/// RGBA color result
	/// </summary>
	[Hide]
	[Output( typeof( Vector4 ) ), Title( "RGBA" )]
	public NodeResult.Func Result => ( GraphCompiler compiler ) =>
	{
		if ( !ProcessTexture2DInput( compiler, Texture2DInput, out var texture2DResult, out var errorResult ) )
		{
			return errorResult;
		}

		CompileTexture();

		var texture = string.IsNullOrWhiteSpace( TexturePath ) ? null : Texture.Load( TexturePath );
		texture ??= Texture.White;

		var samplerGlobal = compiler.ResultSamplerOrDefault( SamplerInput, SamplerState );
		var textureGlobal = texture2DResult.Code;

		var attributeName = texture2DResult.Code.TrimStart( "g_t" ).ToString();
		compiler.SetAttribute( attributeName, texture );

		var coords = compiler.Result( CoordsInput );
		var normal = compiler.Result( NormalInput );

		//var tile = compiler.ResultOrDefault( TileInput, DefaultTile );
		var blendfactor = compiler.ResultOrDefault( BlendFactorInput, DefaultBlendFactor );

		var result = compiler.ResultHLSLFunction( "TexTriplanar_Color",
			textureGlobal,
			samplerGlobal,
			coords.IsValid ? coords.Cast( 3 ) : "(i.vPositionWithOffsetWs.xyz + g_vHighPrecisionLightingOffsetWs.xyz) / 39.3701",
			normal.IsValid ? normal.Cast( 3 ) : "normalize( i.vNormalWs.xyz )",
			$"{blendfactor}"
		);

		return new NodeResult( ResultType.Vector4, result );
	};

	/// <summary>
	/// Red component of result
	/// </summary>
	[Output( typeof( float ) ), Hide, Title( "R" )]
	public NodeResult.Func R => ( GraphCompiler compiler ) => Component( "r", compiler );

	/// <summary>
	/// Green component of result
	/// </summary>
	[Output( typeof( float ) ), Hide, Title( "G" )]
	public NodeResult.Func G => ( GraphCompiler compiler ) => Component( "g", compiler );

	/// <summary>
	/// Blue component of result
	/// </summary>
	[Output( typeof( float ) ), Hide, Title( "B" )]
	public NodeResult.Func B => ( GraphCompiler compiler ) => Component( "b", compiler );

	/// <summary>
	/// Alpha (Opacity) component of result
	/// </summary>
	[Output( typeof( float ) ), Hide, Title( "A" )]
	public NodeResult.Func A => ( GraphCompiler compiler ) => Component( "a", compiler );
}

/// <summary>
/// Sample a 2D texture from 3 directions, then blend based on a normal vector.
/// </summary>
[Title( "Sample Texture 2D Normal Map Triplanar" ), Category( "Textures" ), Icon( "colorize" )]
public sealed class SampleTexture2DNormalMapTriplanarNode : Texture2DSamplerBase
{
	/// <summary>
	/// Texture2D Object input
	/// </summary>
	[Title( "Texture2D" )]
	[Input( typeof( Texture ), Order = 0 )]
	[Hide]
	public NodeInput Texture2DInput { get; set; }

	/// <summary>
	/// Coordinates to sample this texture (Defaults to vertex coordinates)
	/// </summary>
	[Title( "Coordinates" )]
	[Input( typeof( Vector2 ), Order = 1 )]
	[Hide]
	public NodeInput CoordsInput { get; set; }

	/// <summary>
	/// How the texture is filtered and wrapped when sampled
	/// </summary>
	[Title( "Sampler" )]
	[Input( typeof( Sampler ), Order = 2 )]
	[Hide]
	public NodeInput SamplerInput { get; set; }

	/// <summary>
	/// Normal to use when blending between each sampled direction (Defaults to vertex normal)
	/// </summary>
	[Title( "Normal" )]
	[Input( typeof( Vector3 ), Order = 3 )]
	[Hide]
	public NodeInput NormalInput { get; set; }

	/*
	/// <summary>
	/// How many times to file the coordinates.
	/// </summary>
	[Title( "Tile" )]
	[Input( typeof( float ), Order = 4 )]
	[Hide]
	public NodeInput TileInput { get; set; }
	*/

	/// <summary>
	/// Blend factor between different samples.
	/// </summary>
	[Title( "Blend Factor" )]
	[Input( typeof( float ), Order = 5 )]
	[Hide]
	public NodeInput BlendFactorInput { get; set; }

	[InlineEditor( Label = false ), Group( "Sampler" ), Order( 2 )]
	public Sampler SamplerState { get; set; } = new Sampler();

	//public float DefaultTile { get; set; } = 1.0f;
	public float DefaultBlendFactor { get; set; } = 4.0f;

	protected override TextureInput PreviewUI => new TextureInput
	{
		Type = TextureType.Tex2D,
		ImageFormat = TextureFormat.DXT5,
		SrgbRead = false,
		ColorSpace = TextureColorSpace.Linear,
		Extension = TextureExtension.Normal,
		Processor = TextureProcessor.NormalizeNormals,
		DefaultColor = new Color( 0.5f, 0.5f, 1f, 1f )
	};

	public SampleTexture2DNormalMapTriplanarNode() : base()
	{
		ExpandSize = new Vector2( 56, 12 + Inputs.Count() * 24 );
	}

	/// <summary>
	/// RGBA color result
	/// </summary>
	[Hide]
	[Output( typeof( Vector4 ) ), Title( "RGBA" )]
	public NodeResult.Func Result => ( GraphCompiler compiler ) =>
	{
		if ( !ProcessTexture2DInput( compiler, Texture2DInput, out var texture2DResult, out var errorResult ) )
		{
			return errorResult;
		}

		CompileTexture();

		var texture = string.IsNullOrWhiteSpace( TexturePath ) ? null : Texture.Load( TexturePath );
		texture ??= Texture.White;

		var samplerGlobal = compiler.ResultSamplerOrDefault( SamplerInput, SamplerState );
		var textureGlobal = texture2DResult.Code;

		var attributeName = texture2DResult.Code.TrimStart( "g_t" ).ToString();
		compiler.SetAttribute( attributeName, texture );

		var coords = compiler.Result( CoordsInput );
		//var tile = compiler.ResultOrDefault( TileInput, DefaultTile );
		var normal = compiler.Result( NormalInput );
		var blendfactor = compiler.ResultOrDefault( BlendFactorInput, DefaultBlendFactor );

		var result = compiler.ResultHLSLFunction( "TexTriplanar_Normal",
		textureGlobal,
		samplerGlobal,
		coords.IsValid ? coords.Cast( 3 ) : "(i.vPositionWithOffsetWs.xyz + g_vHighPrecisionLightingOffsetWs.xyz) / 39.3701",
		normal.IsValid ? normal.Cast( 3 ) : "normalize( i.vNormalWs.xyz )",
		$"{blendfactor}"
		);

		return new NodeResult( ResultType.Vector3, result );
	};

	/// <summary>
	/// Red component of result
	/// </summary>
	[Output( typeof( float ) ), Hide, Title( "R" )]
	public NodeResult.Func R => ( GraphCompiler compiler ) => Component( "r", compiler );

	/// <summary>
	/// Green component of result
	/// </summary>
	[Output( typeof( float ) ), Hide, Title( "G" )]
	public NodeResult.Func G => ( GraphCompiler compiler ) => Component( "g", compiler );

	/// <summary>
	/// Blue component of result
	/// </summary>
	[Output( typeof( float ) ), Hide, Title( "B" )]
	public NodeResult.Func B => ( GraphCompiler compiler ) => Component( "b", compiler );

	/// <summary>
	/// Alpha (Opacity) component of result
	/// </summary>
	[Output( typeof( float ) ), Hide, Title( "A" )]
	public NodeResult.Func A => ( GraphCompiler compiler ) => Component( "a", compiler );
}

/// <summary>
/// Sample a Cube Texture
/// </summary>
[Title( "Sample Texture Cube" ), Category( "Textures" ), Icon( "colorize" )]
public sealed class SampleTextureCubeNode : ShaderNodePlus
{
	[JsonIgnore, Hide, Browsable( false )]
	public override Color NodeTitleColor => ShaderGraphPlusTheme.NodeHeaderColors.FunctionNode;

	[JsonIgnore, Hide, Browsable( false )]
	public bool IsSubgraph => (Graph is ShaderGraphPlus shaderGraph && shaderGraph.IsSubgraph);

	/// <summary>
	/// Optional TextureCube Object input when outside of subgraphs.
	/// </summary>
	[Title( "TextureCube" )]
	[Input( typeof( Texture ) )]
	[Hide]
	public NodeInput TextureInput { get; set; }
	/// <summary>
	/// Coordinates to sample this cubemap
	/// </summary>
	[Title( "Coordinates" )]
	[Input( typeof( Vector3 ) )]
	[Hide]
	public NodeInput CoordsInput { get; set; }

	/// <summary>
	/// How the texture is filtered and wrapped when sampled
	/// </summary>
	[Title( "Sampler" )]
	[Input( typeof( Sampler ) )]
	[Hide]
	public NodeInput SamplerInput { get; set; }

	/// <summary>
	/// Texture to sample in preview
	/// </summary>
	[Hide, JsonIgnore]
	public string Texture { get; set; }

	[InlineEditor( Label = false ), Group( "Sampler" )]
	[HideIf( nameof( IsSubgraph ), true )]
	public Sampler SamplerState { get; set; } = new Sampler();

	[Hide]
	public TextureInput PreviewUI => new TextureInput
	{
		Type = TextureType.TexCube,
		ImageFormat = TextureFormat.DXT5,
		SrgbRead = true,
		DefaultColor = Color.White,
	};

	public SampleTextureCubeNode() : base()
	{
		Texture = "materials/skybox/skybox_workshop.vtex";
		ExpandSize = new Vector2( 0, 8 + Inputs.Count() * 24 );
	}

	public override void OnPaint( Rect rect )
	{
		rect = rect.Align( 130, TextFlag.LeftBottom ).Shrink( 3 );

		Paint.SetBrush( "/image/transparent-small.png" );
		Paint.DrawRect( rect.Shrink( 2 ), 2 );

		Paint.SetBrush( Theme.ControlBackground.WithAlpha( 0.7f ) );
		Paint.DrawRect( rect, 2 );

		if ( !string.IsNullOrEmpty( Texture ) )
		{
			var tex = Sandbox.Texture.Find( Texture );
			if ( tex is null ) return;
			var pixmap = Pixmap.FromTexture( tex );
			Paint.Draw( rect.Shrink( 2 ), pixmap );
		}
	}

	/// <summary>
	/// RGBA color result
	/// </summary>
	[Hide]
	[Output( typeof( Vector4 ) ), Title( "RGBA" )]
	public NodeResult.Func Result => ( GraphCompiler compiler ) =>
	{
		var input = PreviewUI;
		input.Type = TextureType.TexCube;
		var textureCubeResult = compiler.Result( TextureInput );

		if ( textureCubeResult.IsValid )
		{
			if ( textureCubeResult.ResultType != ResultType.TextureCube )
			{
				return NodeResult.IncorrectInputType( "TextureCube", ResultType.TextureCube );
			}

			ClearError();

			if ( compiler.TryGetPreviewImage( textureCubeResult.Code, out var imagePath ) )
			{
				Texture = imagePath;
			}
			else
			{
				throw new Exception( $"Cannot find PreviewImage for texture input : {textureCubeResult.Code}" );
			}
		}
		else
		{
			Texture = "materials/skybox/skybox_workshop.vtex";
			return NodeResult.MissingInput( "TextureCube" );
		}

		var texture = Sandbox.Texture.Load( Texture );
		var samplerGlobal = compiler.ResultSamplerOrDefault( SamplerInput, SamplerState );
		var textureGlobal = textureCubeResult.Code;
		var coords = compiler.Result( CoordsInput );

		var attributeName = textureCubeResult.Code.TrimStart( "g_t" ).ToString();
		compiler.SetAttribute( attributeName, texture );

		return new NodeResult( ResultType.Vector4, $"TexCubeS( {textureGlobal}," +
			$"{samplerGlobal}," +
			$" {(coords.IsValid ? $"{coords.Cast( 3 )}" : ViewDirection.Result.Invoke( compiler ))} )" );
	};

	private NodeResult Component( string component, GraphCompiler compiler )
	{
		var result = compiler.Result( new NodeInput { Identifier = Identifier, Output = nameof( Result ) } );
		return result.IsValid ? new( ResultType.Float, $"{result}.{component}", true ) : new( ResultType.Float, "0.0f", true );
	}

	/// <summary>
	/// Red component of result
	/// </summary>
	[Output( typeof( float ) ), Hide, Title( "R" )]
	public NodeResult.Func R => ( GraphCompiler compiler ) => Component( "r", compiler );

	/// <summary>
	/// Green component of result
	/// </summary>
	[Output( typeof( float ) ), Hide, Title( "G" )]
	public NodeResult.Func G => ( GraphCompiler compiler ) => Component( "g", compiler );

	/// <summary>
	/// Blue component of result
	/// </summary>
	[Output( typeof( float ) ), Hide, Title( "B" )]
	public NodeResult.Func B => ( GraphCompiler compiler ) => Component( "b", compiler );

	/// <summary>
	/// Alpha (Opacity) component of result
	/// </summary>
	[Output( typeof( float ) ), Hide, Title( "A" )]
	public NodeResult.Func A => ( GraphCompiler compiler ) => Component( "a", compiler );
}

/// <summary>
/// Texture Coordinate from vertex data.
/// </summary>
[Title( "Texture Coordinate" ), Category( "Variables" ), Icon( "texture" )]
public sealed class TextureCoord : ShaderNodePlus
{
	[JsonIgnore, Hide, Browsable( false )]
	public override Color NodeTitleColor => ShaderGraphPlusTheme.NodeHeaderColors.StageInputNode;

	/// <summary>
	/// Use the secondary vertex coordinate
	/// </summary>
	public bool UseSecondaryCoord { get; set; } = false;

	/// <summary>
	/// How many times this coordinate repeats itself to give a tiled effect
	/// </summary>
	public Vector2 Tiling { get; set; } = 1;

	[Hide]
	public override string Title => $"{DisplayInfo.For( this ).Name}{(UseSecondaryCoord ? " 2" : "")}";

	/// <summary>
	/// Coordinate result
	/// </summary>
	[Output( typeof( Vector2 ) )]
	[Hide]
	public NodeResult.Func Result => ( GraphCompiler compiler ) =>
	{
		if ( compiler.IsPreview )
		{
			var result = $"{compiler.ResultValue( UseSecondaryCoord )} ? i.vTextureCoords.zw : i.vTextureCoords.xy";
			return new( ResultType.Vector2, $"{compiler.ResultValue( Tiling.IsNearZeroLength )} ? {result} : ({result}) * {compiler.ResultValue( Tiling )}" );
		}
		else
		{
			var result = UseSecondaryCoord ? "i.vTextureCoords.zw" : "i.vTextureCoords.xy";
			return Tiling.IsNearZeroLength ? new( ResultType.Vector2, result ) : new( ResultType.Vector2, $"{result} * {compiler.ResultValue( Tiling )}" );
		}
	};
}

/// <summary>
/// How a texture is filtered and wrapped when sampled.
/// </summary>
[Title( "Sampler State" ), Category( "Textures" ), Icon( "colorize" )]
public sealed class SamplerNode : ShaderNodePlus//, IParameterNode
{
	[JsonIgnore, Hide, Browsable( false )]
	public override Color NodeTitleColor => ShaderGraphPlusTheme.NodeHeaderColors.ParameterNode;

	[JsonIgnore, Hide, Browsable( false )]
	public override bool CanPreview => false;

	public SamplerNode() : base()
	{
		ExpandSize = new Vector2( 0, 8 );
	}

	[InlineEditor( Label = false ), Group( "Sampler" )]
	[HideIf( nameof( IsSubgraph ), true )]
	public Sampler SamplerState { get; set; } = new Sampler();

	[Hide]
	public override string Title
	{
		get
		{
			var typeName = $"{DisplayInfo.For( this ).Name}";

			if ( !IsSubgraph && !string.IsNullOrWhiteSpace( SamplerState.Name ) )
			{
				return $"{SamplerState.Name}";
			}
			else if ( !IsSubgraph )
			{
				return typeName;
			}
			else if ( IsSubgraph && !string.IsNullOrWhiteSpace( Name ) )
			{
				return $"{Name}";
			}
			else
			{
				return typeName;
			}
		}
	}

	[Hide]
	private bool IsSubgraph => (Graph is ShaderGraphPlus shaderGraph && shaderGraph.IsSubgraph);

	[Hide]
	public string Name { get; set; }

	[Hide, JsonIgnore]
	public FloatParameterUI UI { get; set; }

	[Output( typeof( Sampler ) ), Hide]
	public NodeResult.Func Sampler => ( GraphCompiler compiler ) =>
	{
		var samplerResult = compiler.ResultSampler( SamplerState, Processed );

		return new NodeResult( ResultType.Sampler, samplerResult, true );
	};
}
