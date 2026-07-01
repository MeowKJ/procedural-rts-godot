using Godot;

namespace ProceduralRts.Core;

/// <summary>
/// Executes targeted repair orders as deterministic over-time work. Commands set
/// a RepairOrderComponentState; this system moves the repairer into range, spends
/// owner Credits, and restores friendly damaged health in stable entity order.
/// </summary>
public sealed class RepairSystem : ISimSystem
{
    public void Step(SimContext context)
    {
        foreach (var repairer in context.World.OrderedEntities)
        {
            if (!repairer.Components.TryGet<RepairOrderComponentState>(out var order))
            {
                continue;
            }

            StepRepairer(context.World, repairer, order, context.FixedDelta);
        }
    }

    private static void StepRepairer(EntityWorld world, EntityInstance repairer, RepairOrderComponentState order, float dt)
    {
        if (!world.TryGet(new EntityId(order.TargetId), out var target)
            || !target.Components.TryGet<HealthComponentState>(out var health)
            || health.Hp <= 0
            || !IsFriendly(world, repairer, target))
        {
            repairer.Components.Remove<RepairOrderComponentState>();
            return;
        }

        var canRepairHealth = health.Hp < health.MaxHp;
        var canRestartConstruction = TryGetRestartCapture(target, out _);
        if (!canRepairHealth && !canRestartConstruction)
        {
            StopRepairer(repairer);
            repairer.Components.Remove<RepairOrderComponentState>();
            return;
        }

        var distance = repairer.Transform.Position.DistanceTo(target.Transform.Position);
        if (distance > MathF.Max(0, order.Range))
        {
            MoveTowardTarget(repairer, target.Transform.Position);
            return;
        }

        StopRepairer(repairer);
        ApplyRepair(world, repairer, target, health, order, dt);
    }

    private static void ApplyRepair(
        EntityWorld world,
        EntityInstance repairer,
        EntityInstance target,
        HealthComponentState health,
        RepairOrderComponentState order,
        float dt)
    {
        var potential = order.RepairProgress + MathF.Max(0, order.RepairPerSecond) * dt;
        var costPerHp = MathF.Max(0, order.CreditCostPerHp);
        var inventory = world.ResourceInventory(repairer.OwnerId);
        var affordableWork = costPerHp > 0
            ? MathF.Floor(inventory.Credits / costPerHp)
            : float.MaxValue;
        if (costPerHp > 0 && affordableWork <= 0)
        {
            repairer.Components.Set(order);
            return;
        }

        var appliedWork = 0f;
        var remainingPotential = potential;
        var remainingBudget = affordableWork;
        var nextHealth = health;

        var healthWork = RepairWork(remainingPotential, health.MaxHp - health.Hp, remainingBudget);
        if (healthWork > 0)
        {
            nextHealth = health with { Hp = MathF.Min(health.MaxHp, health.Hp + healthWork) };
            target.Components.Set(nextHealth);
            appliedWork += healthWork;
            remainingPotential -= healthWork;
            remainingBudget -= healthWork;
        }

        if (TryGetRestartCapture(target, out var construction))
        {
            var constructionMax = MathF.Max(1, nextHealth.MaxHp);
            var constructionMissing = (1 - Math.Clamp(construction.Progress, 0, 1)) * constructionMax;
            var constructionWork = RepairWork(remainingPotential, constructionMissing, remainingBudget);
            if (constructionWork > 0)
            {
                var nextProgress = Math.Clamp(construction.Progress + constructionWork / constructionMax, 0, 1);
                target.Components.Set(construction with
                {
                    Progress = nextProgress,
                    Phase = nextProgress >= 1 ? ConstructionPhase.Building : ConstructionPhase.RestartCapture,
                    PauseReason = ConstructionPauseReason.None,
                });
                appliedWork += constructionWork;
                remainingPotential -= constructionWork;
            }
        }

        if (appliedWork <= 0)
        {
            repairer.Components.Set(order with { RepairProgress = potential });
            return;
        }

        var spend = costPerHp <= 0 ? 0 : (int)MathF.Ceiling(appliedWork * costPerHp);
        inventory.Credits -= spend;
        var nextRepairProgress = MathF.Max(0, potential - appliedWork);
        if (nextHealth.Hp >= nextHealth.MaxHp && !TryGetRestartCapture(target, out _))
        {
            repairer.Components.Remove<RepairOrderComponentState>();
        }
        else
        {
            repairer.Components.Set(order with { RepairProgress = nextRepairProgress });
        }
    }

    private static float RepairWork(float potential, float missing, float budget)
    {
        if (potential <= 0 || missing <= 0 || budget <= 0)
        {
            return 0;
        }

        var available = MathF.Min(potential, budget);
        var work = MathF.Min(available, missing);
        return missing <= available && missing < 1f ? missing : MathF.Floor(work);
    }

    private static bool TryGetRestartCapture(EntityInstance target, out ConstructionComponentState construction)
    {
        if (target.Components.TryGet<ConstructionComponentState>(out construction!)
            && construction.Phase == ConstructionPhase.RestartCapture
            && construction.Progress < 1)
        {
            return true;
        }

        construction = default!;
        return false;
    }

    private static void MoveTowardTarget(EntityInstance repairer, Vector2 target)
    {
        var movement = repairer.Components.TryGet<MovementComponentState>(out var existingMovement)
            ? existingMovement
            : new MovementComponentState(Vector2.Zero);
        repairer.Components.Set(movement with { MoveTarget = target, FormationSlot = null });
    }

    private static void StopRepairer(EntityInstance repairer)
    {
        if (repairer.Components.TryGet<MovementComponentState>(out var movement))
        {
            repairer.Components.Set(movement with { Velocity = Vector2.Zero, MoveTarget = null });
        }
    }

    private static bool IsFriendly(EntityWorld world, EntityInstance repairer, EntityInstance target)
    {
        return world.Relations.Relation(repairer.OwnerId, target.OwnerId) is PlayerRelation.Self or PlayerRelation.Allied;
    }
}
