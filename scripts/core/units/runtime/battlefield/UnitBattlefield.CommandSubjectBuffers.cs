namespace ProceduralRts.Core;

public sealed partial class UnitBattlefield
{
    private void CollectSelectedCommandUnits(PlayerSlotId playerSlotId, List<UnitInstance> result)
    {
        result.Clear();
        foreach (var unit in Units)
        {
            if (unit.PlayerSlotId == playerSlotId && unit.Selected)
            {
                result.Add(unit);
            }
        }

        result.Sort(CompareUnitInstanceIds);
    }

    private void CollectSelectedArmedCommandUnits(PlayerSlotId playerSlotId, List<UnitInstance> result)
    {
        result.Clear();
        foreach (var unit in Units)
        {
            if (unit.PlayerSlotId == playerSlotId && unit.Selected && unit.WeaponMounts.Count > 0)
            {
                result.Add(unit);
            }
        }

        result.Sort(CompareUnitInstanceIds);
    }

    private void CollectSelectedCommandUnitsTargeting(PlayerSlotId playerSlotId, UnitInstance target, List<UnitInstance> result)
    {
        result.Clear();
        foreach (var unit in Units)
        {
            if (unit.PlayerSlotId == playerSlotId && unit.Selected && CanUnitTarget(unit, target))
            {
                result.Add(unit);
            }
        }

        result.Sort(CompareUnitInstanceIds);
    }

    private void CollectSelectedCommandUnitsTargeting(PlayerSlotId playerSlotId, BuildSpec targetSpec, List<UnitInstance> result)
    {
        result.Clear();
        foreach (var unit in Units)
        {
            if (unit.PlayerSlotId == playerSlotId && unit.Selected && CanUnitTarget(unit, targetSpec))
            {
                result.Add(unit);
            }
        }

        result.Sort(CompareUnitInstanceIds);
    }

    private void CollectRequestedCommandUnits(PlayerSlotId playerSlotId, IEnumerable<int> unitIds, List<UnitInstance> result)
    {
        CollectRequestedCommandIds(unitIds);
        result.Clear();
        if (_unitCommandIdBuffer.Count == 0)
        {
            return;
        }

        foreach (var unit in Units)
        {
            if (unit.PlayerSlotId == playerSlotId && _unitCommandIdBuffer.Contains(unit.Id))
            {
                result.Add(unit);
            }
        }

        result.Sort(CompareUnitInstanceIds);
    }

    private void CollectRequestedCommandUnitsTargeting(
        PlayerSlotId playerSlotId,
        IEnumerable<int> unitIds,
        UnitInstance target,
        List<UnitInstance> result)
    {
        CollectRequestedCommandIds(unitIds);
        result.Clear();
        if (_unitCommandIdBuffer.Count == 0)
        {
            return;
        }

        foreach (var unit in Units)
        {
            if (unit.PlayerSlotId == playerSlotId && _unitCommandIdBuffer.Contains(unit.Id) && CanUnitTarget(unit, target))
            {
                result.Add(unit);
            }
        }

        result.Sort(CompareUnitInstanceIds);
    }

    private void CollectRequestedCommandUnitsTargeting(
        PlayerSlotId playerSlotId,
        IEnumerable<int> unitIds,
        BuildSpec targetSpec,
        List<UnitInstance> result)
    {
        CollectRequestedCommandIds(unitIds);
        result.Clear();
        if (_unitCommandIdBuffer.Count == 0)
        {
            return;
        }

        foreach (var unit in Units)
        {
            if (unit.PlayerSlotId == playerSlotId && _unitCommandIdBuffer.Contains(unit.Id) && CanUnitTarget(unit, targetSpec))
            {
                result.Add(unit);
            }
        }

        result.Sort(CompareUnitInstanceIds);
    }

    private void CollectRequestedCommandIds(IEnumerable<int> unitIds)
    {
        _unitCommandIdBuffer.Clear();
        foreach (var unitId in unitIds)
        {
            _unitCommandIdBuffer.Add(unitId);
        }
    }
}
