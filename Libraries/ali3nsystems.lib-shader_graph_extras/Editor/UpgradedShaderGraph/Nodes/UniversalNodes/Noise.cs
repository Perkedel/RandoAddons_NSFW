namespace Editor.ShaderGraphExtras.Nodes;

[Title("SGE - Noise"), Category( "Shader Graph Extras - Universal" ), Icon("grain")]
public sealed class SGENoiseNode : ShaderNode
{
	public enum SGENoiseMode
	{
		Static,
		Value,
		Simplex,
		[Title("fBM")]
		fBM,
		Voronoi
	}

	public SGENoiseMode Mode { get; set; } = SGENoiseMode.fBM;

	public enum SGENoiseDimension
	{
		[Title("2D")]
		Noise2D,
		[Title("3D")]
		Noise3D
	}

	public SGENoiseDimension Dimension { get; set; } = SGENoiseDimension.Noise2D;

	[Hide, JsonIgnore]
	int _lastHashCode = 0;

	public override void OnFrame()
	{
		base.OnFrame();

		var hashCode = new HashCode();
		hashCode.Add(Mode);
		hashCode.Add(Dimension);
		var hc = hashCode.ToHashCode();
		if (hc != _lastHashCode)
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
		foreach (var property in serialized)
		{
			if (property.TryGetAttribute<InputAttribute>(out var inputAttr))
			{
				if (property.TryGetAttribute<ConditionalVisibilityAttribute>(out var conditionalVisibilityAttr))
				{
					if (conditionalVisibilityAttr.TestCondition(this.GetSerialized()))
					{
						continue;
					}
				}
				var propertyInfo = typeof(SGENoiseNode).GetProperty(property.Name);
				if (propertyInfo is null) continue;
				var info = new PlugInfo(propertyInfo);
				var displayInfo = info.DisplayInfo;
				displayInfo.Name = property.DisplayName;
				info.DisplayInfo = displayInfo;

				// Try to find existing plug to preserve connections
				var oldPlug = Inputs.FirstOrDefault(x => x is BasePlugIn plugIn && plugIn.Info.Name == property.Name) as BasePlugIn;
				if (oldPlug is not null)
				{
					oldPlug.Info.Name = info.Name;
					oldPlug.Info.Type = info.Type;
					oldPlug.Info.DisplayInfo = info.DisplayInfo;
					plugs.Add(oldPlug);
				}
				else
				{
					var plug = new BasePlugIn(this, info, info.Type);
					plugs.Add(plug);
				}
			}
		}
		Inputs = plugs;
	}

	[Hide]
	private bool IsNoisefBMMode => Mode == SGENoiseMode.fBM;
	[Hide]
	private bool IsNoiseVoronoiMode => Mode == SGENoiseMode.Voronoi;

	[Hide]
	[Input(typeof(Vector3))]
	public NodeInput Coordinates { get; set; }

	[Hide]
	[Input(typeof(int))]
	[ShowIf(nameof(IsNoisefBMMode), true)]
	public NodeInput Octaves { get; set; }

	[InputDefault(nameof(Octaves))]
	[ShowIf(nameof(IsNoisefBMMode), true)]
	public float DefaultOctaves { get; set; } = 6;

	[Hide]
	[Input(typeof(float))]
	[ShowIf(nameof(IsNoiseVoronoiMode), true)]
	public NodeInput Offset { get; set; }

	[InputDefault(nameof(Offset))]
	[ShowIf(nameof(IsNoiseVoronoiMode), true)]
	public float DefaultOffset { get; set; } = 3.14159265359f;

	[Hide]
	[Output(typeof(float))]
	public NodeResult.Func Output => (GraphCompiler compiler) =>
	{
		var coordinates = compiler.Result(Coordinates);

		compiler.RegisterInclude("shaders/HLSL/Functions/FUNC-noise.hlsl");

		NodeResult result = new NodeResult();

		switch (Mode)
		{
			case SGENoiseMode.Static:
				if (Dimension == SGENoiseDimension.Noise2D)
				{
					result = new NodeResult(NodeResultType.Float, $"SGEStaticNoise2D({coordinates})");
				}
				else
				{
					result = new NodeResult(NodeResultType.Float, $"SGEStaticNoise3D({coordinates})");
				}
				break;

			case SGENoiseMode.Value:
				if (Dimension == SGENoiseDimension.Noise2D)
				{
					result = new NodeResult(NodeResultType.Float, $"SGEValueNoise2D({coordinates})");
				}
				else
				{
					result = new NodeResult(NodeResultType.Float, $"SGEValueNoise3D({coordinates})");
				}
				break;

			case SGENoiseMode.Simplex:
				if (Dimension == SGENoiseDimension.Noise2D)
				{
					result = new NodeResult(NodeResultType.Float, $"SGESimplexNoise2D({coordinates})");
				}
				else
				{
					result = new NodeResult(NodeResultType.Float, $"SGESimplexNoise3D({coordinates})");
				}
				break;

			case SGENoiseMode.fBM:
				var octaves = compiler.ResultOrDefault(Octaves, DefaultOctaves);

				if (Dimension == SGENoiseDimension.Noise2D)
				{
					result = new NodeResult(NodeResultType.Float, $"SGEfBMNoise2D({coordinates}, {octaves})");
				}
				else
				{
					result = new NodeResult(NodeResultType.Float, $"SGEfBMNoise3D({coordinates}, {octaves})");
				}
				break;

			case SGENoiseMode.Voronoi:
				var offset = compiler.ResultOrDefault(Offset,DefaultOffset);
				if (Dimension == SGENoiseDimension.Noise2D)
				{
					result = new NodeResult(NodeResultType.Float, $"SGEVoronoiNoise2D({coordinates}, {offset})");
				}
				else
				{
					result = new NodeResult(NodeResultType.Float, $"SGEVoronoiNoise3D({coordinates}, {offset})");
				}
				break;
		}

		return result;
	};
}
