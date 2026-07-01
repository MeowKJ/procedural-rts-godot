namespace ProceduralRts.Core;

static class WeaponEngagementMountLoop
{
    public static int Tick(
        SimContext context,
        EntityInstance attacker,
        EntityInstance target,
        IList<WeaponMountRuntimeState> mounts,
        float desiredFacing,
        float dt,
        WeaponEngagementMountLoopOptions options)
    {
        var fired = 0;
        for (var index = 0; index < mounts.Count; index++)
        {
            var mount = mounts[index];
            var cooldown = WeaponEngagementMath.TickCooldown(mount.CooldownRemaining, dt);
            var facing = WeaponEngagementMath.RotateToward(
                mount.Facing,
                desiredFacing,
                WeaponEngagementMath.MountTurnRate(context.World, attacker, mount) * dt);

            if (CanFire(context.World, target, mount, facing, desiredFacing, cooldown, fired, options, out var weaponDef))
            {
                cooldown = weaponDef.Cooldown;
                var damage = Damage(context.World, attacker, weaponDef, target, options.DamageVariance);
                WeaponEngagementResolution.Fire(context, attacker, target, mount, weaponDef, damage);
                fired++;

                if (options.AnchorMovementOnFire)
                {
                    AnchorMovement(attacker);
                }
            }

            mounts[index] = mount with { Facing = facing, CooldownRemaining = cooldown };
        }

        return fired;
    }

    private static bool CanFire(
        EntityWorld world,
        EntityInstance target,
        WeaponMountRuntimeState mount,
        float facing,
        float desiredFacing,
        float cooldown,
        int fired,
        WeaponEngagementMountLoopOptions options,
        out WeaponDefinition weaponDef)
    {
        weaponDef = null!;
        if (!options.InRange
            || (options.FireOnlyOneMount && fired > 0)
            || cooldown > 0
            || !WeaponEngagementMath.IsAimed(facing, desiredFacing)
            || !world.TryGetWeaponDefinition(mount.WeaponId, out weaponDef))
        {
            return false;
        }

        if (options.RespectMinimumRange
            && WeaponMath.InsideMinRange(weaponDef, options.CenterDistance, options.TargetRadius))
        {
            return false;
        }

        return options.RequirePositivePriority
            ? WeaponMath.TargetPriority(world, weaponDef, target, options.UseStructureTargetDefaults) > 0
            : WeaponMath.CanTarget(world, weaponDef, target, options.UseStructureTargetDefaults);
    }

    private static float Damage(
        EntityWorld world,
        EntityInstance attacker,
        WeaponDefinition weaponDef,
        EntityInstance target,
        float variance)
    {
        var baseDamage = WeaponMath.BaseDamage(world, attacker, weaponDef, target);
        if (baseDamage <= 0 || variance <= 0)
        {
            return baseDamage;
        }

        var jitter = 1f + world.Rng.NextRange(-variance, variance);
        return baseDamage * jitter;
    }

    private static void AnchorMovement(EntityInstance attacker)
    {
        if (!attacker.Components.TryGet<MovementComponentState>(out var movement))
        {
            return;
        }

        attacker.Components.Set(movement with
        {
            Velocity = Godot.Vector2.Zero,
            MoveTarget = null,
            FireAnchorRemaining = MathF.Max(movement.FireAnchorRemaining, CombatSystem.FireAnchorSeconds),
        });
    }
}

readonly record struct WeaponEngagementMountLoopOptions(
    bool InRange,
    float CenterDistance,
    float TargetRadius,
    bool RespectMinimumRange,
    bool UseStructureTargetDefaults,
    bool RequirePositivePriority,
    bool FireOnlyOneMount,
    bool AnchorMovementOnFire,
    float DamageVariance);
