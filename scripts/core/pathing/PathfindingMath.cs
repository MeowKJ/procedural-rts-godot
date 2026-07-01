namespace ProceduralRts.Core;

public readonly record struct PathPoint(float X, float Y);
public readonly record struct PathfindingDebugResult(IReadOnlyList<PathPoint> Path, IReadOnlyList<GridObstacle> RawCells);
public readonly record struct PathfindingCorridorMember(int Id, float StartX, float StartY, float GoalX, float GoalY);
public readonly record struct PathfindingCorridorAssignment(int Id, IReadOnlyList<PathPoint> Path, IReadOnlyList<GridObstacle> RawCells);
public readonly record struct PathfindingSharedCorridorResult(
    IReadOnlyList<PathPoint> SharedPath,
    IReadOnlyList<PathfindingCorridorAssignment> Assignments);

public static partial class PathfindingMath
{
    private static readonly (int X, int Y, float Cost)[] Neighbors =
    [
        (-1, 0, 1), (1, 0, 1), (0, -1, 1), (0, 1, 1),
        (-1, -1, 1.4142f), (1, -1, 1.4142f), (-1, 1, 1.4142f), (1, 1, 1.4142f),
    ];

    public static IReadOnlyList<PathPoint> FindPath(
        float startX,
        float startY,
        float goalX,
        float goalY,
        float worldWidth,
        float worldHeight,
        float cellSize,
        IReadOnlyCollection<GridObstacle> obstacles)
    {
        return FindPath(
            startX,
            startY,
            goalX,
            goalY,
            worldWidth,
            worldHeight,
            cellSize,
            obstacles,
            MovementDomain.Land,
            []);
    }

    public static IReadOnlyList<PathPoint> FindPath(
        float startX,
        float startY,
        float goalX,
        float goalY,
        float worldWidth,
        float worldHeight,
        float cellSize,
        IReadOnlyCollection<GridObstacle> obstacles,
        MovementDomain movementDomain,
        IReadOnlyCollection<GridTerrain> terrain)
    {
        return FindPathWithDebug(
            startX,
            startY,
            goalX,
            goalY,
            worldWidth,
            worldHeight,
            cellSize,
            obstacles,
            movementDomain,
            terrain).Path;
    }

    public static PathfindingDebugResult FindPathWithDebug(
        float startX,
        float startY,
        float goalX,
        float goalY,
        float worldWidth,
        float worldHeight,
        float cellSize,
        IReadOnlyCollection<GridObstacle> obstacles,
        MovementDomain movementDomain,
        IReadOnlyCollection<GridTerrain> terrain)
    {
        if (cellSize <= 0 || worldWidth <= 0 || worldHeight <= 0)
        {
            return new PathfindingDebugResult([new PathPoint(goalX, goalY)], []);
        }

        var width = Math.Max(1, (int)MathF.Ceiling(worldWidth / cellSize));
        var height = Math.Max(1, (int)MathF.Ceiling(worldHeight / cellSize));
        var blocked = TerrainPassability.IgnoresBuildingBlockers(movementDomain)
            ? new HashSet<GridObstacle>()
            : obstacles.ToHashSet();
        var terrainByCell = terrain.ToDictionary(cell => new GridObstacle(cell.X, cell.Y), cell => cell.Layer);
        var allowedLayers = TerrainPassability.AllowedLayers(movementDomain);
        var start = ClampCell(WorldToCell(startX, startY, cellSize), width, height);
        var goal = ClampCell(WorldToCell(goalX, goalY, cellSize), width, height);
        blocked.Remove(start);
        blocked.Remove(goal);

        if (SegmentIsClear(
            new PathPoint(startX, startY),
            new PathPoint(goalX, goalY),
            width,
            height,
            cellSize,
            blocked,
            terrainByCell,
            allowedLayers))
        {
            return new PathfindingDebugResult([new PathPoint(goalX, goalY)], [start, goal]);
        }

        if (start == goal)
        {
            return new PathfindingDebugResult([new PathPoint(goalX, goalY)], [start]);
        }

        var cameFrom = new Dictionary<GridObstacle, GridObstacle>();
        var gScore = new Dictionary<GridObstacle, float> { [start] = 0 };
        var open = new PriorityQueue<GridObstacle, float>();
        open.Enqueue(start, Heuristic(start, goal));

        while (open.TryDequeue(out var current, out _))
        {
            if (current == goal)
            {
                return ReconstructPath(
                    cameFrom,
                    current,
                    cellSize,
                    startX,
                    startY,
                    goalX,
                    goalY,
                    width,
                    height,
                    blocked,
                    terrainByCell,
                    allowedLayers);
            }

            foreach (var neighbor in ValidNeighbors(current, width, height, blocked, terrainByCell, allowedLayers))
            {
                var tentative = gScore[current] + neighbor.Cost + ClearancePenalty(neighbor.Cell, width, height, blocked, terrainByCell, allowedLayers);
                if (gScore.TryGetValue(neighbor.Cell, out var existing) && tentative >= existing)
                {
                    continue;
                }

                cameFrom[neighbor.Cell] = current;
                gScore[neighbor.Cell] = tentative;
                open.Enqueue(neighbor.Cell, tentative + Heuristic(neighbor.Cell, goal));
            }
        }

        return new PathfindingDebugResult([new PathPoint(goalX, goalY)], [start, goal]);
    }

