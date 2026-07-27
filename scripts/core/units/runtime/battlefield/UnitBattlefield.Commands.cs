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

    public void CommandMoveSelected(PlayerSlotId playerSlotId, Vector2 target, Vector2 worldSize, MoveCommandMode mode = MoveCommandMode.Direct)
    {
        CollectSelectedCommandUnits(playerSlotId, _unitCommandBuffer);
        if (_unitCommandBuffer.Count == 0)
        {
            return;
        }

        WorldSize = worldSize;
        _entityWorld.WorldWidth = worldSize.X;
        _entityWorld.WorldHeight = worldSize.Y;
        CollectCommandEntityIds(_unitCommandBuffer, _unitCommandEntityBuffer);
        SubmitAndApplyInputCommand(new GroupMoveEntityCommand(
            OwnerId.FromPlayerSlot(playerSlotId),
            _unitCommandEntityBuffer,
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
        CollectRequestedCommandUnits(playerSlotId, unitIds, _unitCommandBuffer);
        if (_unitCommandBuffer.Count == 0)
        {
            return 0;
        }

        WorldSize = worldSize;
        _entityWorld.WorldWidth = worldSize.X;
        _entityWorld.WorldHeight = worldSize.Y;
        CollectCommandEntityIds(_unitCommandBuffer, _unitCommandEntityBuffer);
        SubmitAndApplyInputCommand(new GroupMoveEntityCommand(
            OwnerId.FromPlayerSlot(playerSlotId),
            _unitCommandEntityBuffer,
            NextInputCommandTick(),
            target,
            mode));
        return _unitCommandBuffer.Count;
    }

    private static bool IsIdleHarvester(PlayerSlotId playerSlotId, UnitInstance unit)
    {
        return unit.PlayerSlotId == playerSlotId
            && unit.Hp > 0
            && IsHarvester(unit)
            && unit.HarvesterMode == HarvesterMode.Idle
            && unit.MoveTarget is null;
    }

    public void CommandAttackSelected(PlayerSlotId playerSlotId, UnitInstance target)
    {
        if (!Relations.CanAttack(playerSlotId, target.PlayerSlotId))
        {
            return;
        }

        CollectSelectedCommandUnitsTargeting(playerSlotId, target, _unitCommandBuffer);
        if (_unitCommandBuffer.Count == 0)
        {
            return;
        }

        CollectCommandEntityIds(_unitCommandBuffer, _unitCommandEntityBuffer);
        SubmitAndApplyInputCommand(new GroupAttackEntityCommand(
            OwnerId.FromPlayerSlot(playerSlotId),
            _unitCommandEntityBuffer,
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
        CollectSelectedCommandUnitsTargeting(playerSlotId, targetSpec, _unitCommandBuffer);
        if (_unitCommandBuffer.Count == 0)
        {
            return false;
        }

        CollectCommandEntityIds(_unitCommandBuffer, _unitCommandEntityBuffer);
        SubmitAndApplyInputCommand(new GroupAttackEntityCommand(
            OwnerId.FromPlayerSlot(playerSlotId),
            _unitCommandEntityBuffer,
            NextInputCommandTick(),
            targetEntity.Id,
            CombatTargetKind.Building));
        return true;
    }

    public void CommandStopSelected(PlayerSlotId playerSlotId)
    {
        CollectSelectedCommandUnits(playerSlotId, _unitCommandBuffer);
        if (_unitCommandBuffer.Count == 0)
        {
            return;
        }

        CollectCommandEntityIds(_unitCommandBuffer, _unitCommandEntityBuffer);
        SubmitAndApplyInputCommand(new StopEntityCommand(
            OwnerId.FromPlayerSlot(playerSlotId),
            _unitCommandEntityBuffer,
            NextInputCommandTick()));
    }

    public int CommandSetSelectedStance(PlayerSlotId playerSlotId, UnitStance stance)
    {
        CollectSelectedArmedCommandUnits(playerSlotId, _unitCommandBuffer);
        if (_unitCommandBuffer.Count == 0)
        {
            return 0;
        }

        CollectCommandEntityIds(_unitCommandBuffer, _unitCommandEntityBuffer);
        SubmitAndApplyInputCommand(new SetStanceEntityCommand(
            OwnerId.FromPlayerSlot(playerSlotId),
            _unitCommandEntityBuffer,
            NextInputCommandTick(),
            stance));
        return _unitCommandBuffer.Count;
    }

    public int CommandAttackUnits(PlayerSlotId playerSlotId, IEnumerable<int> unitIds, UnitInstance target)
    {
        if (!Relations.CanAttack(playerSlotId, target.PlayerSlotId))
        {
            return 0;
        }

        CollectRequestedCommandUnitsTargeting(playerSlotId, unitIds, target, _unitCommandBuffer);
        if (_unitCommandBuffer.Count == 0)
        {
            return 0;
        }

        CollectCommandEntityIds(_unitCommandBuffer, _unitCommandEntityBuffer);
        SubmitAndApplyInputCommand(new GroupAttackEntityCommand(
            OwnerId.FromPlayerSlot(playerSlotId),
            _unitCommandEntityBuffer,
            NextInputCommandTick(),
            target.EntityId,
            CombatTargetKind.Unit));
        return _unitCommandBuffer.Count;
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
        CollectRequestedCommandUnitsTargeting(playerSlotId, unitIds, targetSpec, _unitCommandBuffer);
        if (_unitCommandBuffer.Count == 0)
        {
            return 0;
        }

        CollectCommandEntityIds(_unitCommandBuffer, _unitCommandEntityBuffer);
        SubmitAndApplyInputCommand(new GroupAttackEntityCommand(
            OwnerId.FromPlayerSlot(playerSlotId),
            _unitCommandEntityBuffer,
            NextInputCommandTick(),
            targetEntity.Id,
            CombatTargetKind.Building));
        return _unitCommandBuffer.Count;
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

}
