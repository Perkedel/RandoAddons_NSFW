
HEADER
{
	Description = "";
}

FEATURES
{
	#include "common/features.hlsl"
}

MODES
{
	Forward();
	Depth();
	ToolsShadingComplexity( "tools_shading_complexity.shader" );
}

COMMON
{
	#ifndef S_ALPHA_TEST
	#define S_ALPHA_TEST 0
	#endif
	#ifndef S_TRANSLUCENT
	#define S_TRANSLUCENT 0
	#endif
	
	#include "common/shared.hlsl"
	#include "procedural.hlsl"

	#define S_UV2 1
}

struct VertexInput
{
	#include "common/vertexinput.hlsl"
	float4 vColor : COLOR0 < Semantic( Color ); >;
};

struct PixelInput
{
	#include "common/pixelinput.hlsl"
	float3 vPositionOs : TEXCOORD14;
	float3 vNormalOs : TEXCOORD15;
	float4 vTangentUOs_flTangentVSign : TANGENT	< Semantic( TangentU_SignV ); >;
	float4 vColor : COLOR0;
	float4 vTintColor : COLOR1;
	#if ( PROGRAM == VFX_PROGRAM_PS )
		bool vFrontFacing : SV_IsFrontFace;
	#endif
};

VS
{
	#include "common/vertex.hlsl"

	PixelInput MainVs( VertexInput v )
	{
		
		PixelInput i = ProcessVertex( v );
		i.vPositionOs = v.vPositionOs.xyz;
		i.vColor = v.vColor;
		
		ExtraShaderData_t extraShaderData = GetExtraPerInstanceShaderData( v.nInstanceTransformID );
		i.vTintColor = extraShaderData.vTint;
		
		VS_DecodeObjectSpaceNormalAndTangent( v, i.vNormalOs, i.vTangentUOs_flTangentVSign );
		return FinalizeVertex( i );
		
	}
}

PS
{
	#include "common/pixel.hlsl"
	RenderState( CullMode, F_RENDER_BACKFACES ? NONE : DEFAULT );
		
	SamplerState g_sSampler0 < Filter( ANISO ); AddressU( WRAP ); AddressV( WRAP ); >;
	CreateInputTexture2D( Texture_ps_0, Srgb, 8, "None", "_color", ",0/,0/0", DefaultFile( "materials/dev/white_color.tga" ) );
	CreateInputTexture2D( Texture_ps_1, Linear, 8, "NormalizeNormals", "_normal", ",0/,0/0", DefaultFile( "materials/dev/white_color.tga" ) );
	Texture2D g_tTexture_ps_0 < Channel( RGBA, Box( Texture_ps_0 ), Srgb ); OutputFormat( DXT5 ); SrgbRead( True ); >;
	Texture2D g_tTexture_ps_1 < Channel( RGBA, Box( Texture_ps_1 ), Linear ); OutputFormat( DXT5 ); SrgbRead( False ); >;
	TextureAttribute( LightSim_DiffuseAlbedoTexture, g_tTexture_ps_0 )
	TextureAttribute( RepresentativeTexture, g_tTexture_ps_0 )
		
	float3 TexTriplanar_Normal( in Texture2D tTex, in SamplerState sSampler, float3 vPosition, float3 vNormal )
	{
		float2 uvX = vPosition.zy;
		float2 uvY = vPosition.xz;
		float2 uvZ = vPosition.xy;
	
		float3 triblend = saturate( pow( abs( vNormal ), 4 ) );
		triblend /= max( dot( triblend, half3( 1, 1, 1 ) ), 0.0001 );
	
		half3 axisSign = vNormal < 0 ? -1 : 1;
	
		uvX.x *= axisSign.x;
		uvY.x *= axisSign.y;
		uvZ.x *= -axisSign.z;
	
		float3 tnormalX = DecodeNormal( Tex2DS( tTex, sSampler, uvX ).xyz );
		float3 tnormalY = DecodeNormal( Tex2DS( tTex, sSampler, uvY ).xyz );
		float3 tnormalZ = DecodeNormal( Tex2DS( tTex, sSampler, uvZ ).xyz );
	
		tnormalX.x *= axisSign.x;
		tnormalY.x *= axisSign.y;
		tnormalZ.x *= -axisSign.z;
	
		tnormalX = half3( tnormalX.xy + vNormal.zy, vNormal.x );
		tnormalY = half3( tnormalY.xy + vNormal.xz, vNormal.y );
		tnormalZ = half3( tnormalZ.xy + vNormal.xy, vNormal.z );
	
		return normalize(
			tnormalX.zyx * triblend.x +
			tnormalY.xzy * triblend.y +
			tnormalZ.xyz * triblend.z +
			vNormal
		);
	}
	
	float4 MainPs( PixelInput i ) : SV_Target0
	{
		
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
		
		float4 l_0 = Tex2DS( g_tTexture_ps_0, g_sSampler0, i.vTextureCoords.xy );
		float3 l_1 = TexTriplanar_Normal( g_tTexture_ps_1, g_sSampler0, (i.vPositionWithOffsetWs.xyz + g_vHighPrecisionLightingOffsetWs.xyz) / 39.3701, normalize( i.vNormalWs.xyz ) );
		
		m.Albedo = l_0.xyz;
		m.Opacity = l_0.a;
		m.Normal = l_1;
		m.Roughness = 1;
		m.Metalness = 0;
		m.AmbientOcclusion = 1;
		
		
		m.AmbientOcclusion = saturate( m.AmbientOcclusion );
		m.Roughness = saturate( m.Roughness );
		m.Metalness = saturate( m.Metalness );
		m.Opacity = saturate( m.Opacity );
		
		// Result node takes normal as tangent space, convert it to world space now
		m.Normal = TransformNormal( m.Normal, i.vNormalWs, i.vTangentUWs, i.vTangentVWs );
		
		// for some toolvis shit
		m.WorldTangentU = i.vTangentUWs;
		m.WorldTangentV = i.vTangentVWs;
		m.TextureCoords = i.vTextureCoords.xy;
				
		return ShadingModelStandard::Shade( m );
	}
}
