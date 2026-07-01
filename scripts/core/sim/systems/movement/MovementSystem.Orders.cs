using Godot;

namespace ProceduralRts.Core;

public sealed partial class MovementSystem
{
    private static MovementComponentState ResumePatrolRouteIfNeeded(
        EntityWorld world,
        EntityInstance entity,
        MovementComponentState movement)
    {
        if (!entity.Components.TryGet<PatrolOrderComponentState>(out var patrol)
            || movement.FireAnchorRemaining > 0
            || HasActiveAttackTarget(world, entity))
        {
            return movement;
        }

        var target = PatrolTarget(patrol);
        if (IsFollowingPatrolTarget(entity, movement, target))
        {
            UpdatePatrolCommandable(entity, target);
            return movement;
        }

        entity.Components.Remove<PathfindingComponentState>();
        var resumed = movement with { MoveTarget = target, FormationSlot = null };
        entity.Components.Set(resumed);
        UpdatePatrolCommandable(entity, target);
        return resumed;
    }

    private static MovementComponentState ResumeGuardOrderIfNeeded(
        EntityWorld world,
        EntityInstance entity,
        MovementComponentState movement)
    {
        if (!entity.Components.TryGet<GuardOrderComponentState>(out var guard)
            || movement.FireAnchorRemaining > 0
            || HasActiveAttackTarget(world, entity)
            || !entity.Components.Has<MovementProfileComponentState>())
        {
            return movement;
        }

        var anchor = GuardAnchor(world, entity, guard);
        UpdateGuardCommandable(entity, anchor);

        if (entity.Transform.Position.DistanceSquaredTo(anchor) <= guard.Radius * guard.Radius)
        {
            entity.Components.Remove<PathfindingComponentState>();
            if (movement.MoveTarget is null && movement.Velocity == Vector2.Zero && movement.FormationSlot is null)
            {
                return movement;
            }

            var stopped = movement with
            {
                Velocity = Vector2.Zero,
                MoveTarget = null,
                FormationSlot = null,
            };
            entity.Components.Set(stopped);
            return stopped;
        }

        entity.Components.Remove<PathfindingComponentState>();
        var resumed = movement with { MoveTarget = anchor, FormationSlot = null };
        entity.Components.Set(resumed);
        return resumed;
    }

    private static bool TryAdvancePatrolLeg(
        EntityWorld world,
        EntityInstance entity,
        MovementComponentState movement,
        Vector2 arrivedTarget,
        out MovementComponentState nextMovement)
    {
        nextMovement = movement;
        if (!entity.Components.TryGet<PatrolOrderComponentState>(out var patrol)
            || HasActiveAttackTarget(world, entity)
            || !SamePatrolPoint(arrivedTarget, PatrolTarget(patrol)))
        {
            return false;
        }

        var nextPatrol = patrol with { MovingToB = !patrol.MovingToB };
        var nextTarget = PatrolTarget(nextPatrol);
        entity.Components.Set(nextPatrol);
        entity.Components.Remove<PathfindingComponentState>();
        UpdatePatrolCommandable(entity, nextTarget);

        nextMovement = movement with
        {
            Velocity = Vector2.Zero,
            MoveTarget = nextTarget,
            FormationSlot = null,
        };
        return true;
    }

    private static Vector2 PatrolTarget(PatrolOrderComponentState patrol)
    {
        return patrol.MovingToB ? patrol.PointB : patrol.PointA;
    }

    private static bool IsFollowingPatrolTarget(EntityInstance entity, MovementComponentState movement, Vector2 target)
    {
        if (movement.MoveTarget is { } moveTarget && SamePatrolPoint(moveTarget, target))
        {
            return true;
        }

        return movement.MoveTarget is not null
            && entity.Components.TryGet<PathfindingComponentState>(out var path)
            && SamePatrolPoint(new Vector2(path.Goal.X, path.Goal.Y), target);
    }

    private static bool HasActiveAttackTarget(EntityWorld world, EntityInstance entity)
    {
        if (!entity.Components.TryGet<WeaponUserComponentState>(out var weapon)
            || !weapon.AttackTarget.IsValid
            || !world.TryGet(weapon.AttackTarget, out var target)
            || !world.Relations.CanAttack(entity.OwnerId, target.OwnerId))
        {
            return false;
        }

        return !target.Components.TryGet<HealthComponentState>(out var health) || health.Hp > 0;
    }

    private static void UpdatePatrolCommandable(EntityInstance entity, Vector2 target)
    {
        var commandable = entity.Components.TryGet<CommandableComponentState>(out var existing)
            ? existing
            : new CommandableComponentState();
        entity.Components.Set(commandable with
        {
            PlayerIntentTarget = target,
            CommandVisualTarget = target,
            MoveMode = MoveCommandMode.Attack,
        });
    }

    private static Vector2 GuardAnchor(EntityWorld world, EntityInstance entity, GuardOrderComponentState guard)
    {
        if (guard.TargetEntity.IsValid
            && world.TryGet(guard.TargetEntity, out var target)
            && world.Relations.Relation(entity.OwnerId, target.OwnerId) is PlayerRelation.Self or PlayerRelation.Allied
            && (!target.Components.TryGet<HealthComponentState>(out var health) || health.Hp > 0))
        {
            return target.Transform.Position;
        }

        if (guard.TargetEntity.IsValid)
        {
            entity.Components.Set(guard with { TargetEntity = EntityId.None });
        }

        return guard.GuardPoint;
    }

    private static void UpdateGuardCommandable(EntityInstance entity, Vector2 target)
    {
        var commandable = entity.Components.TryGet<CommandableComponentState>(out var existing)
            ? existing
            : new CommandableComponentState();
        entity.Components.Set(commandable with
        {
            PlayerIntentTarget = target,
            CommandVisualTarget = target,
            MoveMode = MoveCommandMode.Attack,
        });
    }

    private static bool SamePatrolPoint(Vector2 a, Vector2 b)
    {
        return a.DistanceSquaredTo(b) <= PatrolTargetToleranceSquared;
    }
}
