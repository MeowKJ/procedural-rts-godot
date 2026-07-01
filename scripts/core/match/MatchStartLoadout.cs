using Godot;

namespace ProceduralRts.Core;

public sealed record MatchStartBuilding(string Kind, Vector2 Position, float Facing = 0);

public sealed record MatchStartUnit(string DesignId, Vector2 Position, float Facing = 0);

public sealed record MatchStartOwnerLoadout(
    Owner Owner,
    FactionId Faction,
    IReadOnlyList<MatchStartBuilding> Buildings,
    IReadOnlyList<MatchStartUnit> Units);

public static class MatchStartLoadouts
{
    private static readonly Vector2 PlayerReferenceStart = new(505, 610);
    private static readonly Vector2 EnemyReferenceStart = new(2860, 1535);

    private static readonly IReadOnlyDictionary<FactionId, IReadOnlyList<string>> StartingBuildingsByFaction =
        new Dictionary<FactionId, IReadOnlyList<string>>
        {
            [FactionId.Dog] =
            [
                BuildingDesignIds.Headquarters,
                BuildingDesignIds.PowerPlant,
                BuildingDesignIds.Barracks,
                BuildingDesignIds.Refinery,
            ],
            [FactionId.Cat] =
            [
                BuildingDesignIds.Headquarters,
                BuildingDesignIds.PowerPlant,
                BuildingDesignIds.VehicleFactory,
                BuildingDesignIds.Refinery,
            ],
        };

    private static readonly IReadOnlyDictionary<Owner, IReadOnlyDictionary<string, (Vector2 Position, float Facing)>> BuildingSlots =
        new Dictionary<Owner, IReadOnlyDictionary<string, (Vector2 Position, float Facing)>>
        {
            [Owner.Player] = new Dictionary<string, (Vector2 Position, float Facing)>
            {
                [BuildingDesignIds.Headquarters] = (new Vector2(505, 610), 0),
                [BuildingDesignIds.PowerPlant] = (new Vector2(360, 790), -0.1f),
                [BuildingDesignIds.Barracks] = (new Vector2(520, 845), 0.1f),
                [BuildingDesignIds.VehicleFactory] = (new Vector2(650, 965), 0.06f),
                [BuildingDesignIds.Refinery] = (new Vector2(335, 520), 0.05f),
            },
            [Owner.Enemy] = new Dictionary<string, (Vector2 Position, float Facing)>
            {
                [BuildingDesignIds.Headquarters] = (new Vector2(2860, 1535), MathF.PI),
                [BuildingDesignIds.PowerPlant] = (new Vector2(3035, 1365), MathF.PI + 0.1f),
                [BuildingDesignIds.Barracks] = (new Vector2(2940, 1225), MathF.PI - 0.08f),
                [BuildingDesignIds.VehicleFactory] = (new Vector2(2810, 1290), MathF.PI - 0.08f),
                [BuildingDesignIds.Refinery] = (new Vector2(3130, 1635), MathF.PI),
            },
        };

    private static readonly IReadOnlyDictionary<Owner, IReadOnlyList<(Vector2 Position, float Facing)>> UnitSlots =
        new Dictionary<Owner, IReadOnlyList<(Vector2 Position, float Facing)>>
        {
            [Owner.Player] =
            [
                (new Vector2(720, 760), 0),
                (new Vector2(792, 798), 0.15f),
                (new Vector2(648, 804), -0.1f),
                (new Vector2(672, 888), 0),
                (new Vector2(722, 910), 0),
                (new Vector2(774, 892), 0),
                (new Vector2(850, 702), 0.2f),
            ],
            [Owner.Enemy] =
            [
                (new Vector2(2510, 1370), MathF.PI),
                (new Vector2(2590, 1425), MathF.PI),
                (new Vector2(2460, 1464), MathF.PI),
                (new Vector2(2710, 1300), MathF.PI),
                (new Vector2(2522, 1518), MathF.PI),
                (new Vector2(2650, 1488), MathF.PI),
                (new Vector2(2768, 1248), MathF.PI),
            ],
        };

    public static MatchStartOwnerLoadout For(Owner owner, FactionId faction)
    {
        var buildingSlots = BuildingSlots[owner];
        var unitSlots = UnitSlots[owner];

        var buildings = StartingBuildings(faction)
            .Where(buildingSlots.ContainsKey)
            .Select(kind =>
            {
                var slot = buildingSlots[kind];
                return new MatchStartBuilding(kind, slot.Position, slot.Facing);
            })
            .ToArray();

        var units = UnitDesignRuntimeLoadouts.StartingUnits(ProductionKindDesignBridge.UnitFactionFor(faction))
            .Select((spawn, index) =>
            {
                var slot = unitSlots[Math.Min(index, unitSlots.Count - 1)];
                return new MatchStartUnit(spawn.DesignId, slot.Position, slot.Facing);
            })
            .ToArray();

        return new MatchStartOwnerLoadout(owner, faction, buildings, units);
    }

    public static IReadOnlyList<string> StartingBuildings(FactionId faction)
    {
        return StartingBuildingsByFaction[faction];
    }

    public static MatchStartOwnerLoadout For(Owner owner, FactionId faction, SkirmishMapLayout layout)
    {
        var reference = owner == Owner.Player ? PlayerReferenceStart : EnemyReferenceStart;
        var start = owner == Owner.Player ? layout.PlayerStart : layout.EnemyStart;
        var baseLoadout = For(owner, faction);
        return new MatchStartOwnerLoadout(
            owner,
            faction,
            baseLoadout.Buildings
                .Select(building => building with { Position = start + (building.Position - reference) })
                .ToArray(),
            baseLoadout.Units
                .Select(unit => unit with { Position = start + (unit.Position - reference) })
                .ToArray());
    }
}
