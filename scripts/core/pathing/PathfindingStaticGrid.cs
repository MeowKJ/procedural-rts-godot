namespace ProceduralRts.Core;

public readonly record struct StaticPathCircle(float X, float Y, float Radius);
public sealed record PathfindingStaticGridResult(
    IReadOnlyList<GridObstacle> Obstacles,
    IReadOnlyList<GridTerrain> Terrain);

public static class PathfindingStaticGrid
{
    public const float RuntimeCellSize = 64f;

    public static bool FillEnvironment(
        MapRuntimeEnvironment environment,
        float worldWidth,
        float worldHeight,
        float cellSize,
        MovementDomain domain,
        List<GridObstacle> obstacles,
        List<GridTerrain> terrain,
        HashSet<GridObstacle> seen)
    {
        obstacles.Clear();
        terrain.Clear();
        seen.Clear();
        environment.AppendAuthoredTerrainGrid(cellSize, terrain);
        if (TerrainPassability.IgnoresBuildingBlockers(domain)) return false;
        environment.AppendStaticObstacleGrid(cellSize, obstacles, seen);
        return worldWidth > 0 && worldHeight > 0 && cellSize > 0;
    }

    public static void AppendCircle(
        StaticPathCircle blocker,
        float worldWidth,
        float worldHeight,
        float cellSize,
        List<GridObstacle> obstacles,
        HashSet<GridObstacle> seen)
    {
        var width = Math.Max(1, (int)MathF.Ceiling(worldWidth / cellSize));
        var height = Math.Max(1, (int)MathF.Ceiling(worldHeight / cellSize));
        var minX = Math.Clamp((int)MathF.Floor((blocker.X - blocker.Radius) / cellSize), 0, width - 1);
        var maxX = Math.Clamp((int)MathF.Floor((blocker.X + blocker.Radius) / cellSize), 0, width - 1);
        var minY = Math.Clamp((int)MathF.Floor((blocker.Y - blocker.Radius) / cellSize), 0, height - 1);
        var maxY = Math.Clamp((int)MathF.Floor((blocker.Y + blocker.Radius) / cellSize), 0, height - 1);
        for (var x = minX; x <= maxX; x++)
        for (var y = minY; y <= maxY; y++)
        {
            var cell = new GridObstacle(x, y);
            if (seen.Add(cell)) obstacles.Add(cell);
        }
    }

    public static PathfindingStaticGridResult Build(MapSpec map, MovementDomain domain)
    {
        var obstacles = new List<GridObstacle>();
        var terrain = new List<GridTerrain>();
        var seen = new HashSet<GridObstacle>();
        var environment = MapRuntimeEnvironment.From(map);
        if (FillEnvironment(
                environment, map.WorldSize.Width, map.WorldSize.Height, RuntimeCellSize,
                domain, obstacles, terrain, seen))
        {
            foreach (var building in map.Buildings)
            {
                var spec = BuildSpecCatalog.For(building.Kind);
                var footprint = spec.LogicalFootprint(building.Facing);
                AppendCircle(
                    new StaticPathCircle(
                        building.Position.X, building.Position.Y,
                        MathF.Max(footprint.X, footprint.Y) * 0.5f),
                    map.WorldSize.Width, map.WorldSize.Height, RuntimeCellSize, obstacles, seen);
            }
        }
        return new PathfindingStaticGridResult(
            Array.AsReadOnly(obstacles.ToArray()), Array.AsReadOnly(terrain.ToArray()));
    }
}
