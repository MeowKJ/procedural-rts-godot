namespace ProceduralRts.Core;

public readonly record struct PathPoint(float X, float Y);
public readonly record struct PathfindingDebugResult(
    IReadOnlyList<PathPoint> Path,
    IReadOnlyList<GridObstacle> RawCells,
    bool Reached = true);
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
        var assignments = new List<PathfindingCorridorAssignment>(members.Count);
        return FindSharedCorridor(
            members,
            intentX,
            intentY,
            worldWidth,
            worldHeight,
            cellSize,
            obstacles,
            movementDomain,
            terrain,
            assignments);
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
        IReadOnlyCollection<GridTerrain> terrain,
        List<PathfindingCorridorAssignment> assignments)
    {
        return FindSharedCorridor(
            new PathfindingWorkspace(),
            members,
            intentX,
            intentY,
            worldWidth,
            worldHeight,
            cellSize,
            obstacles,
            movementDomain,
            terrain,
            assignments);
    }

    public static PathfindingSharedCorridorResult FindSharedCorridor(
        PathfindingWorkspace workspace,
        IReadOnlyList<PathfindingCorridorMember> members,
        float intentX,
        float intentY,
        float worldWidth,
        float worldHeight,
        float cellSize,
        IReadOnlyCollection<GridObstacle> obstacles,
        MovementDomain movementDomain,
        IReadOnlyCollection<GridTerrain> terrain,
        List<PathfindingCorridorAssignment> assignments)
    {
        assignments.Clear();
        if (members.Count == 0)
        {
            return new PathfindingSharedCorridorResult([], assignments);
        }

        var anchorX = 0f;
        var anchorY = 0f;
        for (var index = 0; index < members.Count; index++)
        {
            anchorX += members[index].StartX;
            anchorY += members[index].StartY;
        }

        anchorX /= members.Count;
        anchorY /= members.Count;
        var shared = FindPathWithDebug(
            workspace,
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
        IReadOnlyList<PathPoint> sharedPath = shared.Path.Count == 0
            ? [new PathPoint(intentX, intentY)]
            : shared.Path;

        var width = Math.Max(1, (int)MathF.Ceiling(worldWidth / cellSize));
        var height = Math.Max(1, (int)MathF.Ceiling(worldHeight / cellSize));
        var blocked = workspace.SharedCorridorBlocked;
        var terrainByCell = workspace.SharedCorridorTerrainByCell;
        BuildPassabilityLookups(obstacles, movementDomain, terrain, blocked, terrainByCell);
        var allowedLayers = TerrainPassability.AllowedLayers(movementDomain);

        foreach (var member in members)
        {
            assignments.Add(BuildSharedCorridorAssignment(
                workspace,
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

    private static void CollectValidNeighbors(
        GridObstacle current,
        int width,
        int height,
        HashSet<GridObstacle> blocked,
        IReadOnlyDictionary<GridObstacle, TerrainLayer> terrainByCell,
        TerrainLayer allowedLayers,
        List<(GridObstacle Cell, float Cost)> validNeighbors)
    {
        validNeighbors.Clear();
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

            validNeighbors.Add((cell, offset.Cost));
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
        PathfindingWorkspace workspace,
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
        var cells = workspace.ReconstructedCells;
        cells.Clear();
        cells.Add(current);
        while (cameFrom.TryGetValue(current, out var parent))
        {
            current = parent;
            cells.Add(current);
        }

        cells.Reverse();
        var points = workspace.ReconstructedPoints;
        points.Clear();
        for (var index = 1; index < cells.Count; index++)
        {
            points.Add(CellCenter(cells[index], cellSize));
        }

        if (points.Count == 0 || DistanceSquared(points[^1], goalX, goalY) > cellSize * cellSize * 0.16f)
        {
            points.Add(new PathPoint(goalX, goalY));
        }
        else
        {
            points[^1] = new PathPoint(goalX, goalY);
        }

        var path = SmoothAndPrunePath(
            workspace,
            points,
            new PathPoint(startX, startY),
            width,
            height,
            cellSize,
            blocked,
            terrainByCell,
            allowedLayers);
        return new PathfindingDebugResult(
            new List<PathPoint>(path),
            new List<GridObstacle>(cells));
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
