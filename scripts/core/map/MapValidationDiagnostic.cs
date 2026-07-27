namespace ProceduralRts.Core;

public enum MapValidationSeverity { Error, Warning, Info }
public enum MapValidationPhase { Authoring, Projection, Owner, Semantic, Environment, Placement, Reachability }
public enum MapValidationSourceKind
{
    Root, OwnerStart, Building, Unit, Resource, Obstacle, Terrain, Trigger, Objective, Narrative,
}

public sealed record MapValidationSource(
    MapValidationSourceKind Kind,
    int Index,
    string Id,
    int SceneOrder = int.MaxValue,
    string Path = "")
{
    public int StableOrder => SceneOrder == int.MaxValue ? (int)Kind * 100_000 + Math.Max(0, Index) : SceneOrder;
}

public sealed record MapValidationDiagnostic(
    MapValidationSeverity Severity,
    string Code,
    MapValidationPhase Phase,
    MapValidationSource Source,
    string Message,
    MapValidationSource? Conflict = null)
{
    public int CodeRank => MapValidationCodes.Rank(Code);
}

public static class MapValidationCodes
{
    public const string CatalogUnknown = "map.catalog.unknown";
    public const string IdEmpty = "map.id.empty";
    public const string IdDuplicate = "map.id.duplicate";
    public const string RuntimeIdInvalid = "map.id.runtime_invalid";
    public const string RuntimeIdDuplicate = "map.id.runtime_duplicate";
    public const string OwnerStartCount = "map.owner.start_count";
    public const string OwnerUnsupported = "map.owner.unsupported";
    public const string OwnerReference = "map.owner.reference";
    public const string WorldInvalidSize = "map.world.invalid_size";
    public const string GeometryInvalidRect = "map.geometry.invalid_rect";
    public const string GeometryInvalidCircle = "map.geometry.invalid_circle";
    public const string GeometryInvalidCost = "map.geometry.invalid_cost";
    public const string GeometryUnrepresentableTransform = "map.geometry.unrepresentable_transform";
    public const string BoundsOutside = "map.bounds.outside";
    public const string GridUnsnapped = "map.grid.unsnapped";
    public const string RotationNonCardinal = "map.rotation.non_cardinal";
    public const string BuildingOverlap = "map.building.overlap";
    public const string BuildingClearance = "map.building.clearance";
    public const string BuildingReserved = "map.building.reserved";
    public const string BuildingTerrain = "map.building.terrain";
    public const string BuildingStaticObstacle = "map.building.static_obstacle";
    public const string BuildingResource = "map.building.resource";
    public const string ReferenceMissing = "map.reference.missing";
    public const string ReachabilityOwnerStart = "map.reachability.owner_start";

    public static IReadOnlyList<string> Ordered { get; } = Array.AsReadOnly(new[]
    {
        CatalogUnknown, IdEmpty, IdDuplicate, RuntimeIdInvalid, RuntimeIdDuplicate,
        OwnerStartCount, OwnerUnsupported, OwnerReference, WorldInvalidSize,
        GeometryInvalidRect, GeometryInvalidCircle, GeometryInvalidCost,
        GeometryUnrepresentableTransform, BoundsOutside, GridUnsnapped,
        RotationNonCardinal, BuildingOverlap, BuildingClearance, BuildingReserved,
        BuildingTerrain, BuildingStaticObstacle, BuildingResource, ReferenceMissing,
        ReachabilityOwnerStart,
    });

    private static readonly IReadOnlyDictionary<string, int> Ranks = Ordered
        .Select((code, index) => (code, index))
        .ToDictionary(item => item.code, item => item.index, StringComparer.Ordinal);

    public static int Rank(string code) => Ranks.TryGetValue(code, out var rank) ? rank : int.MaxValue;
}

public static class MapValidationOrdering
{
    public static IReadOnlyList<MapValidationDiagnostic> Sort(IEnumerable<MapValidationDiagnostic> diagnostics)
    {
        return Array.AsReadOnly(diagnostics
            .OrderBy(value => value.Severity)
            .ThenBy(value => value.CodeRank)
            .ThenBy(value => value.Source.StableOrder)
            .ThenBy(value => value.Conflict?.StableOrder ?? int.MaxValue)
            .ToArray());
    }
}

public static class MapValidationText
{
    public static string Value(object? value)
    {
        var text = Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture) ?? "";
        return Clip(text, 48);
    }

    public static string Message(string text) => Clip(text, 240);

    private static string Clip(string text, int limit)
    {
        return text.Length <= limit ? text : text[..Math.Max(0, limit - 1)] + "…";
    }
}
