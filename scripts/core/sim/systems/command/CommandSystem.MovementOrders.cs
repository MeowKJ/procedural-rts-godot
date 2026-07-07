using Godot;

namespace ProceduralRts.Core;

public sealed partial class CommandSystem
{
    private void ApplyMove(
        EntityWorld world,
        OwnerId issuer,
        IReadOnlyList<EntityId> subjects,
        Vector2 target,
        MoveCommandMode mode,
        bool manualAttack)
    {
        CollectOwnedSubjects(world, issuer, subjects, _scalarOrderMembers);
        foreach (var entity in _scalarOrderMembers)
        {
            var movement = entity.Components.TryGet<MovementComponentState>(out var existing)
                ? existing
                : new MovementComponentState(Velocity: default);
            entity.Components.Set(movement with
            {
                MoveTarget = target,
                FormationSlot = null,
                FireAnchorRemaining = 0,
            });
            ClearReplacedOrders(entity);

            var commandable = entity.Components.TryGet<CommandableComponentState>(out var cmd)
                ? cmd
                : new CommandableComponentState();
            entity.Components.Set(commandable with
            {
                PlayerIntentTarget = target,
                CommandVisualTarget = target,
                MoveMode = mode,
            });

            ClearWeaponFocus(entity);
        }

        _scalarOrderMembers.Clear();
    }

    private void ApplyPatrol(EntityWorld world, PatrolEntityCommand patrol)
    {
        if (patrol.PointA.DistanceSquaredTo(patrol.PointB) <= 1f)
        {
            return;
        }

        CollectOwnedSubjects(world, patrol.Issuer, patrol.Subjects, _scalarOrderMembers);
        foreach (var entity in _scalarOrderMembers)
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

        _scalarOrderMembers.Clear();
    }

    private void ApplyGuard(EntityWorld world, GuardEntityCommand guard)
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
        CollectOwnedSubjects(world, guard.Issuer, guard.Subjects, _scalarOrderMembers);
        foreach (var entity in _scalarOrderMembers)
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

        _scalarOrderMembers.Clear();
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

        FormationMath.CreateMoveDestinationsInto(
            _groupMoveFormationUnits,
            command.Target.X,
            command.Target.Y,
            world.WorldWidth,
            world.WorldHeight,
            _groupMoveDestinationResults,
            _groupMoveOrderedUnits,
            _groupMoveSlots,
            _groupMoveRemainingSlots);

        _groupMoveDestinations.Clear();
        foreach (var destination in _groupMoveDestinationResults)
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
                entity.Components.Set(movement with
                {
                    MoveTarget = slotPoint,
                    FormationSlot = slotPoint,
                    FireAnchorRemaining = 0,
                });
            }

            ClearReplacedOrders(entity);

            // The visible command line points at the shared intent, not the slot.
            var commandable = entity.Components.TryGet<CommandableComponentState>(out var cmd) ? cmd : new CommandableComponentState();
            entity.Components.Set(commandable with
            {
                PlayerIntentTarget = command.Target,
                CommandVisualTarget = command.Target,
                MoveMode = command.Mode,
            });

            ClearWeaponFocus(entity);
        }

        _groupOrderMembers.Clear();
        _groupMoveFormationUnits.Clear();
        _groupMoveDestinationResults.Clear();
        _groupMoveDestinations.Clear();
    }

    private void ApplyStop(EntityWorld world, OwnerId issuer, IReadOnlyList<EntityId> subjects, bool hold)
    {
        CollectOwnedSubjects(world, issuer, subjects, _scalarOrderMembers);
        foreach (var entity in _scalarOrderMembers)
        {
            if (entity.Components.TryGet<MovementComponentState>(out var movement))
            {
                entity.Components.Set(movement with { Velocity = Vector2.Zero, MoveTarget = null });
            }

            entity.Components.Remove<PathfindingComponentState>();
            entity.Components.Remove<PatrolOrderComponentState>();
            entity.Components.Remove<GuardOrderComponentState>();
            entity.Components.Remove<AttackGroundOrderComponentState>();

            ClearWeaponFocus(entity);
            ClearCommandIntent(entity);

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

        _scalarOrderMembers.Clear();
    }

    private static void ClearReplacedOrders(EntityInstance entity)
    {
        entity.Components.Remove<PathfindingComponentState>();
        entity.Components.Remove<PatrolOrderComponentState>();
        entity.Components.Remove<GuardOrderComponentState>();
        entity.Components.Remove<RepairOrderComponentState>();
        entity.Components.Remove<AttackGroundOrderComponentState>();
    }

    private static void ClearCommandIntent(EntityInstance entity)
    {
        var commandable = entity.Components.TryGet<CommandableComponentState>(out var existing)
            ? existing
            : new CommandableComponentState();
        entity.Components.Set(commandable with
        {
            PlayerIntentTarget = null,
            CommandVisualTarget = null,
            MoveMode = MoveCommandMode.Direct,
        });
    }

    private static void ClearWeaponFocus(EntityInstance entity)
    {
        if (!entity.Components.TryGet<WeaponUserComponentState>(out var weapon))
        {
            return;
        }

        entity.Components.Set(weapon with
        {
            AttackTarget = default,
            AttackTargetKind = CombatTargetKind.Unit,
            AttackTargetIsManual = false,
            AutoReacquireCooldownRemaining = 0,
            LastKnownTargetPosition = null,
            LastKnownTargetRemaining = 0,
        });
    }

    private void ApplyStance(EntityWorld world, SetStanceEntityCommand command)
    {
        CollectOwnedSubjects(world, command.Issuer, command.Subjects, _scalarOrderMembers);
        foreach (var entity in _scalarOrderMembers)
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

        _scalarOrderMembers.Clear();
    }
}
