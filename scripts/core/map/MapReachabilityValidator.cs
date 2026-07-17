namespace ProceduralRts.Core;

public sealed record MapReachabilityConflict(
    MapValidationSource Source,
    MapValidationSource Conflict);

public sealed class MapReachabilityValidationException : InvalidOperationException
{
    public MapReachabilityValidationException(string mapId, IReadOnlyList<MapReachabilityConflict> conflicts)
        : base($"Map '{mapId}' has unreachable owner starts: {string.Join(", ", conflicts.Select(value => $"{value.Source.Id}->{value.Conflict.Id}"))}")
    {
        Conflicts = Array.AsReadOnly(conflicts.ToArray());
    }

    public IReadOnlyList<MapReachabilityConflict> Conflicts { get; }
}

public static class MapReachabilityValidator
{
    public static IReadOnlyList<MapReachabilityConflict> Validate(MapSpec map)
    {
        if (map.WorldSize.Width <= 0 || map.WorldSize.Height <= 0) return [];
        var firstIndex = IndexOfUniqueStart(map, 1);
        var secondIndex = IndexOfUniqueStart(map, 2);
        if (firstIndex < 0 || secondIndex < 0) return [];
        var first = map.OwnerStarts[firstIndex];
        var second = map.OwnerStarts[secondIndex];
        var grid = PathfindingStaticGrid.Build(map, MovementDomain.Land);
        var result = PathfindingMath.FindPathWithDebug(
            first.Position.X, first.Position.Y,
            second.Position.X, second.Position.Y,
            map.WorldSize.Width, map.WorldSize.Height,
            PathfindingStaticGrid.RuntimeCellSize,
            grid.Obstacles, MovementDomain.Land, grid.Terrain);
        if (result.Reached) return [];
        return Array.AsReadOnly(new[]
        {
            new MapReachabilityConflict(
                new MapValidationSource(MapValidationSourceKind.OwnerStart, firstIndex, "1"),
                new MapValidationSource(MapValidationSourceKind.OwnerStart, secondIndex, "2")),
        });
    }

    public static void EnsureValid(MapSpec map)
    {
        var conflicts = Validate(map);
        if (conflicts.Count > 0) throw new MapReachabilityValidationException(map.Id, conflicts);
    }

    private static int IndexOfUniqueStart(MapSpec map, int ownerId)
    {
        var index = -1;
        for (var candidate = 0; candidate < map.OwnerStarts.Count; candidate++)
        {
            if (map.OwnerStarts[candidate].OwnerId.Value != ownerId) continue;
            if (index >= 0) return -1;
            index = candidate;
        }
        return index;
    }
}
