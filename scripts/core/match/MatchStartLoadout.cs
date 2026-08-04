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
                [BuildingDesignIds.Headquarters] = (new Vector2(512, 624), 0),
                [BuildingDesignIds.PowerPlant] = (new Vector2(320, 816), 0),
                [BuildingDesignIds.Barracks] = (new Vector2(512, 864), 0),
                [BuildingDesignIds.VehicleFactory] = (new Vector2(512, 1024), 0),
                [BuildingDesignIds.Refinery] = (new Vector2(304, 544), MathF.PI),
            },
            [Owner.Enemy] = new Dictionary<string, (Vector2 Position, float Facing)>
            {
                [BuildingDesignIds.Headquarters] = (new Vector2(2848, 1520), MathF.PI),
                [BuildingDesignIds.PowerPlant] = (new Vector2(3040, 1328), MathF.PI),
                [BuildingDesignIds.Barracks] = (new Vector2(2880, 1280), MathF.PI),
                [BuildingDesignIds.VehicleFactory] = (new Vector2(2848, 1120), MathF.PI),
                [BuildingDesignIds.Refinery] = (new Vector2(3056, 1600), 0),
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
                (new Vector2(2720, 1216), MathF.PI),
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
                return SnappedBuilding(kind, slot.Position, slot.Facing);
            })
            .ToArray();

        var units = UnitDesignRuntimeLoadouts.StartingUnits(FactionCatalog.UnitFactionFor(faction))
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

    public static MatchStartOwnerLoadout For(Owner owner, FactionId faction, Vector2 start)
    {
        var reference = owner == Owner.Player ? PlayerReferenceStart : EnemyReferenceStart;
        var baseLoadout = For(owner, faction);
        return new MatchStartOwnerLoadout(
            owner,
            faction,
            baseLoadout.Buildings
                .Select(building => SnappedBuilding(
                    building.Kind,
                    start + (building.Position - reference),
                    building.Facing))
                .ToArray(),
            baseLoadout.Units
                .Select(unit => unit with { Position = start + (unit.Position - reference) })
                .ToArray());
    }

    private static MatchStartBuilding SnappedBuilding(string kind, Vector2 desiredPosition, float facing)
    {
        if (!PlacementMath.TryNormalizeCardinalFacing(facing, out var cardinalFacing))
        {
            throw new InvalidOperationException($"Starting building '{kind}' must use a cardinal facing, got {facing}.");
        }

        var footprint = BuildSpecCatalog.For(kind).FootprintCells.Rotated(cardinalFacing);
        return new MatchStartBuilding(
            kind,
            new Vector2(
                PlacementMath.SnapAnchor(desiredPosition.X, footprint.WidthCells),
                PlacementMath.SnapAnchor(desiredPosition.Y, footprint.HeightCells)),
            cardinalFacing);
    }
}
