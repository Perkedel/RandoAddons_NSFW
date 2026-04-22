float SGEStaticNoise2D(float2 coordinates, float2 seed = float2(12.9898f, 78.233f))
{
    return frac(sin(dot(coordinates, seed))*43758.5453f);
}

float SGEStaticNoise3D(float3 coordinates, float3 seed = float3(12.9898f, 78.233f, 37.719f))
{
    return frac(sin(dot(coordinates, seed)) * 43758.5453f);
}

float SGEValueNoise2D(float2 coordinates)
{
    int2 cell = int2(floor(coordinates));
    float2 fraction = frac(coordinates);
    fraction = fraction * fraction * (3.0f - 2.0f * fraction);

    // Get UVs for each corner of the square
    int2 corner00 = cell + int2(0, 0);
    int2 corner10 = cell + int2(1, 0);
    int2 corner01 = cell + int2(0, 1);
    int2 corner11 = cell + int2(1, 1);

    // Sample each corner
    float noise00 = SGEStaticNoise2D(corner00);
    float noise10 = SGEStaticNoise2D(corner10);
    float noise01 = SGEStaticNoise2D(corner01);
    float noise11 = SGEStaticNoise2D(corner11);

    // Bilinear interpolation
    float noiseX0 = lerp(noise00, noise10, fraction.x);
    float noiseX1 = lerp(noise01, noise11, fraction.x);

    return lerp(noiseX0, noiseX1, fraction.y);
}

float SGEValueNoise3D(float3 coordinates)
{
    // Get our current cell
    int3 cell = int3(floor(coordinates));
    float3 fraction = frac(coordinates);
    fraction = fraction * fraction * (3.0f - 2.0f * fraction);

    // Get UVs for each corner of the cube
    int3 corner000 = cell + int3(0, 0, 0);
    int3 corner100 = cell + int3(1, 0, 0);
    int3 corner010 = cell + int3(0, 1, 0);
    int3 corner110 = cell + int3(1, 1, 0);
    int3 corner001 = cell + int3(0, 0, 1);
    int3 corner101 = cell + int3(1, 0, 1);
    int3 corner011 = cell + int3(0, 1, 1);
    int3 corner111 = cell + int3(1, 1, 1);

    // Sample each corner
    float noise000 = SGEStaticNoise3D(corner000);
    float noise100 = SGEStaticNoise3D(corner100);
    float noise010 = SGEStaticNoise3D(corner010);
    float noise110 = SGEStaticNoise3D(corner110);
    float noise001 = SGEStaticNoise3D(corner001);
    float noise101 = SGEStaticNoise3D(corner101);
    float noise011 = SGEStaticNoise3D(corner011);
    float noise111 = SGEStaticNoise3D(corner111);

    // Trilinear interpolation
    float noiseX00 = lerp(noise000, noise100, fraction.x);
    float noiseX10 = lerp(noise010, noise110, fraction.x);
    float noiseX01 = lerp(noise001, noise101, fraction.x);
    float noiseX11 = lerp(noise011, noise111, fraction.x);

    float noiseXY0 = lerp(noiseX00, noiseX10, fraction.y);
    float noiseXY1 = lerp(noiseX01, noiseX11, fraction.y);

    return lerp(noiseXY0, noiseXY1, fraction.z);
}

float4 mod289(float4 x)
{
    return x - floor(x / 289.0) * 289.0;
}

float4 permute(float4 x)
{
    return mod289((x * 34.0 + 1.0) * x);
}

// ===== Simplex Noise 2D =====
// Based on: Ashima Arts / Keijiro Takahashi implementation

