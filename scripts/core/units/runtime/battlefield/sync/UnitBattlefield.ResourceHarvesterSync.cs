using Godot;

namespace ProceduralRts.Core;

public sealed partial class UnitBattlefield
{
    private int? ResourceFieldEntityId(int? legacyFieldId)
    {
        return legacyFieldId is int id && _resourceFieldEntityIds.TryGetValue(id, out var entityId)
            ? entityId.Value
            : null;
    }

    private int? BuildingTargetEntityId(int? legacyBuildingId)
    {
        return legacyBuildingId is int id && _buildingTargetEntityIds.TryGetValue(id, out var entityId)
            ? entityId.Value
            : null;
    }

    private int? UnitEntityId(int? legacyUnitId)
    {
        return legacyUnitId is int id
            ? UnitById(id)?.EntityId.Value
            : null;
    }

    private void UpdateResourceHarvestersFromEntityWorld(float dt)
    {
        if (!HasHarvesters())
        {
            return;
        }

        CollectResourceCreditsBefore(_resourceCreditsBefore);
        SyncResourceFieldEntities();
        SyncBuildingTargetEntities();
        SyncUnitEntities();
        _resourceSystem.Step(new SimContext(_entityWorld, _inputCommandTick, dt, []));
        SyncResourceFieldsFromEntities();
        SyncDockStateFromEntities();
        SyncHarvestersFromEntities();
        SyncAllCreditsFromEntityWorld(_resourceCreditsBefore);
    }

    private void SyncHarvestersFromEntities()
    {
        foreach (var unit in Units)
        {
            if (IsHarvester(unit) && _entityWorld.TryGet(unit.EntityId, out var entity))
            {
                ApplyEntityResourceStateToUnit(unit, entity);
            }
        }
    }

    private void ApplyEntityResourceStateToUnit(UnitInstance unit, EntityInstance entity)
    {
        if (entity.Components.TryGet<MovementComponentState>(out var movement))
        {
            unit.Velocity = movement.Velocity;
            unit.MoveTarget = movement.MoveTarget;
            unit.FormationSlot = movement.FormationSlot;
        }

        if (entity.Components.TryGet<CommandableComponentState>(out var commandable))
        {
            unit.PlayerIntentTarget = commandable.PlayerIntentTarget;
            unit.CommandVisualTarget = commandable.CommandVisualTarget;
            unit.MoveMode = commandable.MoveMode;
        }

        if (entity.Components.TryGet<HarvesterComponentState>(out var harvester))
        {
            unit.HarvesterMode = harvester.Mode;
            unit.HarvestFieldId = LegacyResourceFieldId(harvester.FieldId);
            unit.HarvestRefineryId = LegacyBuildingTargetId(harvester.RefineryId);
            unit.HarvestPulse = Mathf.Clamp(harvester.HarvestPulse, 0, 1);
            unit.HarvesterRetreating = harvester.Retreating;
        }

        if (entity.Components.TryGet<ResourceCargoComponentState>(out var cargo))
        {
            unit.Cargo = cargo.Cargo;
        }
    }

    private void SyncDockStateFromEntities()
    {
        CollectBuildingTargetIds(_buildingTargetIdBuffer);
        foreach (var refineryId in _buildingTargetIdBuffer)
        {
            if (BuildingIdentity(refineryId)?.Kind != BuildingDesignIds.Refinery)
            {
                continue;
            }

            if (BuildingEntityByTargetId(refineryId) is not { } entity
                || !entity.Components.TryGet<DockComponentState>(out var dock))
            {
                continue;
            }

            var docked = LegacyUnitId(dock.DockedEntityId);
            var wasDocked = _lastDockedHarvesterIds.TryGetValue(refineryId, out var previous)
                ? previous
                : null;
            _lastDockedHarvesterIds[refineryId] = docked;
            if (docked is not null || wasDocked != docked)
            {
                SetBuildingDeliveryPulseCore(refineryId, 1);
            }
        }
    }

    private int? LegacyUnitId(int? entityId)
    {
        return entityId is int id
            ? UnitByEntityId(new EntityId(id))?.Id
            : null;
    }
}
