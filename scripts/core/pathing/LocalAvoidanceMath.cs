namespace ProceduralRts.Core;

public static class LocalAvoidanceMath
{
    public static void BuildHashInto(
        IReadOnlyList<LocalAvoidanceBody> bodies,
        float cellSize,
        Dictionary<GridObstacle, List<LocalAvoidanceBody>> cells)
    {
        foreach (var bucket in cells.Values)
        {
            bucket.Clear();
        }

        foreach (var body in bodies)
        {
            var cell = Cell(body.X, body.Y, cellSize);
            if (!cells.TryGetValue(cell, out var bucket))
            {
                bucket = [];
                cells[cell] = bucket;
            }

            bucket.Add(body);
        }
    }

    public static LocalAvoidanceVector ResolveVector(
        LocalAvoidanceBody body,
        SpatialGrid<LocalAvoidanceBody> grid,
        float padding = 10,
        float maxLength = 0.72f,
        float anchorBias = 1.35f)
    {
        if (!body.CanBeDisplaced)
        {
            return new LocalAvoidanceVector(0, 0);
        }

        var avoidanceX = 0f;
        var avoidanceY = 0f;

        foreach (var other in grid.Neighbors(body.X, body.Y))
        {
            if (other.Id == body.Id)
            {
                continue;
            }

            AccumulateAvoidance(body, other, padding, anchorBias, ref avoidanceX, ref avoidanceY);
        }

        return LimitLength(avoidanceX, avoidanceY, maxLength);
    }

    public static LocalAvoidanceVector ResolveVector(
        LocalAvoidanceBody body,
        IReadOnlyDictionary<GridObstacle, IReadOnlyList<LocalAvoidanceBody>> hash,
        float cellSize,
        float padding = 10,
        float maxLength = 0.72f,
        float anchorBias = 1.35f)
    {
        if (!body.CanBeDisplaced)
        {
            return new LocalAvoidanceVector(0, 0);
        }

        var cell = Cell(body.X, body.Y, cellSize);
        var avoidanceX = 0f;
        var avoidanceY = 0f;

        for (var x = cell.X - 1; x <= cell.X + 1; x++)
        {
            for (var y = cell.Y - 1; y <= cell.Y + 1; y++)
            {
                if (!hash.TryGetValue(new GridObstacle(x, y), out var neighbors))
                {
                    continue;
                }

                foreach (var other in neighbors)
                {
                    if (other.Id == body.Id)
                    {
                        continue;
                    }

                    AccumulateAvoidance(body, other, padding, anchorBias, ref avoidanceX, ref avoidanceY);
                }
            }
        }

        return LimitLength(avoidanceX, avoidanceY, maxLength);
    }

    public static LocalAvoidanceVector ResolveVector(
        LocalAvoidanceBody body,
        IReadOnlyDictionary<GridObstacle, List<LocalAvoidanceBody>> hash,
        float cellSize,
        float padding = 10,
        float maxLength = 0.72f,
        float anchorBias = 1.35f)
    {
        if (!body.CanBeDisplaced)
        {
            return new LocalAvoidanceVector(0, 0);
        }

        var cell = Cell(body.X, body.Y, cellSize);
        var avoidanceX = 0f;
        var avoidanceY = 0f;

        for (var x = cell.X - 1; x <= cell.X + 1; x++)
        {
            for (var y = cell.Y - 1; y <= cell.Y + 1; y++)
            {
                if (!hash.TryGetValue(new GridObstacle(x, y), out var neighbors))
                {
                    continue;
                }

                foreach (var other in neighbors)
                {
                    if (other.Id == body.Id)
                    {
                        continue;
                    }

                    AccumulateAvoidance(body, other, padding, anchorBias, ref avoidanceX, ref avoidanceY);
                }
            }
        }

        return LimitLength(avoidanceX, avoidanceY, maxLength);
    }

    private static void AccumulateAvoidance(
        LocalAvoidanceBody body,
        LocalAvoidanceBody other,
        float padding,
        float anchorBias,
        ref float avoidanceX,
        ref float avoidanceY)
    {
        var offsetX = body.X - other.X;
        var offsetY = body.Y - other.Y;
        var distanceSquared = offsetX * offsetX + offsetY * offsetY;
        var desiredDistance = body.Radius + other.Radius + padding;
        if (distanceSquared >= desiredDistance * desiredDistance)
        {
            return;
        }

        if (distanceSquared <= 0.01f)
        {
            var angle = ((body.Id * 37 + other.Id * 17) % 360) * MathF.PI / 180f;
            offsetX = MathF.Cos(angle);
            offsetY = MathF.Sin(angle);
            distanceSquared = 1;
        }

        var distance = MathF.Sqrt(distanceSquared);
        var closeness = 1 - Math.Clamp(distance / desiredDistance, 0, 1);
        var bias = other.IsAnchor ? anchorBias + other.AnchorPriority * 0.22f : 1;
        avoidanceX += offsetX / distance * closeness * bias;
        avoidanceY += offsetY / distance * closeness * bias;
    }

    public static GridObstacle Cell(float x, float y, float cellSize)
    {
        return new GridObstacle(
            (int)MathF.Floor(x / cellSize),
            (int)MathF.Floor(y / cellSize));
    }

    private static LocalAvoidanceVector LimitLength(float x, float y, float maxLength)
    {
        var lengthSquared = x * x + y * y;
        if (lengthSquared <= maxLength * maxLength || lengthSquared <= 0.0001f)
        {
            return new LocalAvoidanceVector(x, y);
        }

        var scale = maxLength / MathF.Sqrt(lengthSquared);
        return new LocalAvoidanceVector(x * scale, y * scale);
    }
}
