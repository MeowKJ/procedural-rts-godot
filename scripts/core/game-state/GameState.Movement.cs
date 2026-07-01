using Godot;

namespace ProceduralRts.Core;

public sealed partial class GameState
{
    private void UpdateMovement(UnitModel unit, float dt, IReadOnlyDictionary<GridObstacle, IReadOnlyList<LocalAvoidanceBody>> localAvoidance)
    {
        if (unit.AttackTargetId is not null)
        {
            var targetPosition = ResolveAttackMovementTarget(unit, dt);
            if (targetPosition is null)
            {
                ClearAttackTarget(unit);
            }
            else if (IsUnitAtEngagementRange(unit, unit.AttackTargetKind, unit.AttackTargetId.Value, targetPosition.Value))
            {
                SetCombatAnchor(unit);
            }
            else if (!unit.AttackTargetAllowsPursuit)
            {
                var leash = StationaryThreatLeash(unit);
                if (unit.Position.DistanceTo(targetPosition.Value) > leash)
                {
                    ClearAttackTarget(unit);
                }
                else
                {
                    ClearMoveTarget(unit);
                }
            }
            else if (unit.ReturnToAnchorAfterAttack && targetPosition.Value.DistanceTo(unit.AnchorPosition) > unit.RuntimeDescriptor.SightRange * 1.25f)
            {
                ClearAttackTarget(unit);
            }
            else
            {
                if (unit.AttackTargetIsManual)
                {
                    RefreshManualAttackSlot(unit, targetPosition.Value);
                }
                else
                {
                    unit.FormationSlot = null;
                    unit.MovementState = UnitMovementState.Idle;
                unit.PlayerIntentTarget = targetPosition.Value;
                unit.MoveTarget = targetPosition.Value;
                }
            }
        }

        if (unit.MoveTarget is null)
        {
            unit.Velocity = Vector2.Zero;
            unit.DebugLocalAvoidanceVector = Vector2.Zero;
            unit.DebugSteeringVector = Vector2.Zero;
            unit.TurretFacing = RotateToward(unit.TurretFacing, unit.Facing, dt * 2);
            return;
        }

        var target = unit.MoveTarget.Value;
        var descriptor = unit.RuntimeDescriptor;
        var toTarget = target - unit.Position;
        var distance = toTarget.Length();
        var finalSlot = unit.Path.Count == 0 ? unit.FormationSlot : null;
        var distanceToSlot = finalSlot is { } slot ? unit.Position.DistanceTo(slot) : distance;
        if (MaybeRepathStalledUnit(unit, distance, dt))
        {
            return;
        }

        if (finalSlot is { } slotPosition && distanceToSlot <= SlotHoldRadius)
        {
            HoldFormationSlot(unit, slotPosition, unit.Position);
            return;
        }

        if (distance < 4)
        {
            unit.Position = target;
            if (unit.Path.Count > 0)
            {
                unit.MoveTarget = unit.Path.Dequeue();
                unit.LastMoveTargetDistance = float.PositiveInfinity;
                unit.PathStallSeconds = 0;
                return;
            }

            ClearMoveTarget(unit);
            if (unit.AttackTargetId is null)
            {
                unit.AnchorPosition = unit.Position;
            }
            return;
        }

        unit.MovementState = unit.FormationSlot is not null ? UnitMovementState.MovingToSlot : UnitMovementState.Idle;
        var desired = toTarget.Normalized();
        var avoidance = LocalAvoidanceVector(unit, localAvoidance);
        var steering = SlotPrioritySteering(desired, avoidance, finalSlot, distanceToSlot);
        unit.DebugLocalAvoidanceVector = avoidance;
        unit.DebugSteeringVector = steering;
        if (steering.LengthSquared() <= 0.001f)
        {
            steering = desired;
            unit.DebugSteeringVector = steering;
        }

        steering = steering.Normalized();
        var targetAngle = steering.Angle();
        unit.Facing = RotateToward(unit.Facing, targetAngle, descriptor.TurnRate * dt);
        unit.TurretFacing = RotateToward(unit.TurretFacing, targetAngle, descriptor.TurnRate * dt * 0.8f);
        var slowFactor = finalSlot is null
            ? 1
            : Mathf.Clamp(distanceToSlot / SlotSlowRadius, 0.22f, 1f);
        var step = Mathf.Min(distance, descriptor.Speed * slowFactor * dt);
        var nextPosition = unit.Position + steering * step;
        if (finalSlot is { } slotDestination
            && (nextPosition.DistanceTo(slotDestination) <= SlotHoldRadius || HasCrossedTarget(unit.Position, nextPosition, slotDestination)))
        {
            HoldFormationSlot(unit, slotDestination, nextPosition);
            return;
        }

        unit.Velocity = (nextPosition - unit.Position) / Mathf.Max(dt, 0.001f);
        unit.Position = nextPosition;
    }

