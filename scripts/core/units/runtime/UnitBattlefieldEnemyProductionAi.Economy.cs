using Godot;

namespace ProceduralRts.Core;

public sealed partial class UnitBattlefieldEnemyProductionAi
{
    private void MaintainHarvesterEconomy(UnitBattlefield battlefield, PlayerSlotId enemyPlayerSlotId)
    {
        CollectIdleHarvesters(battlefield, enemyPlayerSlotId, _idleHarvesterBuffer);
        if (_idleHarvesterBuffer.Count == 0)
        {
            return;
        }

        var owner = OwnerId.FromPlayerSlot(enemyPlayerSlotId);
        var baseCenter = EnemyBaseCenter(battlefield, enemyPlayerSlotId);
        var field = NearestVisibleResourceField(battlefield, owner, baseCenter);
        if (field is null)
        {
            return;
        }

        CollectUnitIds(_idleHarvesterBuffer, _idleHarvesterIds);
        battlefield.CommandHarvestUnits(enemyPlayerSlotId, _idleHarvesterIds, field, out _);
    }

    private void SetEnemyRallyPoints(UnitBattlefield battlefield, PlayerSlotId enemyPlayerSlotId)
    {
        var rally = EnemyBaseCenter(battlefield, enemyPlayerSlotId) + new Vector2(-250, -120);
        var buildings = battlefield.BuildingSnapshots();
        for (var index = 0; index < buildings.Count; index++)
        {
            var building = buildings[index];
            if (building.PlayerSlotId != enemyPlayerSlotId)
            {
                continue;
            }

            if (battlefield.BuildingRallyPoint(building.Id) is null)
            {
                battlefield.SetRallyPoint(building.Id, rally, out _);
            }
        }
    }

    private Vector2 EnemyBaseCenter(UnitBattlefield battlefield, PlayerSlotId enemyPlayerSlotId)
    {
        var sum = Vector2.Zero;
        var count = 0;
        var buildings = battlefield.BuildingSnapshots();
        for (var index = 0; index < buildings.Count; index++)
        {
            var building = buildings[index];
            if (building.PlayerSlotId != enemyPlayerSlotId || building.Hp <= 0)
            {
                continue;
            }

            sum += building.Position;
            count++;
        }

        if (count == 0)
        {
            return new Vector2(battlefield.WorldSize.X * 0.78f, battlefield.WorldSize.Y * 0.62f);
        }

        return sum / count;
    }

    private static UnitFactionId FactionFor(UnitBattlefield battlefield, PlayerSlotId enemyPlayerSlotId)
    {
        var buildings = battlefield.BuildingSnapshots();
        for (var index = 0; index < buildings.Count; index++)
        {
            if (buildings[index].PlayerSlotId == enemyPlayerSlotId)
            {
                return buildings[index].Faction;
            }
        }

        return enemyPlayerSlotId == PlayerSlotId.One ? UnitFactionId.Dog : UnitFactionId.Cat;
    }

    private static void CollectIdleHarvesters(UnitBattlefield battlefield, PlayerSlotId enemyPlayerSlotId, List<UnitInstance> result)
    {
        result.Clear();
        foreach (var unit in battlefield.Units)
        {
            if (unit.PlayerSlotId == enemyPlayerSlotId
                && unit.Hp > 0
                && unit.Spec.RoleTags.Contains(UnitRoleTag.Economy)
                && (unit.HarvesterMode == HarvesterMode.Idle || unit.HarvestFieldId is null))
            {
                result.Add(unit);
            }
        }

        result.Sort(CompareUnitIds);
    }

    private static ResourceFieldModel? NearestVisibleResourceField(UnitBattlefield battlefield, OwnerId owner, Vector2 baseCenter)
    {
        ResourceFieldModel? best = null;
        var bestDistance = float.PositiveInfinity;
        foreach (var resource in battlefield.ResourceFields)
        {
            if (resource.Amount <= 0
                || battlefield.ResourceEntityByFieldId(resource.Id) is not { } entity
                || !battlefield.EntityWorld.Visibility.IsVisible(owner, entity.Id))
            {
                continue;
            }

            var distance = resource.Position.DistanceSquaredTo(baseCenter);
            if (distance < bestDistance)
            {
                best = resource;
                bestDistance = distance;
            }
        }

        return best;
    }

    private static void CollectUnitIds(IReadOnlyList<UnitInstance> units, List<int> result)
    {
        result.Clear();
        for (var index = 0; index < units.Count; index++)
        {
            result.Add(units[index].Id);
        }
    }

    private static void CollectOwnedBuildings(
        UnitBattlefield battlefield,
        PlayerSlotId playerSlotId,
        List<UnitBattlefieldBuildingSnapshot> result,
        bool liveOnly)
    {
        result.Clear();
        var buildings = battlefield.BuildingSnapshots();
        for (var index = 0; index < buildings.Count; index++)
        {
            var building = buildings[index];
            if (building.PlayerSlotId != playerSlotId || (liveOnly && building.Hp <= 0))
            {
                continue;
            }

            result.Add(building);
        }
    }

    private static int CompareUnitIds(UnitInstance left, UnitInstance right)
    {
        return left.Id.CompareTo(right.Id);
    }
}
