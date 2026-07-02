using Godot;

namespace ProceduralRts.Core;

public sealed partial class UnitBattlefield
{
    public bool SetBuildingTargetSelected(int id, bool selected)
    {
        if (BuildingEntityByTargetId(id) is not { } entity)
        {
            return false;
        }

        var alertPulse = entity.Components.TryGet<SelectableComponentState>(out var selectable)
            ? selectable.AlertPulse
            : BuildingHitPulseCore(id);
        entity.Components.Set(new SelectableComponentState(selected, alertPulse));
        return true;
    }

    public EntityProjection? BuildingProjection(int id)
    {
        var entity = BuildingEntityByTargetId(id);
        return entity is null ? null : EntityProjector.ProjectOne(_entityWorld, entity);
    }

    public IReadOnlyList<UnitBattlefieldBuildingSnapshot> BuildingSnapshots()
    {
        CollectBuildingTargetIds(_buildingProjectionTargetIdBuffer);
        _buildingSnapshotBuffer.Clear();
        foreach (var buildingId in _buildingProjectionTargetIdBuffer)
        {
            if (BuildingSnapshot(buildingId) is { } snapshot)
            {
                _buildingSnapshotBuffer.Add(snapshot);
            }
        }

        return _buildingSnapshotBuffer;
    }

    private void CollectBuildingTargetIds(List<int> result)
    {
        result.Clear();
        foreach (var entry in _buildingTargetEntityIds)
        {
            if (!_entityWorld.TryGet(entry.Value, out var entity)
                || !entity.Components.TryGet<BuildingIdentityComponentState>(out var identity))
            {
                continue;
            }

            result.Add(identity.LegacyBuildingId);
        }

        result.Sort(CompareBuildingIds);
    }

    public UnitBattlefieldBuildingSnapshot? BuildingSnapshot(int id)
    {
        var identity = BuildingIdentity(id);
        if (identity is null)
        {
            return null;
        }

        if (BuildingProjection(id) is not { } projection)
        {
            return null;
        }

        var spec = BuildSpecCatalog.For(identity.Kind);

        return new UnitBattlefieldBuildingSnapshot(
            identity.LegacyBuildingId,
            identity.Kind,
            identity.PlayerSlotId,
            identity.Faction,
            projection.Position,
            projection.Facing,
            projection.Hp,
            spec.Footprint);
    }

    private UnitBattlefieldBuildingSnapshot RequiredBuildingSnapshot(int id)
    {
        return BuildingSnapshot(id) ?? throw new InvalidOperationException($"Building target {id} does not exist.");
    }

    public int LiveBuildingCount(PlayerSlotId? playerSlotId = null)
    {
        var count = 0;
        foreach (var building in BuildingSnapshots())
        {
            if (building.Hp > 0
                && (playerSlotId is null || building.PlayerSlotId == playerSlotId.Value))
            {
                count++;
            }
        }

        return count;
    }

    public BuildingPresentationProjection? BuildingPresentationProjection(int id)
    {
        var entity = BuildingEntityByTargetId(id);
        return entity is null ? null : BuildingPresentationProjector.ProjectOne(_entityWorld, entity);
    }

    private BuildingIdentityComponentState? BuildingIdentity(int buildingId)
    {
        return BuildingEntityByTargetId(buildingId)?.Components.TryGet<BuildingIdentityComponentState>(out var identity) == true
            ? identity
            : null;
    }

    public BuildingViewProjection? BuildingViewProjection(int id)
    {
        var presentation = BuildingPresentationProjection(id);
        var identity = BuildingIdentity(id);
        if (identity is null || presentation is null)
        {
            return null;
        }

        return new BuildingViewProjection(
            identity.LegacyBuildingId,
            identity.Kind,
            identity.PlayerSlotId,
            identity.Faction,
            presentation.Value);
    }

