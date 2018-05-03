float2 GetFogOfWarUV(float3 worldpos, int weight)
{
	float mapSize = 1 << weight;
	float cellSize = 0.5f;

	float row = floor(worldpos.z / cellSize) + mapSize / 2;
	float col = floor(worldpos.x / cellSize) + mapSize / 2;

	return float2(col / (mapSize - 1), row / (mapSize - 1));
}