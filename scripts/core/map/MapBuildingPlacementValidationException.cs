namespace ProceduralRts.Core;

public sealed class MapBuildingPlacementValidationException : InvalidOperationException
{
    public MapBuildingPlacementValidationException(
        string mapId,
        IReadOnlyList<MapBuildingPlacementConflict> conflicts)
        : this(mapId, conflicts, Array.Empty<MapEnvironmentValidationConflict>())
    {
    }

    public MapBuildingPlacementValidationException(
        string mapId,
        IReadOnlyList<MapBuildingPlacementConflict> conflicts,
        IReadOnlyList<MapEnvironmentValidationConflict> environmentConflicts)
        : base(MessageFor(mapId, conflicts, environmentConflicts))
    {
        MapId = mapId;
        Conflicts = Array.AsReadOnly(conflicts.ToArray());
        EnvironmentConflicts = Array.AsReadOnly(environmentConflicts.ToArray());
    }

    public string MapId { get; }

    public IReadOnlyList<MapBuildingPlacementConflict> Conflicts { get; }

    public IReadOnlyList<MapEnvironmentValidationConflict> EnvironmentConflicts { get; }

    private static string MessageFor(
        string mapId,
        IReadOnlyList<MapBuildingPlacementConflict> conflicts,
        IReadOnlyList<MapEnvironmentValidationConflict> environmentConflicts)
    {
        var diagnostics = environmentConflicts.Cast<object>().Concat(conflicts.Cast<object>());
        return $"Map '{mapId}' has invalid placement: {string.Join("; ", diagnostics)}";
    }
}
