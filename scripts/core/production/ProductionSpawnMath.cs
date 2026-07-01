namespace ProceduralRts.Core;

public readonly record struct SpawnObstacle(float X, float Y, float Radius);

public readonly record struct SpawnPoint(float X, float Y);

public static class ProductionSpawnMath
{
    private static readonly float[] DirectionOffsets =
    [
        0f,
        MathF.PI / 4f,
        -MathF.PI / 4f,
        MathF.PI / 2f,
        -MathF.PI / 2f,
        MathF.PI,
    ];
    private static readonly float[] RingScales = [1f, 1.45f, 1.9f, 2.4f];

    public static SpawnPoint FindSpawnPoint(
        float producerX,
        float producerY,
        float facing,
        float producerWidth,
        float producerHeight,
        float unitRadius,
        float worldWidth,
        float worldHeight,
        IReadOnlyList<SpawnObstacle> obstacles)
    {
        var baseDistance = MathF.Max(producerWidth, producerHeight) * 0.55f + unitRadius + 26;

        foreach (var ring in RingScales)
        {
            foreach (var offset in DirectionOffsets)
            {
                var angle = facing + offset;
                var direction = (X: MathF.Cos(angle), Y: MathF.Sin(angle));
                var x = producerX + direction.X * baseDistance * ring;
                var y = producerY + direction.Y * baseDistance * ring;
                var clamped = ClampToWorld(x, y, unitRadius, worldWidth, worldHeight);

                if (!Overlaps(clamped.X, clamped.Y, unitRadius, obstacles))
                {
                    return new SpawnPoint(clamped.X, clamped.Y);
                }
            }
        }

        var fallback = ClampToWorld(producerX + MathF.Cos(facing) * baseDistance, producerY + MathF.Sin(facing) * baseDistance, unitRadius, worldWidth, worldHeight);
        return new SpawnPoint(fallback.X, fallback.Y);
    }

    private static bool Overlaps(float x, float y, float radius, IReadOnlyList<SpawnObstacle> obstacles)
    {
        foreach (var obstacle in obstacles)
        {
            var dx = x - obstacle.X;
            var dy = y - obstacle.Y;
            var minDistance = radius + obstacle.Radius + 6;
            if (dx * dx + dy * dy < minDistance * minDistance)
            {
                return true;
            }
        }

        return false;
    }

    private static (float X, float Y) ClampToWorld(float x, float y, float radius, float worldWidth, float worldHeight)
    {
        var margin = MathF.Max(80, radius + 28);
        return (
            Math.Clamp(x, margin, worldWidth - margin),
            Math.Clamp(y, margin, worldHeight - margin)
        );
    }
}