    public IReadOnlyList<BuildingRallyProjection> SelectedBuildingRallyProjections(PlayerSlotId playerSlotId)
    {
        SyncBuildingTargetEntities();
        CollectBuildingTargetIds(_buildingProjectionTargetIdBuffer);
        _buildingRallyProjectionBuffer.Clear();
        foreach (var buildingId in _buildingProjectionTargetIdBuffer)
        {
            if (BuildingIdentity(buildingId)?.PlayerSlotId != playerSlotId
                || BuildingPresentationProjection(buildingId) is not { Entity.Selected: true, RallyPoint: not null } projection)
            {
                continue;
            }

            _buildingRallyProjectionBuffer.Add(new BuildingRallyProjection(
                projection.Entity.Id.Value,
                projection.Entity.Position,
                projection.RallyPoint.Value,
                projection.RallyPulse));
        }

        _buildingRallyProjectionBuffer.Sort(CompareBuildingRallyProjectionIds);
        return _buildingRallyProjectionBuffer;
    }

    public bool HasSelectedBuildings(PlayerSlotId playerSlotId)
    {
        SyncBuildingTargetEntities();
        CollectBuildingTargetIds(_buildingProjectionTargetIdBuffer);
        foreach (var buildingId in _buildingProjectionTargetIdBuffer)
        {
            if (BuildingIdentity(buildingId)?.PlayerSlotId == playerSlotId
                && BuildingProjection(buildingId)?.Selected == true)
            {
                return true;
            }
        }

        return false;
    }

    public IReadOnlyList<BuildingSelectionProjection> SelectedBuildingSelectionProjections(PlayerSlotId playerSlotId)
    {
        SyncBuildingTargetEntities();
        CollectBuildingTargetIds(_buildingProjectionTargetIdBuffer);
        _buildingSelectionProjectionBuffer.Clear();
        foreach (var buildingId in _buildingProjectionTargetIdBuffer)
        {
            if (BuildingIdentity(buildingId)?.PlayerSlotId != playerSlotId
                || BuildingSelectionProjection(buildingId) is not { } projection)
            {
                continue;
            }

            _buildingSelectionProjectionBuffer.Add(projection);
        }

        return _buildingSelectionProjectionBuffer;
    }

    private void CollectSelectedBuildingEntityIds(PlayerSlotId playerSlotId, List<EntityId> result)
    {
        result.Clear();
        SyncBuildingTargetEntities();
        CollectBuildingTargetIds(_buildingProjectionTargetIdBuffer);
        foreach (var buildingId in _buildingProjectionTargetIdBuffer)
        {
            if (BuildingIdentity(buildingId)?.PlayerSlotId == playerSlotId
                && BuildingProjection(buildingId)?.Selected == true
                && _buildingTargetEntityIds.TryGetValue(buildingId, out var entityId))
            {
                result.Add(entityId);
            }
        }
    }

    private static int CompareBuildingRallyProjectionIds(BuildingRallyProjection left, BuildingRallyProjection right)
    {
        return left.Id.CompareTo(right.Id);
    }

    private BuildingSelectionProjection? BuildingSelectionProjection(int buildingId)
    {
        var entityProjection = BuildingProjection(buildingId);
        var presentationProjection = BuildingPresentationProjection(buildingId);
        var identity = BuildingIdentity(buildingId);
        if (entityProjection is not { Selected: true } || presentationProjection is null || identity is null)
        {
            return null;
        }

        var spec = BuildSpecCatalog.For(identity.Kind);
        return new BuildingSelectionProjection(
            identity.LegacyBuildingId,
            identity.Kind,
            identity.PlayerSlotId,
            identity.Faction,
            spec.Label,
            entityProjection.Value.Hp,
            entityProjection.Value.MaxHp,
            spec.SightRange,
            presentationProjection.Value.RallyPoint is not null,
            presentationProjection.Value.ProductionQueue,
            spec.Icon,
            spec.ShortCode,
            spec.Accent.Lerp(PlayerSlotAccent(identity.PlayerSlotId), 0.36f));
    }

}
