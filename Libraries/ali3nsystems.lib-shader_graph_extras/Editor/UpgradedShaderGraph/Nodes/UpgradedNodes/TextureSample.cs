namespace Editor.ShaderGraphExtras.Nodes;

[Title("SGE - Texture Sample"), Category( "Shader Graph Extras - Upgraded" ), Icon("colorize")]
public sealed class SGETextureSampleNode : ShaderNode
{
	public enum SGETextureSampleMode
	{
		[Title("2D")]
		Texture2D,
		[Title("Cubemap")]
		TextureCubemap
	}

	public SGETextureSampleMode Mode { get; set; } = SGETextureSampleMode.Texture2D;

	[Hide]
	private bool IsTexture2DMode => Mode == SGETextureSampleMode.Texture2D;

	[Hide]
	private bool IsTextureCubemapMode => Mode == SGETextureSampleMode.TextureCubemap;

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
				var propertyInfo = typeof( SGETextureSampleNode ).GetProperty( property.Name );
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

	[Hide]
	[Title( "Coordinates" )]
	[Input( typeof( Vector2 ) )]
	[ShowIf( nameof( IsTexture2DMode ), true )]
	public NodeInput Texture2DCoordinates { get; set; }

	[Hide]
	[Title( "Coordinates" )]
	[Input( typeof( Vector3 ) )]
	[ShowIf( nameof( IsTextureCubemapMode ), true )]
	public NodeInput TextureCubemapCoordinates { get; set; }

	[Hide]
	[Title("Texture")]
	[Input(typeof(Texture2DObject))]
	[ShowIf( nameof( IsTexture2DMode ), true )]
	public NodeInput Texture2DInput { get; set; }

	[Hide]
	[Title("Sampler")]
	[Input(typeof(Sampler))]
	[ShowIf( nameof( IsTexture2DMode ), true )]
	public NodeInput Texture2DSampler { get; set; }

	[Hide]
	[Title("Texture")]
	[Input(typeof(TextureCubemapObject))]
	[ShowIf( nameof( IsTextureCubemapMode ), true )]
	public NodeInput TextureCubemapInput { get; set; }

	[Hide]
	[Title("Sampler")]
	[Input(typeof(Sampler))]
	[ShowIf( nameof( IsTextureCubemapMode ), true )]
	public NodeInput TextureCubemapSampler { get; set; }

	[Hide]
	[Output(typeof(Color)), Title("Output")]
	public NodeResult.Func Output => (GraphCompiler compiler) =>
	{
		NodeResult result = new NodeResult();

		switch (Mode)
		{
			case SGETextureSampleMode.Texture2D:
			{
				var coordinates = compiler.Result(Texture2DCoordinates);
				var texture = compiler.Result(Texture2DInput);
				var sampler = compiler.Result(Texture2DSampler);

				if (!texture.IsValid)
				{
					return new NodeResult(4, "float4(1, 0, 1, 1)", true);
				}

				if (compiler.Stage == GraphCompiler.ShaderStage.Vertex)
				{
					result = new NodeResult(4, $"{texture}.SampleLevel(" +
						$" {sampler}," +
						$" {(coordinates.IsValid ? $"{coordinates.Cast(2)}" : "i.vTextureCoordinates.xy")}, 0 )");
				}
				else
				{
					result = new NodeResult(4, $"Tex2DS( {texture}," +
						$" {sampler}," +
						$" {(coordinates.IsValid ? $"{coordinates.Cast(2)}" : "i.vTextureCoordinates.xy")} )");
				}
				break;
			}

			case SGETextureSampleMode.TextureCubemap:
			{
				var coordinates = compiler.Result(TextureCubemapCoordinates);
				var texture = compiler.Result(TextureCubemapInput);
				var sampler = compiler.Result(TextureCubemapSampler);

				if (!texture.IsValid)
				{
					return new NodeResult(4, "float4(1, 0, 1, 1)", true);
				}

				if (compiler.Stage == GraphCompiler.ShaderStage.Vertex)
				{
					result = new NodeResult(4, $"{texture}.SampleLevel(" +
						$" {sampler}," +
						$" {(coordinates.IsValid ? $"{coordinates.Cast(3)}" : "i.vNormalWs")}, 0 )");
				}
				else
				{
					result = new NodeResult(4, $"TexCubeS( {texture}," +
						$" {sampler}," +
						$" {(coordinates.IsValid ? $"{coordinates.Cast(3)}" : "i.vNormalWs")} )");
				}
				break;
			}
		}

		return result;
	};
}
