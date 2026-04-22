float3 SGEHeightToNormal( float input, float strength, float3 worldSpacePosition, float3 worldSpaceNormal )
{
    float3 worldSpacePositionDerivativeX = ddx(worldSpacePosition);
    float3 worldSpacePositionDerivativeY = ddy(worldSpacePosition);

    float3 crossX = cross(worldSpaceNormal, worldSpacePositionDerivativeX);
    float3 crossY = cross(worldSpacePositionDerivativeY, worldSpaceNormal);
    float d = dot(worldSpacePositionDerivativeX, crossY);
    float s = d < 0.0 ? (-1.f) : 1.f;
    float surface = s / max(0.00000000000001192093f, abs(d));

    float inputDerivativeX = ddx(input);
    float inputDerivativeY = ddy(input);
    float3 surfaceGradient = surface * (inputDerivativeX * crossY + inputDerivativeY * crossX);
    return normalize(worldSpaceNormal - (strength * surfaceGradient));
}
