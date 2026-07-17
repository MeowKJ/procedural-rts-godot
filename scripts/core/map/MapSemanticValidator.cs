namespace ProceduralRts.Core;

public sealed class MapSemanticValidationException : InvalidOperationException
{
    public MapSemanticValidationException(string mapId, IReadOnlyList<string> diagnostics)
        : base($"Map '{mapId}' has invalid semantic data: {string.Join("; ", diagnostics)}")
    {
        MapId = mapId;
        Diagnostics = Array.AsReadOnly(diagnostics.ToArray());
    }

    public string MapId { get; }

    public IReadOnlyList<string> Diagnostics { get; }
}

public static class MapSemanticValidator
{
    public static void EnsureValid(MapSpec map)
    {
        var diagnostics = new List<string>();
        ValidateCatalogs(map, diagnostics);
        ValidateSemanticIds(map, diagnostics);
        ValidateLegacyBuildingIds(map, diagnostics);
        if (diagnostics.Count > 0)
        {
            throw new MapSemanticValidationException(map.Id, diagnostics);
        }
    }

    private static void ValidateCatalogs(MapSpec map, List<string> diagnostics)
    {
        for (var index = 0; index < map.OwnerStarts.Count; index++)
        {
            var start = map.OwnerStarts[index];
            if (!FactionCatalog.Definitions.ContainsKey(start.Faction))
            {
                diagnostics.Add($"owner_start index={index} faction={(int)start.Faction} unknown_catalog_id");
            }
        }

        for (var index = 0; index < map.Buildings.Count; index++)
        {
            var building = map.Buildings[index];
            if (!BuildSpecCatalog.Definitions.ContainsKey(building.Kind))
            {
                diagnostics.Add($"building index={index} kind={building.Kind} unknown_catalog_id");
            }

            if (!FactionCatalog.Definitions.ContainsKey(building.Faction))
            {
                diagnostics.Add($"building index={index} faction={(int)building.Faction} unknown_catalog_id");
            }
        }

        for (var index = 0; index < map.Units.Count; index++)
        {
            var unit = map.Units[index];
            if (!UnitDesignCatalog.Designs.ContainsKey(unit.DesignId))
            {
                diagnostics.Add($"unit index={index} design={unit.DesignId} unknown_catalog_id");
            }
        }
    }

    private static void ValidateSemanticIds(MapSpec map, List<string> diagnostics)
    {
        var seen = new Dictionary<string, string>(StringComparer.Ordinal);
        AddIds(map.Resources.Select(item => item.Id), "resource", seen, diagnostics);
        AddIds(map.Obstacles.Select(item => item.Id), "obstacle", seen, diagnostics);
        AddIds(map.TerrainCells.Select(item => item.Id), "terrain", seen, diagnostics);
        AddIds(map.Triggers.Select(item => item.Id), "trigger", seen, diagnostics);
        AddIds(map.Objectives.Select(item => item.Id), "objective", seen, diagnostics);
        AddIds(map.NarrativeNodes.Select(item => item.Id), "narrative", seen, diagnostics);

        var triggerIds = map.Triggers.Select(trigger => trigger.Id).ToHashSet(StringComparer.Ordinal);
        for (var index = 0; index < map.NarrativeNodes.Count; index++)
        {
            var triggerId = map.NarrativeNodes[index].TriggerId;
            if (triggerId is not null && !triggerIds.Contains(triggerId))
            {
                diagnostics.Add($"narrative index={index} trigger={triggerId} missing_reference");
            }
        }
    }

    private static void AddIds(
        IEnumerable<string> ids,
        string kind,
        Dictionary<string, string> seen,
        List<string> diagnostics)
    {
        var index = 0;
        foreach (var id in ids)
        {
            var location = $"{kind} index={index}";
            if (string.IsNullOrWhiteSpace(id))
            {
                diagnostics.Add($"{location} empty_id");
            }
            else if (seen.TryGetValue(id, out var first))
            {
                diagnostics.Add($"{location} id={id} duplicate_of=[{first}]");
            }
            else
            {
                seen[id] = location;
            }

            index++;
        }
    }

    private static void ValidateLegacyBuildingIds(MapSpec map, List<string> diagnostics)
    {
        var seen = new HashSet<int>();
        for (var index = 0; index < map.Buildings.Count; index++)
        {
            if (map.Buildings[index].LegacyId is not { } id)
            {
                continue;
            }

            if (id <= 0)
            {
                diagnostics.Add($"building index={index} legacy_id={id} expected_positive");
            }
            else if (!seen.Add(id))
            {
                diagnostics.Add($"building index={index} legacy_id={id} duplicate");
            }
        }
    }
}
