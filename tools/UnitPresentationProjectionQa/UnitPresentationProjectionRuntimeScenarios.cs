using Godot;
using ProceduralRts.Core;
using ProceduralRts.Tools.Qa;

static class UnitPresentationProjectionRuntimeScenarios
{
    public static void Run(List<string> failures)
    {
        var map = new MapSpec
        {
            Id = "qa.unit-presentation-projection",
            Seed = 626,
            WorldSize = new MapSize(2_000, 1_400),
            OwnerStarts =
            [
                new(new OwnerId(1), FactionId.Dog, new MapPoint(160, 180), 0, 0),
                new(new OwnerId(2), FactionId.Cat, new MapPoint(1_500, 700), MathF.PI, 0),
            ],
            Resources =
            [
                new("projection-harvest", new MapPoint(680, 180), 64, 4_000, new MapColor("#f6c55c")),
            ],
        };
        var battlefield = UnitBattlefield.AdoptLoadedMap(MapLoader.Load(map), map);
        battlefield.Relations.Set(PlayerSlotId.One, PlayerSlotId.Two, PlayerRelation.Hostile);

        var infantry = battlefield.Spawn("dog.infantry", PlayerSlotId.One, new Vector2(160, 180));
        var vehicle = battlefield.Spawn("dog.assault_tank", PlayerSlotId.One, new Vector2(240, 180));
        var aircraft = battlefield.Spawn("dog.sky_patrol_aircraft", PlayerSlotId.One, new Vector2(320, 180));
        var harvester = battlefield.Spawn("dog.harvester", PlayerSlotId.One, new Vector2(400, 180));
        var enemyHeadquarters = battlefield.UpsertBuildingTarget(
            1,
            BuildingDesignIds.Headquarters,
            PlayerSlotId.Two,
            UnitFactionId.Cat,
            new Vector2(1_500, 700),
            0,
            BuildSpecCatalog.For(BuildingDesignIds.Headquarters).MaxHp);
        battlefield.UpsertBuildingTarget(
            2,
            BuildingDesignIds.Refinery,
            PlayerSlotId.One,
            UnitFactionId.Dog,
            new Vector2(560, 180),
            0,
            BuildSpecCatalog.For(BuildingDesignIds.Refinery).MaxHp);
        var selected = battlefield.SelectUnitsByIds(
            PlayerSlotId.One,
            [infantry.Id, vehicle.Id, aircraft.Id, harvester.Id]);
        Require(selected.Count == 4, "runtime projection scenario must select its full representative roster", failures);
        Require(QaPlayerCommandDriver.MoveSubjects(
                battlefield,
                PlayerSlotId.One,
                [infantry.EntityId, aircraft.EntityId],
                new Vector2(840, 420)).AcceptedCount == 1,
            "infantry and aircraft move commands must be accepted through the runtime owner", failures);
        Require(QaPlayerCommandDriver.AttackSubjects(
                battlefield,
                PlayerSlotId.One,
                [vehicle.EntityId],
                battlefield.BuildingEntityIdByTargetId(enemyHeadquarters.Id) ?? default,
                CombatTargetKind.Building).AcceptedCount == 1,
            "vehicle attack against a hostile structure must be accepted through the runtime owner", failures);
        Require(battlefield.TryGetResourceEntityId(battlefield.ResourceFields[0], out var resourceEntityId)
            && QaPlayerCommandDriver.HarvestSubjects(
                battlefield,
                PlayerSlotId.One,
                [harvester.EntityId],
                resourceEntityId).AcceptedCount == 1,
            "economy command must be accepted through the runtime owner", failures);

        AssertProjected(battlefield, infantry, "infantry", failures);
        AssertProjected(battlefield, vehicle, "vehicle", failures);
        AssertProjected(battlefield, aircraft, "aircraft", failures);
        AssertProjected(battlefield, harvester, "economy", failures);

        var firstMove = RequiredProjection(battlefield, infantry, failures, "first move");
        Require(firstMove.CommandPulse > 0 && firstMove.MoveTarget is not null && firstMove.IsMoving,
            "accepted infantry movement must produce projected command feedback", failures);
        var vehicleProjection = RequiredProjection(battlefield, vehicle, failures, "structure attack");
        Require(vehicleProjection.CommandPulse > 0 && vehicleProjection.Mounts.Count > 0,
            "accepted vehicle structure attack must retain projected command and mount feedback", failures);
        var harvestProjection = RequiredProjection(battlefield, harvester, failures, "harvest");
        Require(harvestProjection.CommandPulse > 0 && harvestProjection.MoveTarget is not null,
            "accepted economy command must produce projected feedback", failures);

        var replacementTarget = new Vector2(1_080, 520);
        Require(QaPlayerCommandDriver.MoveSubjects(
                battlefield,
                PlayerSlotId.One,
                [infantry.EntityId],
                replacementTarget).AcceptedCount == 1,
            "replacement command must be accepted", failures);
        var replaced = RequiredProjection(battlefield, infantry, failures, "replacement move");
        Require(replaced.MoveTarget is { } moveTarget && moveTarget.DistanceSquaredTo(replacementTarget) < 1,
            "replacement command must remove the stale projected move target", failures);

        var infantryEntity = battlefield.UnitEntityByInstanceId(infantry.Id)
            ?? throw new InvalidOperationException("infantry entity should exist");
        infantryEntity.Components.Set(infantryEntity.Components.Require<HealthComponentState>() with { Hp = 0 });
        battlefield.Update(0);
        Require(battlefield.UnitPresentationProjection(infantry.Id) is null,
            "unit death must remove stale presentation projection state", failures);
    }

    private static void AssertProjected(
        UnitBattlefield battlefield,
        UnitInstance unit,
        string category,
        List<string> failures)
    {
        var projection = RequiredProjection(battlefield, unit, failures, category);
        Require(projection.Entity.Id == unit.EntityId && projection.Entity.Selected,
            string.Concat(category, " projection must keep deterministic identity and selection"), failures);
        Require(projection.Entity.Owner == OwnerId.FromPlayerSlot(PlayerSlotId.One),
            string.Concat(category, " projection must keep ownership"), failures);
    }

    private static UnitPresentationProjection RequiredProjection(
        UnitBattlefield battlefield,
        UnitInstance unit,
        List<string> failures,
        string category)
    {
        var projection = battlefield.UnitPresentationProjection(unit.Id);
        Require(projection is not null, string.Concat(category, " projection must exist"), failures);
        return projection ?? default;
    }

    private static void Require(bool condition, string message, List<string> failures)
    {
        if (!condition)
        {
            failures.Add(message);
        }
    }
}
