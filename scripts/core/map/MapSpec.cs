namespace ProceduralRts.Core;

public readonly record struct MapSize(float Width, float Height);

public readonly record struct MapPoint(float X, float Y)
{
    public static MapPoint operator +(MapPoint point, MapOffset offset)
    {
        return new MapPoint(point.X + offset.X, point.Y + offset.Y);
    }

    public static MapOffset operator -(MapPoint left, MapPoint right)
    {
        return new MapOffset(left.X - right.X, left.Y - right.Y);
    }
}

public readonly record struct MapOffset(float X, float Y);

public readonly record struct MapRect(float X, float Y, float Width, float Height);

public readonly record struct MapColor(string Hex);

public sealed record MapOwnerStartSpec(
    OwnerId OwnerId,
    FactionId Faction,
    MapPoint Position,
    float Facing,
    int StartingCredits);

public sealed record MapUnitSeedSpec(
    string DesignId,
    OwnerId OwnerId,
    MapPoint Position,
    float Facing = 0);

public sealed record MapBuildingSeedSpec(
    string Kind,
    OwnerId OwnerId,
    FactionId Faction,
    MapPoint Position,
    float Facing = 0,
    float? Hp = null,
    float BuildProgress = 1,
    int? LegacyId = null);

public sealed record MapResourceNodeSpec(
    string Id,
    MapPoint Position,
    float Radius,
    int Amount,
    MapColor Accent);

public sealed record MapObstacleSpec(string Id, MapRect Bounds);

public sealed record MapTerrainCellSpec(
    string Id,
    MapRect Bounds,
    string TerrainId,
    float MovementCost = 1,
    bool BlocksLand = false);

public sealed record MapTriggerAreaSpec(string Id, MapRect Bounds, string EventKey);

public sealed record MapObjectiveNodeSpec(
    string Id,
    MapPoint Position,
    string ObjectiveKey,
    bool Primary = true);

public sealed record MapNarrativeNodeSpec(
    string Id,
    MapPoint Position,
    string TextKey,
    string? TriggerId = null);

public sealed record MapSpec
{
    public required string Id { get; init; }
    public required int Seed { get; init; }
    public required MapSize WorldSize { get; init; }
    public IReadOnlyList<MapOwnerStartSpec> OwnerStarts { get; init; } = [];
    public IReadOnlyList<MapTerrainCellSpec> TerrainCells { get; init; } = [];
    public IReadOnlyList<MapResourceNodeSpec> Resources { get; init; } = [];
    public IReadOnlyList<MapObstacleSpec> Obstacles { get; init; } = [];
    public IReadOnlyList<MapBuildingSeedSpec> Buildings { get; init; } = [];
    public IReadOnlyList<MapUnitSeedSpec> Units { get; init; } = [];
    public IReadOnlyList<MapTriggerAreaSpec> Triggers { get; init; } = [];
    public IReadOnlyList<MapObjectiveNodeSpec> Objectives { get; init; } = [];
    public IReadOnlyList<MapNarrativeNodeSpec> NarrativeNodes { get; init; } = [];

    public MapOwnerStartSpec StartFor(OwnerId ownerId)
    {
        return OwnerStarts.First(start => start.OwnerId == ownerId);
    }
}
