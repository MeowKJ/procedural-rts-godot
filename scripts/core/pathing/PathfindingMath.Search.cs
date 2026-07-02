namespace ProceduralRts.Core;

public static partial class PathfindingMath
{
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
        return FindPathWithDebug(
            new PathfindingWorkspace(),
            startX,
            startY,
            goalX,
            goalY,
            worldWidth,
            worldHeight,
            cellSize,
            obstacles,
            movementDomain,
            terrain);
    }

    public static PathfindingDebugResult FindPathWithDebug(
        PathfindingWorkspace workspace,
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
        var blocked = workspace.Blocked;
        var terrainByCell = workspace.TerrainByCell;
        BuildPassabilityLookups(obstacles, movementDomain, terrain, blocked, terrainByCell);
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

        workspace.ClearSearch(start);
        var cameFrom = workspace.CameFrom;
        var gScore = workspace.GScore;
        var open = workspace.Open;
        var validNeighbors = workspace.ValidNeighbors;
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

            CollectValidNeighbors(current, width, height, blocked, terrainByCell, allowedLayers, validNeighbors);
            foreach (var neighbor in validNeighbors)
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
}
