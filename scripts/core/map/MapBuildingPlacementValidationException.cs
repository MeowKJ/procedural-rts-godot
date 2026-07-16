namespace ProceduralRts.Core;

public sealed class MapBuildingPlacementValidationException : InvalidOperationException
{
    public MapBuildingPlacementValidationException(
        string mapId,
        IReadOnlyList<MapBuildingPlacementConflict> conflicts)
        : base(MessageFor(mapId, conflicts))
    {
        MapId = mapId;
        Conflicts = Array.AsReadOnly(conflicts.ToArray());
    }

    public string MapId { get; }

    public IReadOnlyList<MapBuildingPlacementConflict> Conflicts { get; }

    private static string MessageFor(
        string mapId,
        IReadOnlyList<MapBuildingPlacementConflict> conflicts)
    {
        return $"Map '{mapId}' has invalid building placement: {string.Join("; ", conflicts)}";
    }
}
