using Godot;

namespace ProceduralRts.Core;

public sealed record SkirmishResourceNode(Vector2 Position, float Radius, int Amount, Color Accent);

public sealed record SkirmishMapLayout(
    Vector2 WorldSize,
    Vector2 PlayerStart,
    Vector2 EnemyStart,
    IReadOnlyList<SkirmishResourceNode> Resources,
    IReadOnlyList<PlacementObstacle> Obstacles);

public static class SkirmishMapGenerator
{
    public static MapSpec GenerateSpec(MatchConfig config)
    {
        var spec = SkirmishMapSpecGenerator.Generate(new SkirmishMapRequest(
            config.MapSeed,
            config.StartingCredits,
            config.WorldSize.ToMapSize(),
            config.PlayerFaction,
            config.AiFaction,
            SkirmishOptions.DefaultMapSeed));
        var layout = spec.ToSkirmishMapLayout();
        return spec with
        {
            Buildings = StandardBuildings(Owner.Player, config.PlayerFaction, layout)
                .Concat(StandardBuildings(Owner.Enemy, config.AiFaction, layout))
                .ToArray(),
            Units = StandardUnits(Owner.Player, config.PlayerFaction, layout)
                .Concat(StandardUnits(Owner.Enemy, config.AiFaction, layout))
                .ToArray(),
        };
    }

    public static SkirmishMapLayout Generate(MatchConfig config)
    {
        return GenerateSpec(config).ToSkirmishMapLayout();
    }

    public static Vector2 Mirror(Vector2 point, Vector2 worldSize)
    {
        return SkirmishMapSpecGenerator
            .Mirror(point.ToMapPoint(), worldSize.ToMapSize())
            .ToVector2();
    }

    private static IEnumerable<MapBuildingSeedSpec> StandardBuildings(Owner owner, FactionId faction, SkirmishMapLayout layout)
    {
        var ownerId = OwnerIdFor(owner);
        return MatchStartLoadouts.For(owner, faction, layout)
            .Buildings
            .Select(building => new MapBuildingSeedSpec(
                building.Kind,
                ownerId,
                faction,
                building.Position.ToMapPoint(),
                building.Facing));
    }

    private static IEnumerable<MapUnitSeedSpec> StandardUnits(Owner owner, FactionId faction, SkirmishMapLayout layout)
    {
        var ownerId = OwnerIdFor(owner);
        return MatchStartLoadouts.For(owner, faction, layout)
            .Units
            .Select(unit => new MapUnitSeedSpec(
                unit.DesignId,
                ownerId,
                unit.Position.ToMapPoint(),
                unit.Facing));
    }

    private static OwnerId OwnerIdFor(Owner owner)
    {
        return owner == Owner.Player ? new OwnerId(1) : new OwnerId(2);
    }
}
