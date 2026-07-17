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

public enum MapSemanticConflictKind { CatalogUnknown, IdEmpty, IdDuplicate, LegacyInvalid, LegacyDuplicate, ReferenceMissing }
public sealed record MapSemanticConflict(
    MapSemanticConflictKind Kind,
    string LegacyText,
    MapValidationSource Source,
    MapValidationSource? Conflict = null);

public static class MapSemanticValidator
{
    public static void EnsureValid(MapSpec map)
    {
        var conflicts = Validate(map);
        if (conflicts.Count > 0)
        {
            throw new MapSemanticValidationException(map.Id, conflicts.Select(value => value.LegacyText).ToArray());
        }
    }

    public static IReadOnlyList<MapSemanticConflict> Validate(MapSpec map)
    {
        var conflicts = new List<MapSemanticConflict>();
        ValidateCatalogs(map, conflicts);
        ValidateSemanticIds(map, conflicts);
        ValidateLegacyBuildingIds(map, conflicts);
        return conflicts.AsReadOnly();
    }

    private static void ValidateCatalogs(MapSpec map, List<MapSemanticConflict> conflicts)
    {
        for (var index = 0; index < map.OwnerStarts.Count; index++)
        {
            var start = map.OwnerStarts[index];
            if (!FactionCatalog.Definitions.ContainsKey(start.Faction))
            {
                conflicts.Add(new MapSemanticConflict(MapSemanticConflictKind.CatalogUnknown,
                    $"owner_start index={index} faction={(int)start.Faction} unknown_catalog_id",
                    Source(MapValidationSourceKind.OwnerStart, index, start.OwnerId.Value.ToString())));
            }
        }

        for (var index = 0; index < map.Buildings.Count; index++)
        {
            var building = map.Buildings[index];
            if (!BuildSpecCatalog.Definitions.ContainsKey(building.Kind))
            {
                conflicts.Add(new MapSemanticConflict(MapSemanticConflictKind.CatalogUnknown,
                    $"building index={index} kind={building.Kind} unknown_catalog_id",
                    Source(MapValidationSourceKind.Building, index, building.Kind)));
            }

            if (!FactionCatalog.Definitions.ContainsKey(building.Faction))
            {
                conflicts.Add(new MapSemanticConflict(MapSemanticConflictKind.CatalogUnknown,
                    $"building index={index} faction={(int)building.Faction} unknown_catalog_id",
                    Source(MapValidationSourceKind.Building, index, building.Kind)));
            }
        }

        for (var index = 0; index < map.Units.Count; index++)
        {
            var unit = map.Units[index];
            if (!UnitDesignCatalog.Designs.ContainsKey(unit.DesignId))
            {
                conflicts.Add(new MapSemanticConflict(MapSemanticConflictKind.CatalogUnknown,
                    $"unit index={index} design={unit.DesignId} unknown_catalog_id",
                    Source(MapValidationSourceKind.Unit, index, unit.DesignId)));
            }
        }
    }

    private static void ValidateSemanticIds(MapSpec map, List<MapSemanticConflict> conflicts)
    {
        var seen = new Dictionary<string, (string Location, MapValidationSource Source)>(StringComparer.Ordinal);
        AddIds(map.Resources.Select(item => item.Id), "resource", MapValidationSourceKind.Resource, seen, conflicts);
        AddIds(map.Obstacles.Select(item => item.Id), "obstacle", MapValidationSourceKind.Obstacle, seen, conflicts);
        AddIds(map.TerrainCells.Select(item => item.Id), "terrain", MapValidationSourceKind.Terrain, seen, conflicts);
        AddIds(map.Triggers.Select(item => item.Id), "trigger", MapValidationSourceKind.Trigger, seen, conflicts);
        AddIds(map.Objectives.Select(item => item.Id), "objective", MapValidationSourceKind.Objective, seen, conflicts);
        AddIds(map.NarrativeNodes.Select(item => item.Id), "narrative", MapValidationSourceKind.Narrative, seen, conflicts);

        var triggerIds = map.Triggers.Select(trigger => trigger.Id).ToHashSet(StringComparer.Ordinal);
        for (var index = 0; index < map.NarrativeNodes.Count; index++)
        {
            var triggerId = map.NarrativeNodes[index].TriggerId;
            if (triggerId is not null && !triggerIds.Contains(triggerId))
            {
                conflicts.Add(new MapSemanticConflict(MapSemanticConflictKind.ReferenceMissing,
                    $"narrative index={index} trigger={triggerId} missing_reference",
                    Source(MapValidationSourceKind.Narrative, index, map.NarrativeNodes[index].Id)));
            }
        }
    }

    private static void AddIds(
        IEnumerable<string> ids,
        string kind,
        MapValidationSourceKind sourceKind,
        Dictionary<string, (string Location, MapValidationSource Source)> seen,
        List<MapSemanticConflict> conflicts)
    {
        var index = 0;
        foreach (var id in ids)
        {
            var location = $"{kind} index={index}";
            var source = Source(sourceKind, index, id);
            if (string.IsNullOrWhiteSpace(id))
            {
                conflicts.Add(new MapSemanticConflict(MapSemanticConflictKind.IdEmpty, $"{location} empty_id", source));
            }
            else if (seen.TryGetValue(id, out var first))
            {
                conflicts.Add(new MapSemanticConflict(MapSemanticConflictKind.IdDuplicate,
                    $"{location} id={id} duplicate_of=[{first.Location}]", source, first.Source));
            }
            else
            {
                seen[id] = (location, source);
            }

            index++;
        }
    }

    private static void ValidateLegacyBuildingIds(MapSpec map, List<MapSemanticConflict> conflicts)
    {
        var seen = new Dictionary<int, MapValidationSource>();
        for (var index = 0; index < map.Buildings.Count; index++)
        {
            if (map.Buildings[index].LegacyId is not { } id)
            {
                continue;
            }

            if (id <= 0)
            {
                conflicts.Add(new MapSemanticConflict(MapSemanticConflictKind.LegacyInvalid,
                    $"building index={index} legacy_id={id} expected_positive",
                    Source(MapValidationSourceKind.Building, index, map.Buildings[index].Kind)));
            }
            else if (seen.TryGetValue(id, out var first))
            {
                conflicts.Add(new MapSemanticConflict(MapSemanticConflictKind.LegacyDuplicate,
                    $"building index={index} legacy_id={id} duplicate",
                    Source(MapValidationSourceKind.Building, index, map.Buildings[index].Kind), first));
            }
            else
            {
                seen[id] = Source(MapValidationSourceKind.Building, index, map.Buildings[index].Kind);
            }
        }
    }

    private static MapValidationSource Source(MapValidationSourceKind kind, int index, string id)
    {
        return new MapValidationSource(kind, index, id);
    }
}
