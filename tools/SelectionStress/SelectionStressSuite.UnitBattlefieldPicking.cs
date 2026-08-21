using Godot;
using ProceduralRts.Core;

internal static partial class SelectionStressSuite
{
    private static int RunUnitBattlefieldPickingQueries()
    {
        var resourceMap = new MapSpec
        {
            Id = "qa.selection.resource-picking",
            Seed = 626,
            WorldSize = new MapSize(1_200, 900),
            OwnerStarts =
            [
                new(new OwnerId(1), FactionId.Dog, new MapPoint(120, 120), 0, 0),
                new(new OwnerId(2), FactionId.Cat, new MapPoint(1_080, 780), MathF.PI, 0),
            ],
            Resources =
            [
                new("depleted", new MapPoint(500, 500), 45, 0, new MapColor("#ffffff")),
                new("nearest-live", new MapPoint(508, 500), 45, 1_000, new MapColor("#ffffff")),
                new("farther-live", new MapPoint(520, 500), 45, 1_000, new MapColor("#ffffff")),
            ],
        };
        var battlefield = UnitBattlefield.AdoptLoadedMap(MapLoader.Load(resourceMap), resourceMap);
        var farSelf = battlefield.Spawn<DogInfantry>(PlayerSlotId.One, new Vector2(116, 100));
        var nearSelf = battlefield.Spawn<DogHarvester>(PlayerSlotId.One, new Vector2(101, 100));
        var hostile = battlefield.Spawn<CatTank>(PlayerSlotId.Two, new Vector2(103, 100));

        RequirePickedUnit(battlefield.PickUnit(new Vector2(100, 100), PlayerSlotId.One), nearSelf, "owned unit pick");
        RequirePickedUnit(battlefield.PickAnyUnit(new Vector2(100, 100)), nearSelf, "any unit pick");
        RequirePickedUnit(battlefield.PickHostileUnit(new Vector2(100, 100), PlayerSlotId.One), hostile, "hostile unit pick");

        battlefield.Relations.Set(PlayerSlotId.One, PlayerSlotId.Two, PlayerRelation.Allied);
        if (battlefield.PickHostileUnit(new Vector2(100, 100), PlayerSlotId.One) is not null)
        {
            throw new InvalidOperationException("hostile unit pick should honor player relations");
        }

        battlefield.Relations.Set(PlayerSlotId.One, PlayerSlotId.Two, PlayerRelation.Hostile);
        var selfHq = battlefield.UpsertBuildingTarget(
            1,
            BuildingDesignIds.Headquarters,
            PlayerSlotId.One,
            UnitFactionId.Dog,
            new Vector2(300, 300),
            0,
            100);
        var hostileBarracks = battlefield.UpsertBuildingTarget(
            2,
            BuildingDesignIds.Barracks,
            PlayerSlotId.Two,
            UnitFactionId.Cat,
            new Vector2(306, 300),
            0,
            100);
        var deadHostile = battlefield.UpsertBuildingTarget(
            3,
            BuildingDesignIds.PowerPlant,
            PlayerSlotId.Two,
            UnitFactionId.Cat,
            new Vector2(301, 300),
            0,
            0);

        RequirePickedBuilding(battlefield.PickBuildingTargetId(new Vector2(303, 300), PlayerSlotId.One), selfHq.Id, "owned building pick");
        RequirePickedBuilding(battlefield.PickAnyBuildingTargetId(new Vector2(303, 300)), selfHq.Id, "any building tie pick");
        RequirePickedBuilding(battlefield.PickHostileBuildingId(new Vector2(303, 300), PlayerSlotId.One), hostileBarracks.Id, "hostile building pick");
        if (battlefield.PickHostileBuildingId(deadHostile.Position, PlayerSlotId.One) == deadHostile.Id)
        {
            throw new InvalidOperationException("hostile building pick should ignore dead building targets");
        }

        var resources = battlefield.ResourceNodeProjections();
        if (battlefield.PickResourceNode(new Vector2(501, 500))?.EntityId != resources[1].EntityId)
        {
            throw new InvalidOperationException("resource pick should ignore depleted fields and choose nearest live field");
        }

        return 9;
    }

    private static void RequirePickedUnit(UnitInstance? actual, UnitInstance expected, string label)
    {
        if (actual?.Id != expected.Id)
        {
            throw new InvalidOperationException($"{label} expected unit {expected.Id}, got {actual?.Id}");
        }
    }

    private static void RequirePickedBuilding(int? actual, int expected, string label)
    {
        if (actual != expected)
        {
            throw new InvalidOperationException($"{label} expected building {expected}, got {actual}");
        }
    }
}