    public static PathfindingSharedCorridorResult FindSharedCorridor(
        IReadOnlyList<PathfindingCorridorMember> members,
        float intentX,
        float intentY,
        float worldWidth,
        float worldHeight,
        float cellSize,
        IReadOnlyCollection<GridObstacle> obstacles,
        MovementDomain movementDomain,
        IReadOnlyCollection<GridTerrain> terrain)
    {
        if (members.Count == 0)
        {
            return new PathfindingSharedCorridorResult([], []);
        }

        var anchorX = members.Average(member => member.StartX);
        var anchorY = members.Average(member => member.StartY);
        var shared = FindPathWithDebug(
            anchorX,
            anchorY,
            intentX,
            intentY,
            worldWidth,
            worldHeight,
            cellSize,
            obstacles,
            movementDomain,
            terrain);
        var sharedPath = shared.Path.Count == 0
            ? new List<PathPoint> { new(intentX, intentY) }
            : shared.Path.ToList();

        var width = Math.Max(1, (int)MathF.Ceiling(worldWidth / cellSize));
        var height = Math.Max(1, (int)MathF.Ceiling(worldHeight / cellSize));
        var blocked = TerrainPassability.IgnoresBuildingBlockers(movementDomain)
            ? new HashSet<GridObstacle>()
            : obstacles.ToHashSet();
        var terrainByCell = terrain.ToDictionary(cell => new GridObstacle(cell.X, cell.Y), cell => cell.Layer);
        var allowedLayers = TerrainPassability.AllowedLayers(movementDomain);

        var assignments = new List<PathfindingCorridorAssignment>(members.Count);
        foreach (var member in members)
        {
            assignments.Add(BuildSharedCorridorAssignment(
                member,
                sharedPath,
                shared.RawCells,
                worldWidth,
                worldHeight,
                cellSize,
                obstacles,
                movementDomain,
                terrain,
                width,
                height,
                blocked,
                terrainByCell,
                allowedLayers));
        }

        return new PathfindingSharedCorridorResult(sharedPath, assignments);
    }

    private static IEnumerable<(GridObstacle Cell, float Cost)> ValidNeighbors(
        GridObstacle current,
        int width,
        int height,
        HashSet<GridObstacle> blocked,
        IReadOnlyDictionary<GridObstacle, TerrainLayer> terrainByCell,
        TerrainLayer allowedLayers)
    {
        foreach (var offset in Neighbors)
        {
            var cell = new GridObstacle(current.X + offset.X, current.Y + offset.Y);
            if (IsImpassable(cell, width, height, blocked, terrainByCell, allowedLayers))
            {
                continue;
            }

            if (offset.X != 0 && offset.Y != 0
                && (IsImpassable(new GridObstacle(current.X + offset.X, current.Y), width, height, blocked, terrainByCell, allowedLayers)
                    || IsImpassable(new GridObstacle(current.X, current.Y + offset.Y), width, height, blocked, terrainByCell, allowedLayers)))
            {
                continue;
            }

            yield return (cell, offset.Cost);
        }
    }

