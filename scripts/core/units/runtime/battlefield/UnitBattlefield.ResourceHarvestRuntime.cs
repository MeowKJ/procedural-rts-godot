using Godot;

namespace ProceduralRts.Core;

public sealed partial class UnitBattlefield
{
    private int? BuildingTargetEntityId(int? buildingId)
    {
        return buildingId is int id && _buildingTargetEntityIds.TryGetValue(id, out var entityId)
            ? entityId.Value
            : null;
    }

    private int? UnitEntityId(int? unitId)
    {
        return unitId is int id
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
        _resourceSystem.Step(new SimContext(_entityWorld, _inputCommandTick, dt, []));
        UpdateDockDeliveryPulses();
        NotifyCreditChanges(_resourceCreditsBefore);
    }

    private void UpdateDockDeliveryPulses()
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

            var docked = UnitIdForEntity(dock.DockedEntityId);
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

    private int? UnitIdForEntity(int? entityId)
    {
        return entityId is int id
            ? UnitByEntityId(new EntityId(id))?.Id
            : null;
    }
}
