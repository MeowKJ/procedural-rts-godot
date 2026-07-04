using Godot;

namespace ProceduralRts.Core;

public sealed partial class CommandSystem
{
    private void ApplyAttack(EntityWorld world, AttackEntityCommand attack)
    {
        CollectOwnedSubjects(world, attack.Issuer, attack.Subjects, _scalarOrderMembers);
        foreach (var entity in _scalarOrderMembers)
        {
            ClearReplacedOrders(entity);
            if (entity.Components.TryGet<MovementComponentState>(out var movement))
            {
                entity.Components.Set(movement with
                {
                    MoveTarget = null,
                    FormationSlot = null,
                    FireAnchorRemaining = 0,
                });
            }

            SetManualTarget(entity, attack.Target, attack.TargetKind);
        }

        _scalarOrderMembers.Clear();
    }

    private void ApplyGroupAttack(EntityWorld world, GroupAttackEntityCommand command)
    {
        if (!world.TryGet(command.Target, out var target))
        {
            return;
        }

        CollectOwnedSubjects(world, command.Issuer, command.Subjects, _groupOrderMembers);
        if (_groupOrderMembers.Count == 0)
        {
            return;
        }

        var targetRadius = target.Components.TryGet<CollisionComponentState>(out var tc) ? tc.Radius : 0f;

        _groupAttackSlotUnits.Clear();
        foreach (var entity in _groupOrderMembers)
        {
            _groupAttackSlotUnits.Add(new AttackSlotUnit(entity.Id.Value, entity.Transform.Position, WeaponMath.BaseRange(world, entity)));
        }

        AttackSlotMath.AssignAttackSlotsInto(
            _groupAttackSlotUnits,
            target.Transform.Position,
            targetRadius,
            _groupAttackAssignmentResults,
            _groupAttackOrderedUnits,
            _groupAttackAnchors,
            _groupAttackMovers,
            _groupAttackFreeSlots);

        _groupAttackAssignments.Clear();
        foreach (var assignment in _groupAttackAssignmentResults)
        {
            _groupAttackAssignments[assignment.Id] = assignment;
        }

        foreach (var entity in _groupOrderMembers)
        {
            // Every attacker focuses the target; CombatSystem fires when in range.
            ClearReplacedOrders(entity);
            SetManualTarget(entity, command.Target, command.TargetKind);

            if (!_groupAttackAssignments.TryGetValue(entity.Id.Value, out var assignment))
            {
                continue;
            }

            if (entity.Components.TryGet<MovementComponentState>(out var movement))
            {
                // Anchors hold; movers head to their ring slot.
                var slot = assignment.IsAnchor ? (Vector2?)null : assignment.Slot;
                entity.Components.Set(movement with
                {
                    MoveTarget = slot,
                    FormationSlot = assignment.Slot,
                    FireAnchorRemaining = 0,
                });
            }

            var commandable = entity.Components.TryGet<CommandableComponentState>(out var cmd) ? cmd : new CommandableComponentState();
            entity.Components.Set(commandable with
            {
                PlayerIntentTarget = target.Transform.Position,
                CommandVisualTarget = target.Transform.Position,
            });
        }

        _groupOrderMembers.Clear();
        _groupAttackSlotUnits.Clear();
        _groupAttackAssignmentResults.Clear();
        _groupAttackAssignments.Clear();
    }

    private static void SetManualTarget(EntityInstance entity, EntityId target, CombatTargetKind targetKind)
    {
        if (!entity.Components.TryGet<WeaponUserComponentState>(out var weapon))
        {
            return;
        }

        entity.Components.Set(weapon with
        {
            AttackTarget = target,
            AttackTargetKind = targetKind,
            AttackTargetIsManual = true,
            AutoReacquireCooldownRemaining = 0,
        });
    }

}
