using Godot;

namespace ProceduralRts.Core;

public sealed partial class UnitBattlefield
{
    public int SelectRect(PlayerSlotId playerSlotId, Rect2 worldRect, bool additive)
    {
        var normalizedRect = worldRect.Abs();
        var includeEconomy = ShouldIncludeEconomyInSelectionRect(playerSlotId, normalizedRect);
        PrepareUnitSelectionBuffer(playerSlotId, additive);
        foreach (var unit in Units)
        {
            if (unit.PlayerSlotId != playerSlotId)
            {
                continue;
            }

            var selectableByBox = UnitOverlapsSelectionRect(normalizedRect, unit)
                && (!unit.Spec.RoleTags.Contains(UnitRoleTag.Economy) || includeEconomy);
            if (selectableByBox)
            {
                _selectionEntityBuffer.Add(unit.EntityId);
            }
        }

        return SubmitSelectionBuffer(playerSlotId);
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

    public void CommandMoveSelected(PlayerSlotId playerSlotId, Vector2 target, Vector2 worldSize, MoveCommandMode mode = MoveCommandMode.Direct)
    {
        var selected = SelectedUnits(playerSlotId)
            .OrderBy(unit => unit.Id)
            .ToList();
        if (selected.Count == 0)
        {
            return;
        }

        WorldSize = worldSize;
        _entityWorld.WorldWidth = worldSize.X;
        _entityWorld.WorldHeight = worldSize.Y;
        SubmitAndApplyInputCommand(new GroupMoveEntityCommand(
            OwnerId.FromPlayerSlot(playerSlotId),
            selected.Select(unit => unit.EntityId).ToList(),
            NextInputCommandTick(),
            target,
            mode));
    }

    public int CommandMoveUnits(
        PlayerSlotId playerSlotId,
        IEnumerable<int> unitIds,
        Vector2 target,
        Vector2 worldSize,
        MoveCommandMode mode = MoveCommandMode.Direct)
    {
        var requestedIds = unitIds.ToHashSet();
        var orderedUnits = Units
            .Where(unit => unit.PlayerSlotId == playerSlotId && requestedIds.Contains(unit.Id))
            .OrderBy(unit => unit.Id)
            .ToList();
        if (orderedUnits.Count == 0)
        {
            return 0;
        }

        WorldSize = worldSize;
        _entityWorld.WorldWidth = worldSize.X;
        _entityWorld.WorldHeight = worldSize.Y;
        SubmitAndApplyInputCommand(new GroupMoveEntityCommand(
            OwnerId.FromPlayerSlot(playerSlotId),
            orderedUnits.Select(unit => unit.EntityId).ToList(),
            NextInputCommandTick(),
            target,
            mode));
        return orderedUnits.Count;
    }

    public void CommandAttackSelected(PlayerSlotId playerSlotId, UnitInstance target)
    {
        if (!Relations.CanAttack(playerSlotId, target.PlayerSlotId))
        {
            return;
        }

        var attackers = SelectedUnits(playerSlotId)
            .Where(unit => CanUnitTarget(unit, target))
            .OrderBy(unit => unit.Id)
            .ToList();
        if (attackers.Count == 0)
        {
            return;
        }

        SyncUnitEntity(target);
        SubmitAndApplyInputCommand(new GroupAttackEntityCommand(
            OwnerId.FromPlayerSlot(playerSlotId),
            attackers.Select(unit => unit.EntityId).ToList(),
            NextInputCommandTick(),
            target.EntityId,
            CombatTargetKind.Unit));
    }

    public bool CommandAttackSelected(PlayerSlotId playerSlotId, int buildingId)
    {
        if (BuildingSnapshot(buildingId) is not { } target
            || BuildingEntityByTargetId(buildingId) is not { } targetEntity)
        {
            return false;
        }

        if (!Relations.CanAttack(playerSlotId, target.PlayerSlotId))
        {
            return false;
        }

        var targetSpec = BuildSpecCatalog.For(target.Kind);
        var attackers = SelectedUnits(playerSlotId)
            .Where(unit => CanUnitTarget(unit, targetSpec))
            .OrderBy(unit => unit.Id)
            .ToList();
        if (attackers.Count == 0)
        {
            return false;
        }

        SubmitAndApplyInputCommand(new GroupAttackEntityCommand(
            OwnerId.FromPlayerSlot(playerSlotId),
            attackers.Select(unit => unit.EntityId).ToList(),
            NextInputCommandTick(),
            targetEntity.Id,
            CombatTargetKind.Building));
        return true;
    }

    public void CommandStopSelected(PlayerSlotId playerSlotId)
    {
        var selected = SelectedUnits(playerSlotId)
            .OrderBy(unit => unit.Id)
            .ToList();
        if (selected.Count == 0)
        {
            return;
        }

        SubmitAndApplyInputCommand(new StopEntityCommand(
            OwnerId.FromPlayerSlot(playerSlotId),
            selected.Select(unit => unit.EntityId).ToList(),
            NextInputCommandTick()));
    }

    public int CommandSetSelectedStance(PlayerSlotId playerSlotId, UnitStance stance)
    {
        var selected = SelectedUnits(playerSlotId)
            .Where(unit => unit.WeaponMounts.Count > 0)
            .OrderBy(unit => unit.Id)
            .ToList();
        if (selected.Count == 0)
        {
            return 0;
        }

        SubmitAndApplyInputCommand(new SetStanceEntityCommand(
            OwnerId.FromPlayerSlot(playerSlotId),
            selected.Select(unit => unit.EntityId).ToList(),
            NextInputCommandTick(),
            stance));
        return selected.Count;
    }

    public int CommandAttackUnits(PlayerSlotId playerSlotId, IEnumerable<int> unitIds, UnitInstance target)
    {
        if (!Relations.CanAttack(playerSlotId, target.PlayerSlotId))
        {
            return 0;
        }

        var requestedIds = unitIds.ToHashSet();
        var orderedUnits = Units
            .Where(unit => unit.PlayerSlotId == playerSlotId && requestedIds.Contains(unit.Id))
            .Where(unit => CanUnitTarget(unit, target))
            .OrderBy(unit => unit.Id)
            .ToList();
        if (orderedUnits.Count == 0)
        {
            return 0;
        }

        SyncUnitEntity(target);
        SubmitAndApplyInputCommand(new GroupAttackEntityCommand(
            OwnerId.FromPlayerSlot(playerSlotId),
            orderedUnits.Select(unit => unit.EntityId).ToList(),
            NextInputCommandTick(),
            target.EntityId,
            CombatTargetKind.Unit));
        return orderedUnits.Count;
    }

    public int CommandAttackUnits(PlayerSlotId playerSlotId, IEnumerable<int> unitIds, int buildingId)
    {
        if (BuildingSnapshot(buildingId) is not { } target
            || BuildingEntityByTargetId(buildingId) is not { } targetEntity)
        {
            return 0;
        }

        if (!Relations.CanAttack(playerSlotId, target.PlayerSlotId))
        {
            return 0;
        }

        var targetSpec = BuildSpecCatalog.For(target.Kind);
        var requestedIds = unitIds.ToHashSet();
        var orderedUnits = Units
            .Where(unit => unit.PlayerSlotId == playerSlotId && requestedIds.Contains(unit.Id))
            .Where(unit => CanUnitTarget(unit, targetSpec))
            .OrderBy(unit => unit.Id)
            .ToList();
        if (orderedUnits.Count == 0)
        {
            return 0;
        }

        SubmitAndApplyInputCommand(new GroupAttackEntityCommand(
            OwnerId.FromPlayerSlot(playerSlotId),
            orderedUnits.Select(unit => unit.EntityId).ToList(),
            NextInputCommandTick(),
            targetEntity.Id,
            CombatTargetKind.Building));
        return orderedUnits.Count;
    }

    private void CollectRequestedSelectionUnits(PlayerSlotId playerSlotId, IEnumerable<int> unitIds, List<UnitInstance> result)
    {
        _unitCommandIdBuffer.Clear();
        foreach (var unitId in unitIds)
        {
            _unitCommandIdBuffer.Add(unitId);
        }

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

    private bool ShouldIncludeEconomyInSelectionRect(PlayerSlotId playerSlotId, Rect2 worldRect)
    {
        var economyCount = 0;
        var nonEconomyCount = 0;
        var nearestEconomy = float.PositiveInfinity;
        var nearestNonEconomy = float.PositiveInfinity;
        var center = worldRect.Position + worldRect.Size / 2f;

        foreach (var unit in Units)
        {
            if (unit.PlayerSlotId != playerSlotId || !UnitOverlapsSelectionRect(worldRect, unit))
            {
                continue;
            }

            var distanceToCenter = unit.Position.DistanceTo(center);
            if (unit.Spec.RoleTags.Contains(UnitRoleTag.Economy))
            {
                economyCount++;
                nearestEconomy = MathF.Min(nearestEconomy, distanceToCenter);
            }
            else
            {
                nonEconomyCount++;
                nearestNonEconomy = MathF.Min(nearestNonEconomy, distanceToCenter);
            }
        }

        if (economyCount == 0)
        {
            return false;
        }

        if (nonEconomyCount == 0 || economyCount > nonEconomyCount)
        {
            return true;
        }

        var rectSize = worldRect.Size;
        var maxSide = Mathf.Max(Mathf.Abs(rectSize.X), Mathf.Abs(rectSize.Y));
        if (maxSide > SelectionHarvesterIntentMaxSize)
        {
            return false;
        }

        return nearestEconomy <= nearestNonEconomy + SelectionEconomyIntentCenterMargin;
    }

}
