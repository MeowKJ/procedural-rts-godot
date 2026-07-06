using Godot;

namespace ProceduralRts.Core;

public sealed partial class UnitBattlefield
{
    public UnitBattlefieldBuildingSnapshot? VisibleAttackableHeadquarters(
        PlayerSlotId playerSlotId,
        Vector2 origin,
        float aggressionRadius)
    {
        foreach (var building in BuildingSnapshots())
        {
            if (building.Kind == BuildingDesignIds.Headquarters
                && IsVisibleAttackableBuilding(playerSlotId, building)
                && IsInsideQueryRadius(building.Position, origin, aggressionRadius))
            {
                return building;
            }
        }

        return null;
    }

    public UnitBattlefieldBuildingSnapshot? NearestVisibleAttackableBuilding(
        PlayerSlotId playerSlotId,
        Vector2 origin,
        float aggressionRadius)
    {
        UnitBattlefieldBuildingSnapshot? best = null;
        var bestDistance = float.PositiveInfinity;
        foreach (var building in BuildingSnapshots())
        {
            if (!IsVisibleAttackableBuilding(playerSlotId, building)
                || !IsInsideQueryRadius(building.Position, origin, aggressionRadius))
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

    public UnitInstance? NearestVisibleAttackableUnit(
        PlayerSlotId playerSlotId,
        Vector2 origin,
        float aggressionRadius)
    {
        UnitInstance? best = null;
        var bestDistance = float.PositiveInfinity;
        foreach (var unit in Units)
        {
            if (!IsVisibleAttackableUnit(playerSlotId, unit)
                || !IsInsideQueryRadius(unit.Position, origin, aggressionRadius))
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

    public UnitInstance? NearestVisibleDefenseThreatUnit(
        PlayerSlotId playerSlotId,
        Vector2 baseCenter,
        float defenseRadius)
    {
        UnitInstance? best = null;
        var bestDistance = float.PositiveInfinity;
        var bestId = int.MaxValue;
        var defenseRadiusSquared = defenseRadius * defenseRadius;
        foreach (var unit in Units)
        {
            if (!IsVisibleAttackableUnit(playerSlotId, unit))
            {
                continue;
            }

            var distance = unit.Position.DistanceSquaredTo(baseCenter);
            if (distance > defenseRadiusSquared && !IsNearLiveOwnedBuilding(playerSlotId, unit.Position, defenseRadius))
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

    public UnitBattlefieldBuildingSnapshot? NearestVisibleDefenseThreatBuilding(
        PlayerSlotId playerSlotId,
        Vector2 baseCenter,
        float defenseRadius)
    {
        UnitBattlefieldBuildingSnapshot? best = null;
        var bestDistance = float.PositiveInfinity;
        var bestId = int.MaxValue;
        var defenseRadiusSquared = defenseRadius * defenseRadius;
        foreach (var building in BuildingSnapshots())
        {
            if (!IsVisibleAttackableBuilding(playerSlotId, building))
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

    private bool IsVisibleAttackableBuilding(PlayerSlotId playerSlotId, UnitBattlefieldBuildingSnapshot building)
    {
        return Relations.CanAttack(playerSlotId, building.PlayerSlotId)
            && building.Hp > 0
            && IsVisibleTo(playerSlotId, building.Id);
    }

    private bool IsVisibleAttackableUnit(PlayerSlotId playerSlotId, UnitInstance unit)
    {
        return Relations.CanAttack(playerSlotId, unit.PlayerSlotId)
            && unit.Hp > 0
            && IsVisibleTo(playerSlotId, unit);
    }

    public bool IsNearLiveOwnedBuilding(PlayerSlotId playerSlotId, Vector2 position, float radius)
    {
        var radiusSquared = radius * radius;
        foreach (var building in BuildingSnapshots())
        {
            if (building.PlayerSlotId == playerSlotId
                && building.Hp > 0
                && building.Position.DistanceSquaredTo(position) <= radiusSquared)
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsInsideQueryRadius(Vector2 position, Vector2 origin, float radius)
    {
        return radius <= 0 || position.DistanceSquaredTo(origin) <= radius * radius;
    }
}
