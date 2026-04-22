namespace Editor.ShaderGraphExtras.Nodes;

[Title( "SGE - Chromatic Aberration" ), Category( "Shader Graph Extras - Upgraded" ), Icon( "lens_blur" )]
public sealed class SGEChromaticAberrationNode : ShaderNode
{
    public enum SGEChromaticAberrationMode
	{
        Texture2D,
		Vector
	}

    public SGEChromaticAberrationMode Mode { get; set; } = SGEChromaticAberrationMode.Texture2D;

    [Hide]
    private bool IsTexture2DMode => Mode == SGEChromaticAberrationMode.Texture2D;

    [Hide]
    private bool IsVectorMode => Mode == SGEChromaticAberrationMode.Vector;

    [Hide, JsonIgnore]
    int _lastHashCode = 0;

    public override void OnFrame()
    {
        base.OnFrame();

        var hashCode = new HashCode();
        hashCode.Add(Mode);
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
                var propertyInfo = typeof(SGEChromaticAberrationNode).GetProperty(property.Name);
                if (propertyInfo is null) continue;
                var info = new PlugInfo(propertyInfo);
                var displayInfo = info.DisplayInfo;
                displayInfo.Name = property.DisplayName;
                info.DisplayInfo = displayInfo;

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
    public static string SGEChromaticAberrationTexture2D => @"
        float4 SGEChromaticAberrationTexture2D(float2 coordinates, Texture2D texture, SamplerState sampler, float scale, float3 offset)
        {
            float2 offsetScale = (coordinates - 0.5) * (scale * 0.005);

            float4 r = texture.Sample(sampler, coordinates - (offsetScale * offset.r));
            float4 g = texture.Sample(sampler, coordinates - (offsetScale * offset.g));
            float4 b = texture.Sample(sampler, coordinates - (offsetScale * offset.b));

            return float4(r.r, g.g, b.b, g.a);
        }
        ";

    [Hide]
    public static string SGEChromaticAberrationVector => @"
        float4 SGEChromaticAberrationVector(float4 input, float scale, float3 offset)
        {
            float4 derivative = ddx(input);
            float3 offsetScale = float3(scale, scale, scale) * offset;
            float4 aberration = derivative * float4(offsetScale, 0);
            return input + aberration;
        }
        ";

	[Input( typeof( Vector2 ) )]
	[Hide]
    [ShowIf(nameof(IsTexture2DMode), true)]
	public NodeInput Coordinates { get; set; }

	[Input(typeof(Texture2DObject))]
	[Hide]
    [ShowIf(nameof(IsTexture2DMode), true)]
	public NodeInput Texture { get; set; }

	[Input(typeof(Sampler))]
	[Hide]
    [ShowIf(nameof(IsTexture2DMode), true)]
	public NodeInput Sampler { get; set; }

    [Input(typeof(Color))]
    [Hide]
    [ShowIf(nameof(IsVectorMode), true)]
    public NodeInput Input { get; set; }

    [Input( typeof( float ) )]
    [Hide]
    public NodeInput Scale { get; set; }

    [Input( typeof( Vector3 ) )]
    [Hide]
    public NodeInput Offset { get; set; }

    [InputDefault( nameof( Scale ) )]
	public float DefaultScale { get; set; } = 1;

	[InputDefault( nameof( Offset ) )]
	public Vector3 DefaultOffset { get; set; } = new Vector3 (1, 0, -1);

    [Output( typeof( Color ) )]
    [Hide]
    public NodeResult.Func Output => ( GraphCompiler compiler ) =>
    {
        var scale = compiler.ResultOrDefault(Scale, DefaultScale);
        var offset = compiler.ResultOrDefault(Offset, DefaultOffset);

        NodeResult nodeResult = new NodeResult();
        
        switch( Mode )
        {
            case SGEChromaticAberrationMode.Texture2D:
                var coordinates = compiler.Result(Coordinates);
                var texture = compiler.Result(Texture);
                var sampler = compiler.Result(Sampler);
                nodeResult = new NodeResult(NodeResultType.Color, compiler.ResultFunction( compiler.RegisterFunction( SGEChromaticAberrationTexture2D ), $"{coordinates}, {texture}, {sampler}, {scale}, {offset}"));
                break;

            case SGEChromaticAberrationMode.Vector:
                var color = compiler.Result(Input);
                nodeResult = new NodeResult(NodeResultType.Color, compiler.ResultFunction( compiler.RegisterFunction( SGEChromaticAberrationVector ), $"{color}, {scale}, {offset}"));
                break;
        }
        return nodeResult;
    };
}