#ifndef SHADING_MODEL_TOON_H
#define SHADING_MODEL_TOON_H

#include "common/material.hlsl"
#include "common/GBuffer.hlsl"
#include "common/classes/Decals.hlsl"
#include "common/classes/Light.hlsl"

//-----------------------------------------------------------------------------
// Toon/Cel Shading Model - Simple anime-style two-tone shading
//-----------------------------------------------------------------------------

// Vibe coded since I couldn't be bothered, it's only meant to be an example. -eEight
class ShadingModelToon
{
    static float4 Shade( Material m )
    {
        Decals::Apply( m.WorldPosition, m );

        #if ( S_ALPHA_TEST )
        {
            float eps = 1.0f / 255.0f;
            clip( m.Opacity - eps );
            m.Opacity = AdjustOpacityForAlphaToCoverage( m.Opacity, g_flAlphaTestReference, g_flAntiAliasedEdgeStrength, m.TextureCoords.xy );
            if ( g_nMSAASampleCount == 1 )
                OpaqueFadeDepth( ( m.Opacity + 0.5f + eps ) * 0.5f, m.ScreenPosition.xy );
            else
                clip( m.Opacity - 0.000001 );
        }
        #endif

        // Start with shadow color as base
        float3 diffuse = m.Albedo * 0.5;

        // Process all lights with cel shading
        uint nLightCount = Light::Count( m.WorldPosition );
        for ( uint i = 0; i < nLightCount; i++ )
        {
            Light light = Light::From( m.WorldPosition, i, m.LightmapUV );

            float NdotL = dot( m.Normal, light.Direction );

            // Simple two-tone cel shading: lit or shadow
            float cel = smoothstep( -0.01, 0.01, NdotL );

            // Add light contribution with cel shading
            diffuse += cel * light.Color * light.Attenuation * light.Visibility * m.Albedo * 0.5;
        }

        // Add emission
        diffuse += m.Emission;

        // Apply ambient occlusion
        diffuse *= m.AmbientOcclusion;

        float4 color = float4( diffuse, m.Opacity );

        if ( DepthNormals::WantsDepthNormals() )
            return DepthNormals::Output( m.Normal, m.Roughness, color.a );

        if ( ToolsVis::WantsToolsVis() )
        {
            ToolsVis toolVis = ToolsVis::Init( color, diffuse, float3(0,0,0), float3(0,0,0), float3(0,0,0), float3(0,0,0) );
            toolVis.HandleFullbright( color, m.Albedo, m.WorldPosition, m.Normal );
            toolVis.HandleAlbedo( color, m.Albedo );
            toolVis.HandleNormalWs( color, m.Normal );
            return color;
        }

        if ( g_bWireframeMode )
            return g_vWireframeColor;

        color.rgb = Fog::Apply( m.WorldPosition, m.ScreenPosition.xy, color.rgb );

        return color;
    }
};

#endif // SHADING_MODEL_TOON_H
