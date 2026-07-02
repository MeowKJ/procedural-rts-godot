using Godot;

namespace ProceduralRts.Core;

public sealed partial class UnitBattlefield
{
    private void ApplySelectionCommandStateToUnits(SetSelectionEntityCommand command)
    {
        var issuerSlot = command.Issuer.ToPlayerSlot();
        foreach (var unit in Units)
        {
            if (unit.PlayerSlotId != issuerSlot)
            {
                continue;
            }

            if (!_entityWorld.TryGet(unit.EntityId, out var entity)
                || !entity.Components.TryGet<SelectableComponentState>(out var selectable))
            {
                unit.Selected = false;
                continue;
            }

            unit.Selected = selectable.Selected;
        }
    }

    private void ApplyEntityCommandStateToUnit(UnitInstance unit, EntityInstance entity, EntityCommand command)
    {
        if (entity.Components.TryGet<MovementComponentState>(out var movement))
        {
            unit.Velocity = movement.Velocity;
            unit.MoveTarget = movement.MoveTarget;
            unit.FormationSlot = movement.FormationSlot;
        }

        if (entity.Components.TryGet<CommandableComponentState>(out var commandable))
        {
            unit.PlayerIntentTarget = commandable.PlayerIntentTarget;
            unit.CommandVisualTarget = commandable.CommandVisualTarget;
            unit.MoveMode = commandable.MoveMode;
        }

        if (command is GroupMoveEntityCommand or MoveEntityCommand or AttackMoveEntityCommand or RepairEntityCommand or StopEntityCommand)
        {
            unit.AttackTargetId = null;
            unit.AttackTargetKind = CombatTargetKind.Unit;
            unit.AttackTargetIsManual = false;
        }
        else if (entity.Components.TryGet<WeaponUserComponentState>(out var weapon))
        {
            unit.AttackTargetKind = weapon.AttackTargetKind;
            unit.AttackTargetIsManual = weapon.AttackTargetIsManual;
            unit.AttackTargetId = LegacyTargetId(weapon.AttackTarget, weapon.AttackTargetKind);
            if (weapon.AttackTargetIsManual)
            {
                unit.MoveMode = MoveCommandMode.Attack;
            }
        }

        if (entity.Components.TryGet<StanceComponentState>(out var stance))
        {
            unit.Stance = stance.Stance;
        }

        if (entity.Components.TryGet<HarvesterComponentState>(out var harvester))
        {
            unit.HarvesterMode = harvester.Mode;
            unit.HarvestFieldId = LegacyResourceFieldId(harvester.FieldId);
            unit.HarvestRefineryId = LegacyBuildingTargetId(harvester.RefineryId);
            unit.HarvestPulse = harvester.HarvestPulse;
        }

        if (entity.Components.TryGet<ResourceCargoComponentState>(out var cargo))
        {
            unit.Cargo = cargo.Cargo;
        }

        if (command is GroupMoveEntityCommand or MoveEntityCommand or AttackMoveEntityCommand or GroupAttackEntityCommand or AttackEntityCommand or RepairEntityCommand or StopEntityCommand)
        {
            StopHarvesting(unit);
        }

        unit.CommandPulse = 1;
    }

}
