using Godot;

namespace ProceduralRts.Core;

static class WeaponSystem
{
    public static int Tick(
        SimContext context,
        EntityInstance attacker,
        EntityInstance target,
        IList<WeaponMountRuntimeState> mounts,
        float desiredFacing,
        float dt,
        WeaponSystemOptions options)
    {
        var fired = 0;
        for (var index = 0; index < mounts.Count; index++)
        {
            var mount = TickRecovery(mounts[index], dt);
            var facing = WeaponEngagementMath.RotateToward(
                mount.Facing,
                desiredFacing,
                WeaponEngagementMath.MountTurnRate(context.World, attacker, mount) * dt);

            if (!CanEngageTarget(context.World, target, mount, fired, options, out var weaponDef))
            {
                mounts[index] = mount with { Facing = facing, Phase = WeaponMountPhase.Acquire, WarmupRemaining = 0 };
                continue;
            }

            if (!WeaponEngagementMath.IsAimed(facing, desiredFacing))
            {
                mounts[index] = mount with { Facing = facing, Phase = WeaponMountPhase.Rotate, WarmupRemaining = 0 };
                continue;
            }

            if (IsRecovering(mount))
            {
                mounts[index] = mount with { Facing = facing };
                continue;
            }

            if (weaponDef.Warmup > 0 && mount.Phase != WeaponMountPhase.Warmup)
            {
                mounts[index] = mount with
                {
                    Facing = facing,
                    Phase = WeaponMountPhase.Warmup,
                    WarmupRemaining = weaponDef.Warmup,
                };
                continue;
            }

            if (mount.Phase == WeaponMountPhase.Warmup)
            {
                var warmup = WeaponEngagementMath.TickCooldown(mount.WarmupRemaining, dt);
                if (warmup > 0)
                {
                    mounts[index] = mount with { Facing = facing, WarmupRemaining = warmup };
                    continue;
                }

                mount = mount with { WarmupRemaining = 0 };
            }

            var damage = Damage(context.World, attacker, weaponDef, target, options.DamageVariance);
            WeaponEngagementResolution.Fire(context, attacker, target, mount, weaponDef, damage);
            fired++;

            mount = BeginRecovery(mount, weaponDef) with { Facing = facing };
            if (options.AnchorMovementOnFire)
            {
                AnchorMovement(attacker);
            }

            mounts[index] = mount;
        }

        return fired;
    }

    public static bool IsRecovering(WeaponMountRuntimeState mount)
    {
        return mount.Phase == WeaponMountPhase.Fire
            || mount.CooldownRemaining > 0
            || mount.ReloadRemaining > 0;
    }

    public static WeaponMountRuntimeState BeginRecovery(WeaponMountRuntimeState mount, WeaponDefinition definition)
    {
        return mount with
        {
            Phase = WeaponMountPhase.Fire,
            CooldownRemaining = MathF.Max(0, definition.Cooldown),
            ReloadRemaining = MathF.Max(0, definition.Reload),
            WarmupRemaining = 0,
        };
    }

    public static WeaponMountRuntimeState TickIdle(WeaponMountRuntimeState mount, float dt)
    {
        mount = TickRecovery(mount, dt);
        var phase = RecoveryPhase(mount);
        return mount with
        {
            Phase = phase,
            WarmupRemaining = 0,
        };
    }

    private static WeaponMountRuntimeState TickRecovery(WeaponMountRuntimeState mount, float dt)
    {
        var cooldown = WeaponEngagementMath.TickCooldown(mount.CooldownRemaining, dt);
        var reload = mount.ReloadRemaining;
        if (cooldown <= 0)
        {
            reload = WeaponEngagementMath.TickCooldown(reload, dt);
        }

        return mount with
        {
            CooldownRemaining = cooldown,
            ReloadRemaining = reload,
            Phase = RecoveryPhase(mount, cooldown, reload),
        };
    }

    private static WeaponMountPhase RecoveryPhase(WeaponMountRuntimeState mount)
    {
        return RecoveryPhase(mount, mount.CooldownRemaining, mount.ReloadRemaining);
    }

    private static WeaponMountPhase RecoveryPhase(WeaponMountRuntimeState mount, float cooldown, float reload)
    {
        if (cooldown > 0)
        {
            return WeaponMountPhase.Cooldown;
        }

        if (reload > 0)
        {
            return WeaponMountPhase.Reload;
        }

        return mount.Phase is WeaponMountPhase.Fire or WeaponMountPhase.Cooldown or WeaponMountPhase.Reload
            ? WeaponMountPhase.Acquire
            : mount.Phase;
    }

    private static bool CanEngageTarget(
        EntityWorld world,
        EntityInstance target,
        WeaponMountRuntimeState mount,
        int fired,
        WeaponSystemOptions options,
        out WeaponDefinition weaponDef)
    {
        weaponDef = null!;
        if (!options.InRange
            || (options.FireOnlyOneMount && fired > 0)
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
            Velocity = Vector2.Zero,
            MoveTarget = null,
            FireAnchorRemaining = MathF.Max(movement.FireAnchorRemaining, CombatSystem.FireAnchorSeconds),
        });
    }
}

readonly record struct WeaponSystemOptions(
    bool InRange,
    float CenterDistance,
    float TargetRadius,
    bool RespectMinimumRange,
    bool UseStructureTargetDefaults,
    bool RequirePositivePriority,
    bool FireOnlyOneMount,
    bool AnchorMovementOnFire,
    float DamageVariance);
