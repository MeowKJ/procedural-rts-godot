namespace ProceduralRts.Core;

public sealed partial class AbilitySystem
{
    private static int ApplyRepairField(
        EntityWorld world,
        EntityInstance caster,
        Godot.Vector2 targetPoint,
        AbilitySpec ability)
    {
        var radius = MathF.Max(0, ability.Radius);
        var heal = MathF.Max(0, ability.Value);
        if (radius <= 0 || heal <= 0)
        {
            return 0;
        }

        var healed = 0;
        var radiusSq = radius * radius;
        foreach (var candidate in world.OrderedEntities)
        {
            if (!IsFriendly(world, caster, candidate)
                || !candidate.Components.TryGet<HealthComponentState>(out var health)
                || health.Hp <= 0
                || health.Hp >= health.MaxHp)
            {
                continue;
            }

            if (candidate.Transform.Position.DistanceSquaredTo(targetPoint) > radiusSq)
            {
                continue;
            }

            var nextHp = MathF.Min(health.MaxHp, health.Hp + heal);
            if (nextHp > health.Hp)
            {
                candidate.Components.Set(health with { Hp = nextHp });
                healed++;
            }
        }

        return healed;
    }

    private static int ApplyShieldField(
        EntityWorld world,
        EntityInstance caster,
        Godot.Vector2 targetPoint,
        AbilitySpec ability)
    {
        var radius = MathF.Max(0, ability.Radius);
        var absorb = MathF.Max(0, ability.Value);
        if (radius <= 0 || absorb <= 0)
        {
            return 0;
        }

        var applied = 0;
        var radiusSq = radius * radius;
        foreach (var candidate in world.OrderedEntities)
        {
            if (!IsFriendly(world, caster, candidate)
                || !candidate.Components.Has<HealthComponentState>()
                || IsDead(candidate)
                || candidate.Transform.Position.DistanceSquaredTo(targetPoint) > radiusSq)
            {
                continue;
            }

            var existing = candidate.Components.TryGet<ShieldComponentState>(out var shield)
                ? shield
                : new ShieldComponentState(0, 0);
            candidate.Components.Set(existing with
            {
                AbsorbRemaining = MathF.Max(existing.AbsorbRemaining, absorb),
                DurationRemaining = MathF.Max(existing.DurationRemaining, ShieldFieldDurationSeconds),
            });
            applied++;
        }

        return applied;
    }

    private static bool ApplyScan(
        EntityWorld world,
        EntityInstance caster,
        Godot.Vector2 targetPoint,
        AbilitySpec ability)
    {
        var radius = MathF.Max(0, ability.Radius);
        if (radius <= 0)
        {
            return false;
        }

        var duration = ability.Value > 0 ? ability.Value : DefaultScanDurationSeconds;
        var spec = new EntitySpec
        {
            Id = ScanRevealSpecId,
            Kind = EntityKind.Objective,
            Display = new EntityDisplaySpec("Scan Reveal", "ability.scan.name", "ability.scan.role", "SCN", IconGlyph.Settings),
        };
        world.Spawn(spec, caster.OwnerId, EntityTransform.At(targetPoint), new EntityComponentState[]
        {
            new ScanRevealComponentState(radius, duration),
        });
        return true;
    }

    private static bool ApplyDeployToggle(EntityInstance caster, AbilitySpec ability)
    {
        if (caster.Components.TryGet<DeployComponentState>(out var existing) && existing.IsDeployed)
        {
            caster.Components.Set(existing with
            {
                IsDeployed = false,
                SetupRemaining = 0,
                RangeMultiplier = 1,
            });
            return true;
        }

        if (caster.Components.TryGet<MovementComponentState>(out var movement))
        {
            caster.Components.Set(movement with { Velocity = Godot.Vector2.Zero, MoveTarget = null });
        }

        caster.Components.Set(new DeployComponentState(
            IsDeployed: true,
            SetupRemaining: ability.Radius > 0 ? ability.Radius : DefaultDeploySetupSeconds,
            RangeMultiplier: ability.Value > 0 ? ability.Value : DefaultDeployRangeMultiplier));
        return true;
    }
}
