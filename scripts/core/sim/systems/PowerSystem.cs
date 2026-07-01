namespace ProceduralRts.Core;

/// <summary>
/// Deterministic owner-level power budget. Active providers contribute to their
/// owner's supply; active consumers are powered only when that owner has enough
/// total supply for total demand. Consequences live in consumer systems such as
/// ProductionSystem and CombatSystem.
/// </summary>
public sealed class PowerSystem : ISimSystem
{
    public void Step(SimContext context)
    {
        var budgets = BuildBudgets(context.World);

        foreach (var entity in context.World.OrderedEntities)
        {
            if (!entity.Components.TryGet<PowerComponentState>(out var power))
            {
                continue;
            }

            var active = TryGetPowerBudgetContribution(entity, power, out _);
            var ownerBudget = budgets.TryGetValue(entity.OwnerId.Value, out var budget)
                ? budget
                : default;
            var shouldBePowered = active && ShouldBePowered(power, ownerBudget);
            if (power.Powered != shouldBePowered)
            {
                entity.Components.Set(power with { Powered = shouldBePowered });
            }
        }
    }

    private static SortedDictionary<int, PowerBudget> BuildBudgets(EntityWorld world)
    {
        var budgets = new SortedDictionary<int, PowerBudget>();
        foreach (var entity in world.OrderedEntities)
        {
            if (!entity.Components.TryGet<PowerComponentState>(out var power)
                || !TryGetPowerBudgetContribution(entity, power, out var contribution))
            {
                continue;
            }

            budgets.TryGetValue(entity.OwnerId.Value, out var budget);
            budget = budget with
            {
                Provided = budget.Provided + contribution.Provided,
                Used = budget.Used + contribution.Used,
            };
            budgets[entity.OwnerId.Value] = budget;
        }

        return budgets;
    }

    private static bool TryGetPowerBudgetContribution(EntityInstance entity, PowerComponentState power, out PowerBudget contribution)
    {
        contribution = default;

        if (entity.Components.TryGet<HealthComponentState>(out var health) && health.Hp <= 0)
        {
            return false;
        }

        if (!entity.Components.TryGet<ConstructionComponentState>(out var construction) || construction.Progress >= 1)
        {
            contribution = new PowerBudget(power.Provided, power.Used);
            return true;
        }

        if (construction.Progress <= 0 || power.Used <= 0)
        {
            return false;
        }

        contribution = new PowerBudget(Provided: 0, Used: power.Used);
        return true;
    }

    private static bool ShouldBePowered(PowerComponentState power, PowerBudget budget)
    {
        if (power.Used <= 0)
        {
            return power.Provided > 0 || budget.Provided >= budget.Used;
        }

        return budget.Provided >= budget.Used;
    }

    private readonly record struct PowerBudget(int Provided, int Used);
}
