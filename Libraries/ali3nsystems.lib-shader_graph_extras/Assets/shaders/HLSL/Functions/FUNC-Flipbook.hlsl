float4 SGEFlipbookInterpolated(float2 coordinates, Texture2D texture, SamplerState sampler, float width, float height, float tile, float2 invert, float loop)
{
	float totalTiles = width * height;

	float currentTile = floor(tile);

	if (loop == 1)
	{
		currentTile = fmod(currentTile, totalTiles);
	}
	else
	{
		currentTile = min(currentTile, totalTiles - 1);
	}

	float nextTile;
	if (loop == 1)
	{
		nextTile = fmod(currentTile + 1, totalTiles);
	}
	else
	{
		nextTile = min(currentTile + 1, totalTiles - 1);
	}

	float2 tileCount = float2(1.0, 1.0) / float2(width, height);
	float currentTileY = floor(currentTile / width);
	float currentTileX = currentTile - width * currentTileY;

	if (invert.x == 1) currentTileX = width - currentTileX - 1;
	if (invert.y == 1) currentTileY = height - currentTileY - 1;

	float nextTileY = floor(nextTile / width);
	float nextTileX = nextTile - width * nextTileY;

	if (invert.x == 1) nextTileX = width - nextTileX - 1;
	if (invert.y == 1) nextTileY = height - nextTileY - 1;

	float2 currentCoordinates = (coordinates + float2(currentTileX, currentTileY)) * tileCount;
	float2 nextCoordinates = (coordinates + float2(nextTileX, nextTileY)) * tileCount;

	float4 currentFrame = texture.Sample(sampler, currentCoordinates);
	float4 nextFrame = texture.Sample(sampler, nextCoordinates);

	return lerp(currentFrame, nextFrame, frac(tile));
}

float4 SGEFlipbookStandard(float2 coordinates, Texture2D texture, SamplerState sampler, float width, float height, float tile, float2 invert, float loop)
{
	float totalTiles = width * height;

	float currentTile = floor(tile);

	if (loop == 1)
	{
		currentTile = fmod(currentTile, totalTiles);
	}
	else
	{
		currentTile = min(currentTile, totalTiles - 1);
	}

	float2 tileCount = float2(1.0, 1.0) / float2(width, height);
	float currentTileY = floor(currentTile / width);
	float currentTileX = currentTile - width * currentTileY;

	if (invert.x == 1) currentTileX = width - currentTileX - 1;
	if (invert.y == 1) currentTileY = height - currentTileY - 1;

	float2 currentCoordinates = (coordinates + float2(currentTileX, currentTileY)) * tileCount;
	
	return texture.Sample(sampler, currentCoordinates);
}