FEATURES
{
    #include "common/features.hlsl"
}

MODES
{
	
	VrForward();
	Depth(); 
	ToolsVis( S_MODE_TOOLS_VIS );
	ToolsWireframe( "vr_tools_wireframe.shader" );
	ToolsShadingComplexity( "tools_shading_complexity.shader" );

}

COMMON
{

	#define CUSTOM_MATERIAL_INPUTS
    #define CUSTOM_TEXTURE_FILTERING




    #include "procedural.hlsl"
    #include "common/shared.hlsl"
    #include "common/Bindless.hlsl"
    #include "terrain/TerrainCommon.hlsl"

    int g_nDebugView < Attribute( "DebugView" ); >;
    int g_nPreviewLayer < Attribute( "PreviewLayer" ); >;

}

struct VertexInput
{
	float3 PositionAndLod : POSITION < Semantic( PosXyz ); >;
};

struct PixelInput
{
    float4 vTextureCoords : TEXCOORD2;
	float3 LocalPosition : TEXCOORD0;
    float3 WorldPosition : TEXCOORD1;


    uint LodLevel : COLOR0;

    #if ( PROGRAM == VFX_PROGRAM_VS )
        float4 PixelPosition : SV_Position;
    #endif

    #if ( PROGRAM == VFX_PROGRAM_PS )
        float4 ScreenPosition : SV_Position;
    #endif
};






VS
{
    #include "terrain/TerrainClipmap.hlsl"

	


	PixelInput MainVs( VertexInput i )
	{
        PixelInput o;

        o.LocalPosition = Terrain_ClipmapSingleMesh( i.PositionAndLod, Terrain::GetHeightMap(), Terrain::Get().Resolution, Terrain::Get().TransformInv );


		


        o.LocalPosition.z *= (Terrain::Get().HeightScale );


        o.WorldPosition = mul( Terrain::Get().Transform, float4( o.LocalPosition, 1.0 ) ).xyz;
        o.PixelPosition = Position3WsToPs( o.WorldPosition.xyz );
        o.LodLevel = i.PositionAndLod.z;




        // check for holes in vertex shader, better results and faster
        float hole = Terrain::GetHolesMap().Load( int3( o.LocalPosition.xy / Terrain::Get().Resolution, 0 ) ).r;
        if ( hole > 0.0f )
        {
            o.LocalPosition = float3( 0. / 0., 0, 0 );
            o.WorldPosition = mul( Terrain::Get().Transform, float4( o.LocalPosition, 1.0 ) ).xyz;
            o.PixelPosition = Position3WsToPs( o.WorldPosition.xyz );            
        }

		return o;
	}
}

