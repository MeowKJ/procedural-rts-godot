using Godot;

namespace ProceduralRts.Core;

public sealed partial class UnitBattlefield
{
    public void ActiveRepairFeedbackProjections(List<ActiveRepairFeedbackProjection> result)
    {
        result.Clear();
        foreach (var repairer in _entityWorld.OrderedEntities)
        {
            if (!repairer.Components.TryGet<RepairOrderComponentState>(out var order)
                || !_entityWorld.TryGet(new EntityId(order.TargetId), out var target)
                || !IsActiveRepairFeedbackTarget(repairer, target, order, out var targetRadius))
            {
                continue;
            }

            result.Add(new ActiveRepairFeedbackProjection(
                repairer.Id,
                target.Id,
                repairer.Transform.Position,
                target.Transform.Position,
                targetRadius,
                MathF.Max(0, order.RepairPerSecond),
                MathF.Max(0, order.RepairProgress),
                PlayerSlotAccent(repairer.OwnerId.ToPlayerSlot()).Lerp(new Color("#8fffe1"), 0.34f)));
        }
    }

    public int ActiveRepairFeedbackProjectionCount()
    {
        var count = 0;
        foreach (var repairer in _entityWorld.OrderedEntities)
        {
            if (repairer.Components.TryGet<RepairOrderComponentState>(out var order)
                && _entityWorld.TryGet(new EntityId(order.TargetId), out var target)
                && IsActiveRepairFeedbackTarget(repairer, target, order, out _))
            {
                count++;
            }
        }

        return count;
    }

    private bool IsActiveRepairFeedbackTarget(
        EntityInstance repairer,
        EntityInstance target,
        RepairOrderComponentState order,
        out float targetRadius)
    {
        targetRadius = RepairFeedbackTargetRadius(target);
        if (repairer.Transform.Position.DistanceTo(target.Transform.Position) > MathF.Max(0, order.Range))
        {
            return false;
        }

        return target.Components.TryGet<HealthComponentState>(out var health)
            && health.Hp > 0
            && CanFundRepairFeedback(repairer, order)
            && HasRepairFeedbackThroughput(order)
            && IsFriendlyRepairTarget(repairer, target)
            && (health.Hp < health.MaxHp || IsRestartCaptureTarget(target));
    }

    private bool CanFundRepairFeedback(EntityInstance repairer, RepairOrderComponentState order)
    {
        var costPerHp = MathF.Max(0, order.CreditCostPerHp);
        return costPerHp <= 0
            || MathF.Floor(_entityWorld.ResourceInventory(repairer.OwnerId).Credits / costPerHp) > 0;
    }

    private static bool HasRepairFeedbackThroughput(RepairOrderComponentState order)
    {
        return MathF.Max(0, order.RepairPerSecond) > 0 || order.RepairProgress > 0;
    }

    private static bool IsRestartCaptureTarget(EntityInstance target)
    {
        return target.Components.TryGet<ConstructionComponentState>(out var construction)
            && construction.Phase == ConstructionPhase.RestartCapture
            && construction.Progress < 1;
    }

    private bool IsFriendlyRepairTarget(EntityInstance repairer, EntityInstance target)
    {
        return _entityWorld.Relations.Relation(repairer.OwnerId, target.OwnerId) is PlayerRelation.Self or PlayerRelation.Allied;
    }

    private static float RepairFeedbackTargetRadius(EntityInstance target)
    {
        if (target.Components.TryGet<CollisionComponentState>(out var collision))
        {
            return MathF.Max(10, collision.Radius);
        }

        return target.Components.TryGet<FootprintComponentState>(out var footprint)
            ? MathF.Max(18, footprint.Radius)
            : 22;
    }
}
