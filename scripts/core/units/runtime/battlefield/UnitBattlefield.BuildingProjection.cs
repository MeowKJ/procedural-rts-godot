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
        return BuildingTargetIds()
            .Select(BuildingSnapshot)
            .Where(snapshot => snapshot is not null)
            .Select(snapshot => snapshot!.Value)
            .ToArray();
    }

    private IReadOnlyList<int> BuildingTargetIds()
    {
        var ids = new List<int>();
        var seen = new HashSet<int>();
        foreach (var entity in _entityWorld.OrderedEntities)
        {
            if (entity.Components.TryGet<BuildingIdentityComponentState>(out var identity)
                && seen.Add(identity.LegacyBuildingId))
            {
                ids.Add(identity.LegacyBuildingId);
            }
        }

        return ids;
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
        return BuildingSnapshots().Count(building => building.Hp > 0
            && (playerSlotId is null || building.PlayerSlotId == playerSlotId.Value));
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
        return BuildingTargetIds()
            .Where(buildingId => BuildingIdentity(buildingId)?.PlayerSlotId == playerSlotId)
            .Select(BuildingPresentationProjection)
            .Where(projection => projection is { Entity.Selected: true, RallyPoint: not null })
            .Select(projection => new BuildingRallyProjection(
                projection!.Value.Entity.Id.Value,
                projection.Value.Entity.Position,
                projection.Value.RallyPoint!.Value,
                projection.Value.RallyPulse))
            .OrderBy(projection => projection.Id)
            .ToList();
    }

    public bool HasSelectedBuildings(PlayerSlotId playerSlotId)
    {
        SyncBuildingTargetEntities();
        return BuildingTargetIds()
            .Where(buildingId => BuildingIdentity(buildingId)?.PlayerSlotId == playerSlotId)
            .Select(BuildingProjection)
            .Any(projection => projection?.Selected == true);
    }

    public IReadOnlyList<BuildingSelectionProjection> SelectedBuildingSelectionProjections(PlayerSlotId playerSlotId)
    {
        SyncBuildingTargetEntities();
        return BuildingTargetIds()
            .Where(buildingId => BuildingIdentity(buildingId)?.PlayerSlotId == playerSlotId)
            .OrderBy(buildingId => buildingId)
            .Select(BuildingSelectionProjection)
            .Where(projection => projection is not null)
            .Select(projection => projection!.Value)
            .ToList();
    }

    private IEnumerable<EntityId> SelectedBuildingEntityIds(PlayerSlotId playerSlotId)
    {
        SyncBuildingTargetEntities();
        return BuildingTargetIds()
            .Where(buildingId => BuildingIdentity(buildingId)?.PlayerSlotId == playerSlotId)
            .Where(buildingId => BuildingProjection(buildingId)?.Selected == true)
            .Where(buildingId => _buildingTargetEntityIds.ContainsKey(buildingId))
            .Select(buildingId => _buildingTargetEntityIds[buildingId]);
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
