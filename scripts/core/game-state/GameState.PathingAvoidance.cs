using Godot;

namespace ProceduralRts.Core;

public sealed partial class GameState
{
    private void AssignPath(
        UnitModel unit,
        Vector2 destination,
        Vector2? playerIntentTarget = null,
        IReadOnlyList<PathPoint>? plannedPath = null,
        IReadOnlyList<GridObstacle>? rawPathCells = null)
    {
        unit.Path.Clear();
        unit.GlobalCorridor.Clear();
        unit.DebugRawPathCells.Clear();
        unit.PlayerIntentTarget = playerIntentTarget;
        unit.PathStallSeconds = 0;
        unit.LastMoveTargetDistance = float.PositiveInfinity;
        unit.FormationSlot = destination;
        unit.MovementState = UnitMovementState.MovingToSlot;
        IReadOnlyList<PathPoint> path;
        if (plannedPath is null)
        {
            var pathResult = PathfindingMath.FindPathWithDebug(
                unit.Position.X,
                unit.Position.Y,
                destination.X,
                destination.Y,
                WorldSize.X,
                WorldSize.Y,
                PathCellSize,
                PathObstacles(unit.RuntimeDescriptor.MovementDomain, unit.Id),
                unit.RuntimeDescriptor.MovementDomain,
                TerrainCells());
            unit.DebugRawPathCells.AddRange(pathResult.RawCells);
            path = pathResult.Path;
        }
        else
        {
            if (rawPathCells is not null)
            {
                unit.DebugRawPathCells.AddRange(rawPathCells);
            }

            path = plannedPath;
        }

        if (path.Count == 0)
        {
            unit.MoveTarget = destination;
            unit.GlobalCorridor.Add(destination);
            return;
        }

        foreach (var point in path)
        {
            unit.GlobalCorridor.Add(new Vector2(point.X, point.Y));
        }

        unit.MoveTarget = unit.GlobalCorridor[0];
        for (var index = 1; index < unit.GlobalCorridor.Count; index++)
        {
            unit.Path.Enqueue(unit.GlobalCorridor[index]);
        }
    }

    private static void ClearMoveTarget(UnitModel unit)
    {
        unit.MoveTarget = null;
        unit.Path.Clear();
        unit.GlobalCorridor.Clear();
        unit.DebugRawPathCells.Clear();
        unit.DebugLocalAvoidanceVector = Vector2.Zero;
        unit.DebugSteeringVector = Vector2.Zero;
        unit.Velocity = Vector2.Zero;
        unit.PathStallSeconds = 0;
        unit.LastMoveTargetDistance = float.PositiveInfinity;
        if (unit.AttackTargetId is null)
        {
            unit.CommandVisualTarget = null;
            unit.PlayerIntentTarget = null;
        }

        unit.MoveMode = MoveCommandMode.Direct;
        if (unit.FormationSlot is { } slot && unit.Position.DistanceTo(slot) <= SlotHoldRadius * 1.5f)
        {
            HoldFormationSlot(unit, slot, unit.Position);
            return;
        }

        unit.FormationSlot = null;
        unit.MovementState = UnitMovementState.Idle;
    }

    private IReadOnlyDictionary<GridObstacle, List<LocalAvoidanceBody>> BuildLocalAvoidanceHash()
    {
        _legacyLocalAvoidanceBodies.Clear();
        foreach (var unit in Units)
        {
            if (unit.Hp <= 0)
            {
                continue;
            }

            var descriptor = unit.RuntimeDescriptor;
            _legacyLocalAvoidanceBodies.Add(new LocalAvoidanceBody(
                unit.Id,
                unit.Position.X,
                unit.Position.Y,
                descriptor.Radius,
                unit.AnchorPriority,
                unit.CanBeDisplaced));
        }

        LocalAvoidanceMath.BuildHashInto(_legacyLocalAvoidanceBodies, LocalAvoidanceCellSize, _legacyLocalAvoidanceHash);
        return _legacyLocalAvoidanceHash;
    }

    private Vector2 LocalAvoidanceVector(UnitModel unit, IReadOnlyDictionary<GridObstacle, List<LocalAvoidanceBody>> localAvoidance)
    {
        var descriptor = unit.RuntimeDescriptor;
        var avoidance = LocalAvoidanceMath.ResolveVector(
            new LocalAvoidanceBody(
                unit.Id,
                unit.Position.X,
                unit.Position.Y,
                descriptor.Radius,
                unit.AnchorPriority,
                unit.CanBeDisplaced),
            localAvoidance,
            LocalAvoidanceCellSize);

        return new Vector2(avoidance.X, avoidance.Y);
    }

    private static Vector2 SlotPrioritySteering(Vector2 desired, Vector2 avoidance, Vector2? finalSlot, float distanceToSlot)
    {
        avoidance = LateralAvoidance(desired, avoidance, finalSlot);
        if (finalSlot is null)
        {
            return desired * 1.25f + avoidance;
        }

        var slotProgress = Mathf.Clamp(distanceToSlot / SlotSlowRadius, 0, 1);
        var avoidanceScale = Mathf.Lerp(SlotAvoidanceMinimumScale, 1, slotProgress);
        var desiredWeight = Mathf.Lerp(1.65f, 1, slotProgress);
        var steering = desired * desiredWeight + avoidance * avoidanceScale;
        var forward = steering.Dot(desired);
        if (forward >= SlotMinimumForwardSteering)
        {
            return steering;
        }

        var lateral = steering - desired * forward;
        return desired * SlotMinimumForwardSteering + lateral.LimitLength(0.45f);
    }

    private static Vector2 LateralAvoidance(Vector2 desired, Vector2 avoidance, Vector2? finalSlot)
    {
        if (avoidance.LengthSquared() <= 0.0001f || desired.LengthSquared() <= 0.0001f)
        {
            return avoidance;
        }

        var forward = avoidance.Dot(desired);
        var lateral = avoidance - desired * forward;
        var forwardAllowance = finalSlot is null ? 0.18f : 0.08f;
        return lateral + desired * MathF.Min(0, forward) * forwardAllowance;
    }

    private static bool HasCrossedTarget(Vector2 previousPosition, Vector2 nextPosition, Vector2 target)
    {
        var before = target - previousPosition;
        var after = target - nextPosition;
        if (before.Dot(after) > 0 || before.LengthSquared() > SlotSlowRadius * SlotSlowRadius)
        {
            return false;
        }

        var segment = nextPosition - previousPosition;
        var segmentLengthSquared = segment.LengthSquared();
        if (segmentLengthSquared <= 0.001f)
        {
            return false;
        }

        var closestT = Mathf.Clamp((target - previousPosition).Dot(segment) / segmentLengthSquared, 0, 1);
        var closestPoint = previousPosition + segment * closestT;
        return closestPoint.DistanceTo(target) <= SlotHoldRadius;
    }
}
