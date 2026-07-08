using Godot;

namespace ProceduralRts.Core;

/// <summary>
/// Advances entities toward their movement target by the fixed delta, with soft
/// local avoidance so units in transit do not overlap and path around units that
/// are standing/firing (those become non-displaceable anchors). Weight order
/// follows docs/RTS99Design.md "局部避让": target attraction dominates, avoidance
/// only bends the path. Direct-line travel with soft arrival; corridor pathing is
/// layered on later. Iterates in stable EntityId order.
/// </summary>
public sealed partial class MovementSystem : ISimSystem
{
    private const float AvoidanceCellSize = 96f;
    // Avoidance bends but never overrides target attraction (which is unit length).
    // It reduces crossings; SeparationSystem guarantees no residual hard overlap.
    private const float AvoidanceWeight = 0.6f;
    private const float AvoidancePadding = 10f;
    private const float AvoidanceMaxLength = 0.72f;
    private const float ArrivalPadding = 2f;
    private const float ArrivalMaxOffset = 120f;
    private const float CrowdedArrivalRadiusMultiplier = 3f;
    private const float PatrolTargetToleranceSquared = 1f;
    private readonly SpatialGrid<LocalAvoidanceBody> _avoidanceGrid = new(AvoidanceCellSize);

    public void Step(SimContext context)
    {
        var dt = context.FixedDelta;
        var world = context.World;

        // Build the avoidance hash once per tick from every collidable entity.
        // A unit with no move target or a recent shot is treated as an anchor
        // others route around.
        _avoidanceGrid.Clear();
        foreach (var entity in world.OrderedEntities)
        {
            if (!entity.Components.TryGet<CollisionComponentState>(out var collision)
                || !collision.BlocksMovement)
            {
                continue;
            }

            var moving = entity.Components.TryGet<MovementComponentState>(out var m) && m.MoveTarget is not null;
            var fireAnchor = m?.FireAnchorRemaining > 0;
            var combatAnchor = m is not null && IsCombatAnchor(world, entity, m);
            var body = new LocalAvoidanceBody(
                entity.Id.Value,
                entity.Transform.Position.X,
                entity.Transform.Position.Y,
                collision.Radius,
                AnchorPriority: fireAnchor ? 3 : combatAnchor ? 2 : moving ? 0 : 1,
                CanBeDisplaced: moving && !fireAnchor && !combatAnchor);
            _avoidanceGrid.Add(body.X, body.Y, body);
        }

        foreach (var entity in world.OrderedEntities)
        {
            if (!entity.Components.TryGet<MovementComponentState>(out var movement))
            {
                continue;
            }

            if (entity.Components.TryGet<DeployComponentState>(out var deploy) && deploy.IsDeployed)
            {
                entity.Components.Set(movement with { Velocity = Vector2.Zero, MoveTarget = null });
                world.Metrics.RecordMovementIdle(entity.Id.Value);
                continue;
            }

            var anchorRemaining = MathF.Max(0, movement.FireAnchorRemaining - dt);
            if (!Mathf.IsEqualApprox(anchorRemaining, movement.FireAnchorRemaining))
            {
                movement = movement with { FireAnchorRemaining = anchorRemaining };
                entity.Components.Set(movement);
            }

            movement = ResumePatrolRouteIfNeeded(world, entity, movement);
            movement = ResumeGuardOrderIfNeeded(world, entity, movement);
            movement = ResumeAttackMoveIntentIfNeeded(world, entity, movement);

            if (movement.MoveTarget is not Vector2 target
                || !entity.Components.TryGet<MovementProfileComponentState>(out var profile))
            {
                world.Metrics.RecordMovementIdle(entity.Id.Value);
                continue;
            }

            var position = entity.Transform.Position;
            var toTarget = target - position;
            var distance = toTarget.Length();
            var maxSpeed = UpgradeResolver.MoveSpeed(world, entity, profile.MaxSpeed);
            var step = maxSpeed * dt;
            var crowdedStopPosition = target;
            var crowdedArrivalRadius = 0f;
            var crowdedArrival = false;
            if (movement.FormationSlot is null
                && entity.Components.TryGet<CollisionComponentState>(out var arrivalCollision))
            {
                crowdedArrivalRadius = arrivalCollision.Radius;
                crowdedArrival = TryResolveCrowdedArrivalStop(
                    entity.Id.Value,
                    target,
                    arrivalCollision.Radius,
                    _avoidanceGrid,
                    out crowdedStopPosition);
            }

            if (distance <= profile.ArriveRadius
                || distance <= step
                || (crowdedArrival && distance <= crowdedArrivalRadius * CrowdedArrivalRadiusMultiplier))
            {
                // Soft arrival: slot moves snap to their exact slot. Unslotted
                // same-point moves stop at a deterministic open pocket when the
                // target is already crowded, avoiding the stack-then-explode tail.
                var stopPosition = crowdedArrival ? crowdedStopPosition : target;
                entity.Transform = entity.Transform with { Position = stopPosition };
                var arrivedMovement = movement with { Velocity = Vector2.Zero, MoveTarget = null };
                if (TryAdvancePatrolLeg(world, entity, movement, target, out var patrolMovement))
                {
                    arrivedMovement = patrolMovement;
                }
                else if (TryAdvanceQueuedMovementOrder(entity, movement, target, out var queuedMovement))
                {
                    arrivedMovement = queuedMovement;
                }

                entity.Components.Set(arrivedMovement);
                world.Metrics.RecordMovementSample(entity.Id.Value, position, stopPosition, target, dt);
                world.Metrics.RecordMovementArrival(entity.Id.Value, stopPosition.DistanceTo(target));
                continue;
            }

            var direction = toTarget / distance;

            // Blend in soft avoidance; target attraction stays dominant.
            if (entity.Components.TryGet<CollisionComponentState>(out var collision))
            {
                var body = new LocalAvoidanceBody(entity.Id.Value, position.X, position.Y, collision.Radius, 0, true);
                var avoid = LocalAvoidanceMath.ResolveVector(body, _avoidanceGrid, AvoidancePadding, AvoidanceMaxLength);
                direction = (direction + new Vector2(avoid.X, avoid.Y) * AvoidanceWeight);
                if (direction.LengthSquared() > 0.0001f)
                {
                    direction = direction.Normalized();
                }
                else
                {
                    direction = toTarget / distance;
                }
            }

            var targetAngle = direction.Angle();
            var facing = TurnModeMath.NextFacing(entity.Transform.Facing, targetAngle, profile.TurnRate, dt, profile.TurnMode);
            var movementDirection = TurnModeMath.MovementDirection(profile.TurnMode, direction, facing);
            var turnSpeedScale = TurnModeMath.SpeedScale(profile.TurnMode, facing, targetAngle);
            var nextPosition = position + (movementDirection * step * turnSpeedScale);

            entity.Transform = new EntityTransform(nextPosition, facing);
            entity.Components.Set(movement with { Velocity = movementDirection * maxSpeed * turnSpeedScale });
            world.Metrics.RecordMovementSample(entity.Id.Value, position, nextPosition, target, dt);
        }
    }

}
