using Godot;

namespace ProceduralRts.Core;

public sealed partial class UnitBattlefield
{
    public int SelectRect(PlayerSlotId playerSlotId, Rect2 worldRect, bool additive)
    {
        var candidates = CollectSelectionRectCandidates(playerSlotId, worldRect);
        PrepareUnitSelectionBuffer(playerSlotId, additive);
        foreach (var entityId in candidates)
        {
            _selectionEntityBuffer.Add(entityId);
        }

        return SubmitSelectionBuffer(playerSlotId);
    }

    public int CountSelectionRectCandidates(PlayerSlotId playerSlotId, Rect2 worldRect)
    {
        return CollectSelectionRectCandidates(playerSlotId, worldRect).Count;
    }

    private IReadOnlyCollection<EntityId> CollectSelectionRectCandidates(PlayerSlotId playerSlotId, Rect2 worldRect)
    {
        var normalizedRect = worldRect.Abs();
        _selectionRectCandidateBuffer.Clear();
        _selectionRectEconomyUnits.Clear();
        _selectionRectCombatUnits.Clear();

        foreach (var unit in Units)
        {
            if (unit.PlayerSlotId != playerSlotId || !UnitOverlapsSelectionRect(normalizedRect, unit))
            {
                continue;
            }

            if (unit.Spec.RoleTags.Contains(UnitRoleTag.Economy))
            {
                _selectionRectEconomyUnits.Add(unit);
            }
            else
            {
                _selectionRectCombatUnits.Add(unit);
            }
        }

        foreach (var unit in _selectionRectCombatUnits)
        {
            _selectionRectCandidateBuffer.Add(unit.EntityId);
        }

        if (ShouldIncludeEconomyInSelectionRect(
                normalizedRect,
                _selectionRectEconomyUnits,
                _selectionRectCombatUnits))
        {
            foreach (var unit in _selectionRectEconomyUnits)
            {
                _selectionRectCandidateBuffer.Add(unit.EntityId);
            }
        }

        return _selectionRectCandidateBuffer;
    }

    public IReadOnlyList<UnitInstance> SelectUnitsByIds(PlayerSlotId playerSlotId, IEnumerable<int> unitIds)
    {
        CollectRequestedSelectionUnits(playerSlotId, unitIds, _selectionUnitBuffer);
        _selectionEntityBuffer.Clear();
        foreach (var unit in _selectionUnitBuffer)
        {
            _selectionEntityBuffer.Add(unit.EntityId);
        }

        SubmitSelectionBuffer(playerSlotId);
        return _selectionUnitBuffer;
    }

    public int SelectArmy(PlayerSlotId playerSlotId)
    {
        _selectionEntityBuffer.Clear();
        foreach (var unit in Units)
        {
            if (unit.PlayerSlotId == playerSlotId
                && unit.Hp > 0
                && !IsHarvester(unit))
            {
                _selectionEntityBuffer.Add(unit.EntityId);
            }
        }

        return SubmitSelectionBuffer(playerSlotId);
    }

    public UnitInstance? SelectNextIdleHarvester(PlayerSlotId playerSlotId)
    {
        var selectedIdleSeen = false;
        UnitInstance? firstIdleHarvester = null;
        UnitInstance? nextIdleHarvester = null;
        foreach (var unit in Units)
        {
            if (!IsIdleHarvester(playerSlotId, unit))
            {
                continue;
            }

            firstIdleHarvester ??= unit;
            if (selectedIdleSeen)
            {
                nextIdleHarvester = unit;
                break;
            }

            if (unit.Selected)
            {
                selectedIdleSeen = true;
            }
        }

        var target = nextIdleHarvester ?? firstIdleHarvester;
        if (target is null)
        {
            return null;
        }

        _selectionEntityBuffer.Clear();
        _selectionEntityBuffer.Add(target.EntityId);
        SubmitSelectionBuffer(playerSlotId);
        return target;
    }

    public int IdleHarvesterCount(PlayerSlotId playerSlotId, out Vector2? firstWorldPosition)
    {
        firstWorldPosition = null;
        var count = 0;
        foreach (var unit in Units)
        {
            if (!IsIdleHarvester(playerSlotId, unit))
            {
                continue;
            }

            firstWorldPosition ??= unit.Position;
            count++;
        }

        return count;
    }

    private static bool IsIdleHarvester(PlayerSlotId playerSlotId, UnitInstance unit)
    {
        return unit.PlayerSlotId == playerSlotId
            && unit.Hp > 0
            && IsHarvester(unit)
            && unit.HarvesterMode == HarvesterMode.Idle
            && unit.MoveTarget is null;
    }

    private void CollectRequestedSelectionUnits(PlayerSlotId playerSlotId, IEnumerable<int> unitIds, List<UnitInstance> result)
    {
        _selectionUnitIdBuffer.Clear();
        foreach (var unitId in unitIds)
        {
            _selectionUnitIdBuffer.Add(unitId);
        }

        result.Clear();
        if (_selectionUnitIdBuffer.Count == 0)
        {
            return;
        }

        foreach (var unit in Units)
        {
            if (unit.PlayerSlotId == playerSlotId && _selectionUnitIdBuffer.Contains(unit.Id))
            {
                result.Add(unit);
            }
        }

        result.Sort(CompareUnitInstanceIds);
    }

}
