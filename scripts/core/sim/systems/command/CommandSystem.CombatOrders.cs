using Godot;

namespace ProceduralRts.Core;

public sealed partial class CommandSystem
{
    private static void ApplyAttack(EntityWorld world, AttackEntityCommand attack)
    {
        foreach (var entity in OwnedSubjects(world, attack.Issuer, attack.Subjects))
        {
            entity.Components.Remove<PatrolOrderComponentState>();
            entity.Components.Remove<GuardOrderComponentState>();
            SetManualTarget(entity, attack.Target, attack.TargetKind);
        }
    }

    private static void ApplyGroupAttack(EntityWorld world, GroupAttackEntityCommand command)
    {
        if (!world.TryGet(command.Target, out var target))
        {
            return;
        }

        var members = OwnedSubjects(world, command.Issuer, command.Subjects).ToList();
        if (members.Count == 0)
        {
            return;
        }

        var targetRadius = target.Components.TryGet<CollisionComponentState>(out var tc) ? tc.Radius : 0f;

        var slotUnits = members
            .Select(entity => new AttackSlotUnit(entity.Id.Value, entity.Transform.Position, WeaponRange(world, entity)))
            .ToList();

        var assignments = AttackSlotMath
            .AssignAttackSlots(slotUnits, target.Transform.Position, targetRadius)
            .ToDictionary(a => a.Id);

        foreach (var entity in members)
        {
            // Every attacker focuses the target; CombatSystem fires when in range.
            entity.Components.Remove<PatrolOrderComponentState>();
            entity.Components.Remove<GuardOrderComponentState>();
            SetManualTarget(entity, command.Target, command.TargetKind);

            if (!assignments.TryGetValue(entity.Id.Value, out var assignment))
            {
                continue;
            }

            if (entity.Components.TryGet<MovementComponentState>(out var movement))
            {
                // Anchors hold; movers head to their ring slot.
                var slot = assignment.IsAnchor ? (Vector2?)null : assignment.Slot;
                entity.Components.Set(movement with { MoveTarget = slot, FormationSlot = assignment.Slot });
            }

            var commandable = entity.Components.TryGet<CommandableComponentState>(out var cmd) ? cmd : new CommandableComponentState();
            entity.Components.Set(commandable with
            {
                PlayerIntentTarget = target.Transform.Position,
                CommandVisualTarget = target.Transform.Position,
            });
        }
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

    private static float WeaponRange(EntityWorld world, EntityInstance entity)
    {
        // Base mount range, no deploy bonus (group attack-slot positioning).
        return entity.Components.TryGet<WeaponUserComponentState>(out var weapon)
            ? UpgradeResolver.WeaponRange(world, entity, WeaponMath.MaxMountRange(world, weapon))
            : 0f;
    }
}
