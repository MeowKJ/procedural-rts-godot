namespace ProceduralRts.Core;

public static class TerrainPassability
{
    public static TerrainLayer AllowedLayers(MovementDomain domain)
    {
        return domain switch
        {
            MovementDomain.Land => TerrainLayer.Ground | TerrainLayer.Coast,
            MovementDomain.Naval => TerrainLayer.Water | TerrainLayer.Coast,
            MovementDomain.Air => TerrainLayer.Ground | TerrainLayer.Water | TerrainLayer.Coast | TerrainLayer.Air,
            MovementDomain.Amphibious => TerrainLayer.Ground | TerrainLayer.Water | TerrainLayer.Coast,
            _ => TerrainLayer.Ground,
        };
    }

    public static bool IgnoresBuildingBlockers(MovementDomain domain)
    {
        return domain == MovementDomain.Air;
    }
}
