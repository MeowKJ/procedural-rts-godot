namespace ProceduralRts.Core;

public static partial class PathfindingMath
{
    private static PathfindingCorridorAssignment BuildSharedCorridorAssignment(
        PathfindingCorridorMember member,
        IReadOnlyList<PathPoint> sharedPath,
        IReadOnlyList<GridObstacle> sharedRawCells,
        float worldWidth,
        float worldHeight,
        float cellSize,
        IReadOnlyCollection<GridObstacle> obstacles,
        MovementDomain movementDomain,
        IReadOnlyCollection<GridTerrain> terrain,
        int width,
        int height,
        HashSet<GridObstacle> blocked,
        IReadOnlyDictionary<GridObstacle, TerrainLayer> terrainByCell,
        TerrainLayer allowedLayers)
    {
        var memberStart = new PathPoint(member.StartX, member.StartY);
        var memberGoal = new PathPoint(member.GoalX, member.GoalY);
        var rawCells = new List<GridObstacle>(sharedRawCells);
        var points = new List<PathPoint>();

        if (sharedPath.Count == 0)
        {
            var fallback = FindPathWithDebug(
                member.StartX,
                member.StartY,
                member.GoalX,
                member.GoalY,
                worldWidth,
                worldHeight,
                cellSize,
                obstacles,
                movementDomain,
                terrain);
            return new PathfindingCorridorAssignment(member.Id, fallback.Path, fallback.RawCells);
        }

        var entryIndex = -1;
        for (var index = sharedPath.Count - 1; index >= 0; index--)
        {
            if (SegmentIsClear(memberStart, sharedPath[index], width, height, cellSize, blocked, terrainByCell, allowedLayers))
            {
                entryIndex = index;
                break;
            }
        }

        if (entryIndex >= 0)
        {
            AppendUnique(points, sharedPath.Skip(entryIndex));
        }
        else
        {
            var connector = FindPathWithDebug(
                member.StartX,
                member.StartY,
                sharedPath[0].X,
                sharedPath[0].Y,
                worldWidth,
                worldHeight,
                cellSize,
                obstacles,
                movementDomain,
                terrain);
            rawCells.AddRange(connector.RawCells);
            AppendUnique(points, connector.Path);
            AppendUnique(points, sharedPath.Skip(1));
        }

        if (points.Count == 0)
        {
            points.Add(memberGoal);
        }

        var last = points[^1];
        if (DistanceSquared(last, member.GoalX, member.GoalY) > 0.25f)
        {
            if (SegmentIsClear(last, memberGoal, width, height, cellSize, blocked, terrainByCell, allowedLayers))
            {
                AppendUnique(points, [memberGoal]);
            }
            else
            {
                var exit = FindPathWithDebug(
                    last.X,
                    last.Y,
                    member.GoalX,
                    member.GoalY,
                    worldWidth,
                    worldHeight,
                    cellSize,
                    obstacles,
                    movementDomain,
                    terrain);
                rawCells.AddRange(exit.RawCells);
                AppendUnique(points, exit.Path);
            }
        }

        var path = PruneByLineOfSight(
            SmoothCollinear(points),
            memberStart,
            width,
            height,
            cellSize,
            blocked,
            terrainByCell,
            allowedLayers);
        return new PathfindingCorridorAssignment(member.Id, path, rawCells);
    }

    private static void AppendUnique(List<PathPoint> target, IEnumerable<PathPoint> points)
    {
        foreach (var point in points)
        {
            if (target.Count == 0 || DistanceSquared(target[^1], point.X, point.Y) > 0.25f)
            {
                target.Add(point);
            }
        }
    }
}
