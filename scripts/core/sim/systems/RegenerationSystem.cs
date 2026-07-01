namespace ProceduralRts.Core;

public sealed class RegenerationSystem : ISimSystem
{
    public void Step(SimContext context)
    {
        foreach (var entity in context.World.OrderedEntities)
        {
            if (!entity.Components.TryGet<RegenerationComponentState>(out var regen)
                || !entity.Components.TryGet<HealthComponentState>(out var health))
            {
                continue;
            }

            StepRegeneration(context.World, entity, regen, health, context.FixedDelta);
        }
    }

    private static void StepRegeneration(
        EntityWorld world,
        EntityInstance entity,
        RegenerationComponentState regen,
        HealthComponentState health,
        float dt)
    {
        if (health.Hp <= 0 || health.Hp >= health.MaxHp || regen.HpPerSecond <= 0)
        {
            if (regen.Progress != 0)
            {
                entity.Components.Set(regen with { Progress = 0 });
            }

            return;
        }

        var potential = regen.Progress + UpgradeResolver.HealthRegen(world, entity, regen.HpPerSecond) * dt;
        var missing = health.MaxHp - health.Hp;
        var applied = MathF.Min(missing, potential);
        if (applied <= 0)
        {
            entity.Components.Set(regen with { Progress = potential });
            return;
        }

        entity.Components.Set(health with { Hp = health.Hp + applied });
        entity.Components.Set(regen with { Progress = MathF.Max(0, potential - applied) });
    }
}
