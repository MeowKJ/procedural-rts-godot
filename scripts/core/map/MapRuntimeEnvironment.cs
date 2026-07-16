using Godot;

namespace ProceduralRts.Core;

public sealed record MapRuntimeTerrainCell(
    string Id,
    PlacementRect Bounds,
    string TerrainId,
    float MovementCost,
    bool BlocksLand)
{
    public TerrainLayer Layer => BlocksLand ? TerrainLayer.Water : TerrainLayer.Ground;
}

public sealed record MapRuntimeStaticObstacle(string Id, PlacementRect Bounds);

public readonly record struct MapRuntimeTerrainSample(
    TerrainLayer Layer,
    string SourceId,
    bool IsAuthored);

/// <summary>
/// Immutable authored map environment owned by EntityWorld. Terrain cells retain
/// source order because later authored layers override earlier containing cells.
/// </summary>
public sealed class MapRuntimeEnvironment
{
    private readonly IReadOnlyList<MapRuntimeTerrainCell> _terrainCells;
    private readonly IReadOnlyList<MapRuntimeStaticObstacle> _staticObstacles;

    private MapRuntimeEnvironment(
        MapSize worldSize,
        IReadOnlyList<MapRuntimeTerrainCell> terrainCells,
        IReadOnlyList<MapRuntimeStaticObstacle> staticObstacles)
    {
        WorldSize = worldSize;
        _terrainCells = Array.AsReadOnly(terrainCells.ToArray());
        _staticObstacles = Array.AsReadOnly(staticObstacles.ToArray());
    }

    public static MapRuntimeEnvironment Empty { get; } = new(new MapSize(0, 0), [], []);

    public MapSize WorldSize { get; }

    public IReadOnlyList<MapRuntimeTerrainCell> TerrainCells => _terrainCells;

    public IReadOnlyList<MapRuntimeStaticObstacle> StaticObstacles => _staticObstacles;

    public static MapRuntimeEnvironment From(MapSpec map)
    {
        var terrain = new MapRuntimeTerrainCell[map.TerrainCells.Count];
        for (var index = 0; index < terrain.Length; index++)
        {
            var source = map.TerrainCells[index];
            terrain[index] = new MapRuntimeTerrainCell(
                source.Id,
                source.Bounds.ToPlacementRect(),
                source.TerrainId,
                source.MovementCost,
                source.BlocksLand);
        }

        var obstacles = new MapRuntimeStaticObstacle[map.Obstacles.Count];
        for (var index = 0; index < obstacles.Length; index++)
        {
            var source = map.Obstacles[index];
            obstacles[index] = new MapRuntimeStaticObstacle(source.Id, source.Bounds.ToPlacementRect());
        }

        return new MapRuntimeEnvironment(map.WorldSize, terrain, obstacles);
    }

    public MapRuntimeTerrainSample SampleTerrain(
        float x,
        float y,
        float fallbackWorldWidth,
        float fallbackWorldHeight)
    {
        for (var index = _terrainCells.Count - 1; index >= 0; index--)
        {
            var cell = _terrainCells[index];
            if (Contains(cell.Bounds, x, y))
            {
                return new MapRuntimeTerrainSample(cell.Layer, cell.Id, IsAuthored: true);
            }
        }

        var kind = TerrainFloorMath.KindAt(
            new Vector2(x, y),
            new Vector2(fallbackWorldWidth, fallbackWorldHeight));
        var layer = kind switch
        {
            TerrainFloorKind.Water => TerrainLayer.Water,
            TerrainFloorKind.Coast => TerrainLayer.Coast,
            _ => TerrainLayer.Ground,
        };
        return new MapRuntimeTerrainSample(layer, "procedural", IsAuthored: false);
    }

    public void AppendAuthoredTerrainGrid(float cellSize, List<GridTerrain> destination)
    {
        if (cellSize <= 0 || WorldSize.Width <= 0 || WorldSize.Height <= 0)
        {
            return;
        }

        var width = Math.Max(1, (int)MathF.Ceiling(WorldSize.Width / cellSize));
        var height = Math.Max(1, (int)MathF.Ceiling(WorldSize.Height / cellSize));
        for (var terrainIndex = 0; terrainIndex < _terrainCells.Count; terrainIndex++)
        {
            var terrain = _terrainCells[terrainIndex];
            var authoredMinX = (int)MathF.Ceiling(terrain.Bounds.X / cellSize - 0.5f);
            var authoredMaxX = (int)MathF.Floor(terrain.Bounds.EndX / cellSize - 0.5f);
            var authoredMinY = (int)MathF.Ceiling(terrain.Bounds.Y / cellSize - 0.5f);
            var authoredMaxY = (int)MathF.Floor(terrain.Bounds.EndY / cellSize - 0.5f);
            var minX = Math.Max(0, authoredMinX);
            var maxX = Math.Min(width - 1, authoredMaxX);
            var minY = Math.Max(0, authoredMinY);
            var maxY = Math.Min(height - 1, authoredMaxY);
            if (minX > maxX || minY > maxY)
            {
                continue;
            }

            for (var x = minX; x <= maxX; x++)
            {
                for (var y = minY; y <= maxY; y++)
                {
                    destination.Add(new GridTerrain(x, y, terrain.Layer));
                }
            }
        }
    }

    public void AppendStaticObstacleGrid(
        float cellSize,
        List<GridObstacle> destination,
        HashSet<GridObstacle> seen)
    {
        if (cellSize <= 0 || WorldSize.Width <= 0 || WorldSize.Height <= 0)
        {
            return;
        }

        var width = Math.Max(1, (int)MathF.Ceiling(WorldSize.Width / cellSize));
        var height = Math.Max(1, (int)MathF.Ceiling(WorldSize.Height / cellSize));
        for (var obstacleIndex = 0; obstacleIndex < _staticObstacles.Count; obstacleIndex++)
        {
            var bounds = _staticObstacles[obstacleIndex].Bounds;
            var minX = Math.Clamp((int)MathF.Floor(bounds.X / cellSize), 0, width - 1);
            var maxX = Math.Clamp((int)MathF.Floor(MathF.BitDecrement(bounds.EndX) / cellSize), 0, width - 1);
            var minY = Math.Clamp((int)MathF.Floor(bounds.Y / cellSize), 0, height - 1);
            var maxY = Math.Clamp((int)MathF.Floor(MathF.BitDecrement(bounds.EndY) / cellSize), 0, height - 1);
            for (var x = minX; x <= maxX; x++)
            {
                for (var y = minY; y <= maxY; y++)
                {
                    var cell = new GridObstacle(x, y);
                    if (seen.Add(cell))
                    {
                        destination.Add(cell);
                    }
                }
            }
        }
    }

    private static bool Contains(PlacementRect bounds, float x, float y)
    {
        return x >= bounds.X && x <= bounds.EndX && y >= bounds.Y && y <= bounds.EndY;
    }
}

internal static class MapRectPlacementExtensions
{
    public static PlacementRect ToPlacementRect(this MapRect rect)
    {
        return new PlacementRect(rect.X, rect.Y, rect.Width, rect.Height);
    }
}
