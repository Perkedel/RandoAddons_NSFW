float2 SGEBumpOffsetIterative(float2 coordinates, Texture2D texture, SamplerState sampler, float3 tangentSpaceViewDirection, float amplitude, float minimumIterations,float maximumIterations, float levelOfDetail, float offset, int channel)
{
        float layerNumbers = lerp(minimumIterations, maximumIterations, levelOfDetail);

        float ratioPerPass = (amplitude / max(ceil(layerNumbers), 1));
        offset = offset - 1;

        float2 currentCoordinates = coordinates;

        [loop]
        for (int i = 0; i < ceil(layerNumbers); i++)
        {
            float currentDepthValue = texture.Sample(sampler, currentCoordinates)[channel];
            currentCoordinates += ((currentDepthValue + offset) * ratioPerPass * (tangentSpaceViewDirection.xy / tangentSpaceViewDirection.z));
        }

        return lerp(coordinates, currentCoordinates, saturate(layerNumbers));
}

float2 SGEBumpOffsetStandard(float2 coordinates, Texture2D texture, SamplerState sampler, float3 tangentSpaceViewDirection, float amplitude, float levelOfDetail, float offset, int channel)
{
    float currentDepthValue = texture.Sample(sampler, coordinates)[channel];
    float layerNumbers = lerp(1, currentDepthValue,levelOfDetail);
    return coordinates + tangentSpaceViewDirection.xy * (-offset + 1 - layerNumbers) * amplitude;
}