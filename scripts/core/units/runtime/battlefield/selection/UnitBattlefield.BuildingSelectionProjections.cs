using Godot;

namespace ProceduralRts.Core;

public sealed partial class UnitBattlefield
{
    public BuildingHoverProjection? BuildingHoverProjection(int buildingId, PlayerSlotId viewer)
    {
        var projection = BuildingPresentationProjection(buildingId);
        var identity = BuildingIdentity(buildingId);
        if (projection is null || identity is null)
        {
            return null;
        }

        return new BuildingHoverProjection(
            identity.BuildingId,
            identity.Kind,
            identity.PlayerSlotId,
            projection.Value.Entity.Position,
            projection.Value.Radius,
            Relations.Relation(viewer, identity.PlayerSlotId));
    }

    public IReadOnlyList<BuildingHitPulseProjection> BuildingHitPulseProjections()
    {
        SyncBuildingTargetEntities();
        CollectBuildingTargetIds(_buildingProjectionTargetIdBuffer);
        _buildingHitPulseProjectionBuffer.Clear();
        foreach (var buildingId in _buildingProjectionTargetIdBuffer)
        {
            if (BuildingProjection(buildingId) is not { IsAlive: true }
                || BuildingHitPulseProjection(buildingId) is not { HitPulse: > 0 } projection)
            {
                continue;
            }

            _buildingHitPulseProjectionBuffer.Add(projection);
        }

        return _buildingHitPulseProjectionBuffer;
    }

    private BuildingHitPulseProjection? BuildingHitPulseProjection(int buildingId)
    {
        var projection = BuildingPresentationProjection(buildingId);
        var identity = BuildingIdentity(buildingId);
        if (projection is null || identity is null)
        {
            return null;
        }

        var spec = BuildSpecCatalog.For(identity.Kind);
        return new BuildingHitPulseProjection(
            identity.BuildingId,
            projection.Value.Entity.Position,
            projection.Value.Radius,
            projection.Value.HitPulse,
            spec.Accent.Lerp(PlayerSlotAccent(identity.PlayerSlotId), 0.36f));
    }

    public IReadOnlyList<BuildingMinimapProjection> BuildingMinimapProjections(
        PlayerSlotId viewer,
        Func<Rect2, bool>? isExplored = null)
    {
        SyncBuildingTargetEntities();
        CollectBuildingTargetIds(_buildingProjectionTargetIdBuffer);
        var result = NextBuildingMinimapProjectionBuffer();
        foreach (var buildingId in _buildingProjectionTargetIdBuffer)
        {
            if (BuildingProjection(buildingId) is not { IsAlive: true }
                || BuildingMinimapProjection(viewer, buildingId, isExplored) is not { } projection)
            {
                continue;
            }

            result.Add(projection);
        }

        return result;
    }

    private List<BuildingMinimapProjection> NextBuildingMinimapProjectionBuffer()
    {
        _useSecondaryBuildingMinimapProjectionBuffer = !_useSecondaryBuildingMinimapProjectionBuffer;
        var result = _useSecondaryBuildingMinimapProjectionBuffer
            ? _buildingMinimapProjectionSecondaryBuffer
            : _buildingMinimapProjectionBuffer;
        result.Clear();
        return result;
    }

    private BuildingMinimapProjection? BuildingMinimapProjection(
        PlayerSlotId viewer,
        int buildingId,
        Func<Rect2, bool>? isExplored)
    {
        var projection = BuildingPresentationProjection(buildingId);
        var identity = BuildingIdentity(buildingId);
        if (projection is null || identity is null)
        {
            return null;
        }

        var footprint = projection.Value.Footprint;
        var rect = new Rect2(projection.Value.Entity.Position - footprint / 2f, footprint);
        var relation = Relations.Relation(viewer, identity.PlayerSlotId);
        if (relation is not (PlayerRelation.Self or PlayerRelation.Allied)
            && !(isExplored?.Invoke(rect) ?? true))
        {
            return null;
        }

        return new BuildingMinimapProjection(
            identity.BuildingId,
            projection.Value.Entity.Position,
            footprint,
            identity.PlayerSlotId,
            identity.Faction,
            projection.Value.Entity.Selected,
            projection.Value.HitPulse);
    }

    public UnitBattlefieldPowerStatusProjection PowerStatus(PlayerSlotId playerSlotId)
    {
        SyncBuildingTargetEntities();
        var owner = OwnerId.FromPlayerSlot(playerSlotId);
        var provided = 0;
        var used = 0;
        var hasProvider = false;

        foreach (var entity in _entityWorld.OrderedEntities)
        {
            if (entity.OwnerId != owner || !entity.Components.TryGet<PowerComponentState>(out var power))
            {
                continue;
            }

            if (!IsActiveBuildingPowerEntity(entity))
            {
                continue;
            }

            provided += power.Provided;
            used += power.Used;
            hasProvider |= power.Provided > 0;
        }

        return new UnitBattlefieldPowerStatusProjection(
            playerSlotId,
            provided,
            used,
            hasProvider,
            hasProvider && provided >= used);
    }

    private static bool IsActiveBuildingPowerEntity(EntityInstance entity)
    {
        if (entity.Components.TryGet<HealthComponentState>(out var health) && health.Hp <= 0)
        {
            return false;
        }

        if (entity.Components.TryGet<ConstructionComponentState>(out var construction) && construction.Progress < 1)
        {
            return false;
        }

        return true;
    }
}
