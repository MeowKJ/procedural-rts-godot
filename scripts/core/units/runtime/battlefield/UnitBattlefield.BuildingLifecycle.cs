using Godot;

namespace ProceduralRts.Core;

public sealed partial class UnitBattlefield
{
    public UnitBattlefieldBuildingSnapshot UpsertBuildingTarget(
        int id,
        string kind,
        PlayerSlotId playerSlotId,
        UnitFactionId faction,
        Vector2 position,
        float facing,
        float hp,
        bool powered = true,
        float buildProgress = 1,
        Vector2? rallyPoint = null)
    {
        _nextBuildingTargetId = Math.Max(_nextBuildingTargetId, id + 1);
        var identity = BuildingIdentity(id);
        var target = identity is null
            ? new BuildingEntitySeed(id, kind, playerSlotId, faction, position, facing, hp)
            : new BuildingEntitySeed(id, identity.Kind, identity.PlayerSlotId, identity.Faction, position, facing, hp);
        SyncBuildingTargetEntity(target.Id, rallyPoint, powered, buildProgress, hp, target);
        return RequiredBuildingSnapshot(target.Id);
    }

    public PlacementResult ValidateBuildingPlacement(string kind, PlayerSlotId playerSlotId, Vector2 desiredPosition)
    {
        var spec = BuildSpecCatalog.For(kind);
        var requiresBuildAuthority = spec.RequiredProducer is not null || spec.RequiredBuildings.Count > 0;
        CollectBuildingBuildAnchors(playerSlotId, _placementBuildAnchors);
        CollectBuildingPlacementObstacles(_placementObstacles);
        return PlacementMath.ValidateBuildableArea(
            desiredPosition.X,
            desiredPosition.Y,
            spec.Footprint.X,
            spec.Footprint.Y,
            WorldSize.X,
            WorldSize.Y,
            spec.PlacementDomain,
            _placementBuildAnchors,
            _placementObstacles,
            terrainAt: TerrainLayerAt,
            requiresBuildAuthority: requiresBuildAuthority,
            padding: 12);
    }

    public bool ConstructBuilding(
        PlayerSlotId playerSlotId,
        UnitFactionId faction,
        string kind,
        Vector2 position,
        out UnitBattlefieldBuildingSnapshot? building,
        out string status,
        float facing = 0)
    {
        building = null;
        var owner = OwnerId.FromPlayerSlot(playerSlotId);
        var spec = BuildSpecCatalog.For(kind);
        var inventory = ResourceInventory(playerSlotId);
        if (inventory.Credits < spec.Cost)
        {
            status = "placement.needCredits";
            return false;
        }

        SyncOwnerRelations();
        SyncBuildingTargetEntities();
        _entityWorld.WorldWidth = WorldSize.X;
        _entityWorld.WorldHeight = WorldSize.Y;
        _entityWorld.ResourceInventory(owner).Credits = inventory.Credits;

        CollectEntityIds(_constructionEntityIdsBefore);
        var command = new StartConstructionEntityCommand(
            owner,
            ConstructionSubjectEntities(playerSlotId, spec),
            NextInputCommandTick(),
            kind,
            ClampInsideWorld(position, MathF.Max(spec.Footprint.X, spec.Footprint.Y) * 0.5f + 8),
            facing);
        SubmitConstructionCommand(command);

        var rejection = DrainConstructionRejection(command.Tick, owner, kind);
        if (rejection is not null)
        {
            status = rejection.Reason;
            SyncCreditsFromEntityWorld(playerSlotId);
            return false;
        }

        var entity = LastNewConstructedEntity(owner, kind, _constructionEntityIdsBefore);
        if (entity is null)
        {
            status = "placement.rejected";
            SyncCreditsFromEntityWorld(playerSlotId);
            return false;
        }

        if (entity.Components.TryGet<PowerComponentState>(out var power))
        {
            entity.Components.Set(power with { Powered = true });
        }

        var adoptedId = AdoptConstructedBuildingId(entity, kind, playerSlotId, faction);
        building = RequiredBuildingSnapshot(adoptedId);
        SyncCreditsFromEntityWorld(playerSlotId);
        ResourceInventoryChanged?.Invoke(playerSlotId, inventory);
        status = GameText.Format("build.placed", spec.Label);
        return true;
    }

    public void RemoveBuildingTarget(int id)
    {
        if (RemoveBuildingTargetEntityId(id, out var entityId))
        {
            _entityWorld.Remove(entityId);
        }

        foreach (var unit in Units)
        {
            if (unit.AttackTargetKind != CombatTargetKind.Building || unit.AttackTargetId != id)
            {
                continue;
            }

            ClearAttackTarget(unit);
        }
    }

    public EntityId? BuildingEntityIdByTargetId(int id)
    {
        return _buildingTargetEntityIds.TryGetValue(id, out var entityId) && _entityWorld.TryGet(entityId, out _)
            ? entityId
            : null;
    }

    private EntityInstance? BuildingEntityByTargetId(int id)
    {
        return _buildingTargetEntityIds.TryGetValue(id, out var entityId) && _entityWorld.TryGet(entityId, out var entity)
            ? entity
            : null;
    }

    private void SetBuildingTargetEntityId(int buildingId, EntityId entityId)
    {
        if (_buildingTargetEntityIds.TryGetValue(buildingId, out var previousEntityId))
        {
            _buildingTargetIdsByEntityId.Remove(previousEntityId);
        }

        _buildingTargetEntityIds[buildingId] = entityId;
        _buildingTargetIdsByEntityId[entityId] = buildingId;
    }

    private bool RemoveBuildingTargetEntityId(int buildingId, out EntityId entityId)
    {
        if (!_buildingTargetEntityIds.Remove(buildingId, out entityId))
        {
            return false;
        }

        _buildingTargetIdsByEntityId.Remove(entityId);
        return true;
    }

}