PS
{
    //StaticCombo( S_MODE_DEPTH, 0..1, Sys( ALL ) );
    //DynamicCombo( D_GRID, 0..1, Sys( ALL ) );    
    //DynamicCombo( D_AUTO_SPLAT, 0..1, Sys( ALL ) );    


    #include "common/pixel.hlsl"
    #include "common/material.hlsl"
    #include "common/shadingmodel.hlsl"
    #include "terrain/TerrainNoTile.hlsl"
	#include "terrain/TerrainCommon.hlsl"

	

	SamplerState g_sSampler0 < Filter( ANISO ); AddressU( Wrap ); AddressV( Wrap ); >;
	SamplerState g_sSampler1 < Filter( ANISO ); AddressU( Wrap ); AddressV( Wrap ); >;
	CreateInputTexture2D( TextureAtHight0, Srgb, 8, "None", "_color", "Terrain Textures ,0/0.0", Default4( 1.00, 1.00, 1.00, 1.00 ) );
	CreateInputTexture2D( TextureAtHight1, Srgb, 8, "None", "_color", "Terrain Textures ,0/0.1", Default4( 1.00, 1.00, 1.00, 1.00 ) );
	CreateInputTexture2D( NoiseTexture, Srgb, 8, "None", "_color", "HeightMap, ", Default4( 1.00, 1.00, 1.00, 1.00 ) );
	CreateInputTexture2D( TextureAtHight2, Srgb, 8, "None", "_color", "Terrain Textures ,0/0.2", Default4( 1.00, 1.00, 1.00, 1.00 ) );
	CreateInputTexture2D( TextureAtHight3, Srgb, 8, "None", "_color", "Terrain Textures ,0/0.3", Default4( 1.00, 1.00, 1.00, 1.00 ) );
	CreateInputTexture2D( TextureAtHight4, Srgb, 8, "None", "_color", "Terrain Textures ,0/0.4", Default4( 1.00, 1.00, 1.00, 1.00 ) );
	CreateInputTexture2D( TextureAtHight5, Srgb, 8, "None", "_color", "Terrain Textures ,0/0.5", Default4( 1.00, 1.00, 1.00, 1.00 ) );
	CreateInputTexture2D( TextureAtHight6, Srgb, 8, "None", "_color", "Terrain Textures ,0/0.6", Default4( 1.00, 1.00, 1.00, 1.00 ) );
	CreateInputTexture2D( TextureAtHight7, Srgb, 8, "None", "_color", "Terrain Textures ,0/0.7", Default4( 1.00, 1.00, 1.00, 1.00 ) );
	CreateInputTexture2D( TextureAtHight8, Srgb, 8, "None", "_color", "Terrain Textures ,0/0.8", Default4( 1.00, 1.00, 1.00, 1.00 ) );
	CreateInputTexture2D( TextureAtHight9, Srgb, 8, "None", "_color", "Terrain Textures ,0/0.9", Default4( 1.00, 1.00, 1.00, 1.00 ) );
	CreateInputTexture2D( TextureAtHight10, Srgb, 8, "None", "_color", "Terrain Textures ,0/1.0", Default4( 1.00, 1.00, 1.00, 1.00 ) );

	Texture2D g_Texture_Zero < Channel( RGBA, Box( TextureAtHight0 ), Srgb ); OutputFormat( DXT5 ); SrgbRead( True ); >;
	Texture2D g_Texture_One < Channel( RGBA, Box( TextureAtHight1 ), Srgb ); OutputFormat( DXT5 ); SrgbRead( True ); >;
	Texture2D g_tNoiseTexture < Channel( RGBA, Box( NoiseTexture ), Srgb ); OutputFormat( DXT5 ); SrgbRead( True ); >;
	Texture2D g_Texture_Two < Channel( RGBA, Box( TextureAtHight2 ), Srgb ); OutputFormat( DXT5 ); SrgbRead( True ); >;
	Texture2D g_Texture_Three < Channel( RGBA, Box( TextureAtHight3 ), Srgb ); OutputFormat( DXT5 ); SrgbRead( True ); >;
	Texture2D g_Texture_Four < Channel( RGBA, Box( TextureAtHight4 ), Srgb ); OutputFormat( DXT5 ); SrgbRead( True ); >;
	Texture2D g_Texture_Five < Channel( RGBA, Box( TextureAtHight5 ), Srgb ); OutputFormat( DXT5 ); SrgbRead( True ); >;
	Texture2D g_Texture_Six < Channel( RGBA, Box( TextureAtHight6 ), Srgb ); OutputFormat( DXT5 ); SrgbRead( True ); >;
	Texture2D g_Texture_Seven < Channel( RGBA, Box( TextureAtHight7 ), Srgb ); OutputFormat( DXT5 ); SrgbRead( True ); >;
	Texture2D g_Texture_Eight < Channel( RGBA, Box( TextureAtHight8 ), Srgb ); OutputFormat( DXT5 ); SrgbRead( True ); >;
	Texture2D g_Texture_Nine < Channel( RGBA, Box( TextureAtHight9 ), Srgb ); OutputFormat( DXT5 ); SrgbRead( True ); >;
	Texture2D g_Texture_Ten < Channel( RGBA, Box( TextureAtHight10 ), Srgb ); OutputFormat( DXT5 ); SrgbRead( True ); >;


	float2 TexCoordMulti < Default2(40, 40); UiType( Slider ); >;




    /*

    float HeightBlend( float h1, float h2, float c1, float c2, out float ctrlHeight )
    {
        float h1Prefilter = h1 * sign( c1 );
        float h2Prefilter = h2 * sign( c2 );
        float height1 = h1Prefilter + c1;
        float height2 = h2Prefilter + c2;
        float blendFactor = (clamp(((height1 - height2) / ( 1.0f - Terrain::Get().HeightBlendSharpness )), -1, 1) + 1) / 2;
        ctrlHeight = c1 + c2;
        return blendFactor;
    }

    void Terrain_Splat4( in float2 texUV, in float4 control, out float3 albedo, out float3 normal, out float roughness, out float ao, out float metal )
    {
        texUV /= 32;

        float3 albedos[4], normals[4];
        float heights[4], roughnesses[4], aos[4], metalness[4];
        for ( int i = 0; i < 4; i++ )
        {
            float4 bcr = Bindless::GetTexture2D( g_TerrainMaterials[ i ].bcr_texid ).Sample( g_sAnisotropic, texUV * g_TerrainMaterials[ i ].uvscale );
            float4 nho = Bindless::GetTexture2D( g_TerrainMaterials[ i ].nho_texid ).Sample( g_sAnisotropic, texUV * g_TerrainMaterials[ i ].uvscale );

            float3 normal = ComputeNormalFromRGTexture( nho.rg );
            normal.xz *= g_TerrainMaterials[ i ].normalstrength;
            normal = normalize( normal );

            albedos[i] = SrgbGammaToLinear( bcr.rgb );
            normals[i] = normal;
            roughnesses[i] = bcr.a;
            heights[i] = nho.b * g_TerrainMaterials[ i ].heightstrength;
            aos[i] = nho.a;
            metalness[i] = g_TerrainMaterials[ i ].metalness;
        }

        

        float ctrlHeight1, ctrlHeight2, ctrlHeight3;
        float blend01 = HeightBlend( heights[0], heights[1], control.r, control.g, ctrlHeight1 );
        float blend12 = HeightBlend( heights[1], heights[2], ctrlHeight1, control.b, ctrlHeight2 );
        float blend23 = HeightBlend( heights[2], heights[3], ctrlHeight2, control.a, ctrlHeight3 );

        if ( Terrain::Get().HeightBlending )
        {
            // Blend Textures based on calculated blend factors
            albedo = albedos[0] * blend01 + albedos[1] * (1 - blend01);
            albedo = albedo * blend12 + albedos[2] * (1 - blend12);
            albedo = albedo * blend23 + albedos[3] * (1 - blend23);

            normal = normals[0] * blend01 + normals[1] * (1 - blend01);
            normal = normal * blend12 + normals[2] * (1 - blend12);
            normal = normal * blend23 + normals[3] * (1 - blend23);        

            roughness = roughnesses[0] * blend01 + roughnesses[1] * (1 - blend01);
            roughness = roughness * blend12 + roughnesses[2] * (1 - blend12);
            roughness = roughness * blend23 + roughnesses[3] * (1 - blend23);          

            ao = aos[0] * blend01 + aos[1] * (1 - blend01);
            ao = ao * blend12 + aos[2] * (1 - blend12);
            ao = ao * blend23 + aos[3] * (1 - blend23);            

            metal = metalness[0] * blend01 + metalness[1] * (1 - blend01);
            metal = metal * blend12 + metalness[2] * (1 - blend12);
            metal = metal * blend23 + metalness[3] * (1 - blend23);            
        }
        else
        {
            albedo = albedos[0] * control.r + albedos[1] * control.g + albedos[2] * control.b + albedos[3] * control.a;
            normal = normals[0] * control.r + normals[1] * control.g + normals[2] * control.b + normals[3] * control.a; // additive?
            roughness = roughnesses[0] * control.r + roughnesses[1] * control.g + roughnesses[2] * control.b + roughnesses[3] * control.a;
            ao = aos[0] * control.r + aos[1] * control.g + aos[2] * control.b + aos[3] * control.a;
            metal = metalness[0] * control.r + metalness[1] * control.g + metalness[2] * control.b + metalness[3] * control.a;
        }

        

    }

	*/

	// 
	// Main
	//



	// Move to another file:

	//
	// Takes 4 samples
	// This is easy for now, an optimization would be to generate this once in a compute shader
	// Less texture sampling but higher memory requirements
	// This is between -1 and 1;
	//

	float3 TerrainNormal( Texture2D HeightMap, float2 uv, float maxheight, out float3 TangentU, out float3 TangentV )
	{

	    float2 texelSize = 1.0f / ( float2 )TextureDimensions2D( HeightMap, 0 );

		

		float l = abs( Tex2DLevelS( HeightMap, g_sSampler1, uv + texelSize * float2( -1, 0 ), 0 ).r );
		float r = abs( Tex2DLevelS( HeightMap, g_sSampler1, uv + texelSize * float2( 1, 0 ), 0 ).r );
		float t = abs( Tex2DLevelS( HeightMap, g_sSampler1, uv + texelSize * float2( 0, -1 ), 0 ).r );
	    float b = abs( Tex2DLevelS( HeightMap, g_sSampler1, uv + texelSize * float2( 0, 1 ), 0 ).r );

		// Compute dx using central differences
		float dX = l - r;

		// Compute dy using central differences
	    float dY = b - t;

		// Normal strength needs to take in account terrain dimensions rather than just texel scale
	    float normalStrength = maxheight / Terrain::Get(  ).Resolution;

		float3 normal = normalize( float3( dX, dY * -1, 1.0f / normalStrength ) );

		TangentU = normalize( cross( normal, float3( 0, -1, 0 ) ) );
	    TangentV = normalize( cross( normal, -TangentU ) );

		return normal;
	}


    

	float4 MainPs( PixelInput i ) : SV_Target0
	{

		Texture2D tHeightMap = Bindless::GetTexture2D( Terrain::Get().HeightMapTexture );
		float2 texSize = TextureDimensions2D( tHeightMap, 0 );


		float2 heightUv = ( i.WorldPosition.xy ) / ( ( texSize ) * (Terrain::Get().Resolution ) );
		heightUv = TileAndOffsetUv( heightUv, float2( 1, 1 ), float2( -0.006, 0.48 ) );
		float Height = Tex2DLevelS( tHeightMap, g_sSampler1, heightUv , 0 ).r * Terrain::Get().HeightScale;        

		float2 uv = (i.LocalPosition.xy / ( (texSize) * (Terrain::Get().Resolution ) ));


		float2 l_00 = TileAndOffsetUv( uv, float2( 1, 1 ), float2( 0, 0 ) );
        float2 l_0 = TileAndOffsetUv( uv, TexCoordMulti, float2( 0, 0 ) );
		float4 l_1 = Tex2DS( g_Texture_Zero, g_sSampler0, l_0 );
		float4 l_2 = float4( l_1.r, l_1.g, l_1.b, l_1.a );
		float4 l_3 = Tex2DS( g_Texture_One, g_sSampler0, l_0 );
		float4 l_4 = float4( l_3.r, l_3.g, l_3.b, l_3.a );
		float4 l_5 = Tex2DS( g_tNoiseTexture, g_sSampler0, l_00 ); //float4( 0, 0, saturate( ( Height - 0 ) / ( 600 - 0 ) ) * ( 1 - 0 ) + 0 , 0 );// NoiseTexture
		float l_6 = saturate( ( l_5.r - 0 ) / ( 0.1 - 0 ) ) * ( 1 - 0 ) + 0;
		float4 l_7 = lerp( l_2, l_4, l_6 );
		float4 l_8 = float4( 1, 1, 1, 0 );
		float l_9 = l_5.r >= 0 ? 1 : 0;
		float l_10 = 1 == l_9 ? 1 : 0;
		float l_11 = l_5.r <= 0.1 ? 1 : 0;
		float l_12 = 1 == l_11 ? 1 : 2;
		float l_13 = l_10 == l_12 ? 0 : 1;
		float4 l_14 = lerp( l_7, l_8, l_13 );
		float4 l_15 = Tex2DS( g_Texture_One, g_sSampler0, l_0 );
		float4 l_16 = float4( l_15.r, l_15.g, l_15.b, l_15.a );
		float4 l_17 = Tex2DS( g_Texture_Two, g_sSampler0, l_0 );
		float4 l_18 = float4( l_17.r, l_17.g, l_17.b, l_17.a );
		float l_19 = saturate( ( l_5.r - 0.1 ) / ( 0.2 - 0.1 ) ) * ( 1 - 0 ) + 0;
		float4 l_20 = lerp( l_16, l_18, l_19 );
		float4 l_21 = float4( 1, 1, 1, 0 );
		float l_22 = l_5.r >= 0.1 ? 1 : 0;
		float l_23 = 1 == l_22 ? 1 : 0;
		float l_24 = l_5.r <= 0.2 ? 1 : 0;
		float l_25 = 1 == l_24 ? 1 : 2;
		float l_26 = l_23 == l_25 ? 0 : 1;
		float4 l_27 = lerp( l_20, l_21, l_26 );
		float4 l_28 = lerp( l_14, l_27, l_13 );
		float4 l_29 = Tex2DS( g_Texture_Two, g_sSampler0, l_0 );
		float4 l_30 = float4( l_29.r, l_29.g, l_29.b, l_29.a );
		float4 l_31 = Tex2DS( g_Texture_Three, g_sSampler0, l_0 );
		float4 l_32 = float4( l_31.r, l_31.g, l_31.b, l_31.a );
		float l_33 = saturate( ( l_5.r - 0.2 ) / ( 0.3 - 0.2 ) ) * ( 1 - 0 ) + 0;
		float4 l_34 = lerp( l_30, l_32, l_33 );
		float4 l_35 = float4( 1, 1, 1, 0 );
		float l_36 = l_5.r >= 0.2 ? 1 : 0;
		float l_37 = 1 == l_36 ? 1 : 0;
		float l_38 = l_5.r <= 0.3 ? 1 : 0;
		float l_39 = 1 == l_38 ? 1 : 2;
		float l_40 = l_37 == l_39 ? 0 : 1;
		float4 l_41 = lerp( l_34, l_35, l_40 );
		float4 l_42 = Tex2DS( g_Texture_Three, g_sSampler0, l_0 );
		float4 l_43 = float4( l_42.r, l_42.g, l_42.b, l_42.a );
		float4 l_44 = Tex2DS( g_Texture_Four, g_sSampler0, l_0 );
		float4 l_45 = float4( l_44.r, l_44.g, l_44.b, l_44.a );
		float l_46 = saturate( ( l_5.r - 0.3 ) / ( 0.4 - 0.3 ) ) * ( 1 - 0 ) + 0;
		float4 l_47 = lerp( l_43, l_45, l_46 );
		float4 l_48 = float4( 1, 1, 1, 0 );
		float l_49 = l_5.r >= 0.3 ? 1 : 0;
		float l_50 = 1 == l_49 ? 1 : 0;
		float l_51 = l_5.r <= 0.4 ? 1 : 0;
		float l_52 = 1 == l_51 ? 1 : 2;
		float l_53 = l_50 == l_52 ? 0 : 1;
		float4 l_54 = lerp( l_47, l_48, l_53 );
		float4 l_55 = lerp( l_41, l_54, l_40 );
		float l_56 = l_13 == 0 ? 1 : 0;
		float l_57 = l_26 == 0 ? 2 : 0;
		float l_58 = l_56 == l_57 ? 1 : 0;
		float4 l_59 = lerp( l_28, l_55, l_58 );
		float4 l_60 = Tex2DS( g_Texture_Four, g_sSampler0, l_0 );
		float4 l_61 = float4( l_60.r, l_60.g, l_60.b, l_60.a );
		float4 l_62 = Tex2DS( g_Texture_Five, g_sSampler0, l_0 );
		float4 l_63 = float4( l_62.r, l_62.g, l_62.b, l_62.a );
		float l_64 = saturate( ( l_5.r - 0.4 ) / ( 0.5 - 0.4 ) ) * ( 1 - 0 ) + 0;
		float4 l_65 = lerp( l_61, l_63, l_64 );
		float4 l_66 = float4( 1, 1, 1, 0 );
		float l_67 = l_5.r >= 0.4 ? 1 : 0;
		float l_68 = 1 == l_67 ? 1 : 0;
		float l_69 = l_5.r <= 0.5 ? 1 : 0;
		float l_70 = 1 == l_69 ? 1 : 2;
		float l_71 = l_68 == l_70 ? 0 : 1;
		float4 l_72 = lerp( l_65, l_66, l_71 );
		float4 l_73 = Tex2DS( g_Texture_Five, g_sSampler0, l_0 );
		float4 l_74 = float4( l_73.r, l_73.g, l_73.b, l_73.a );
		float4 l_75 = Tex2DS( g_Texture_Six, g_sSampler0, l_0 );
		float4 l_76 = float4( l_75.r, l_75.g, l_75.b, l_75.a );
		float l_77 = saturate( ( l_5.r - 0.5 ) / ( 0.6 - 0.5 ) ) * ( 1 - 0 ) + 0;
		float4 l_78 = lerp( l_74, l_76, l_77 );
		float4 l_79 = float4( 1, 1, 1, 0 );
		float l_80 = l_5.r >= 0.5 ? 1 : 0;
		float l_81 = 1 == l_80 ? 1 : 0;
		float l_82 = l_5.r <= 0.6 ? 1 : 0;
		float l_83 = 1 == l_82 ? 1 : 2;
		float l_84 = l_81 == l_83 ? 0 : 1;
		float4 l_85 = lerp( l_78, l_79, l_84 );
		float4 l_86 = lerp( l_72, l_85, l_71 );
		float4 l_87 = Tex2DS( g_Texture_Six, g_sSampler0, l_0 );
		float4 l_88 = float4( l_87.r, l_87.g, l_87.b, l_87.a );
		float4 l_89 = Tex2DS( g_Texture_Seven, g_sSampler0, l_0 );
		float4 l_90 = float4( l_89.r, l_89.g, l_89.b, l_89.a );
		float l_91 = saturate( ( l_5.r - 0.6 ) / ( 0.7 - 0.6 ) ) * ( 1 - 0 ) + 0;
		float4 l_92 = lerp( l_88, l_90, l_91 );
		float4 l_93 = float4( 1, 1, 1, 0 );
		float l_94 = l_5.r >= 0.6 ? 1 : 0;
		float l_95 = 1 == l_94 ? 1 : 0;
		float l_96 = l_5.r <= 0.7 ? 1 : 0;
		float l_97 = 1 == l_96 ? 1 : 2;
		float l_98 = l_95 == l_97 ? 0 : 1;
		float4 l_99 = lerp( l_92, l_93, l_98 );
		float4 l_100 = Tex2DS( g_Texture_Seven, g_sSampler0, l_0 );
		float4 l_101 = float4( l_100.r, l_100.g, l_100.b, l_100.a );
		float4 l_102 = Tex2DS( g_Texture_Eight, g_sSampler0, l_0 );
		float4 l_103 = float4( l_102.r, l_102.g, l_102.b, l_102.a );
		float l_104 = saturate( ( l_5.r - 0.7 ) / ( 0.8 - 0.7 ) ) * ( 1 - 0 ) + 0;
		float4 l_105 = lerp( l_101, l_103, l_104 );
		float4 l_106 = float4( 1, 1, 1, 0 );
		float l_107 = l_5.r >= 0.7 ? 1 : 0;
		float l_108 = 1 == l_107 ? 1 : 0;
		float l_109 = l_5.r <= 0.8 ? 1 : 0;
		float l_110 = 1 == l_109 ? 1 : 2;
		float l_111 = l_108 == l_110 ? 0 : 1;
		float4 l_112 = lerp( l_105, l_106, l_111 );
		float4 l_113 = lerp( l_99, l_112, l_98 );
		float l_114 = l_71 == 0 ? 1 : 0;
		float l_115 = l_84 == 0 ? 2 : 0;
		float l_116 = l_114 == l_115 ? 1 : 0;
		float4 l_117 = lerp( l_86, l_113, l_116 );
		float l_118 = l_58 == 0 ? 1 : 0;
		float l_119 = l_40 == 0 ? 1 : 0;
		float l_120 = l_53 == 0 ? 2 : 0;
		float l_121 = l_119 == l_120 ? 1 : 0;
		float l_122 = l_121 == 0 ? 2 : 0;
		float l_123 = l_118 == l_122 ? 1 : 0;
		float4 l_124 = lerp( l_59, l_117, l_123 );
		float4 l_125 = Tex2DS( g_Texture_Eight, g_sSampler0, l_0 );
		float4 l_126 = float4( l_125.r, l_125.g, l_125.b, l_125.a );
		float4 l_127 = Tex2DS( g_Texture_Nine, g_sSampler0, l_0 );
		float4 l_128 = float4( l_127.r, l_127.g, l_127.b, l_127.a );
		float l_129 = saturate( ( l_5.r - 0.8 ) / ( 0.9 - 0.8 ) ) * ( 1 - 0 ) + 0;
		float4 l_130 = lerp( l_126, l_128, l_129 );
		float4 l_131 = float4( 1, 1, 1, 0 );
		float l_132 = l_5.r >= 0.8 ? 1 : 0;
		float l_133 = 1 == l_132 ? 1 : 0;
		float l_134 = l_5.r <= 0.9 ? 1 : 0;
		float l_135 = 1 == l_134 ? 1 : 2;
		float l_136 = l_133 == l_135 ? 0 : 1;
		float4 l_137 = lerp( l_130, l_131, l_136 );
		float4 l_138 = Tex2DS( g_Texture_Nine, g_sSampler0, l_0 );
		float4 l_139 = float4( l_138.r, l_138.g, l_138.b, l_138.a );
		float4 l_140 = Tex2DS( g_Texture_Ten, g_sSampler0, l_0 );
		float4 l_141 = float4( l_140.r, l_140.g, l_140.b, l_140.a );
		float l_142 = saturate( ( l_5.r - 0.9 ) / ( 1 - 0.9 ) ) * ( 1 - 0 ) + 0;
		float4 l_143 = lerp( l_139, l_141, l_142 );
		float4 l_144 = float4( 1, 1, 1, 0 );
		float l_145 = l_5.r >= 0.9 ? 1 : 0;
		float l_146 = 1 == l_145 ? 1 : 0;
		float l_147 = l_5.r <= 1 ? 1 : 0;
		float l_148 = 1 == l_147 ? 1 : 2;
		float l_149 = l_146 == l_148 ? 0 : 1;
		float4 l_150 = lerp( l_143, l_144, l_149 );
		float4 l_151 = lerp( l_137, l_150, l_136 );
		float l_152 = l_123 == 0 ? 1 : 0;
		float l_153 = l_116 == 0 ? 1 : 0;
		float l_154 = l_98 == 0 ? 1 : 0;
		float l_155 = l_111 == 0 ? 2 : 0;
		float l_156 = l_154 == l_155 ? 1 : 0;
		float l_157 = l_156 == 0 ? 2 : 0;
		float l_158 = l_153 == l_157 ? 1 : 0;
		float l_159 = l_158 == 0 ? 2 : 0;
		float l_160 = l_152 == l_159 ? 1 : 0;
		float4 l_161 = lerp( l_124, l_151, l_160 );


        
        

        Texture2D tControlMap = Bindless::GetTexture2D( Terrain::Get().ControlMapTexture );

        

        // Clip any of the clipmap that exceeds the heightmap bounds


        if ( uv.x < 0.0 || uv.y < 0.0 || uv.x > 1.0 || uv.y > 1.0 )
        {
            clip( -1 );
            return float4( 0, 0, 0, 0 );
        }

        // calculate geometric normal

		

        float3 tangentU;
		float3 tangentV;
		




        float3 geoNormal = TerrainNormal( tHeightMap, uv, Terrain::Get().HeightScale, tangentU, tangentV );
        

		geoNormal = mul( Terrain::Get().Transform, float4( geoNormal, 0.0 ) ).xyz;

		


        float3 albedo = l_161.xyz;
        float3 norm = float3( 0, 0, 1 );
        float roughness = 1;
        float ao = 1;
        float metalness = 0;

		/*

    #if D_GRID
        Terrain_ProcGrid( i.LocalPosition.xy, albedo, roughness );
    #else
        // Not adding up to 1 is invalid, but lets just give everything to the first layer
        float4 control = Tex2DS( Terrain::GetControlMap(), g_sBilinearBorder, uv );
        float sum = control.x + control.y + control.z + control.w;

        #if D_AUTO_SPLAT
        if ( sum != 1.0f )
        {
            float invsum = 1.0f - sum;
            float slope_weight = saturate( ( geoNormal.z - 0.99 ) * 100 );
            control.x += ( slope_weight ) * invsum;
            control.y += ( 1.0f - slope_weight ) * invsum;
        }
        #else
        // anything unsplatted, defualt to channel 0
        if ( sum != 1.0f ) { control.x += 1.0f - sum; }
        #endif

        Terrain_Splat4( i.LocalPosition.xy, control, albedo, norm, roughness, ao, metalness );
    #endif

	*/
    

        Material p = Material::Init();
        p.Albedo = albedo;

        
        p.Normal = TransformNormal( norm, geoNormal, tangentU, tangentV );
        p.Roughness = roughness;
        p.Metalness = metalness;
        p.AmbientOcclusion = ao;
        p.TextureCoords = uv;

        p.WorldPosition = i.WorldPosition;
        p.WorldPositionWithOffset = i.WorldPosition - g_vHighPrecisionLightingOffsetWs.xyz;
        p.ScreenPosition = i.ScreenPosition;
        p.GeometricNormal = geoNormal;

        p.WorldTangentU = tangentU;
        p.WorldTangentV = tangentV;

        if ( g_nDebugView != 0 )
        {
            // return Terrain_Debug( i.LodLevel, p.TextureCoords );
        }
        
	    return ShadingModelStandard::Shade( p );


        

	}


    
}
