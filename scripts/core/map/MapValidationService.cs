namespace ProceduralRts.Core;

public static partial class MapValidationService
{
    public static IReadOnlyList<MapValidationDiagnostic> Validate(MapSpec map)
    {
        var diagnostics = new List<MapValidationDiagnostic>();
        var owner = MapOwnerTopologyValidator.Validate(map);
        diagnostics.AddRange(owner.Select(Owner));
        var semantic = MapSemanticValidator.Validate(map);
        diagnostics.AddRange(semantic.Select(Semantic));
        if (owner.Count > 0 || semantic.Count > 0) return MapValidationOrdering.Sort(diagnostics);

        var environment = MapEnvironmentSpecValidator.Validate(map);
        diagnostics.AddRange(environment.Select(value => Environment(map, value)));
        if (environment.Count > 0) return MapValidationOrdering.Sort(diagnostics);

        var placement = MapBuildingPlacementValidator.Validate(map);
        diagnostics.AddRange(placement.Select(value => Placement(map, value)));
        if (placement.Count > 0) return MapValidationOrdering.Sort(diagnostics);

        diagnostics.AddRange(MapReachabilityValidator.Validate(map).Select(Reachability));
        return MapValidationOrdering.Sort(diagnostics);
    }

    public static MapValidationDiagnostic UnrepresentableTransform(
        MapValidationSource source,
        string detail)
    {
        return Diagnostic(
            MapValidationCodes.GeometryUnrepresentableTransform,
            MapValidationPhase.Authoring,
            source,
            detail);
    }

    public static MapValidationDiagnostic AuthoringDiagnostic(
        string code,
        MapValidationSource source,
        string detail)
    {
        if (MapValidationCodes.Rank(code) == int.MaxValue)
        {
            throw new ArgumentOutOfRangeException(nameof(code), code, "Unknown map validation code.");
        }
        return Diagnostic(code, MapValidationPhase.Authoring, source, detail);
    }

    private static MapValidationDiagnostic Diagnostic(
        string code,
        MapValidationPhase phase,
        MapValidationSource source,
        string detail,
        MapValidationSource? conflict = null)
    {
        source = Normalize(source);
        conflict = conflict is null ? null : Normalize(conflict);
        var message = conflict is null
            ? $"{code}: {MapValidationText.Value(detail)} at {source.Kind}:{source.Id}"
            : $"{code}: {MapValidationText.Value(detail)} at {source.Kind}:{source.Id} conflicts with {conflict.Kind}:{conflict.Id}";
        return new MapValidationDiagnostic(
            MapValidationSeverity.Error,
            code,
            phase,
            source,
            MapValidationText.Message(message),
            conflict);
    }

    private static MapValidationSource Normalize(MapValidationSource source)
    {
        return source with
        {
            Id = MapValidationText.Value(source.Id),
            Path = MapValidationText.Value(source.Path),
        };
    }
}
