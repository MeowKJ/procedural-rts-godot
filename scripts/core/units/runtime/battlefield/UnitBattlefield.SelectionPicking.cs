using Godot;

namespace ProceduralRts.Core;

public sealed partial class UnitBattlefield
{
    public IEnumerable<UnitInstance> SelectedUnits(PlayerSlotId playerSlotId)
    {
        return Units.Where(unit => unit.PlayerSlotId == playerSlotId && unit.Selected);
    }

    public int SelectedCount(PlayerSlotId playerSlotId)
    {
        return SelectedUnits(playerSlotId).Count();
    }

    public void ClearSelection(PlayerSlotId playerSlotId)
    {
        SubmitSelectionCommand(playerSlotId, []);
    }

    public UnitInstance? PickUnit(Vector2 worldPoint, PlayerSlotId playerSlotId, float pickPadding = 8)
    {
        return Units
            .Where(unit => unit.PlayerSlotId == playerSlotId)
            .Where(unit => unit.Position.DistanceSquaredTo(worldPoint) <= Mathf.Pow(unit.Spec.Collision.Radius + pickPadding, 2))
            .OrderBy(unit => unit.Position.DistanceSquaredTo(worldPoint))
            .FirstOrDefault();
    }

    public UnitInstance? PickAnyUnit(Vector2 worldPoint, float pickPadding = 8)
    {
        return Units
            .Where(unit => unit.Position.DistanceSquaredTo(worldPoint) <= Mathf.Pow(unit.Spec.Collision.Radius + pickPadding, 2))
            .OrderBy(unit => unit.Position.DistanceSquaredTo(worldPoint))
            .FirstOrDefault();
    }

    public UnitInstance? PickHostileUnit(Vector2 worldPoint, PlayerSlotId attackerPlayerSlotId, float pickPadding = 8)
    {
        return Units
            .Where(unit => Relations.CanAttack(attackerPlayerSlotId, unit.PlayerSlotId))
            .Where(unit => unit.Position.DistanceSquaredTo(worldPoint) <= Mathf.Pow(unit.Spec.Collision.Radius + pickPadding, 2))
            .OrderBy(unit => unit.Position.DistanceSquaredTo(worldPoint))
            .FirstOrDefault();
    }

    private int? PickHostileBuildingIdCore(Vector2 worldPoint, PlayerSlotId attackerPlayerSlotId, float pickPadding = 8)
    {
        return BuildingTargetIds()
            .Select(BuildingSnapshot)
            .Where(snapshot => snapshot is not null)
            .Select(snapshot => snapshot!.Value)
            .Where(building => building.Hp > 0 && Relations.CanAttack(attackerPlayerSlotId, building.PlayerSlotId))
            .Where(building => building.Position.DistanceSquaredTo(worldPoint) <= Mathf.Pow(BuildingTargetRadiusCore(building.Id, building.Kind) + pickPadding, 2))
            .OrderBy(building => building.Position.DistanceSquaredTo(worldPoint))
            .Select(building => (int?)building.Id)
            .FirstOrDefault();
    }

    public int? PickHostileBuildingId(Vector2 worldPoint, PlayerSlotId attackerPlayerSlotId, float pickPadding = 8)
    {
        return PickHostileBuildingIdCore(worldPoint, attackerPlayerSlotId, pickPadding);
    }

    public BuildingHoverProjection? PickHostileBuildingHoverProjection(Vector2 worldPoint, PlayerSlotId viewer, float pickPadding = 8)
    {
        var buildingId = PickHostileBuildingId(worldPoint, viewer, pickPadding);
        return buildingId is null ? null : BuildingHoverProjection(buildingId.Value, viewer);
    }

    private int? PickBuildingTargetIdCore(Vector2 worldPoint, PlayerSlotId playerSlotId, float pickPadding = 8)
    {
        return BuildingTargetIds()
            .Select(BuildingSnapshot)
            .Where(snapshot => snapshot is not null)
            .Select(snapshot => snapshot!.Value)
            .Where(building => building.Hp > 0 && building.PlayerSlotId == playerSlotId)
            .Where(building => building.Position.DistanceSquaredTo(worldPoint) <= Mathf.Pow(BuildingTargetRadiusCore(building.Id, building.Kind) + pickPadding, 2))
            .OrderBy(building => building.Position.DistanceSquaredTo(worldPoint))
            .ThenBy(building => building.Id)
            .Select(building => (int?)building.Id)
            .FirstOrDefault();
    }

    public int? PickBuildingTargetId(Vector2 worldPoint, PlayerSlotId playerSlotId, float pickPadding = 8)
    {
        return PickBuildingTargetIdCore(worldPoint, playerSlotId, pickPadding);
    }

    private int? PickAnyBuildingTargetIdCore(Vector2 worldPoint, float pickPadding = 8)
    {
        return BuildingTargetIds()
            .Select(BuildingSnapshot)
            .Where(snapshot => snapshot is not null)
            .Select(snapshot => snapshot!.Value)
            .Where(building => building.Hp > 0)
            .Where(building => building.Position.DistanceSquaredTo(worldPoint) <= Mathf.Pow(BuildingTargetRadiusCore(building.Id, building.Kind) + pickPadding, 2))
            .OrderBy(building => building.Position.DistanceSquaredTo(worldPoint))
            .ThenBy(building => building.Id)
            .Select(building => (int?)building.Id)
            .FirstOrDefault();
    }

