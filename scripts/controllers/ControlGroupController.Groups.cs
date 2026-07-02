using Godot;
using ProceduralRts.Core;

namespace ProceduralRts.Controllers;

public partial class ControlGroupController
{
    private void SaveGroup(int groupNumber)
    {
        if (!_groups.TryGetValue(groupNumber, out var selectedIds))
        {
            selectedIds = [];
            _groups[groupNumber] = selectedIds;
        }

        CollectSelectedUnitIds(selectedIds);
        _groups[groupNumber] = selectedIds;
        _feedbackPulses[groupNumber] = 1;
        StatusChanged?.Invoke(GameText.Format("group.saved", groupNumber, selectedIds.Count));
    }

    private void RecallGroup(int groupNumber)
    {
        if (!_groups.TryGetValue(groupNumber, out var groupIds) || groupIds.Count == 0)
        {
            _feedbackPulses[groupNumber] = 1;
            StatusChanged?.Invoke(GameText.Format("group.empty", groupNumber));
            SelectionChanged?.Invoke(SelectUnitsByIds(_emptySelection));
            RememberRecall(groupNumber);
            return;
        }

        var doubleTap = IsDoubleTapRecall(groupNumber);
        var selectedCount = SelectUnitsByIds(groupIds);
        _feedbackPulses[groupNumber] = 1;
        SelectionChanged?.Invoke(selectedCount);
        if (doubleTap && selectedCount > 0 && GroupCenter(groupIds) is { } center)
        {
            FocusRequested?.Invoke(center);
        }

        RememberRecall(groupNumber);
        StatusChanged?.Invoke(GameText.Format("group.recalled", groupNumber, selectedCount));
    }

    private void CollectSelectedUnitIds(List<int> result)
    {
        result.Clear();
        if (UseUnitBattlefieldGroups())
        {
            foreach (var unit in UnitBattlefield!.Units)
            {
                if (unit.PlayerSlotId == LocalPlayerSlotId && unit.Selected)
                {
                    result.Add(unit.Id);
                }
            }

            return;
        }

        foreach (var unit in State.Units)
        {
            if (unit.Owner == ProceduralRts.Core.Owner.Player && unit.Selected)
            {
                result.Add(unit.Id);
            }
        }
    }

    private int SelectUnitsByIds(IReadOnlyList<int> unitIds)
    {
        if (!UseUnitBattlefieldGroups())
        {
            return SelectLegacyUnitsByIds(unitIds);
        }

        State.ClearSelection();
        return UnitBattlefield!.SelectUnitsByIds(LocalPlayerSlotId, unitIds).Count;
    }

    private int SelectLegacyUnitsByIds(IReadOnlyList<int> unitIds)
    {
        var selectedCount = 0;
        foreach (var unit in State.Units)
        {
            unit.Selected = unit.Owner == ProceduralRts.Core.Owner.Player
                && ContainsUnitId(unitIds, unit.Id);
            if (unit.Selected)
            {
                selectedCount++;
            }
        }

        foreach (var building in State.Buildings)
        {
            building.Selected = false;
        }

        return selectedCount;
    }

    private Vector2? GroupCenter(IReadOnlyList<int> unitIds)
    {
        var sum = Vector2.Zero;
        var count = 0;
        if (UseUnitBattlefieldGroups())
        {
            foreach (var unit in UnitBattlefield!.Units)
            {
                if (unit.PlayerSlotId != LocalPlayerSlotId
                    || unit.Hp <= 0
                    || !ContainsUnitId(unitIds, unit.Id))
                {
                    continue;
                }

                sum += unit.Position;
                count++;
            }
        }
        else
        {
            foreach (var unit in State.Units)
            {
                if (unit.Owner != ProceduralRts.Core.Owner.Player
                    || unit.Hp <= 0
                    || !ContainsUnitId(unitIds, unit.Id))
                {
                    continue;
                }

                sum += unit.Position;
                count++;
            }
        }

        if (count == 0)
        {
            return null;
        }

        return sum / count;
    }

    private static bool ContainsUnitId(IReadOnlyList<int> unitIds, int unitId)
    {
        for (var index = 0; index < unitIds.Count; index++)
        {
            if (unitIds[index] == unitId)
            {
                return true;
            }
        }

        return false;
    }

    private bool IsDoubleTapRecall(int groupNumber)
    {
        return _lastRecalledGroup == groupNumber
            && CurrentSeconds() - _lastRecallSeconds <= RecallCenterDoubleTapSeconds;
    }

    private void RememberRecall(int groupNumber)
    {
        _lastRecalledGroup = groupNumber;
        _lastRecallSeconds = CurrentSeconds();
    }

    private static double CurrentSeconds()
    {
        return Time.GetTicksMsec() / 1000.0;
    }
}
