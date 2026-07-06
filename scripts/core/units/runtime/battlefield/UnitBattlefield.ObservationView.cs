namespace ProceduralRts.Core;

public sealed partial class UnitBattlefield
{
    public ObservationView CreateObservationView(PlayerSlotId viewerSlotId, int tick)
    {
        var viewerOwner = OwnerId.FromPlayerSlot(viewerSlotId);
        RebuildVisibilityIndex();

        CollectOwnerRelationSlots(_ownerRelationSlots);
        var knownPlayers = new List<ObservedPlayerState>(_ownerRelationSlots.Count);
        foreach (var slot in _ownerRelationSlots)
        {
            knownPlayers.Add(CreateObservedPlayerState(slot));
        }

        var visibleEntities = new List<ObservedEntity>();
        AddVisibleUnitObservations(viewerSlotId, visibleEntities);
        AddVisibleBuildingObservations(viewerSlotId, visibleEntities);
        visibleEntities.Sort(CompareObservedEntities);

        return new ObservationView(
            viewerSlotId,
            viewerOwner,
            tick,
            CreateObservedPlayerState(viewerSlotId),
            knownPlayers,
            visibleEntities,
            CreateCommandAffordances(viewerSlotId));
    }

    private ObservedPlayerState CreateObservedPlayerState(PlayerSlotId slot)
    {
        return new ObservedPlayerState(
            slot,
            OwnerId.FromPlayerSlot(slot),
            Credits(slot),
            IsKnownDefeatedSlot(slot));
    }

    private bool IsKnownDefeatedSlot(PlayerSlotId slot)
    {
        if (Outcome == GameOutcome.InProgress)
        {
            return false;
        }

        return slot == OutcomeViewer
            ? Outcome == GameOutcome.Defeat
            : Outcome == GameOutcome.Victory && Relations.Relation(OutcomeViewer, slot) == PlayerRelation.Hostile;
    }

    private void AddVisibleUnitObservations(PlayerSlotId viewerSlotId, List<ObservedEntity> result)
    {
        foreach (var unit in Units)
        {
            if (unit.Hp <= 0 || (unit.PlayerSlotId != viewerSlotId && !IsVisibleTo(viewerSlotId, unit)))
            {
                continue;
            }

            result.Add(new ObservedEntity(
                unit.EntityId,
                unit.Spec.Id,
                EntityKind.Unit,
                OwnerId.FromPlayerSlot(unit.PlayerSlotId),
                unit.Position.X,
                unit.Position.Y,
                unit.Facing,
                unit.Spec.Stats.MaxHp <= 0 ? 0 : Math.Clamp(unit.Hp / unit.Spec.Stats.MaxHp, 0, 1),
                unit.PlayerSlotId == viewerSlotId));
        }
    }

    private void AddVisibleBuildingObservations(PlayerSlotId viewerSlotId, List<ObservedEntity> result)
    {
        CollectBuildingTargetIds(_buildingTargetIdBuffer);
        foreach (var buildingId in _buildingTargetIdBuffer)
        {
            if (BuildingSnapshot(buildingId) is not { } building
                || building.Hp <= 0
                || (building.PlayerSlotId != viewerSlotId && !IsVisibleTo(viewerSlotId, building.Id))
                || BuildingEntityIdByTargetId(building.Id) is not { } entityId)
            {
                continue;
            }

            var spec = BuildSpecCatalog.For(building.Kind);
            var kind = _entityWorld.TryGetSpec(spec.EntitySpecId, out var entitySpec)
                ? entitySpec.Kind
                : EntityKind.Building;
            result.Add(new ObservedEntity(
                entityId,
                building.Kind,
                kind,
                OwnerId.FromPlayerSlot(building.PlayerSlotId),
                building.Position.X,
                building.Position.Y,
                building.Facing,
                spec.MaxHp <= 0 ? 0 : Math.Clamp(building.Hp / spec.MaxHp, 0, 1),
                building.PlayerSlotId == viewerSlotId));
        }
    }

    private IReadOnlyList<ObservedCommandAffordance> CreateCommandAffordances(PlayerSlotId viewerSlotId)
    {
        var selectedUnits = SelectedCount(viewerSlotId);
        var selectedBuildings = HasSelectedBuildings(viewerSlotId);
        return
        [
            new ObservedCommandAffordance(PlayerCommandKind.Select, string.Empty, true, string.Empty),
            new ObservedCommandAffordance(PlayerCommandKind.Move, string.Empty, selectedUnits > 0, selectedUnits > 0 ? string.Empty : "select.units"),
            new ObservedCommandAffordance(PlayerCommandKind.Attack, string.Empty, selectedUnits > 0, selectedUnits > 0 ? string.Empty : "select.units"),
            new ObservedCommandAffordance(PlayerCommandKind.AttackMove, string.Empty, selectedUnits > 0, selectedUnits > 0 ? string.Empty : "select.units"),
            new ObservedCommandAffordance(PlayerCommandKind.Stop, string.Empty, selectedUnits > 0, selectedUnits > 0 ? string.Empty : "select.units"),
            new ObservedCommandAffordance(PlayerCommandKind.HoldPosition, string.Empty, selectedUnits > 0, selectedUnits > 0 ? string.Empty : "select.units"),
            new ObservedCommandAffordance(PlayerCommandKind.Build, string.Empty, true, string.Empty),
            new ObservedCommandAffordance(PlayerCommandKind.Produce, string.Empty, true, string.Empty),
            new ObservedCommandAffordance(PlayerCommandKind.Rally, string.Empty, selectedBuildings, selectedBuildings ? string.Empty : "select.producer"),
            new ObservedCommandAffordance(PlayerCommandKind.Harvest, string.Empty, selectedUnits > 0, selectedUnits > 0 ? string.Empty : "select.harvester"),
            new ObservedCommandAffordance(PlayerCommandKind.Repair, string.Empty, selectedUnits > 0, selectedUnits > 0 ? string.Empty : "select.repairer"),
        ];
    }

    private static int CompareObservedEntities(ObservedEntity left, ObservedEntity right)
    {
        return left.Id.Value.CompareTo(right.Id.Value);
    }
}
