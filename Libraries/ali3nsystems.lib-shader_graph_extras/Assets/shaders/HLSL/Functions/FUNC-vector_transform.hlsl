//Position

//Tangent
float3 SGETangentToTangentPosition(float3 input)
{
    return input;
}

float3 SGETangentToObjectPosition( float3 input, float3 objectSpaceNormal, float3 objectSpaceTangentU, float3 objectSpaceTangentV )
{
    float3 output;
    output.x = dot( input.xyz, float3(objectSpaceTangentU.x, objectSpaceTangentV.x, objectSpaceNormal.x) );
    output.y = dot( input.xyz, float3(objectSpaceTangentU.y, objectSpaceTangentV.y, objectSpaceNormal.y) );
    output.z = dot( input.xyz, float3(objectSpaceTangentU.z, objectSpaceTangentV.z, objectSpaceNormal.z) );
    return output.xyz;
}

float3 SGETangentToWorldPosition ( float3 input, float3 worldSpaceNormal, float3 worldSpaceTangentU, float3 worldSpaceTangentV )
{
	float3 output;
	output.xyz = input.x * worldSpaceTangentU.xyz;
	output.xyz += input.y * worldSpaceTangentV.xyz;
	output.xyz += input.z * worldSpaceNormal.xyz;
	return output.xyz;
}

//Object
float3 SGEObjectToTangentPosition( float3 input, float3 objectSpaceNormal, float3 objectSpaceTangentU, float3 objectSpaceTangentV )
{
    float3 output;
    output.x = dot( input.xyz, objectSpaceTangentU.xyz );
    output.y = dot( input.xyz, objectSpaceTangentV.xyz );
    output.z = dot( input.xyz, objectSpaceNormal.xyz );
    return output.xyz;
}

float3 SGEObjectToObjectPosition(float3 input)
{
    return input;
}

float3 SGEObjectToWorldPosition( float3 input, float3 objectSpacePosition, float3 worldSpacePosition )
{
    return worldSpacePosition + input - objectSpacePosition;
}

//World
float3 SGEWorldToTangentPosition( float3 input, float3 worldSpaceNormal, float3 worldSpaceTangentU, float3 worldSpaceTangentV )
{
	float3 output;
	output.x = dot( input.xyz, worldSpaceTangentU.xyz );
	output.y = dot( input.xyz, worldSpaceTangentV.xyz );
	output.z = dot( input.xyz, worldSpaceNormal.xyz );
	return output.xyz;
}

float3 SGEWorldToObjectPosition( float3 input, float3 worldSpacePosition, float3 objectSpacePosition )
{
    return objectSpacePosition + input - worldSpacePosition;
}

float3 SGEWorldToWorldPosition(float3 input)
{
    return input;
}

//Normal

//Tangent
float3 SGETangentToTangentNormal(float3 input)
{
    return input;
}

float3 SGETangentToObjectNormal( float3 input, float3 objectSpaceNormal, float3 objectSpaceTangentU, float3 objectSpaceTangentV )
{
    float3 output;
    output.x = dot( input.xyz, float3(objectSpaceTangentU.x, objectSpaceTangentV.x, objectSpaceNormal.x) );
    output.y = dot( input.xyz, float3(objectSpaceTangentU.y, objectSpaceTangentV.y, objectSpaceNormal.y) );
    output.z = dot( input.xyz, float3(objectSpaceTangentU.z, objectSpaceTangentV.z, objectSpaceNormal.z) );
    return normalize( output.xyz );
}

float3 SGETangentToWorldNormal( float3 input, float3 worldSpaceNormal, float3 worldSpaceTangentU, float3 worldSpaceTangentV )
{
    worldSpaceTangentU = normalize( worldSpaceTangentU.xyz );
    worldSpaceTangentV = normalize( worldSpaceTangentV.xyz );

    input.y = -input.y;

    return normalize( SGETangentToWorldPosition (input, worldSpaceNormal, worldSpaceTangentU, worldSpaceTangentV ));
}

//Object
float3 SGEObjectToTangentNormal( float3 input, float3 objectSpaceNormal, float3 objectSpaceTangentU, float3 objectSpaceTangentV )
{
    float3 output;
    output.x = dot( input.xyz, objectSpaceTangentU.xyz );
    output.y = dot( input.xyz, objectSpaceTangentV.xyz );
    output.z = dot( input.xyz, objectSpaceNormal.xyz );
    return normalize( output.xyz );
}

float3 SGEObjectToObjectNormal(float3 input)
{
    return input;
}

float3 SGEObjectToWorldNormal(float3 input, float3 worldSpaceNormal, float3 worldSpaceTangentU, float3 worldSpaceTangentV, float3 objectSpaceNormal, float3 objectSpaceTangentU, float3 objectSpaceTangentV)
{
    return SGETangentToWorldNormal( SGEObjectToTangentPosition( input, objectSpaceNormal, objectSpaceTangentU, objectSpaceTangentV ), worldSpaceNormal, worldSpaceTangentU, worldSpaceTangentV );
}

//World
float3 SGEWorldToTangentNormal( float3 input, float3 worldSpaceNormal, float3 worldSpaceTangentU, float3 worldSpaceTangentV )
{
    return SGEWorldToTangentPosition( input, worldSpaceNormal, worldSpaceTangentU, worldSpaceTangentV );
}

float3 SGEWorldToObjectNormal( float3 input, float3 worldSpaceNormal, float3 worldSpaceTangentU, float3 worldSpaceTangentV, float3 objectSpaceNormal, float3 objectSpaceTangentU, float3 objectSpaceTangentV )
{
    return SGETangentToObjectNormal( SGEWorldToTangentPosition( input, worldSpaceNormal, worldSpaceTangentU, worldSpaceTangentV ), objectSpaceNormal, objectSpaceTangentU, objectSpaceTangentV );
}

float3 SGEWorldToWorldNormal(float3 input)
{
    return input;
}

//Direction

//Tangent
float4 SGETangentToViewDirection ( float3 input, float3 worldSpaceNormal, float3 worldSpaceTangentU, float3 worldSpaceTangentV )
{
    return mul(g_matWorldToProjection, float4(SGETangentToWorldNormal(input, worldSpaceNormal, worldSpaceTangentU, worldSpaceTangentV), 1.0));
}

//Object
float4 SGEObjectToViewDirection( float3 input, float3 objectSpacePosition, float3 worldSpacePosition )
{
    return mul(g_matWorldToProjection, float4(worldSpacePosition + input - objectSpacePosition, 1.0));
}

//World
float4 SGEWorldToViewDirection( float3 input)
{
    return mul(g_matWorldToProjection, float4(input, 1.0));
}