    private static bool IsImpassable(
        GridObstacle cell,
        int width,
        int height,
        HashSet<GridObstacle> blocked,
        IReadOnlyDictionary<GridObstacle, TerrainLayer> terrainByCell,
        TerrainLayer allowedLayers)
    {
        if (cell.X < 0 || cell.Y < 0 || cell.X >= width || cell.Y >= height || blocked.Contains(cell))
        {
            return true;
        }

        var layer = terrainByCell.TryGetValue(cell, out var overrideLayer)
            ? overrideLayer
            : TerrainLayer.Ground;
        return (layer & allowedLayers) == 0;
    }

    private static float ClearancePenalty(
        GridObstacle cell,
        int width,
        int height,
        HashSet<GridObstacle> blocked,
        IReadOnlyDictionary<GridObstacle, TerrainLayer> terrainByCell,
        TerrainLayer allowedLayers)
    {
        var penalty = 0f;
        for (var x = cell.X - 2; x <= cell.X + 2; x++)
        {
            for (var y = cell.Y - 2; y <= cell.Y + 2; y++)
            {
                if (x == cell.X && y == cell.Y)
                {
                    continue;
                }

                var neighbor = new GridObstacle(x, y);
                if (!IsImpassable(neighbor, width, height, blocked, terrainByCell, allowedLayers))
                {
                    continue;
                }

                var manhattan = Math.Abs(x - cell.X) + Math.Abs(y - cell.Y);
                penalty += manhattan <= 1 ? 0.55f : 0.16f;
            }
        }

        return penalty;
    }

    private static PathfindingDebugResult ReconstructPath(
        Dictionary<GridObstacle, GridObstacle> cameFrom,
        GridObstacle current,
        float cellSize,
        float startX,
        float startY,
        float goalX,
        float goalY,
        int width,
        int height,
        HashSet<GridObstacle> blocked,
        IReadOnlyDictionary<GridObstacle, TerrainLayer> terrainByCell,
        TerrainLayer allowedLayers)
    {
        var cells = new List<GridObstacle> { current };
        while (cameFrom.TryGetValue(current, out var parent))
        {
            current = parent;
            cells.Add(current);
        }

        cells.Reverse();
        var points = cells
            .Skip(1)
            .Select(cell => CellCenter(cell, cellSize))
            .ToList();

        if (points.Count == 0 || DistanceSquared(points[^1], goalX, goalY) > cellSize * cellSize * 0.16f)
        {
            points.Add(new PathPoint(goalX, goalY));
        }
        else
        {
            points[^1] = new PathPoint(goalX, goalY);
        }

        var path = PruneByLineOfSight(
            SmoothCollinear(points),
            new PathPoint(startX, startY),
            width,
            height,
            cellSize,
            blocked,
            terrainByCell,
            allowedLayers);
        return new PathfindingDebugResult(path, cells);
    }

    private static GridObstacle WorldToCell(float x, float y, float cellSize)
    {
        return new GridObstacle((int)MathF.Floor(x / cellSize), (int)MathF.Floor(y / cellSize));
    }

    private static GridObstacle ClampCell(GridObstacle cell, int width, int height)
    {
        return new GridObstacle(Math.Clamp(cell.X, 0, width - 1), Math.Clamp(cell.Y, 0, height - 1));
    }

    private static PathPoint CellCenter(GridObstacle cell, float cellSize)
    {
        return new PathPoint((cell.X + 0.5f) * cellSize, (cell.Y + 0.5f) * cellSize);
    }

    private static float Heuristic(GridObstacle a, GridObstacle b)
    {
        var dx = MathF.Abs(a.X - b.X);
        var dy = MathF.Abs(a.Y - b.Y);
        return MathF.Max(dx, dy) + (1.4142f - 1) * MathF.Min(dx, dy);
    }

    private static float DistanceSquared(PathPoint point, float x, float y)
    {
        var dx = point.X - x;
        var dy = point.Y - y;
        return dx * dx + dy * dy;
    }
}
