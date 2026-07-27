using Godot;

namespace ProceduralRts.Core;

public sealed partial class UnitBattlefield
{
    public IReadOnlyList<UnitMinimapPip> MinimapPips(PlayerSlotId viewer)
    {
        var result = NextUnitMinimapPipBuffer();
        foreach (var unit in Units)
        {
            result.Add(new UnitMinimapPip(
                unit.Position,
                unit.PlayerSlotId,
                unit.Spec.Faction,
                Relations.Relation(viewer, unit.PlayerSlotId),
                unit.Selected,
                unit.AlertPulse,
                IsVisibleTo(viewer, unit)));
        }

        return result;
    }

    private List<UnitMinimapPip> NextUnitMinimapPipBuffer()
    {
        _useSecondaryUnitMinimapPipBuffer = !_useSecondaryUnitMinimapPipBuffer;
        var result = _useSecondaryUnitMinimapPipBuffer ? _unitMinimapPipSecondaryBuffer : _unitMinimapPipBuffer;
        result.Clear();
        return result;
    }

    public void RebuildVisibilityIndex()
    {
        SyncOwnerRelations();
        _visionSystem.Step(new SimContext(_entityWorld, _inputCommandTick, 0, []));
        MarkVisibleBuildingFootprints();
    }

    public bool IsVisibleTo(PlayerSlotId viewer, UnitInstance unit)
    {
        return _entityWorld.Visibility.IsVisible(OwnerId.FromPlayerSlot(viewer), unit.EntityId);
    }

    public bool IsVisibleTo(PlayerSlotId viewer, int buildingId)
    {
        return IsVisibleToCore(viewer, buildingId);
    }

    private bool IsVisibleToCore(PlayerSlotId viewer, int buildingId)
    {
        return BuildingEntityByTargetId(buildingId) is { } entity
            && _entityWorld.Visibility.IsVisible(OwnerId.FromPlayerSlot(viewer), entity.Id);
    }

    private void MarkVisibleBuildingFootprints()
    {
        foreach (var viewer in Units)
        {
            if (viewer.Hp <= 0)
            {
                continue;
            }

            MarkVisibleBuildingFootprints(
                viewer.PlayerSlotId,
                viewer.Position,
                viewer.Spec.Stats.SightRange);
        }

        CollectBuildingTargetIds(_buildingVisibilityViewerIdBuffer);
        foreach (var buildingId in _buildingVisibilityViewerIdBuffer)
        {
            if (BuildingSnapshot(buildingId) is not { } viewer
                || viewer.Hp <= 0
                || BuildingBuildProgress(viewer.Id) < 1)
            {
                continue;
            }

            MarkVisibleBuildingFootprints(
                viewer.PlayerSlotId,
                viewer.Position,
                BuildSpecCatalog.For(viewer.Kind).SightRange);
        }
    }

    private void MarkVisibleBuildingFootprints(PlayerSlotId viewer, Vector2 viewerPosition, float sightRange)
    {
        if (sightRange <= 0)
        {
            return;
        }

        var owner = OwnerId.FromPlayerSlot(viewer);
        CollectBuildingTargetIds(_buildingVisibilityTargetIdBuffer);
        foreach (var buildingId in _buildingVisibilityTargetIdBuffer)
        {
            if (BuildingSnapshot(buildingId) is not { } building)
            {
                continue;
            }

            if (building.Hp <= 0
                || !Relations.CanAttack(viewer, building.PlayerSlotId)
                || !_buildingTargetEntityIds.TryGetValue(building.Id, out var entityId)
                || _entityWorld.Visibility.IsVisible(owner, entityId))
            {
                continue;
            }

            var visibleRange = sightRange + BuildingTargetRadiusCore(building.Id, building.Kind);
            if (viewerPosition.DistanceSquaredTo(building.Position) <= visibleRange * visibleRange)
            {
                _entityWorld.Visibility.MarkVisible(owner, entityId);
            }
        }
    }

    public IReadOnlyList<UnitSelectionSummaryItem> SelectionSummary()
    {
        _selectionSummaryBuffer.Clear();
        foreach (var unit in Units)
        {
            if (unit.Selected)
            {
                AddSelectionSummaryUnit(unit);
            }
        }

        _selectionSummaryBuffer.Sort(CompareUnitSelectionSummaryItems);
        return _selectionSummaryBuffer;
    }

    private void AddSelectionSummaryUnit(UnitInstance unit)
    {
        for (var index = 0; index < _selectionSummaryBuffer.Count; index++)
        {
            var item = _selectionSummaryBuffer[index];
            if (item.DesignId == unit.Spec.Id && item.PlayerSlotId == unit.PlayerSlotId)
            {
                _selectionSummaryBuffer[index] = item with { Count = item.Count + 1 };
                return;
            }
        }

        _selectionSummaryBuffer.Add(new UnitSelectionSummaryItem(
            unit.Spec.Id,
            unit.PlayerSlotId,
            unit.Spec.Faction,
            unit.Spec.Icon,
            unit.Spec.Label,
            unit.Spec.ShortCode,
            1));
    }

    private static int CompareUnitSelectionSummaryItems(UnitSelectionSummaryItem left, UnitSelectionSummaryItem right)
    {
        return string.Compare(left.DesignId, right.DesignId, StringComparison.Ordinal);
    }

}
