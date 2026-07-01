using Godot;

namespace ProceduralRts.Core;

public sealed partial class UnitBattlefieldEnemyProductionAi
{
    private static void MaintainHarvesterEconomy(UnitBattlefield battlefield, PlayerSlotId enemyPlayerSlotId)
    {
        var idleHarvesters = battlefield.Units
            .Where(unit => unit.PlayerSlotId == enemyPlayerSlotId)
            .Where(unit => unit.Hp > 0)
            .Where(unit => unit.Spec.RoleTags.Contains(UnitRoleTag.Economy))
            .Where(unit => unit.HarvesterMode == HarvesterMode.Idle || unit.HarvestFieldId is null)
            .OrderBy(unit => unit.Id)
            .ToList();
        if (idleHarvesters.Count == 0)
        {
            return;
        }

        var owner = OwnerId.FromPlayerSlot(enemyPlayerSlotId);
        var baseCenter = EnemyBaseCenter(battlefield, enemyPlayerSlotId);
        var field = battlefield.ResourceFields
            .Where(resource => resource.Amount > 0)
            .Where(resource => battlefield.ResourceEntityByFieldId(resource.Id) is { } entity
                && battlefield.EntityWorld.Visibility.IsVisible(owner, entity.Id))
            .OrderBy(resource => resource.Position.DistanceSquaredTo(baseCenter))
            .FirstOrDefault();
        if (field is null)
        {
            return;
        }

        battlefield.CommandHarvestUnits(enemyPlayerSlotId, idleHarvesters.Select(unit => unit.Id), field, out _);
    }

    private static void SetEnemyRallyPoints(UnitBattlefield battlefield, PlayerSlotId enemyPlayerSlotId)
    {
        var rally = EnemyBaseCenter(battlefield, enemyPlayerSlotId) + new Vector2(-250, -120);
        foreach (var building in battlefield.BuildingSnapshots().Where(building => building.PlayerSlotId == enemyPlayerSlotId))
        {
            if (battlefield.BuildingRallyPoint(building.Id) is null)
            {
                battlefield.SetRallyPoint(building.Id, rally, out _);
            }
        }
    }

    private static Vector2 EnemyBaseCenter(UnitBattlefield battlefield, PlayerSlotId enemyPlayerSlotId)
    {
        var enemyBuildings = battlefield.BuildingSnapshots()
            .Where(building => building.PlayerSlotId == enemyPlayerSlotId && building.Hp > 0)
            .ToList();
        if (enemyBuildings.Count == 0)
        {
            return new Vector2(battlefield.WorldSize.X * 0.78f, battlefield.WorldSize.Y * 0.62f);
        }

        return enemyBuildings
            .Select(building => building.Position)
            .Aggregate(Vector2.Zero, (sum, position) => sum + position) / enemyBuildings.Count;
    }

    private static UnitFactionId FactionFor(UnitBattlefield battlefield, PlayerSlotId enemyPlayerSlotId)
    {
        return battlefield.BuildingSnapshots()
            .Where(building => building.PlayerSlotId == enemyPlayerSlotId)
            .Select(building => (UnitFactionId?)building.Faction)
            .FirstOrDefault()
            ?? (enemyPlayerSlotId == PlayerSlotId.One ? UnitFactionId.Dog : UnitFactionId.Cat);
    }
}
