float2 SGEParallaxOcclusionMappingStandard(float2 coordinates, Texture2D texture, SamplerState sampler, float3 tangentSpaceViewDirection, float amplitude, float minimumIterations,float maximumIterations, float levelOfDetail, float offset, int channel)
{
    float layerNumbers = lerp(minimumIterations, maximumIterations, levelOfDetail);
    float layerDepth = 1 / layerNumbers;
    float currentLayerDepth = 0;

    float2 S = tangentSpaceViewDirection.xy / tangentSpaceViewDirection.z * amplitude;
    float2 deltaCoordinates = S / layerNumbers;

    float2 currentCoordinates = coordinates + offset * S;

    float currentDepthValue = 1 - Tex2DS(texture, sampler, currentCoordinates)[channel];

    while (currentLayerDepth < currentDepthValue)
    {
        currentCoordinates -= deltaCoordinates;
        currentDepthValue = 1 - Tex2DS(texture, sampler, currentCoordinates)[channel];
        currentLayerDepth += layerDepth;
    }

    float2 previousCoordinates = currentCoordinates + deltaCoordinates;
    float afterDepth = currentDepthValue - currentLayerDepth;
    float beforeDepth = 1 - Tex2DS(texture, sampler, previousCoordinates)[channel] - currentLayerDepth + layerDepth;
    float weight = afterDepth / (afterDepth - beforeDepth);
    currentCoordinates = previousCoordinates * weight + currentCoordinates * (1 - weight);

    return currentCoordinates;
}

float2 SGEParallaxOcclusionMappingSteep(float2 coordinates, Texture2D texture, SamplerState sampler, float3 tangentSpaceViewDirection, float amplitude, float minimumIterations,float maximumIterations, float levelOfDetail, float offset, int channel)
    {
        float layerNumbers = lerp(minimumIterations, maximumIterations, levelOfDetail);
        float layerDepth = 1 / layerNumbers;
        float currentLayerDepth = 0;

        float2 S = tangentSpaceViewDirection.xy / tangentSpaceViewDirection.z * amplitude;
        float2 deltaCoordinates = S / layerNumbers;

        float2 currentCoordinates = coordinates + offset * S;

        float currentDepthValue = 1 - Tex2DS(texture, sampler, currentCoordinates)[channel];

        while (currentLayerDepth < currentDepthValue)
        {
            currentCoordinates -= deltaCoordinates;
            currentDepthValue = 1 - Tex2DS(texture, sampler, currentCoordinates)[channel];
            currentLayerDepth += layerDepth;
        }
        return currentCoordinates;
    }