    private bool MaybeRepathStalledUnit(UnitModel unit, float distanceToMoveTarget, float dt)
    {
        if (unit.FormationSlot is null || unit.MoveTarget is null || unit.RepathCooldownRemaining > 0)
        {
            unit.LastMoveTargetDistance = distanceToMoveTarget;
            unit.PathStallSeconds = 0;
            return false;
        }

        if (distanceToMoveTarget < SlotHoldRadius * 2)
        {
            unit.LastMoveTargetDistance = distanceToMoveTarget;
            unit.PathStallSeconds = 0;
            return false;
        }

        if (distanceToMoveTarget < unit.LastMoveTargetDistance - RepathProgressEpsilon)
        {
            unit.LastMoveTargetDistance = distanceToMoveTarget;
            unit.PathStallSeconds = 0;
            return false;
        }

        unit.PathStallSeconds += dt;
        unit.LastMoveTargetDistance = distanceToMoveTarget;
        if (unit.PathStallSeconds < StuckRepathAfterSeconds)
        {
            return false;
        }

        var destination = unit.FormationSlot.Value;
        var intent = unit.PlayerIntentTarget;
        unit.RepathCooldownRemaining = RepathCooldownSeconds;
        unit.PathStallSeconds = 0;
        unit.LastMoveTargetDistance = float.PositiveInfinity;
        AssignPath(unit, destination, intent);
        unit.RepathCooldownRemaining = RepathCooldownSeconds;
        return true;
    }

    private static void HoldFormationSlot(UnitModel unit, Vector2 slotPosition, Vector2 settledPosition)
    {
        unit.Position = settledPosition.DistanceTo(slotPosition) <= SlotInvisibleSnapRadius
            ? slotPosition
            : settledPosition;
        unit.MoveTarget = null;
        unit.Path.Clear();
        unit.GlobalCorridor.Clear();
        unit.DebugRawPathCells.Clear();
        unit.DebugLocalAvoidanceVector = Vector2.Zero;
        unit.DebugSteeringVector = Vector2.Zero;
        unit.Velocity = Vector2.Zero;
        unit.PathStallSeconds = 0;
        unit.LastMoveTargetDistance = float.PositiveInfinity;
        unit.AnchorPosition = unit.Position;
        unit.MovementState = UnitMovementState.HoldingSlot;
        unit.MoveMode = MoveCommandMode.Direct;
    }

    private bool IsUnitAtEngagementRange(UnitModel unit, CombatTargetKind targetKind, int targetId, Vector2 targetPosition)
    {
        return unit.Position.DistanceTo(targetPosition) <= EngagementRange(unit, targetKind, targetId);
    }

    private float EngagementRange(UnitModel unit, CombatTargetKind targetKind, int targetId)
    {
        var targetRadius = CombatTargetRadius(targetKind, targetId);
        var minimum = targetRadius + unit.RuntimeDescriptor.Radius + 18;
        return MathF.Max(minimum, Weapon(unit).Range * EngagementRangeScale + targetRadius * 0.14f);
    }

    private float FireAuthorizationRange(WeaponDefinition weapon, CombatTargetKind targetKind, int targetId)
    {
        return weapon.Range + CombatTargetRadius(targetKind, targetId) * 0.18f + FireRangeSlack;
    }