float3 SGESimplexNoise2D(float2 coordinates)
{
    const float skewFactor = (sqrt(3.0) - 1.0) / 2.0;
    const float unskewFactor = (3.0 - sqrt(3.0)) / 6.0;

    // First corner
    float2 cell = floor(coordinates + dot(coordinates, skewFactor));
    float2 corner0 = coordinates - cell + dot(cell, unskewFactor);

    // Other corners
    float2 cell1 = corner0.x > corner0.y ? float2(1.0, 0.0) : float2(0.0, 1.0);
    float2 corner1 = corner0 + unskewFactor - cell1;
    float2 corner2 = corner0 + unskewFactor * 2.0 - 1.0;

    // Permutations - avoid truncation effects
    cell = mod289(float4(cell, 0.0, 0.0)).xy;
    float3 permutation = permute(permute(cell.y + float4(0.0, cell1.y, 1.0, 0.0)) + cell.x + float4(0.0, cell1.x, 1.0, 0.0)).xyz;

    // Gradients: 41 points uniformly over a unit circle
    // The ring size 17*17 = 289 is close to a multiple of 41 (41*7 = 287)
    float3 phi = permutation / 41.0 * 3.14159265359 * 2.0;
    float2 gradient0 = float2(cos(phi.x), sin(phi.x));
    float2 gradient1 = float2(cos(phi.y), sin(phi.y));
    float2 gradient2 = float2(cos(phi.z), sin(phi.z));

    // Compute noise and gradient at P
    float3 falloff = float3(dot(corner0, corner0), dot(corner1, corner1), dot(corner2, corner2));
    float3 projection = float3(dot(gradient0, corner0), dot(gradient1, corner1), dot(gradient2, corner2));

    falloff = max(0.5 - falloff, 0.0);
    float3 falloff3 = falloff * falloff * falloff;
    float3 falloff4 = falloff * falloff3;

    float3 gradientWeight = -8.0 * falloff3 * projection;
    float2 gradient = falloff4.x * gradient0 + gradientWeight.x * corner0 +
                      falloff4.y * gradient1 + gradientWeight.y * corner1 +
                      falloff4.z * gradient2 + gradientWeight.z * corner2;

    return 99.2 * float3(gradient, dot(falloff4, projection));
}

// ===== Simplex Noise 3D =====
// Based on: Ashima Arts / Keijiro Takahashi implementation

float4 SGESimplexNoise3D(float3 coordinates)
{
    const float skewFactor = 1.0 / 3.0;
    const float unskewFactor = 1.0 / 6.0;

    // First corner
    float3 cell = floor(coordinates + dot(coordinates, skewFactor));
    float3 corner0 = coordinates - cell + dot(cell, unskewFactor);

    // Other corners
    float3 greater = step(corner0.yzx, corner0.xyz);
    float3 lesser = 1.0 - greater;
    float3 cell1 = min(greater.xyz, lesser.zxy);
    float3 cell2 = max(greater.xyz, lesser.zxy);

    float3 corner1 = corner0 - cell1 + unskewFactor;
    float3 corner2 = corner0 - cell2 + unskewFactor * 2.0;
    float3 corner3 = corner0 - 0.5;

    // Permutations - avoid truncation effects
    cell = mod289(float4(cell, 0.0)).xyz;
    float4 permutation = permute(permute(permute(cell.z + float4(0.0, cell1.z, cell2.z, 1.0))
        + cell.y + float4(0.0, cell1.y, cell2.y, 1.0))
        + cell.x + float4(0.0, cell1.x, cell2.x, 1.0));

    // Gradients: 7x7 points over a square, mapped onto an octahedron
    // The ring size 17*17 = 289 is close to a multiple of 49 (49*6 = 294)
    float4 gradientX = lerp(-1.0, 1.0, frac(permutation / 7.0));
    float4 gradientY = lerp(-1.0, 1.0, frac(floor(permutation / 7.0) / 7.0));
    float4 gradientZ = 1.0 - abs(gradientX) - abs(gradientY);

    bool4 zNegative = gradientZ < 0.0;
    gradientX += (float4)zNegative * select(-1.0, 1.0, gradientX < 0.0);
    gradientY += (float4)zNegative * select(-1.0, 1.0, gradientY < 0.0);

    float3 gradient0 = normalize(float3(gradientX.x, gradientY.x, gradientZ.x));
    float3 gradient1 = normalize(float3(gradientX.y, gradientY.y, gradientZ.y));
    float3 gradient2 = normalize(float3(gradientX.z, gradientY.z, gradientZ.z));
    float3 gradient3 = normalize(float3(gradientX.w, gradientY.w, gradientZ.w));

    float4 falloff = float4(dot(corner0, corner0), dot(corner1, corner1), dot(corner2, corner2), dot(corner3, corner3));
    float4 projection = float4(dot(gradient0, corner0), dot(gradient1, corner1), dot(gradient2, corner2), dot(gradient3, corner3));

    falloff = max(0.5 - falloff, 0.0);
    float4 falloff3 = falloff * falloff * falloff;
    float4 falloff4 = falloff * falloff3;

    float4 gradientWeight = -8.0 * falloff3 * projection;
    float3 gradient = falloff4.x * gradient0 + gradientWeight.x * corner0 +
                      falloff4.y * gradient1 + gradientWeight.y * corner1 +
                      falloff4.z * gradient2 + gradientWeight.z * corner2 +
                      falloff4.w * gradient3 + gradientWeight.w * corner3;

    return 107.0 * float4(gradient, dot(falloff4, projection));
}

