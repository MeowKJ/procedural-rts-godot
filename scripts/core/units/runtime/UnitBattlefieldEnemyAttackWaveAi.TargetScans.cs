using Godot;

namespace ProceduralRts.Core;

public sealed partial class UnitBattlefieldEnemyAttackWaveAi
{
    private static UnitBattlefieldBuildingSnapshot? NearestVisibleAttackableBuilding(
        UnitBattlefield battlefield,
        PlayerSlotId playerSlotId,
        IReadOnlyList<UnitBattlefieldBuildingSnapshot> buildings,
        Vector2 origin,
        float aggressionRadius)
    {
        UnitBattlefieldBuildingSnapshot? best = null;
        var bestDistance = float.PositiveInfinity;
        foreach (var building in buildings)
        {
            if (!IsVisibleAttackableBuilding(battlefield, playerSlotId, building)
                || !IsInsideAggressionRadius(building.Position, origin, aggressionRadius))
            {
                continue;
            }

            var distance = building.Position.DistanceSquaredTo(origin);
            if (distance < bestDistance)
            {
                best = building;
                bestDistance = distance;
            }
        }

        return best;
    }

    private static UnitInstance? NearestVisibleAttackableUnit(UnitBattlefield battlefield, PlayerSlotId playerSlotId, Vector2 origin, float aggressionRadius)
    {
        UnitInstance? best = null;
        var bestDistance = float.PositiveInfinity;
        foreach (var unit in battlefield.Units)
        {
            if (!IsVisibleAttackableUnit(battlefield, playerSlotId, unit)
                || !IsInsideAggressionRadius(unit.Position, origin, aggressionRadius))
            {
                continue;
            }

            var distance = unit.Position.DistanceSquaredTo(origin);
            if (distance < bestDistance)
            {
                best = unit;
                bestDistance = distance;
            }
        }

        return best;
    }

    private static UnitInstance? NearestVisibleDefenseThreatUnit(UnitBattlefield battlefield, PlayerSlotId playerSlotId, Vector2 baseCenter)
    {
        UnitInstance? best = null;
        var bestDistance = float.PositiveInfinity;
        var bestId = int.MaxValue;
        var defenseRadiusSquared = DefenseRadius * DefenseRadius;
        foreach (var unit in battlefield.Units)
        {
            if (!IsVisibleAttackableUnit(battlefield, playerSlotId, unit))
            {
                continue;
            }

            var distance = unit.Position.DistanceSquaredTo(baseCenter);
            if (distance > defenseRadiusSquared && !IsNearOwnedBuilding(battlefield, playerSlotId, unit.Position, DefenseRadius))
            {
                continue;
            }

            if (distance < bestDistance || (distance.Equals(bestDistance) && unit.Id < bestId))
            {
                best = unit;
                bestDistance = distance;
                bestId = unit.Id;
            }
        }

        return best;
    }

    private static UnitBattlefieldBuildingSnapshot? NearestVisibleDefenseThreatBuilding(UnitBattlefield battlefield, PlayerSlotId playerSlotId, Vector2 baseCenter)
    {
        UnitBattlefieldBuildingSnapshot? best = null;
        var bestDistance = float.PositiveInfinity;
        var bestId = int.MaxValue;
        var defenseRadiusSquared = DefenseRadius * DefenseRadius;
        foreach (var building in battlefield.BuildingSnapshots())
        {
            if (!IsVisibleAttackableBuilding(battlefield, playerSlotId, building))
            {
                continue;
            }

            var distance = building.Position.DistanceSquaredTo(baseCenter);
            if (distance <= defenseRadiusSquared
                && (distance < bestDistance || (distance.Equals(bestDistance) && building.Id < bestId)))
            {
                best = building;
                bestDistance = distance;
                bestId = building.Id;
            }
        }

        return best;
    }

    private static bool IsVisibleAttackableBuilding(UnitBattlefield battlefield, PlayerSlotId playerSlotId, UnitBattlefieldBuildingSnapshot building)
    {
        return battlefield.Relations.CanAttack(playerSlotId, building.PlayerSlotId)
            && building.Hp > 0
            && battlefield.IsVisibleTo(playerSlotId, building.Id);
    }

    private static bool IsVisibleAttackableUnit(UnitBattlefield battlefield, PlayerSlotId playerSlotId, UnitInstance unit)
    {
        return battlefield.Relations.CanAttack(playerSlotId, unit.PlayerSlotId)
            && unit.Hp > 0
            && battlefield.IsVisibleTo(playerSlotId, unit);
    }
}
