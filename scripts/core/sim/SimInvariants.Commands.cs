using Godot;

namespace ProceduralRts.Core;

public static partial class SimInvariants
{
    private static void ValidateRepairOrder(
        EntityWorld world,
        EntityInstance entity,
        RepairOrderComponentState repairOrder,
        List<SimInvariantViolation> violations)
    {
        CheckEntityReference(world, entity, "RepairOrder.TargetId", repairOrder.TargetId, violations);
        CheckFinite(entity, "RepairOrder.Range", repairOrder.Range, violations);
        CheckFinite(entity, "RepairOrder.RepairPerSecond", repairOrder.RepairPerSecond, violations);
        CheckFinite(entity, "RepairOrder.CreditCostPerHp", repairOrder.CreditCostPerHp, violations);
        CheckFinite(entity, "RepairOrder.RepairProgress", repairOrder.RepairProgress, violations);
        if (repairOrder.Range < 0
            || repairOrder.RepairPerSecond < 0
            || repairOrder.CreditCostPerHp < 0
            || repairOrder.RepairProgress < 0)
        {
            Add(entity, "RepairOrder", "range, repair rate, cost, and progress must be non-negative", violations);
        }
    }

    private static void ValidatePatrolOrder(
        EntityInstance entity,
        PatrolOrderComponentState patrol,
        List<SimInvariantViolation> violations)
    {
        CheckFinite(entity, "Patrol.PointA", patrol.PointA, violations);
        CheckFinite(entity, "Patrol.PointB", patrol.PointB, violations);

        if (patrol.PointA.DistanceSquaredTo(patrol.PointB) <= 1f)
        {
            Add(entity, "Patrol", "endpoints must be distinct", violations);
        }
    }

    private static void ValidateGuardOrder(
        EntityInstance entity,
        GuardOrderComponentState guard,
        List<SimInvariantViolation> violations)
    {
        CheckFinite(entity, "Guard.GuardPoint", guard.GuardPoint, violations);
        CheckFinite(entity, "Guard.Radius", guard.Radius, violations);

        if (guard.TargetEntity.Value < 0)
        {
            Add(entity, "Guard", $"target entity id must be non-negative, got {guard.TargetEntity.Value}", violations);
        }

        if (guard.Radius <= 0)
        {
            Add(entity, "Guard", $"radius must be positive, got {guard.Radius}", violations);
        }
    }

    private static void ValidateCommandQueue(
        EntityInstance entity,
        CommandQueueComponentState commandQueue,
        List<SimInvariantViolation> violations)
    {
        if (commandQueue.Items is null)
        {
            Add(entity, "CommandQueue", "items must not be null", violations);
            return;
        }

        if (commandQueue.Items.Count > MaxCommandQueueItems)
        {
            Add(entity, "CommandQueue", $"queue length {commandQueue.Items.Count} exceeds {MaxCommandQueueItems}", violations);
        }
    }
}
