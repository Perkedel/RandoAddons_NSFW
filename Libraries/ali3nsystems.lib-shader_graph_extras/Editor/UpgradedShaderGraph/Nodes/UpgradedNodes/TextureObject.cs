namespace Editor.ShaderGraphExtras.Nodes;

[Title( "SGE - Texture Object" ), Category( "Shader Graph Extras - Upgraded" ), Icon( "image" )]
public sealed class SGETextureObjectNode : ShaderNode, ITextureParameterNode
{
	public enum SGETextureObjectMode
	{
		[Title("2D")]
		Texture2D,
		[Title("Cubemap")]
		TextureCubemap
	}

	public SGETextureObjectMode Mode { get; set; } = SGETextureObjectMode.Texture2D;

	[Hide]
	private bool IsTexture2DMode => Mode == SGETextureObjectMode.Texture2D;

	[Hide]
	private bool IsTextureCubemapMode => Mode == SGETextureObjectMode.TextureCubemap;

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
			CreateOutputs();
			Update();
		}
	}

	private void CreateOutputs()
	{
		var plugs = new List<IPlugOut>();
		var serialized = this.GetSerialized();
		foreach ( var property in serialized )
		{
			if ( property.TryGetAttribute<OutputAttribute>( out var outputAttr ) )
			{
				if ( property.TryGetAttribute<ConditionalVisibilityAttribute>( out var conditionalVisibilityAttr ) )
				{
					if ( conditionalVisibilityAttr.TestCondition( this.GetSerialized() ) )
					{
						continue;
					}
				}
				var propertyInfo = typeof( SGETextureObjectNode ).GetProperty( property.Name );
				if ( propertyInfo is null ) continue;
				var info = new PlugInfo( propertyInfo );
				var displayInfo = info.DisplayInfo;
				displayInfo.Name = property.DisplayName;
				info.DisplayInfo = displayInfo;

				// Try to find existing plug to preserve connections
				var oldPlug = Outputs.FirstOrDefault( x => x is BasePlugOut plugOut && plugOut.Info.Name == property.Name ) as BasePlugOut;
				if ( oldPlug is not null )
				{
					oldPlug.Info.Name = info.Name;
					oldPlug.Info.Type = info.Type;
					oldPlug.Info.DisplayInfo = info.DisplayInfo;
					plugs.Add( oldPlug );
				}
				else
				{
					var plug = new BasePlugOut( this, info, info.Type );
					plugs.Add( plug );
				}
			}
		}
		Outputs = plugs;
	}

	[ImageAssetPath]
	public string Image
	{
		get => _image;
		set
		{
			_image = value;
			_asset = AssetSystem.FindByPath( _image );

			if ( _asset == null )
				return;

			CompileTexture();
		}
	}
	[Hide]
	private Asset _asset;
	[Hide]
	private string _texture;
	[Hide]
	private string _image;
	[Hide]
	private string _resourceText;

	[JsonIgnore, Hide]
	private Asset Asset => _asset;

	[JsonIgnore, Hide]
	private string TexturePath => _texture;

	[InlineEditor( Label = false ), Group( "Sampler" )]
	public Sampler Sampler { get; set; }

	private void CompileTexture()
	{
		if ( _asset == null )
			return;

		if ( string.IsNullOrWhiteSpace( _image ) )
			return;

		var ui = UI;
		ui.DefaultTexture = _image;
		UI = ui;

		var resourceText = string.Format( ShaderTemplate.TextureDefinition,
			_image,
			UI.ColorSpace,
			UI.ImageFormat,
			UI.Processor );

		if ( _resourceText == resourceText )
			return;

		_resourceText = resourceText;

		var assetPath = $"shadergraph/{_image.Replace( ".", "_" )}_shadergraph.generated.vtex";
		var resourcePath = FileSystem.Root.GetFullPath( "/.source2/temp" );
		resourcePath = System.IO.Path.Combine( resourcePath, assetPath );

		if ( AssetSystem.CompileResource( resourcePath, resourceText ) )
		{
			_texture = assetPath;
		}
		else
		{
			Log.Warning( $"Failed to compile {_image}" );
		}
	}

	[InlineEditor( Label = false ), Group( "UI" )]
	public TextureInput UI { get; set; } = new TextureInput
	{
		ImageFormat = TextureFormat.DXT5,
		SrgbRead = true,
		Default = Color.White,
	};

	[Hide]
	public override string Title => string.IsNullOrWhiteSpace( UI.Name ) ? null : $"{DisplayInfo.For( this ).Name} {UI.Name}";

	public SGETextureObjectNode() : base()
	{
		Image = "materials/dev/white_color.tga";
		ExpandSize = new Vector2( 0, 128 );
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
	[Hide]

	private int _texture2DSamplerIndex = 0;

	[Hide]
	[Title("Texture")]
	[Output( typeof( Texture2DObject ) )]
	[ShowIf( nameof( IsTexture2DMode ), true )]
	public NodeResult.Func Texture2DOutput => ( GraphCompiler compiler ) =>
	{
		var input = UI;
		input.Type = TextureType.Tex2D;

		CompileTexture();

		var texture = string.IsNullOrWhiteSpace( TexturePath ) ? null : Texture.Load( TexturePath );
		texture ??= Texture.White;

		var result = compiler.ResultTexture( Sampler, input, texture );
		_texture2DSamplerIndex = result.Item2;

		return new NodeResult( NodeResultType.Texture2DObject, result.Item1, constant: true );
	};

	[Hide]
	[Title("Sampler")]
	[Output( typeof( Sampler ) )]
	[ShowIf( nameof( IsTexture2DMode ), true )]
	public NodeResult.Func Texture2DSampler => ( GraphCompiler compiler ) =>
	{
		// Trigger the texture output to ensure sampler index is set
		var textureResult = Texture2DOutput( compiler );

		return new NodeResult( NodeResultType.Sampler, $"g_sSampler{_texture2DSamplerIndex}", constant: true );
	};
	[Hide]

	private int _textureCubemapSamplerIndex = 0;

	[Hide]
	[Title("Texture")]
	[Output( typeof( TextureCubemapObject ) )]
	[ShowIf( nameof( IsTextureCubemapMode ), true )]
	public NodeResult.Func TextureCubemapOutput => ( GraphCompiler compiler ) =>
	{
		var input = UI;
		input.Type = TextureType.TexCube;

		CompileTexture();

		var texture = string.IsNullOrWhiteSpace( TexturePath ) ? null : Texture.Load( TexturePath );
		texture ??= Texture.White;

		var result = compiler.ResultTexture( Sampler, input, texture );
		_textureCubemapSamplerIndex = result.Item2;

		return new NodeResult( NodeResultType.TextureCubemapObject, result.Item1, constant: true );
	};

	[Hide]
	[Title("Sampler")]
	[Output( typeof( Sampler ) )]
	[ShowIf( nameof( IsTextureCubemapMode ), true )]
	public NodeResult.Func TextureCubemapSampler => ( GraphCompiler compiler ) =>
	{
		// Trigger the texture output to ensure sampler index is set
		var textureResult = TextureCubemapOutput( compiler );

		return new NodeResult( NodeResultType.Sampler, $"g_sSampler{_textureCubemapSamplerIndex}", constant: true );
	};
}