float SGEfBMNoise2D(float2 coordinates, int octaves)
{
    float value = 0.0;
    float amplitude = 0.5;
    float frequency = 1.0;

    [unroll]
    for(int i = 0; i < octaves; i++)
    {
        value += amplitude * SGEValueNoise2D(frequency * coordinates);
        amplitude *= 0.5;
        frequency *= 2.0;
    }
    return value;
}

float SGEfBMNoise3D(float3 coordinates, int octaves)
{
    float value = 0.0;
    float amplitude = 0.5;
    float frequency = 1.0;

    [unroll]
    for (int i = 0; i < octaves; i++)
    {
        value += amplitude * SGEValueNoise3D(frequency * coordinates);
        amplitude *= 0.5;
        frequency *= 2.0;
    }
    return value;
}

float2 SGEOffsetStaticNoise2D(float2 coordinates, float offset)
{
    float2 noise = float2(
        SGEStaticNoise2D(coordinates, float2(12.9898f, 78.233f)),
        SGEStaticNoise2D(coordinates, float2(78.233f, 12.9898f))
    );
    return float2(
        sin(noise.y * offset) * 0.5 + 0.5,
        cos(noise.x * offset) * 0.5 + 0.5
    );
}

float3 SGEOffsetStaticNoise3D(float3 coordinates, float offset)
{
    float3 noise = float3(
        SGEStaticNoise3D(coordinates, float3(12.9898f, 78.233f, 37.719f)),
        SGEStaticNoise3D(coordinates, float3(78.233f, 37.719f, 12.9898f)),
        SGEStaticNoise3D(coordinates, float3(37.719f, 12.9898f, 78.233f))
    );
    return float3(
        sin(noise.x * offset) * 0.5 + 0.5,
        sin(noise.y * offset) * 0.5 + 0.5,
        sin(noise.z * offset) * 0.5 + 0.5
    );
}

float SGEVoronoiNoise2D(float2 coordinates, float offset)
{
    float2 cell = floor(coordinates);
    float2 cellFraction = frac(coordinates);

    float minimumDistance = 4;

    [unroll]
    for(int y = -1; y <= 1; y++)
    {
        [unroll]
        for(int x = -1; x <= 1; x++)
        {
            float2 neighborCell = float2(x, y);
            float2 randomPointInCell = SGEOffsetStaticNoise2D(neighborCell + cell, offset);
            float2 absolutePosition = neighborCell + randomPointInCell;
            float distanceToPoint = distance(absolutePosition, cellFraction);

            minimumDistance = min(minimumDistance, distanceToPoint);
        }
    }
    return minimumDistance;
}

float SGEVoronoiNoise3D(float3 coordinates, float offset)
{
    float3 cell = floor(coordinates);
    float3 cellFraction = frac(coordinates);

    float minimumDistance = 4;

    [unroll]
    for(int z = -1; z <= 1; z++)
    {
        [unroll]
        for(int y = -1; y <= 1; y++)
        {
            [unroll]
            for(int x = -1; x <= 1; x++)
            {
                float3 neighborCell = float3(x, y, z);
                float3 randomPointInCell = SGEOffsetStaticNoise3D(neighborCell + cell, offset);
                float3 absolutePosition = neighborCell + randomPointInCell;
                float distanceToPoint = distance(absolutePosition, cellFraction);

                minimumDistance = min(minimumDistance, distanceToPoint);
            }
        }
    }
    return minimumDistance;
}