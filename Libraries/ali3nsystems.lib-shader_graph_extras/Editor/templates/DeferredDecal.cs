namespace Editor.ShaderGraphExtras;
public static class SGEDeferredDecalTemplate
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
		{ "SupportsPositionOffset", false },
		{ "SupportsPixelDepthOffset", false},

        {"SupportsLitShadingModel", true},
		{"SupportsUnlitShadingModel", true},
		{"SupportsCustomShadingModel", true},

		{"SupportsOpaqueBlendMode", false},
		{"SupportsMaskedBlendMode", false},
		{"SupportsTranslucentBlendMode", true},
		{"SupportsDynamicBlendMode", false},
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
	float3 vDecalOrigin : TEXCOORD14;
	float3 vDecalRight : TEXCOORD15;
	float3 vDecalUp : TEXCOORD16;
	float3 vDecalForward : TEXCOORD17;
	float3 vDecalScale : TEXCOORD18;
	float3 vPositionOs : TEXCOORD19;
	float4 vColor : COLOR0;
	float4 vTintColor : COLOR1;
{4}
}};

VS
{{
	#include ""common/vertex.hlsl""
	#include ""instancing.fxc""
{5}{6}{7}
	PixelInput MainVs( VertexInput v )
	{{
		float3x4 matObjectToWorld = GetTransformMatrix( v.nInstanceTransformID );

		float3 vRight = mul( matObjectToWorld, float4( 1, 0, 0, 0 ) );
		float3 vUp = mul( matObjectToWorld, float4( 0, 1, 0, 0 ) );
		float3 vForward = mul( matObjectToWorld, float4( 0, 0, 1, 0 ) );

		float3 vScale = float3( length( vRight ), length( vUp ), length( vForward ) );

		float3 vBoundsHalfSize = abs( v.vPositionOs.xyz );

		vBoundsHalfSize *= vScale;

		float projectionDepth = 1;
		float3 vScaledPositionOs = v.vPositionOs.xyz;
		vScaledPositionOs.x *= projectionDepth;
		vBoundsHalfSize.x *= projectionDepth;

		float3 vPositionWs = mul( matObjectToWorld, float4( vScaledPositionOs, 1.0 ) );

		PixelInput i = ProcessVertex( v );

		i.vPositionOs = v.vPositionOs.xyz;
		i.vColor = v.vColor;

		ExtraShaderData_t extraShaderData = GetExtraPerInstanceShaderData( v.nInstanceTransformID );
		i.vTintColor = extraShaderData.vTint;

		i.vPositionPs = Position3WsToPs( vPositionWs );

		i.vDecalOrigin = mul( matObjectToWorld, float4( 0, 0, 0, 1.0 ) );

		i.vDecalScale = vBoundsHalfSize;

		i.vDecalRight = vRight / vScale.x;
		i.vDecalUp = vUp / vScale.y;
		i.vDecalForward = vForward / vScale.z;
{8}
		return FinalizeVertex( i );
	}}
}}

PS
{{
	#include ""common/pixel.hlsl""
	#include ""common/classes/Depth.hlsl""
{9}{10}{11}
	RenderState( BlendEnable, true );
	RenderState( SrcBlend, SRC_ALPHA );
	RenderState( DstBlend, INV_SRC_ALPHA );
	RenderState( DepthWriteEnable, false );
	RenderState( DepthEnable, false );
	RenderState( CullMode, Front );

	float4 MainPs( PixelInput i ) : SV_Target0
	{{
		float3 worldPos = Depth::GetWorldPosition( i.vPositionSs.xy );

		float3 vDelta = worldPos - i.vDecalOrigin;

		float3 vProjected = float3( dot( vDelta, i.vDecalRight ), dot( vDelta, i.vDecalUp ), dot( vDelta, i.vDecalForward ) );

		float3 vHalfBounds = i.vDecalScale;
		float3 vDecalBounds = vHalfBounds * 2.0;

		if( abs( vProjected.y ) > vHalfBounds.y || abs( vProjected.z ) > vHalfBounds.z )
			discard;

		float distanceFromBoxStart = vProjected.x + vHalfBounds.x;
		if( distanceFromBoxStart < 0.0 || distanceFromBoxStart > vDecalBounds.x )
			discard;

		float2 decalUV = (vProjected.yz / vDecalBounds.yz) + 0.5;

		i.vTextureCoords.xy = 1.0 - decalUV;

		i.vNormalWs = -i.vDecalRight;
		i.vTangentUWs = -i.vDecalUp;
		i.vTangentVWs = -i.vDecalForward;

		float3 rotatedY = float3( -vProjected.z, vProjected.y, vProjected.x );
		i.vPositionOs = float3( -rotatedY.x, -rotatedY.y, rotatedY.z );

		i.vPositionWithOffsetWs.xyz = worldPos - g_vHighPrecisionLightingOffsetWs.xyz;

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

		// Toolvis:
		m.WorldTangentU = i.vTangentUWs;
		m.WorldTangentV = i.vTangentVWs;
		m.TextureCoords = i.vTextureCoords.xy;

{13}
	}}
}}";
}
