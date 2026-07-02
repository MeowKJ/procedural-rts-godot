namespace ProceduralRts.Core;

public static partial class PathfindingMath
{
    private static void BuildPassabilityLookups(
        IReadOnlyCollection<GridObstacle> obstacles,
        MovementDomain movementDomain,
        IReadOnlyCollection<GridTerrain> terrain,
        HashSet<GridObstacle> blocked,
        Dictionary<GridObstacle, TerrainLayer> terrainByCell)
    {
        blocked.Clear();
        if (!TerrainPassability.IgnoresBuildingBlockers(movementDomain))
        {
            foreach (var obstacle in obstacles)
            {
                blocked.Add(obstacle);
            }
        }

        terrainByCell.Clear();
        foreach (var cell in terrain)
        {
            terrainByCell[new GridObstacle(cell.X, cell.Y)] = cell.Layer;
        }
    }
}
