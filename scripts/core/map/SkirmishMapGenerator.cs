using Godot;

namespace ProceduralRts.Core;

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
        var playerStart = spec.StartFor(new OwnerId(1)).Position.ToVector2();
        var enemyStart = spec.StartFor(new OwnerId(2)).Position.ToVector2();
        var withLoadout = spec with
        {
            Buildings = StandardBuildings(Owner.Player, config.PlayerFaction, playerStart)
                .Concat(StandardBuildings(Owner.Enemy, config.AiFaction, enemyStart))
                .ToArray(),
            Units = StandardUnits(Owner.Player, config.PlayerFaction, playerStart)
                .Concat(StandardUnits(Owner.Enemy, config.AiFaction, enemyStart))
                .ToArray(),
        };
        return SkirmishStartingEnvironmentAdapter.Apply(withLoadout);
    }

    public static Vector2 Mirror(Vector2 point, Vector2 worldSize)
    {
        return SkirmishMapSpecGenerator
            .Mirror(point.ToMapPoint(), worldSize.ToMapSize())
            .ToVector2();
    }

    private static IEnumerable<MapBuildingSeedSpec> StandardBuildings(Owner owner, FactionId faction, Vector2 start)
    {
        var ownerId = OwnerIdFor(owner);
        return MatchStartLoadouts.For(owner, faction, start)
            .Buildings
            .Select(building => new MapBuildingSeedSpec(
                building.Kind,
                ownerId,
                faction,
                building.Position.ToMapPoint(),
                building.Facing));
    }

    private static IEnumerable<MapUnitSeedSpec> StandardUnits(Owner owner, FactionId faction, Vector2 start)
    {
        var ownerId = OwnerIdFor(owner);
        return MatchStartLoadouts.For(owner, faction, start)
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
