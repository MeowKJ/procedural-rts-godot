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

public static class MapOwnerTopologyValidator
{
    public static void EnsureValid(MapSpec map)
    {
        var diagnostics = new List<string>();
        var counts = new SortedDictionary<int, int>();
        foreach (var start in map.OwnerStarts)
        {
            counts[start.OwnerId.Value] = counts.GetValueOrDefault(start.OwnerId.Value) + 1;
        }

        RequireExactlyOne(1, counts, diagnostics);
        RequireExactlyOne(2, counts, diagnostics);
        foreach (var pair in counts)
        {
            if (pair.Key is not (1 or 2))
            {
                diagnostics.Add($"owner_start owner={pair.Key} count={pair.Value} unsupported");
            }
        }

        for (var index = 0; index < map.Buildings.Count; index++)
        {
            var building = map.Buildings[index];
            if (!HasExactlyOneStart(building.OwnerId.Value, counts))
            {
                diagnostics.Add($"building index={index} kind={building.Kind} owner={building.OwnerId.Value} missing_unique_start");
            }
        }

        for (var index = 0; index < map.Units.Count; index++)
        {
            var unit = map.Units[index];
            if (!HasExactlyOneStart(unit.OwnerId.Value, counts))
            {
                diagnostics.Add($"unit index={index} design={unit.DesignId} owner={unit.OwnerId.Value} missing_unique_start");
            }
        }

        if (diagnostics.Count > 0)
        {
            throw new MapOwnerTopologyValidationException(map.Id, diagnostics);
        }
    }

    private static void RequireExactlyOne(
        int ownerId,
        IReadOnlyDictionary<int, int> counts,
        List<string> diagnostics)
    {
        var count = counts.GetValueOrDefault(ownerId);
        if (count != 1)
        {
            diagnostics.Add($"owner_start owner={ownerId} count={count} expected=1");
        }
    }

    private static bool HasExactlyOneStart(int ownerId, IReadOnlyDictionary<int, int> counts)
    {
        return ownerId is 1 or 2 && counts.GetValueOrDefault(ownerId) == 1;
    }
}
