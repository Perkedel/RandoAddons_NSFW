namespace Editor.ShaderGraphExtras;
public static class SGEBlendableTemplate
{
	public static Dictionary<string, bool> Features => new()
	{
		{ "SupportsAlbedo", true},
		{ "SupportsEmission", true },
		{ "SupportsOpacity", true},
		{ "SupportsNormal", true },
		{ "SupportsRoughness", true },
		{ "SupportsMetalness", true },
		{ "SupportsAmbientOcclusion", true },
		{ "SupportsPositionOffset", true },
        { "SupportsPixelDepthOffset", true},

        {"SupportsLitShadingModel", true},
		{"SupportsUnlitShadingModel", true},
		{"SupportsCustomShadingModel", true},

		{"SupportsOpaqueBlendMode", true},
		{"SupportsMaskedBlendMode", true},
		{"SupportsTranslucentBlendMode", true},
		{"SupportsDynamicBlendMode", true},
		{"SupportsCustomBlendMode", false},
	};

	public static string Code => @"
HEADER
{{
	Description = ""{0}"";
}}

FEATURES
{{
	#include ""common/features.hlsl""
	Feature( F_MULTIBLEND, 0..3 ( 0=""1 Layers"", 1=""2 Layers"", 2=""3 Layers"", 3=""4 Layers"" ), ""Number Of Blendable Layers"" );
	{1}

}}

MODES
{{
	Forward();
	Depth();
	ToolsShadingComplexity( ""tools_shading_complexity.shader"" );
}}

COMMON
{{
{2}
	#include ""common/shared.hlsl""
	#include ""procedural.hlsl""
	#define S_UV2 1
}}

struct VertexInput
{{
	#include ""common/vertexinput.hlsl""
	float4 vColorBlendValues : TEXCOORD4 < Semantic( VertexPaintBlendParams ); >;
	float4 vColorPaintValues : TEXCOORD5 < Semantic( VertexPaintTintColor ); >;
	float4 vColor : COLOR0 < Semantic( Color ); >;
{3}
}};

struct PixelInput
{{
	#include ""common/pixelinput.hlsl""
	float4 vBlendValues	: TEXCOORD14;
	float4 vPaintValues : TEXCOORD15;
	float4 vColor : COLOR0;

{4}
}};

VS
{{
	StaticCombo( S_MULTIBLEND, F_MULTIBLEND, Sys( PC ) );

	#include ""common/vertex.hlsl""

	BoolAttribute( VertexPaintUI2Layer, F_MULTIBLEND == 1 );
	BoolAttribute( VertexPaintUI3Layer, F_MULTIBLEND == 2 );
	BoolAttribute( VertexPaintUI4Layer, F_MULTIBLEND == 3 );
	BoolAttribute( VertexPaintUIPickColor, true );
	BoolAttribute( ShadowFastPath, true );

{5}{6}{7}
	PixelInput MainVs( VertexInput v )
	{{
		PixelInput i = ProcessVertex( v );
		i.vBlendValues = v.vColorBlendValues;
		i.vPaintValues = v.vColorPaintValues;
		i.vColor = v.vColor;

		// Models don't have vertex paint data, let's avoid painting them black
		[flatten]
		if( i.vPaintValues.w == 0 )
			i.vPaintValues = 1.0f;

		{8}

		return FinalizeVertex( i );
	}}
}}

PS
{{
	StaticCombo( S_MULTIBLEND, F_MULTIBLEND, Sys( PC ) );

	#include ""common/pixel.hlsl""
{9}{10}{11}
	float4 MainPs( PixelInput i ) : SV_Target0
	{{

		Material m = Material::Init( i );
		m.Albedo = float3( 1, 1, 1 );
		m.Normal = float3( 0, 0, 1 );
		m.Roughness = 1;
		m.Metalness = 0;
		m.AmbientOcclusion = 1;
		m.TintMask = 1;
		m.Opacity = 1;
		m.Emission = float3( 0, 0, 0 );
		m.Transmission = 0;
{12}
		m.AmbientOcclusion = saturate( m.AmbientOcclusion );
		m.Roughness = saturate( m.Roughness );
		m.Metalness = saturate( m.Metalness );
		m.Opacity = saturate( m.Opacity );

		// Result node takes normal as tangent space, convert it to world space now
		m.Normal = TransformNormal( m.Normal, i.vNormalWs, i.vTangentUWs, i.vTangentVWs );

		// Toolvis:
		m.WorldTangentU = i.vTangentUWs;
		m.WorldTangentV = i.vTangentVWs;
		m.TextureCoords = i.vTextureCoords.xy;
{13}
	}}
}}";
}
