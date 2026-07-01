namespace ProceduralRts.Core;

public sealed record SkirmishMapRequest(
    int Seed,
    int StartingCredits,
    MapSize WorldSize,
    FactionId PlayerFaction,
    FactionId EnemyFaction,
    int ReferenceSeed = 1729);

public static class SkirmishMapSpecGenerator
{
    private static readonly MapPoint PlayerReferenceStart = new(505, 610);
    private static readonly MapPoint EnemyReferenceStart = new(2860, 1535);

    public static MapSpec Generate(SkirmishMapRequest request)
    {
        var playerStart = request.Seed == request.ReferenceSeed
            ? PlayerReferenceStart
            : Clamp(
                new MapPoint(request.WorldSize.Width * 0.16f, request.WorldSize.Height * 0.26f)
                    + Jitter(request.Seed, 11, 72, 56),
                request.WorldSize,
                280);
        var enemyStart = request.Seed == request.ReferenceSeed
            ? EnemyReferenceStart
            : Mirror(playerStart, request.WorldSize);

        return new MapSpec
        {
            Id = $"skirmish.seed.{request.Seed}",
            Seed = request.Seed,
            WorldSize = request.WorldSize,
            OwnerStarts =
            [
                new(new OwnerId(1), request.PlayerFaction, playerStart, 0, request.StartingCredits),
                new(new OwnerId(2), request.EnemyFaction, enemyStart, MathF.PI, request.StartingCredits),
            ],
            Resources = PairedResources(request.Seed, request.WorldSize).ToArray(),
            Obstacles = PairedObstacles(request.Seed, request.WorldSize).ToArray(),
        };
    }

    public static MapPoint Mirror(MapPoint point, MapSize worldSize)
    {
        return new MapPoint(worldSize.Width - point.X, worldSize.Height - point.Y);
    }

    private static IEnumerable<MapResourceNodeSpec> PairedResources(int seed, MapSize worldSize)
    {
        var specs = new[]
        {
            (Point: new MapPoint(worldSize.Width * 0.27f, worldSize.Height * 0.38f), Radius: 184f, Amount: 5200, Accent: new MapColor("#f6c55c"), Salt: 101),
            (Point: new MapPoint(worldSize.Width * 0.34f, worldSize.Height * 0.72f), Radius: 162f, Amount: 4300, Accent: new MapColor("#8fffe1"), Salt: 202),
            (Point: new MapPoint(worldSize.Width * 0.45f, worldSize.Height * 0.28f), Radius: 148f, Amount: 3300, Accent: new MapColor("#ff5d75"), Salt: 303),
        };

        foreach (var spec in specs)
        {
            var position = Clamp(spec.Point + Jitter(seed, spec.Salt, 110, 92), worldSize, 260);
            yield return new MapResourceNodeSpec($"resource.{spec.Salt}.a", position, spec.Radius, spec.Amount, spec.Accent);
            yield return new MapResourceNodeSpec($"resource.{spec.Salt}.b", Mirror(position, worldSize), spec.Radius, spec.Amount, spec.Accent);
        }
    }

    private static IEnumerable<MapObstacleSpec> PairedObstacles(int seed, MapSize worldSize)
    {
        var centers = new[]
        {
            new MapPoint(worldSize.Width * 0.38f, worldSize.Height * 0.50f) + Jitter(seed, 401, 42, 28),
            new MapPoint(worldSize.Width * 0.45f, worldSize.Height * 0.34f) + Jitter(seed, 402, 34, 24),
            new MapPoint(worldSize.Width * 0.45f, worldSize.Height * 0.68f) + Jitter(seed, 403, 34, 24),
        };
        var sizes = new[]
        {
            new MapSize(210, 132),
            new MapSize(160, 210),
            new MapSize(160, 210),
        };

        for (var index = 0; index < centers.Length; index++)
        {
            yield return RectObstacle($"obstacle.{index}.a", centers[index], sizes[index]);
            yield return RectObstacle($"obstacle.{index}.b", Mirror(centers[index], worldSize), sizes[index]);
        }
    }

    private static MapObstacleSpec RectObstacle(string id, MapPoint center, MapSize size)
    {
        return new MapObstacleSpec(id, new MapRect(center.X - size.Width * 0.5f, center.Y - size.Height * 0.5f, size.Width, size.Height));
    }

    private static MapPoint Clamp(MapPoint point, MapSize worldSize, float margin)
    {
        return new MapPoint(
            Math.Clamp(point.X, margin, worldSize.Width - margin),
            Math.Clamp(point.Y, margin, worldSize.Height - margin));
    }

    private static MapOffset Jitter(int seed, int salt, float xRange, float yRange)
    {
        var x = SeedNoise(unchecked((uint)(seed * 1103515245 + salt * 374761393)));
        var y = SeedNoise(unchecked((uint)(seed * 214013 + salt * 668265263)));
        return new MapOffset((x - 0.5f) * xRange, (y - 0.5f) * yRange);
    }

    private static float SeedNoise(uint seed)
    {
        seed ^= seed >> 16;
        seed *= 0x7feb352d;
        seed ^= seed >> 15;
        seed *= 0x846ca68b;
        seed ^= seed >> 16;
        return (seed & 0xffff) / 65535f;
    }
}
