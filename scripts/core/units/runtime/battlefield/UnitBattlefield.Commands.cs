using Godot;

namespace ProceduralRts.Core;

public sealed partial class UnitBattlefield
{
    public int SelectRect(PlayerSlotId playerSlotId, Rect2 worldRect, bool additive)
    {
        var normalizedRect = worldRect.Abs();
        var unitsInRect = Units
            .Where(unit => unit.PlayerSlotId == playerSlotId && UnitOverlapsSelectionRect(normalizedRect, unit))
            .ToList();
        var economyUnits = unitsInRect
            .Where(unit => unit.Spec.RoleTags.Contains(UnitRoleTag.Economy))
            .ToList();
        var nonEconomyUnits = unitsInRect
            .Where(unit => !unit.Spec.RoleTags.Contains(UnitRoleTag.Economy))
            .ToList();
        var includeEconomy = ShouldIncludeEconomyInSelectionRect(normalizedRect, economyUnits, nonEconomyUnits);

        var selected = additive
            ? SelectedUnits(playerSlotId).Select(unit => unit.EntityId).ToHashSet()
            : new HashSet<EntityId>();
        foreach (var unit in Units.Where(unit => unit.PlayerSlotId == playerSlotId))
        {
            var selectableByBox = UnitOverlapsSelectionRect(normalizedRect, unit)
                && (!unit.Spec.RoleTags.Contains(UnitRoleTag.Economy) || includeEconomy);
            if (selectableByBox)
            {
                selected.Add(unit.EntityId);
            }
        }

        return SubmitSelectionCommand(playerSlotId, selected);
    }

    public IReadOnlyList<UnitInstance> SelectUnitsByIds(PlayerSlotId playerSlotId, IEnumerable<int> unitIds)
    {
        var requestedIds = unitIds.ToHashSet();
        var selectedEntityIds = Units
            .Where(unit => unit.PlayerSlotId == playerSlotId && requestedIds.Contains(unit.Id))
            .OrderBy(unit => unit.Id)
            .Select(unit => unit.EntityId)
            .ToList();
        SubmitSelectionCommand(playerSlotId, selectedEntityIds);
        return SelectedUnits(playerSlotId).ToList();
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

}
