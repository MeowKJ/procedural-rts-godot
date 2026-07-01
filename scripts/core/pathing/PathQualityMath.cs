namespace ProceduralRts.Core;

public readonly record struct PathQualityMetrics(
    float DirectDistance,
    float TravelDistance,
    float TravelInflation,
    int CornerCount,
    float Straightness);

public static class PathQualityMath
{
    public static PathQualityMetrics Measure(float startX, float startY, IReadOnlyList<PathPoint> path)
    {
        if (path.Count == 0)
        {
            return new PathQualityMetrics(0, 0, 1, 0, 1);
        }

        var direct = Distance(startX, startY, path[^1].X, path[^1].Y);
        var travel = 0f;
        var previousX = startX;
        var previousY = startY;
        var previousDirectionX = 0f;
        var previousDirectionY = 0f;
        var corners = 0;

        foreach (var point in path)
        {
            var segmentX = point.X - previousX;
            var segmentY = point.Y - previousY;
            var segmentLength = MathF.Sqrt(segmentX * segmentX + segmentY * segmentY);
            if (segmentLength > 0.001f)
            {
                var directionX = segmentX / segmentLength;
                var directionY = segmentY / segmentLength;
                if (travel > 0.001f && DirectionChanged(previousDirectionX, previousDirectionY, directionX, directionY))
                {
                    corners++;
                }

                previousDirectionX = directionX;
                previousDirectionY = directionY;
                travel += segmentLength;
            }

            previousX = point.X;
            previousY = point.Y;
        }

        var inflation = direct <= 0.001f ? 1 : travel / direct;
        var straightness = travel <= 0.001f ? 1 : Math.Clamp(direct / travel, 0, 1);
        return new PathQualityMetrics(direct, travel, inflation, corners, straightness);
    }

    public static float FinalCompactness(IEnumerable<PathPoint> finalPoints)
    {
        var points = finalPoints.ToList();
        if (points.Count <= 1)
        {
            return 0;
        }

        var centerX = points.Average(point => point.X);
        var centerY = points.Average(point => point.Y);
        return points.Max(point => Distance(centerX, centerY, point.X, point.Y));
    }

    public static float JitterAfterArrival(IEnumerable<PathPoint> positions)
    {
        var points = positions.ToList();
        if (points.Count <= 1)
        {
            return 0;
        }

        var first = points[0];
        return points.Skip(1).Max(point => Distance(first.X, first.Y, point.X, point.Y));
    }

    private static bool DirectionChanged(float ax, float ay, float bx, float by)
    {
        var dot = Math.Clamp(ax * bx + ay * by, -1, 1);
        return MathF.Acos(dot) > 0.18f;
    }

    private static float Distance(float ax, float ay, float bx, float by)
    {
        var dx = ax - bx;
        var dy = ay - by;
        return MathF.Sqrt(dx * dx + dy * dy);
    }
}
