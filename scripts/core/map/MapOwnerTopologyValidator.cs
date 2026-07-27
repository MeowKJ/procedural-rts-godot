namespace ProceduralRts.Core;

public sealed class MapOwnerTopologyValidationException : InvalidOperationException
{
    public MapOwnerTopologyValidationException(string mapId, IReadOnlyList<string> diagnostics)
        : base($"Map '{mapId}' has invalid owner topology: {string.Join("; ", diagnostics)}")
    {
        MapId = mapId;
        Diagnostics = Array.AsReadOnly(diagnostics.ToArray());
    }

    public string MapId { get; }

    public IReadOnlyList<string> Diagnostics { get; }
}

public enum MapOwnerTopologyConflictKind { StartCount, Unsupported, Reference }
public sealed record MapOwnerTopologyConflict(
    MapOwnerTopologyConflictKind Kind,
    string Message,
    MapValidationSource Source);

public static class MapOwnerTopologyValidator
{
    public static void EnsureValid(MapSpec map)
    {
        var conflicts = Validate(map);
        if (conflicts.Count > 0)
        {
            throw new MapOwnerTopologyValidationException(map.Id, conflicts.Select(value => value.Message).ToArray());
        }
    }

    public static IReadOnlyList<MapOwnerTopologyConflict> Validate(MapSpec map)
    {
        var conflicts = new List<MapOwnerTopologyConflict>();
        var counts = new SortedDictionary<int, int>();
        foreach (var start in map.OwnerStarts)
        {
            counts[start.OwnerId.Value] = counts.GetValueOrDefault(start.OwnerId.Value) + 1;
        }

        RequireExactlyOne(map, 1, counts, conflicts);
        RequireExactlyOne(map, 2, counts, conflicts);
        foreach (var pair in counts)
        {
            if (pair.Key is not (1 or 2))
            {
                var index = map.OwnerStarts.Select((value, index) => (value, index))
                    .First(value => value.value.OwnerId.Value == pair.Key).index;
                conflicts.Add(new MapOwnerTopologyConflict(MapOwnerTopologyConflictKind.Unsupported,
                    $"owner_start owner={pair.Key} count={pair.Value} unsupported",
                    Source(MapValidationSourceKind.OwnerStart, index, pair.Key.ToString())));
            }
        }

        for (var index = 0; index < map.Buildings.Count; index++)
        {
            var building = map.Buildings[index];
            if (!HasExactlyOneStart(building.OwnerId.Value, counts))
            {
                conflicts.Add(new MapOwnerTopologyConflict(MapOwnerTopologyConflictKind.Reference,
                    $"building index={index} kind={building.Kind} owner={building.OwnerId.Value} missing_unique_start",
                    Source(MapValidationSourceKind.Building, index, building.Kind)));
            }
        }

        for (var index = 0; index < map.Units.Count; index++)
        {
            var unit = map.Units[index];
            if (!HasExactlyOneStart(unit.OwnerId.Value, counts))
            {
                conflicts.Add(new MapOwnerTopologyConflict(MapOwnerTopologyConflictKind.Reference,
                    $"unit index={index} design={unit.DesignId} owner={unit.OwnerId.Value} missing_unique_start",
                    Source(MapValidationSourceKind.Unit, index, unit.DesignId)));
            }
        }

        return conflicts.AsReadOnly();
    }

    private static void RequireExactlyOne(
        MapSpec map,
        int ownerId,
        IReadOnlyDictionary<int, int> counts,
        List<MapOwnerTopologyConflict> conflicts)
    {
        var count = counts.GetValueOrDefault(ownerId);
        if (count != 1)
        {
            conflicts.Add(new MapOwnerTopologyConflict(MapOwnerTopologyConflictKind.StartCount,
                $"owner_start owner={ownerId} count={count} expected=1",
                Source(MapValidationSourceKind.Root, 0, map.Id)));
        }
    }

    private static bool HasExactlyOneStart(int ownerId, IReadOnlyDictionary<int, int> counts)
    {
        return ownerId is 1 or 2 && counts.GetValueOrDefault(ownerId) == 1;
    }

    private static MapValidationSource Source(MapValidationSourceKind kind, int index, string id)
    {
        return new MapValidationSource(kind, index, id);
    }
}
