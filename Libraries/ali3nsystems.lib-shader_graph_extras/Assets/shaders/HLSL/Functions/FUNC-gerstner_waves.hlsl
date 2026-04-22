float3 SGEGerstnerWavesLinearNormal(float3 coordinates, float time, float2 direction, float amplitude, float length, float gravity, float steepness)
{
    float2 normalizedDirection = normalize(direction + float2(1E-05, 0));
    float waveNumber = 6.2831855 / length;
    float2 waveDirection = normalizedDirection * waveNumber;
    float phase = dot(coordinates, float3(waveDirection, 0)) - sqrt(waveNumber * gravity) * time;
    float theta = phase * 6.2831855;
    float waveAmplitude = waveNumber * amplitude;
    float2 tangent = (sin(theta) * waveAmplitude) * normalizedDirection;
    float steep = saturate(steepness) * 0.16 / waveAmplitude;
    float normal = 1.0 - cos(theta) * waveAmplitude * steep;
    return float3(tangent, normal);
}

float3 SGEGerstnerWavesLinearDisplacement(float3 coordinates, float time, float2 direction, float amplitude, float length, float gravity, float steepness)
{
    float2 normalizedDirection = normalize(direction + float2(1E-05, 0));
    float waveNumber = 6.2831855 / length;
    float2 waveDirection = normalizedDirection * waveNumber;
    float phase = dot(coordinates, float3(waveDirection, 0)) - sqrt(waveNumber * gravity) * time;
    float theta = phase * 6.2831855;
    float3 vertical = float3(0, 0, cos(theta) * amplitude);
    float steep = saturate(steepness) * 0.16 / (waveNumber * amplitude) * amplitude;
    float2 horizontal = normalizedDirection * (sin(theta) * steep);
    return vertical - float3(horizontal, 0);
}

float3 SGEGerstnerWavesCircularNormal(float3 coordinates, float time, float3 center, float amplitude, float length, float gravity, float steepness)
{
    float3 offset = coordinates - center + float3(0, 0, 1E-05);
    float3 normalizedDirection = normalize(offset);
    float waveNumber = 6.2831855 / length;
    float3 waveDirection = normalizedDirection * waveNumber;
    float phase = dot(offset, waveDirection) - sqrt(waveNumber * gravity) * time;
    float theta = phase * 6.2831855;
    float waveAmplitude = waveNumber * amplitude;
    float3 tangentOffset = sin(theta) * waveAmplitude * normalizedDirection;
    float2 tangent = tangentOffset.xy;
    float fade = smoothstep(0, length / 12.8, sqrt(dot(offset, offset)));
    float steep = lerp(0, saturate(steepness), fade) * 0.16 / waveAmplitude;
    float normal = 1.0 - cos(theta) * waveAmplitude * steep;
    return float3(tangent, normal);
}

float3 SGEGerstnerWavesCircularDisplacement(float3 coordinates, float time, float3 center, float amplitude, float length, float gravity, float steepness)
{
    float3 offset = coordinates - center + float3(0, 0, 1E-05);
    float3 normalizedDirection = normalize(offset);
    float waveNumber = 6.2831855 / length;
    float3 waveDirection = normalizedDirection * waveNumber;
    float phase = dot(offset, waveDirection) - sqrt(waveNumber * gravity) * time;
    float theta = phase * 6.2831855;
    float3 vertical = float3(0, 0, cos(theta) * amplitude);
    float fade = smoothstep(0, length / 12.8, sqrt(dot(offset, offset)));
    float steep = lerp(0, saturate(steepness), fade) * 0.16 / (waveNumber * amplitude) * amplitude;
    float3 horizontal = normalizedDirection * (sin(theta) * steep);
    return vertical - horizontal;
}