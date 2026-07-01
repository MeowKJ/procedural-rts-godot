namespace ProceduralRts.Core;

public sealed partial class AbilitySystem
{
    private static void TickCooldowns(EntityWorld world, float dt)
    {
        foreach (var entity in world.OrderedEntities)
        {
            if (!entity.Components.TryGet<AbilityRuntimeComponentState>(out var runtime)
                || runtime.Cooldowns.Count == 0)
            {
                continue;
            }

            var changed = false;
            var cooldowns = runtime.Cooldowns.ToArray();
            for (var index = 0; index < cooldowns.Length; index++)
            {
                var cooldown = cooldowns[index];
                var next = MathF.Max(0, cooldown.CooldownRemaining - dt);
                if (MathF.Abs(next - cooldown.CooldownRemaining) > 0.0001f)
                {
                    cooldowns[index] = cooldown with { CooldownRemaining = next };
                    changed = true;
                }
            }

            if (changed)
            {
                entity.Components.Set(runtime with { Cooldowns = cooldowns });
            }
        }
    }

    private static void TickShields(EntityWorld world, float dt)
    {
        foreach (var entity in world.OrderedEntities)
        {
            if (!entity.Components.TryGet<ShieldComponentState>(out var shield))
            {
                continue;
            }

            var nextDuration = MathF.Max(0, shield.DurationRemaining - dt);
            if (nextDuration <= 0 || shield.AbsorbRemaining <= 0)
            {
                entity.Components.Remove<ShieldComponentState>();
                continue;
            }

            entity.Components.Set(shield with { DurationRemaining = nextDuration });
        }
    }

    private static void TickScanReveals(EntityWorld world, float dt)
    {
        foreach (var entity in world.OrderedEntities)
        {
            if (!entity.Components.TryGet<ScanRevealComponentState>(out var scan))
            {
                continue;
            }

            var nextDuration = MathF.Max(0, scan.DurationRemaining - dt);
            if (nextDuration <= 0 || scan.Radius <= 0)
            {
                world.QueueRemoval(entity.Id);
                continue;
            }

            entity.Components.Set(scan with { DurationRemaining = nextDuration });
        }
    }

    private static void TickDeploySetup(EntityWorld world, float dt)
    {
        foreach (var entity in world.OrderedEntities)
        {
            if (!entity.Components.TryGet<DeployComponentState>(out var deploy)
                || !deploy.IsDeployed
                || deploy.SetupRemaining <= 0)
            {
                continue;
            }

            entity.Components.Set(deploy with { SetupRemaining = MathF.Max(0, deploy.SetupRemaining - dt) });
        }
    }
}
