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

        SyncUnitEntity(unit);
        return EntityProjector.ProjectOne(_entityWorld, entity);
    }

    public IReadOnlyList<EntityProjection> UnitProjections()
    {
        SyncUnitEntities();
        _unitProjectionBuffer.Clear();
        foreach (var unit in Units)
        {
            if (!_entityWorld.TryGet(unit.EntityId, out var entity))
            {
                throw new InvalidOperationException($"Unit {unit.Id} is missing EntityWorld mirror {unit.EntityId}.");
            }

            _unitProjectionBuffer.Add(EntityProjector.ProjectOne(_entityWorld, entity));
        }

        _unitProjectionBuffer.Sort(CompareEntityProjectionIds);
        return _unitProjectionBuffer;
    }

    public UnitProjectionDriftReport UnitProjectionDrift()
    {
        var maxPositionDrift = 0f;
        var maxFacingDrift = 0f;
        var missingMirrors = 0;

        foreach (var unit in Units)
        {
            if (!_entityWorld.TryGet(unit.EntityId, out var entity))
            {
                missingMirrors++;
                continue;
            }

            var projection = EntityProjector.ProjectOne(_entityWorld, entity);
            maxPositionDrift = MathF.Max(maxPositionDrift, unit.Position.DistanceTo(projection.Position));
            maxFacingDrift = MathF.Max(maxFacingDrift, MathF.Abs(Mathf.AngleDifference(unit.Facing, projection.Facing)));
        }

        return new UnitProjectionDriftReport(Units.Count, missingMirrors, maxPositionDrift, maxFacingDrift);
    }

    public ResourceInventory ResourceInventory(PlayerSlotId playerSlotId)
    {
        if (!ResourceInventories.TryGetValue(playerSlotId, out var inventory))
        {
            inventory = new ResourceInventory { Credits = 0 };
            ResourceInventories[playerSlotId] = inventory;
        }

        return inventory;
    }

    public int Credits(PlayerSlotId playerSlotId)
    {
        return ResourceInventory(playerSlotId).Credits;
    }

    public void SetCredits(PlayerSlotId playerSlotId, int credits)
    {
        var inventory = ResourceInventory(playerSlotId);
        inventory.Credits = Mathf.Max(0, credits);
        _entityWorld.ResourceInventory(OwnerId.FromPlayerSlot(playerSlotId)).Credits = inventory.Credits;
        ResourceInventoryChanged?.Invoke(playerSlotId, inventory);
    }

    public void SetResourceFields(IEnumerable<ResourceFieldModel> fields)
    {
        ResourceFields.Clear();
        ResourceFields.AddRange(fields);
        SyncResourceFieldEntities();
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