    private Vector2? ResolveAttackMovementTarget(UnitModel unit, float dt)
    {
        if (unit.AttackTargetId is null)
        {
            return null;
        }

        var actualPosition = CombatTargetPosition(unit.AttackTargetKind, unit.AttackTargetId.Value);
        if (actualPosition is null)
        {
            return null;
        }

        if (CanTrackActualTarget(unit, actualPosition.Value))
        {
            RememberAttackTargetPosition(unit, actualPosition.Value);
            unit.PlayerIntentTarget = actualPosition.Value;
            unit.CommandVisualTarget = actualPosition.Value;
            return actualPosition.Value;
        }

        if (unit.AttackTargetLastKnownPosition is null)
        {
            RememberAttackTargetPosition(unit, actualPosition.Value);
            unit.PlayerIntentTarget = actualPosition.Value;
            unit.CommandVisualTarget = actualPosition.Value;
            return actualPosition.Value;
        }

        unit.AttackTargetLostTrailRemaining = Mathf.Max(0, unit.AttackTargetLostTrailRemaining - dt);
        if (unit.AttackTargetLostTrailRemaining <= 0)
        {
            return null;
        }

        var lastKnownTrailPoint = ClampInsideWorld(
            unit.AttackTargetLastKnownPosition.Value + unit.AttackTargetLastKnownDirection * AttackLostTrailLeadDistance,
            unit.RuntimeDescriptor.Radius + 28);
        unit.PlayerIntentTarget = lastKnownTrailPoint;
        unit.CommandVisualTarget = lastKnownTrailPoint;
        return lastKnownTrailPoint;
    }

    private bool CanTrackActualTarget(UnitModel unit, Vector2 actualPosition)
    {
        if (!IsAlliedWithPlayer(unit) || unit.AttackTargetKind != CombatTargetKind.Unit)
        {
            return true;
        }

        return FogOfWar.IsVisible(actualPosition)
            || unit.Position.DistanceTo(actualPosition) <= unit.RuntimeDescriptor.SightRange;
    }

    private void RememberAttackTargetPosition(UnitModel unit, Vector2 position)
    {
        if (unit.AttackTargetLastKnownPosition is { } previous)
        {
            var movement = position - previous;
            if (movement.LengthSquared() > 1)
            {
                unit.AttackTargetLastKnownDirection = movement.Normalized();
            }
        }
        else
        {
            var direction = position - unit.Position;
            if (direction.LengthSquared() > 1)
            {
                unit.AttackTargetLastKnownDirection = direction.Normalized();
            }
        }

        unit.AttackTargetLastKnownPosition = position;
        unit.AttackTargetLostTrailRemaining = AttackLostTrailSeconds;
    }

    private static void ClearAttackTrackingMemory(UnitModel unit)
    {
        unit.AttackTargetLastKnownPosition = null;
        unit.AttackTargetLastKnownDirection = Vector2.Right;
        unit.AttackTargetLostTrailRemaining = 0;
        unit.CommandVisualTarget = null;
    }

    private static void SetCombatAnchor(UnitModel unit)
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
        unit.FormationSlot = unit.Position;
        unit.AnchorPosition = unit.Position;
        unit.MovementState = UnitMovementState.CombatAnchor;
        unit.MoveMode = MoveCommandMode.Direct;
    }

    private void RefreshManualAttackSlot(UnitModel unit, Vector2 targetPosition)
    {
        if (unit.AttackTargetId is null)
        {
            return;
        }

        var occupiedSlots = Units
            .Where(other => other.Id != unit.Id && other.Hp > 0 && other.MovementState == UnitMovementState.CombatAnchor)
            .Where(other => other.AttackTargetId == unit.AttackTargetId && other.AttackTargetKind == unit.AttackTargetKind)
            .Select(other => (Position: other.Position, Radius: other.RuntimeDescriptor.Radius))
            .ToList();
        var destination = CreateAttackSlot(unit, unit.AttackTargetKind, unit.AttackTargetId.Value, targetPosition, occupiedSlots);
        if (unit.FormationSlot is { } slot
            && slot.DistanceTo(destination) <= AttackSlotRepathDistance
            && unit.MoveTarget is not null
            && unit.MovementState != UnitMovementState.CombatAnchor)
        {
            return;
        }

        AssignPath(unit, destination, targetPosition);
        unit.AnchorPosition = destination;
    }
}