    public int? PickAnyBuildingTargetId(Vector2 worldPoint, float pickPadding = 8)
    {
        return PickAnyBuildingTargetIdCore(worldPoint, pickPadding);
    }

    public BuildingHoverProjection? PickAnyBuildingHoverProjection(Vector2 worldPoint, PlayerSlotId viewer, float pickPadding = 8)
    {
        var buildingId = PickAnyBuildingTargetId(worldPoint, pickPadding);
        return buildingId is null ? null : BuildingHoverProjection(buildingId.Value, viewer);
    }

    public BuildingHoverProjection? BuildingHoverProjection(int buildingId, PlayerSlotId viewer)
    {
        var projection = BuildingPresentationProjection(buildingId);
        var identity = BuildingIdentity(buildingId);
        if (projection is null || identity is null)
        {
            return null;
        }

        return new BuildingHoverProjection(
            identity.LegacyBuildingId,
            identity.Kind,
            identity.PlayerSlotId,
            projection.Value.Entity.Position,
            projection.Value.Radius,
            Relations.Relation(viewer, identity.PlayerSlotId));
    }

    public IReadOnlyList<BuildingHitPulseProjection> BuildingHitPulseProjections()
    {
        SyncBuildingTargetEntities();
        return BuildingTargetIds()
            .Where(buildingId => BuildingProjection(buildingId) is { IsAlive: true })
            .OrderBy(buildingId => buildingId)
            .Select(BuildingHitPulseProjection)
            .Where(projection => projection is { HitPulse: > 0 })
            .Select(projection => projection!.Value)
            .ToList();
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
            identity.LegacyBuildingId,
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
        return BuildingTargetIds()
            .Where(buildingId => BuildingProjection(buildingId) is { IsAlive: true })
            .OrderBy(buildingId => buildingId)
            .Select(buildingId => BuildingMinimapProjection(viewer, buildingId, isExplored))
            .Where(projection => projection is not null)
            .Select(projection => projection!.Value)
            .ToList();
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
            identity.LegacyBuildingId,
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

    public int SelectSingleAt(PlayerSlotId playerSlotId, Vector2 worldPoint, bool additive, float pickPadding = 8)
    {
        var hit = PickUnit(worldPoint, playerSlotId, pickPadding);
        PrepareUnitSelectionBuffer(playerSlotId, additive);

        if (hit is not null)
        {
            if (additive && _selectionEntityBuffer.Contains(hit.EntityId))
            {
                _selectionEntityBuffer.Remove(hit.EntityId);
            }
            else
            {
                _selectionEntityBuffer.Add(hit.EntityId);
            }

            return SubmitSelectionBuffer(playerSlotId);
        }

        if (additive)
        {
            _selectionEntityBuffer.Clear();
            return SelectedCount(playerSlotId);
        }

        return SubmitSelectionBuffer(playerSlotId);
    }

    public int SelectSameUnitsAt(PlayerSlotId playerSlotId, Vector2 worldPoint, Rect2 visibleWorldRect, bool additive, float pickPadding = 8)
    {
        var hit = PickUnit(worldPoint, playerSlotId, pickPadding);
        if (hit is null)
        {
            return SelectSingleAt(playerSlotId, worldPoint, additive, pickPadding);
        }

        PrepareUnitSelectionBuffer(playerSlotId, additive);
        foreach (var unit in Units)
        {
            if (unit.PlayerSlotId == playerSlotId
                && unit.Spec.Id == hit.Spec.Id
                && visibleWorldRect.HasPoint(unit.Position))
            {
                _selectionEntityBuffer.Add(unit.EntityId);
            }
        }

        return SubmitSelectionBuffer(playerSlotId);
    }

    public int SelectBuildingTargetAt(PlayerSlotId playerSlotId, Vector2 worldPoint, bool additive, float pickPadding = 8)
    {
        var hitId = PickBuildingTargetId(worldPoint, playerSlotId, pickPadding);
        PrepareBuildingSelectionBuffer(playerSlotId, additive);

        if (hitId is not null && _buildingTargetEntityIds.TryGetValue(hitId.Value, out var entityId))
        {
            if (additive && _selectionEntityBuffer.Contains(entityId))
            {
                _selectionEntityBuffer.Remove(entityId);
            }
            else
            {
                _selectionEntityBuffer.Add(entityId);
            }
        }

        SubmitSelectionBuffer(playerSlotId);
        return SelectedBuildingSelectionProjections(playerSlotId).Count;
    }

    private void PrepareUnitSelectionBuffer(PlayerSlotId playerSlotId, bool additive)
    {
        _selectionEntityBuffer.Clear();
        if (!additive)
        {
            return;
        }

        foreach (var unit in Units)
        {
            if (unit.PlayerSlotId == playerSlotId && unit.Selected)
            {
                _selectionEntityBuffer.Add(unit.EntityId);
            }
        }
    }

    private void PrepareBuildingSelectionBuffer(PlayerSlotId playerSlotId, bool additive)
    {
        _selectionEntityBuffer.Clear();
        if (!additive)
        {
            return;
        }

        foreach (var entityId in SelectedBuildingEntityIds(playerSlotId))
        {
            _selectionEntityBuffer.Add(entityId);
        }
    }

    private int SubmitSelectionBuffer(PlayerSlotId playerSlotId)
    {
        var count = SubmitSelectionCommand(playerSlotId, _selectionEntityBuffer);
        _selectionEntityBuffer.Clear();
        return count;
    }

}
