using Godot;

namespace ProceduralRts.Core;

public sealed partial class GameState
{
    public void CollectAvailableAttackWaveUnits(Owner owner, List<UnitModel> result)
    {
        result.Clear();
        foreach (var unit in Units)
        {
            if (unit.Owner == owner
                && unit.Hp > 0
                && !IsHarvesterUnit(unit)
                && (unit.AttackTargetId is null || !unit.AttackTargetIsManual))
            {
                result.Add(unit);
            }
        }
    }

    public bool TryFindAttackWaveTarget(
        Owner viewerOwner,
        Vector2 fallbackCenter,
        float aggressionRadius,
        out CombatTargetKind targetKind,
        out int targetId,
        out Vector2 targetPosition)
    {
        var origin = LiveUnitCenter(viewerOwner, fallbackCenter);
        var headquarters = AttackableHeadquarters(viewerOwner, origin, aggressionRadius);
        if (headquarters is not null)
        {
            targetKind = CombatTargetKind.Building;
            targetId = headquarters.Id;
            targetPosition = headquarters.Position;
            return true;
        }

        var buildingTarget = NearestAttackableBuilding(viewerOwner, origin, aggressionRadius);
        if (buildingTarget is not null)
        {
            targetKind = CombatTargetKind.Building;
            targetId = buildingTarget.Id;
            targetPosition = buildingTarget.Position;
            return true;
        }

        var unitTarget = NearestAttackableUnit(viewerOwner, origin, aggressionRadius);
        if (unitTarget is not null)
        {
            targetKind = CombatTargetKind.Unit;
            targetId = unitTarget.Id;
            targetPosition = unitTarget.Position;
            return true;
        }

        targetKind = CombatTargetKind.Unit;
        targetId = 0;
        targetPosition = Vector2.Zero;
        return false;
    }

    public Vector2 LiveUnitCenter(Owner owner, Vector2 fallback)
    {
        var sum = Vector2.Zero;
        var count = 0;
        foreach (var unit in Units)
        {
            if (unit.Owner == owner && unit.Hp > 0)
            {
                sum += unit.Position;
                count++;
            }
        }

        return count == 0 ? fallback : sum / count;
    }

    private UnitModel? BestUnitTargetForWeapon(
        Owner viewerOwner,
        WeaponDefinition weapon,
        Vector2 sourcePosition,
        float range,
        bool requirePositiveHp)
    {
        UnitModel? best = null;
        var bestScore = float.NegativeInfinity;
        foreach (var candidate in Units)
        {
            if ((requirePositiveHp && candidate.Hp <= 0)
                || !IsTargetableHostile(viewerOwner, candidate)
                || !WeaponCanTarget(weapon, candidate.RuntimeDescriptor)
                || candidate.Position.DistanceTo(sourcePosition) > range)
            {
                continue;
            }

            var score = TargetScore(weapon, sourcePosition, CombatTargetKind.Unit, candidate.Id, range);
            if (score > bestScore)
            {
                bestScore = score;
                best = candidate;
            }
        }

        return best;
    }

    private BuildingModel? AttackableHeadquarters(Owner viewerOwner, Vector2 origin, float aggressionRadius)
    {
        foreach (var building in Buildings)
        {
            if (building.Kind == BuildingDesignIds.Headquarters
                && building.Hp > 0
                && IsTargetableHostile(viewerOwner, building)
                && IsInsideAttackWaveRadius(building.Position, origin, aggressionRadius))
            {
                return building;
            }
        }

        return null;
    }

    private BuildingModel? NearestAttackableBuilding(Owner viewerOwner, Vector2 origin, float aggressionRadius)
    {
        BuildingModel? buildingTarget = null;
        var buildingDistance = float.PositiveInfinity;
        foreach (var building in Buildings)
        {
            if (!IsTargetableHostile(viewerOwner, building)
                || building.Hp <= 0
                || !IsInsideAttackWaveRadius(building.Position, origin, aggressionRadius))
            {
                continue;
            }

            var distance = building.Position.DistanceSquaredTo(origin);
            if (distance < buildingDistance)
            {
                buildingTarget = building;
                buildingDistance = distance;
            }
        }

        return buildingTarget;
    }

    private UnitModel? NearestAttackableUnit(Owner viewerOwner, Vector2 origin, float aggressionRadius)
    {
        UnitModel? unitTarget = null;
        var unitDistance = float.PositiveInfinity;
        foreach (var unit in Units)
        {
            if (!IsTargetableHostile(viewerOwner, unit)
                || unit.Hp <= 0
                || !IsInsideAttackWaveRadius(unit.Position, origin, aggressionRadius))
            {
                continue;
            }

            var distance = unit.Position.DistanceSquaredTo(origin);
            if (distance < unitDistance)
            {
                unitTarget = unit;
                unitDistance = distance;
            }
        }

        return unitTarget;
    }

    private static bool IsInsideAttackWaveRadius(Vector2 targetPosition, Vector2 origin, float aggressionRadius)
    {
        return float.IsPositiveInfinity(aggressionRadius)
            || targetPosition.DistanceSquaredTo(origin) <= aggressionRadius * aggressionRadius;
    }
}
