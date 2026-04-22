namespace Editor.ShaderGraphExtras;
public static class PostProcessTemplate
{
	public static Dictionary<string, bool> Features => new()
	{
		{ "SupportsAlbedo", true },
		{ "SupportsEmission", false },
		{ "SupportsOpacity", true },
		{ "SupportsNormal", false },
		{ "SupportsRoughness", false },
		{ "SupportsMetalness", false },
		{ "SupportsAmbientOcclusion", false },
		{ "SupportsPositionOffset", false },
		{ "SupportsPixelDepthOffset", false },

		{ "SupportsLitShadingModel", false },
		{ "SupportsUnlitShadingModel", true },
		{ "SupportsCustomShadingModel", false },

		{ "SupportsOpaqueBlendMode", true },
		{ "SupportsMaskedBlendMode", true },
		{ "SupportsTranslucentBlendMode", true },
		{ "SupportsDynamicBlendMode", true }
	};
	public static string Code => @"
HEADER
{{
	Description = ""{0}"";
}}

FEATURES
{{
	#include ""common/features.hlsl""
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
	float4 vColor : COLOR0 < Semantic( Color ); >;
{3}
}};

struct PixelInput
{{
	#include ""common/pixelinput.hlsl""
	float3 vPositionOs : TEXCOORD14;
	float3 vNormalOs : TEXCOORD15;
	float4 vTangentUOs_flTangentVSign : TANGENT	< Semantic( TangentU_SignV ); >;
	float4 vColor : COLOR0;
	float4 vTintColor : COLOR1;
	#if ( PROGRAM == VFX_PROGRAM_PS )
		bool vFrontFacing : SV_IsFrontFace;
	#endif
{4}
}};

VS
{{
	#include ""common/vertex.hlsl""
{5}{6}{7}
	PixelInput MainVs( VertexInput v )
	{{

		PixelInput i;
		i.vPositionPs = float4(v.vPositionOs.xy, 0.0f, 1.0f );
		i.vPositionWs = float3(v.vTexCoord, 0.0f);
{8}					
		return i;
	}}
}}

PS
{{
	#include ""common/pixel.hlsl""
	#include ""postprocess/functions.hlsl""
	#include ""postprocess/common.hlsl""

{9}{10}{11}

	Texture2D g_tColorBuffer < Attribute( ""ColorBuffer"" ); SrgbRead ( true ); >;

	float4 MainPs( PixelInput i ) : SV_Target0
	{{
		Material m = Material::Init( i );
		m.Albedo = float3( 1, 1, 1 );
		m.Opacity = 1;
{12}
		m.Opacity = saturate( m.Opacity );
{13}

	}}
}}
";
}
