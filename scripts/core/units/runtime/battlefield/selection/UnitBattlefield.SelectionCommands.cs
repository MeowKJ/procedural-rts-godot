using Godot;

namespace ProceduralRts.Core;

public sealed partial class UnitBattlefield
{
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

        CollectSelectedBuildingEntityIds(playerSlotId, _selectedBuildingEntityIdBuffer);
        foreach (var entityId in _selectedBuildingEntityIdBuffer)
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
