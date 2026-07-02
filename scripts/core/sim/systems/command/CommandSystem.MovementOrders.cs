using Godot;

namespace ProceduralRts.Core;

public sealed partial class CommandSystem
{
    private static void ApplyMove(
        EntityWorld world,
        OwnerId issuer,
        IReadOnlyList<EntityId> subjects,
        Vector2 target,
        MoveCommandMode mode,
        bool manualAttack)
    {
        foreach (var entity in OwnedSubjects(world, issuer, subjects))
        {
            var movement = entity.Components.TryGet<MovementComponentState>(out var existing)
                ? existing
                : new MovementComponentState(Velocity: default);
            entity.Components.Set(movement with { MoveTarget = target });
            entity.Components.Remove<PatrolOrderComponentState>();
            entity.Components.Remove<GuardOrderComponentState>();

            var commandable = entity.Components.TryGet<CommandableComponentState>(out var cmd)
                ? cmd
                : new CommandableComponentState();
            entity.Components.Set(commandable with
            {
                PlayerIntentTarget = target,
                CommandVisualTarget = target,
                MoveMode = mode,
            });

            // A move order cancels any manual attack focus.
            if (entity.Components.TryGet<WeaponUserComponentState>(out var weapon) && weapon.AttackTargetIsManual)
            {
                entity.Components.Set(weapon with
                {
                    AttackTarget = default,
                    AttackTargetIsManual = false,
                    AutoReacquireCooldownRemaining = 0,
                });
            }
        }
    }

    private static void ApplyPatrol(EntityWorld world, PatrolEntityCommand patrol)
    {
        if (patrol.PointA.DistanceSquaredTo(patrol.PointB) <= 1f)
        {
            return;
        }

        foreach (var entity in OwnedSubjects(world, patrol.Issuer, patrol.Subjects))
        {
            if (!entity.Components.Has<MovementProfileComponentState>())
            {
                continue;
            }

            entity.Components.Set(new PatrolOrderComponentState(
                patrol.PointA,
                patrol.PointB,
                MovingToB: true));
            entity.Components.Remove<GuardOrderComponentState>();

            var movement = entity.Components.TryGet<MovementComponentState>(out var existing)
                ? existing
                : new MovementComponentState(Velocity: default);
            entity.Components.Set(movement with
            {
                MoveTarget = patrol.PointB,
                FormationSlot = null,
            });
            entity.Components.Remove<PathfindingComponentState>();

            var commandable = entity.Components.TryGet<CommandableComponentState>(out var cmd)
                ? cmd
                : new CommandableComponentState();
            entity.Components.Set(commandable with
            {
                PlayerIntentTarget = patrol.PointB,
                CommandVisualTarget = patrol.PointB,
                MoveMode = MoveCommandMode.Attack,
            });

            if (entity.Components.TryGet<WeaponUserComponentState>(out var weapon) && weapon.AttackTargetIsManual)
            {
                entity.Components.Set(weapon with
                {
                    AttackTarget = default,
                    AttackTargetIsManual = false,
                    AutoReacquireCooldownRemaining = 0,
                });
            }
        }
    }

    private static void ApplyGuard(EntityWorld world, GuardEntityCommand guard)
    {
        if (float.IsNaN(guard.Radius) || float.IsInfinity(guard.Radius) || guard.Radius <= 0)
        {
            return;
        }

        EntityInstance? guardedEntity = null;
        if (guard.TargetEntity.IsValid)
        {
            if (!world.TryGet(guard.TargetEntity, out guardedEntity)
                || world.Relations.Relation(guard.Issuer, guardedEntity.OwnerId) is not (PlayerRelation.Self or PlayerRelation.Allied)
                || (guardedEntity.Components.TryGet<HealthComponentState>(out var guardedHealth) && guardedHealth.Hp <= 0))
            {
                return;
            }
        }

        var anchor = guardedEntity?.Transform.Position ?? guard.GuardPoint;
        var radiusSq = guard.Radius * guard.Radius;
        foreach (var entity in OwnedSubjects(world, guard.Issuer, guard.Subjects))
        {
            if (!entity.Components.Has<MovementProfileComponentState>()
                && !entity.Components.Has<WeaponUserComponentState>())
            {
                continue;
            }

            entity.Components.Set(new GuardOrderComponentState(
                guardedEntity?.Id ?? EntityId.None,
                guard.GuardPoint,
                guard.Radius));
            entity.Components.Remove<PatrolOrderComponentState>();
            entity.Components.Remove<PathfindingComponentState>();

            var movement = entity.Components.TryGet<MovementComponentState>(out var existing)
                ? existing
                : new MovementComponentState(Velocity: default);
            entity.Components.Set(movement with
            {
                MoveTarget = entity.Transform.Position.DistanceSquaredTo(anchor) > radiusSq ? anchor : null,
                FormationSlot = null,
            });

            var commandable = entity.Components.TryGet<CommandableComponentState>(out var cmd)
                ? cmd
                : new CommandableComponentState();
            entity.Components.Set(commandable with
            {
                PlayerIntentTarget = anchor,
                CommandVisualTarget = anchor,
                MoveMode = MoveCommandMode.Attack,
            });

            if (entity.Components.TryGet<WeaponUserComponentState>(out var weapon))
            {
                entity.Components.Set(weapon with
                {
                    AttackTarget = default,
                    AttackTargetKind = CombatTargetKind.Unit,
                    AttackTargetIsManual = false,
                    AutoReacquireCooldownRemaining = 0,
                });
            }
        }
    }

