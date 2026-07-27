using Godot;

namespace ProceduralRts.Core;

public sealed partial class UnitBattlefield
{
    private int? TargetIdForEntity(EntityId entityId, CombatTargetKind targetKind)
    {
        if (!entityId.IsValid)
        {
            return null;
        }

        if (targetKind == CombatTargetKind.Building)
        {
            return _buildingTargetIdsByEntityId.TryGetValue(entityId, out var buildingId)
                ? buildingId
                : null;
        }

        return UnitByEntityId(entityId)?.Id;
    }

    private int? ResourceFieldIdForEntity(int? entityId)
    {
        if (entityId is not int id)
        {
            return null;
        }

        foreach (var pair in _resourceFieldEntityIds)
        {
            if (pair.Value.Value == id)
            {
                return pair.Key;
            }
        }

        return null;
    }

    private int? BuildingIdForEntity(int? entityId)
    {
        if (entityId is not int id)
        {
            return null;
        }

        return _buildingTargetIdsByEntityId.TryGetValue(new EntityId(id), out var buildingId)
            ? buildingId
            : null;
    }

    private UnitInstance? UnitByEntityId(EntityId entityId)
    {
        return Units.FirstOrDefault(unit => unit.EntityId == entityId);
    }

    private int? BuildingTargetIdByEntityId(EntityId entityId)
    {
        return _buildingTargetIdsByEntityId.TryGetValue(entityId, out var buildingId)
            ? buildingId
            : null;
    }

    private ResourceFieldModel? ResourceFieldById(int id)
    {
        return ResourceFields.FirstOrDefault(field => field.Id == id);
    }

    private int? FindBestRefineryIdForHarvester(PlayerSlotId playerSlotId, Vector2 position)
    {
        int? bestId = null;
        var bestDistance = 0f;
        foreach (var entity in _entityWorld.OrderedEntities)
        {
            if (!entity.Components.TryGet<BuildingIdentityComponentState>(out var identity)
                || identity.PlayerSlotId != playerSlotId
                || identity.Kind != BuildingDesignIds.Refinery
                || BuildingSnapshot(identity.BuildingId) is not { } building
                || building.Hp <= 0
                || BuildingBuildProgress(building.Id) < 1)
            {
                continue;
            }

            var distance = building.Position.DistanceTo(position);
            if (bestId is null || distance < bestDistance)
            {
                bestId = building.Id;
                bestDistance = distance;
            }
        }

        return bestId;
    }

    private void ClearRefineryDockClaim(int harvesterId)
    {
        var harvesterEntityId = UnitEntityId(harvesterId);
        if (harvesterEntityId is null)
        {
            return;
        }

        foreach (var entity in _entityWorld.OrderedEntities)
        {
            if (!entity.Components.TryGet<BuildingIdentityComponentState>(out var identity)
                || identity.Kind != BuildingDesignIds.Refinery
                || !entity.Components.TryGet<DockComponentState>(out var dock))
            {
                continue;
            }

            entity.Components.Set(dock with
            {
                ReservedByEntityId = dock.ReservedByEntityId == harvesterEntityId.Value ? null : dock.ReservedByEntityId,
                DockedEntityId = dock.DockedEntityId == harvesterEntityId.Value ? null : dock.DockedEntityId,
            });
        }
    }

}
