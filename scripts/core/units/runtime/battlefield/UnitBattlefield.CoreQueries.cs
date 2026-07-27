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

    public EntityInstance? ResourceEntityByFieldId(int id)
    {
        return _resourceFieldEntityIds.TryGetValue(id, out var entityId) && _entityWorld.TryGet(entityId, out var entity)
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

    public ResourceInventory ResourceInventory(PlayerSlotId playerSlotId)
    {
        return _entityWorld.ResourceInventory(OwnerId.FromPlayerSlot(playerSlotId));
    }

    public int Credits(PlayerSlotId playerSlotId)
    {
        return ResourceInventory(playerSlotId).Credits;
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

    public void SetCredits(PlayerSlotId playerSlotId, int credits)
    {
        var inventory = ResourceInventory(playerSlotId);
        inventory.Credits = Mathf.Max(0, credits);
        ResourceInventoryChanged?.Invoke(playerSlotId, inventory);
    }

    public void SetResourceFields(IEnumerable<ResourceFieldModel> fields)
    {
        _resourceFields.Clear();
        _resourceFields.AddRange(fields);
        CreateResourceFieldEntities();
    }

    public ResourceFieldModel? PickResourceField(Vector2 worldPoint, float pickPadding = 8)
    {
        return NearestResourceField(worldPoint, pickPadding);
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

    public IReadOnlyList<UnitBattlefieldResourcePip> ResourcePips(Func<Vector2, bool>? isExplored = null)
    {
        var result = NextResourcePipBuffer();
        foreach (var field in ResourceFields)
        {
            if (field.Amount <= 0 || !(isExplored?.Invoke(field.Position) ?? true))
            {
                continue;
            }

            result.Add(new UnitBattlefieldResourcePip(
                field.Position,
                field.Radius,
                field.MaxAmount <= 0 ? 0 : Mathf.Clamp((float)field.Amount / field.MaxAmount, 0, 1)));
        }

        return result;
    }

    private List<UnitBattlefieldResourcePip> NextResourcePipBuffer()
    {
        _useSecondaryResourcePipBuffer = !_useSecondaryResourcePipBuffer;
        var result = _useSecondaryResourcePipBuffer ? _resourcePipSecondaryBuffer : _resourcePipBuffer;
        result.Clear();
        return result;
    }

    private static int CompareEntityProjectionIds(EntityProjection left, EntityProjection right)
    {
        return left.Id.Value.CompareTo(right.Id.Value);
    }

}
