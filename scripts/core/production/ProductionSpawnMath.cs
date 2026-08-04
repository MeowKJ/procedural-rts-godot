namespace ProceduralRts.Core;

public readonly record struct SpawnObstacle(float X, float Y, float Radius);

public static class ProductionSpawnMath
{
    public static bool IsSpawnPointAvailable(
        float x,
        float y,
        float unitRadius,
        IReadOnlyList<SpawnObstacle> obstacles)
    {
        return !Overlaps(x, y, unitRadius, obstacles);
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
}
