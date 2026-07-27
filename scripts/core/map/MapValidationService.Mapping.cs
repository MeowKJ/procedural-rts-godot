namespace ProceduralRts.Core;

public static partial class MapValidationService
{
    private static MapValidationDiagnostic Owner(MapOwnerTopologyConflict conflict)
    {
        var code = conflict.Kind switch
        {
            MapOwnerTopologyConflictKind.StartCount => MapValidationCodes.OwnerStartCount,
            MapOwnerTopologyConflictKind.Unsupported => MapValidationCodes.OwnerUnsupported,
            _ => MapValidationCodes.OwnerReference,
        };
        return Diagnostic(code, MapValidationPhase.Owner, conflict.Source, conflict.Message);
    }

    private static MapValidationDiagnostic Semantic(MapSemanticConflict conflict)
    {
        var code = conflict.Kind switch
        {
            MapSemanticConflictKind.CatalogUnknown => MapValidationCodes.CatalogUnknown,
            MapSemanticConflictKind.IdEmpty => MapValidationCodes.IdEmpty,
            MapSemanticConflictKind.IdDuplicate => MapValidationCodes.IdDuplicate,
            MapSemanticConflictKind.RuntimeIdInvalid => MapValidationCodes.RuntimeIdInvalid,
            MapSemanticConflictKind.RuntimeIdDuplicate => MapValidationCodes.RuntimeIdDuplicate,
            _ => MapValidationCodes.ReferenceMissing,
        };
        return Diagnostic(code, MapValidationPhase.Semantic, conflict.Source, conflict.Message, conflict.Conflict);
    }

    private static MapValidationDiagnostic Environment(MapSpec map, MapEnvironmentValidationConflict conflict)
    {
        var source = EnvironmentSource(map, conflict.Target);
        var code = conflict.Detail switch
        {
            "invalid_size" => MapValidationCodes.WorldInvalidSize,
            "invalid_rect" => MapValidationCodes.GeometryInvalidRect,
            "invalid_circle" => MapValidationCodes.GeometryInvalidCircle,
            "invalid_movement_cost" => MapValidationCodes.GeometryInvalidCost,
            _ => MapValidationCodes.BoundsOutside,
        };
        return Diagnostic(code, MapValidationPhase.Environment, source, conflict.Detail);
    }

    private static MapValidationDiagnostic Placement(MapSpec map, MapBuildingPlacementConflict conflict)
    {
        var code = conflict.Conflict switch
        {
            MapBuildingPlacementConflictKind.Rotation => MapValidationCodes.RotationNonCardinal,
            MapBuildingPlacementConflictKind.Unsnapped => MapValidationCodes.GridUnsnapped,
            MapBuildingPlacementConflictKind.Outside => MapValidationCodes.BoundsOutside,
            MapBuildingPlacementConflictKind.Terrain => MapValidationCodes.BuildingTerrain,
            MapBuildingPlacementConflictKind.StaticObstacle => MapValidationCodes.BuildingStaticObstacle,
            MapBuildingPlacementConflictKind.Resource => MapValidationCodes.BuildingResource,
            MapBuildingPlacementConflictKind.Overlap => MapValidationCodes.BuildingOverlap,
            MapBuildingPlacementConflictKind.Clearance => MapValidationCodes.BuildingClearance,
            _ => MapValidationCodes.BuildingReserved,
        };
        var source = BuildingSource(map, conflict.BuildingIndex);
        var other = conflict.Other is not null
            ? BuildingSource(map, conflict.OtherIndex ?? -1)
            : conflict.Target is not null ? EnvironmentSource(map, conflict.Target) : null;
        return Diagnostic(code, MapValidationPhase.Placement, source, conflict.Conflict.ToString(), other);
    }

    private static MapValidationDiagnostic Reachability(MapReachabilityConflict conflict)
    {
        return Diagnostic(
            MapValidationCodes.ReachabilityOwnerStart,
            MapValidationPhase.Reachability,
            conflict.Source,
            "unreachable",
            conflict.Conflict);
    }

    private static MapValidationSource BuildingSource(MapSpec map, int index)
    {
        if ((uint)index >= (uint)map.Buildings.Count)
        {
            throw new InvalidOperationException($"Placement conflict carried invalid building index {index}.");
        }
        return new MapValidationSource(MapValidationSourceKind.Building, index, map.Buildings[index].Kind);
    }

    private static MapValidationSource EnvironmentSource(MapSpec map, MapPlacementConflictTarget target)
    {
        return target.Kind switch
        {
            MapEnvironmentObjectKind.World => new(MapValidationSourceKind.Root, 0, map.Id),
            MapEnvironmentObjectKind.Terrain => Source(MapValidationSourceKind.Terrain, map.TerrainCells.Select(value => value.Id), target.Id),
            MapEnvironmentObjectKind.StaticObstacle => Source(MapValidationSourceKind.Obstacle, map.Obstacles.Select(value => value.Id), target.Id),
            MapEnvironmentObjectKind.Resource => Source(MapValidationSourceKind.Resource, map.Resources.Select(value => value.Id), target.Id),
            _ => new MapValidationSource(MapValidationSourceKind.Root, 0, map.Id),
        };
    }

    private static MapValidationSource Source(MapValidationSourceKind kind, IEnumerable<string> ids, string id)
    {
        var values = ids.ToArray();
        var index = Array.FindIndex(values, value => value == id);
        return new MapValidationSource(kind, Math.Max(0, index), id);
    }
}
