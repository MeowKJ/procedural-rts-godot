namespace ProceduralRts.Core;

public static partial class PathfindingMath
{
    private static IReadOnlyList<PathPoint> SmoothCollinear(IReadOnlyList<PathPoint> points)
    {
        if (points.Count <= 2)
        {
            return points;
        }

        var result = new List<PathPoint> { points[0] };
        for (var index = 1; index < points.Count - 1; index++)
        {
            var previous = result[^1];
            var current = points[index];
            var next = points[index + 1];
            var abx = MathF.Sign(current.X - previous.X);
            var aby = MathF.Sign(current.Y - previous.Y);
            var bcx = MathF.Sign(next.X - current.X);
            var bcy = MathF.Sign(next.Y - current.Y);
            if (MathF.Abs(abx - bcx) > 0.001f || MathF.Abs(aby - bcy) > 0.001f)
            {
                result.Add(current);
            }
        }

        result.Add(points[^1]);
        return result;
    }

    private static IReadOnlyList<PathPoint> PruneByLineOfSight(
        IReadOnlyList<PathPoint> points,
        PathPoint start,
        int width,
        int height,
        float cellSize,
        HashSet<GridObstacle> blocked,
        IReadOnlyDictionary<GridObstacle, TerrainLayer> terrainByCell,
        TerrainLayer allowedLayers)
    {
        if (points.Count <= 1)
        {
            return points;
        }

        var result = new List<PathPoint>();
        var anchor = start;
        var index = 0;
        while (index < points.Count)
        {
            var farthest = index;
            for (var candidate = points.Count - 1; candidate > index; candidate--)
            {
                if (SegmentIsClear(anchor, points[candidate], width, height, cellSize, blocked, terrainByCell, allowedLayers))
                {
                    farthest = candidate;
                    break;
                }
            }

            result.Add(points[farthest]);
            anchor = points[farthest];
            index = farthest + 1;
        }

        return SmoothCollinear(result);
    }

    private static bool SegmentIsClear(
        PathPoint start,
        PathPoint end,
        int width,
        int height,
        float cellSize,
        HashSet<GridObstacle> blocked,
        IReadOnlyDictionary<GridObstacle, TerrainLayer> terrainByCell,
        TerrainLayer allowedLayers)
    {
        var dx = end.X - start.X;
        var dy = end.Y - start.Y;
        var distance = MathF.Sqrt(dx * dx + dy * dy);
        var steps = Math.Max(1, (int)MathF.Ceiling(distance / Math.Max(1f, cellSize * AdvancedPathingPolicy.LineOfSightProbeCellFraction)));

        var previous = ClampCell(WorldToCell(start.X, start.Y, cellSize), width, height);
        for (var step = 0; step <= steps; step++)
        {
            var t = step / (float)steps;
            var x = start.X + dx * t;
            var y = start.Y + dy * t;
            var cell = ClampCell(WorldToCell(x, y, cellSize), width, height);
            if (IsImpassable(cell, width, height, blocked, terrainByCell, allowedLayers))
            {
                return false;
            }

            if (cell.X != previous.X && cell.Y != previous.Y
                && (IsImpassable(new GridObstacle(cell.X, previous.Y), width, height, blocked, terrainByCell, allowedLayers)
                    || IsImpassable(new GridObstacle(previous.X, cell.Y), width, height, blocked, terrainByCell, allowedLayers)))
            {
                return false;
            }

            previous = cell;
        }

        return true;
    }
}