    private void ApplyGroupMove(EntityWorld world, GroupMoveEntityCommand command)
    {
        CollectOwnedSubjects(world, command.Issuer, command.Subjects, _groupOrderMembers);
        if (_groupOrderMembers.Count == 0)
        {
            return;
        }

        _groupMoveFormationUnits.Clear();
        foreach (var entity in _groupOrderMembers)
        {
            var radius = entity.Components.TryGet<CollisionComponentState>(out var c) ? c.Radius : 12f;
            _groupMoveFormationUnits.Add(new FormationUnit(entity.Id.Value, entity.Transform.Position.X, entity.Transform.Position.Y, radius));
        }

        _groupMoveDestinations.Clear();
        foreach (var destination in FormationMath.CreateMoveDestinations(
            _groupMoveFormationUnits,
            command.Target.X,
            command.Target.Y,
            world.WorldWidth,
            world.WorldHeight))
        {
            _groupMoveDestinations[destination.Id] = destination;
        }

        foreach (var entity in _groupOrderMembers)
        {
            if (!_groupMoveDestinations.TryGetValue(entity.Id.Value, out var slot))
            {
                continue;
            }

            var slotPoint = new Vector2(slot.X, slot.Y);
            if (entity.Components.TryGet<MovementComponentState>(out var movement))
            {
                entity.Components.Set(movement with { MoveTarget = slotPoint, FormationSlot = slotPoint });
            }

            entity.Components.Remove<PatrolOrderComponentState>();
            entity.Components.Remove<GuardOrderComponentState>();

            // The visible command line points at the shared intent, not the slot.
            var commandable = entity.Components.TryGet<CommandableComponentState>(out var cmd) ? cmd : new CommandableComponentState();
            entity.Components.Set(commandable with
            {
                PlayerIntentTarget = command.Target,
                CommandVisualTarget = command.Target,
                MoveMode = command.Mode,
            });

            if (entity.Components.TryGet<WeaponUserComponentState>(out var weapon) && weapon.AttackTargetIsManual)
            {
                entity.Components.Set(weapon with { AttackTarget = default, AttackTargetIsManual = false, AutoReacquireCooldownRemaining = 0 });
            }
        }

        _groupOrderMembers.Clear();
        _groupMoveFormationUnits.Clear();
        _groupMoveDestinations.Clear();
    }

    private static void ApplyStop(EntityWorld world, OwnerId issuer, IReadOnlyList<EntityId> subjects, bool hold)
    {
        foreach (var entity in OwnedSubjects(world, issuer, subjects))
        {
            if (entity.Components.TryGet<MovementComponentState>(out var movement))
            {
                entity.Components.Set(movement with { Velocity = Vector2.Zero, MoveTarget = null });
            }

            entity.Components.Remove<PathfindingComponentState>();
            entity.Components.Remove<PatrolOrderComponentState>();
            entity.Components.Remove<GuardOrderComponentState>();

            if (entity.Components.TryGet<WeaponUserComponentState>(out var weapon))
            {
                entity.Components.Set(weapon with { AttackTarget = default, AttackTargetIsManual = false, AutoReacquireCooldownRemaining = 0 });
            }

            if (hold && entity.Components.TryGet<StanceComponentState>(out var stance))
            {
                entity.Components.Set(stance with
                {
                    Stance = UnitStance.Hold,
                    AnchorPosition = entity.Transform.Position,
                });
            }

            if (hold && entity.Components.TryGet<AutonomyComponentState>(out var autonomy))
            {
                entity.Components.Set(autonomy with { AnchorPosition = entity.Transform.Position });
            }
        }
    }

    private static void ApplyStance(EntityWorld world, SetStanceEntityCommand command)
    {
        foreach (var entity in OwnedSubjects(world, command.Issuer, command.Subjects))
        {
            var stance = entity.Components.TryGet<StanceComponentState>(out var existing)
                ? existing
                : new StanceComponentState();
            entity.Components.Set(stance with
            {
                Stance = command.Stance,
                AnchorPosition = entity.Transform.Position,
            });

            if (entity.Components.TryGet<AutonomyComponentState>(out var autonomy))
            {
                entity.Components.Set(autonomy with { AnchorPosition = entity.Transform.Position });
            }
        }
    }
}
