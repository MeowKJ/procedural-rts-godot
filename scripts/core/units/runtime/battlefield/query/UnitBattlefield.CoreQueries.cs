using Godot;

namespace ProceduralRts.Core;

public sealed partial class UnitBattlefield
{
    public EntityInstance? UnitEntityByInstanceId(int id)
    {
        var unit = UnitById(id);
        return unit is not null && _entityWorld.TryGet(unit.EntityId, out var entity)
            ? entity
            : null;
    }

    public EntityProjection? UnitProjection(int id)
    {
        var unit = UnitById(id);
        if (unit is null || !_entityWorld.TryGet(unit.EntityId, out var entity))
        {
            return null;
        }

        return EntityProjector.ProjectOne(_entityWorld, entity);
    }

    public UnitPresentationProjection? UnitPresentationProjection(int id)
    {
        var unit = UnitById(id);
        return unit is not null && _entityWorld.TryGet(unit.EntityId, out var entity)
            ? UnitPresentationProjector.ProjectOne(_entityWorld, entity)
            : null;
    }

    public IReadOnlyList<EntityProjection> UnitProjections()
    {
        _unitProjectionBuffer.Clear();
        foreach (var unit in Units)
        {
            if (!_entityWorld.TryGet(unit.EntityId, out var entity))
            {
                throw new InvalidOperationException($"Unit projection {unit.Id} is missing EntityWorld entity {unit.EntityId}.");
            }

            _unitProjectionBuffer.Add(EntityProjector.ProjectOne(_entityWorld, entity));
        }

        _unitProjectionBuffer.Sort(CompareEntityProjectionIds);
        return _unitProjectionBuffer;
    }

    public int LiveUnitDesignCount(PlayerSlotId playerSlotId, string designId)
    {
        var count = 0;
        foreach (var unit in Units)
        {
            if (unit.PlayerSlotId == playerSlotId && unit.Hp > 0 && unit.Spec.Id == designId)
            {
                count++;
            }
        }

        return count;
    }

    public int LiveEconomyUnitCount(PlayerSlotId playerSlotId)
    {
        var count = 0;
        foreach (var unit in Units)
        {
            if (unit.PlayerSlotId == playerSlotId
                && unit.Hp > 0
                && unit.Spec.RoleTags.Contains(UnitRoleTag.Economy))
            {
                count++;
            }
        }

        return count;
    }

    public int LiveNonEconomyUnitsNear(PlayerSlotId playerSlotId, Vector2 center, float radius)
    {
        var count = 0;
        var radiusSquared = radius * radius;
        foreach (var unit in Units)
        {
            if (unit.PlayerSlotId == playerSlotId
                && unit.Hp > 0
                && !unit.Spec.RoleTags.Contains(UnitRoleTag.Economy)
                && unit.Position.DistanceSquaredTo(center) <= radiusSquared)
            {
                count++;
            }
        }

        return count;
    }

    public void CollectAvailableCombatUnits(PlayerSlotId playerSlotId, List<UnitInstance> result)
    {
        result.Clear();
        foreach (var unit in Units)
        {
            if (IsAvailableCombatUnit(playerSlotId, unit))
            {
                result.Add(unit);
            }
        }
    }

    public void CollectAvailableCombatUnitsNearEither(
        PlayerSlotId playerSlotId,
        Vector2 firstCenter,
        Vector2 secondCenter,
        float radius,
        List<UnitInstance> result)
    {
        result.Clear();
        var radiusSquared = radius * radius;
        foreach (var unit in Units)
        {
            if (!IsAvailableCombatUnit(playerSlotId, unit))
            {
                continue;
            }

            if (unit.Position.DistanceSquaredTo(firstCenter) <= radiusSquared
                || unit.Position.DistanceSquaredTo(secondCenter) <= radiusSquared)
            {
                result.Add(unit);
            }
        }
    }

    private static bool IsAvailableCombatUnit(PlayerSlotId playerSlotId, UnitInstance unit)
    {
        return unit.PlayerSlotId == playerSlotId
            && unit.Hp > 0
            && !unit.Spec.RoleTags.Contains(UnitRoleTag.Economy)
            && (unit.AttackTargetId is null || !unit.AttackTargetIsManual);
    }

    public IReadOnlyList<UnitBattlefieldVisionSource> VisionSources(PlayerSlotId viewer)
    {
        _visionSourceBuffer.Clear();
        foreach (var unit in Units)
        {
            if (unit.Hp > 0
                && Relations.Relation(viewer, unit.PlayerSlotId) is PlayerRelation.Self or PlayerRelation.Allied)
            {
                _visionSourceBuffer.Add(new UnitBattlefieldVisionSource(unit.Position, unit.Spec.Stats.SightRange));
            }
        }

        return _visionSourceBuffer;
    }

    private static int CompareEntityProjectionIds(EntityProjection left, EntityProjection right)
    {
        return left.Id.Value.CompareTo(right.Id.Value);
    }

}
