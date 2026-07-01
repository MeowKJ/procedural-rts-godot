using Godot;

namespace ProceduralRts.Core;

public sealed partial class CombatSystem
{
    private static WeaponUserComponentState SetAutoTarget(
        EntityWorld world,
        EntityInstance attacker,
        WeaponUserComponentState weapon,
        EntityInstance target)
    {
        if (!weapon.AttackTargetIsManual && weapon.AttackTarget.Value == target.Id.Value)
        {
            return RememberVisibleTarget(attacker, weapon, target);
        }

        var next = weapon with
        {
            AttackTarget = target.Id,
            AttackTargetKind = WeaponEngagementQueries.TargetKind(world, target),
            AttackTargetIsManual = false,
            AutoReacquireCooldownRemaining = 0,
            LastKnownTargetPosition = target.Transform.Position,
            LastKnownTargetRemaining = LastKnownTargetMemorySeconds,
        };
        attacker.Components.Set(next);
        return next;
    }

    private static WeaponUserComponentState RememberVisibleTarget(
        EntityInstance attacker,
        WeaponUserComponentState weapon,
        EntityInstance target)
    {
        if (weapon.AttackTargetIsManual)
        {
            return weapon;
        }

        var position = target.Transform.Position;
        if (weapon.LastKnownTargetPosition is { } existing
            && existing.DistanceSquaredTo(position) <= 0.01f
            && Mathf.IsEqualApprox(weapon.LastKnownTargetRemaining, LastKnownTargetMemorySeconds))
        {
            return weapon;
        }

        var next = weapon with
        {
            LastKnownTargetPosition = position,
            LastKnownTargetRemaining = LastKnownTargetMemorySeconds,
        };
        attacker.Components.Set(next);
        return next;
    }

    private static WeaponUserComponentState ClearAutoTarget(
        EntityInstance attacker,
        WeaponUserComponentState weapon,
        bool startReacquireCooldown = false,
        bool clearLastKnownTarget = false)
    {
        return weapon.AttackTargetIsManual ? weapon : ClearAttackTarget(attacker, weapon, startReacquireCooldown, clearLastKnownTarget);
    }

    private static WeaponUserComponentState ClearAttackTarget(
        EntityInstance attacker,
        WeaponUserComponentState weapon,
        bool startAutoReacquireCooldown = false,
        bool clearLastKnownTarget = false)
    {
        var cooldown = startAutoReacquireCooldown && !weapon.AttackTargetIsManual && weapon.AttackTarget.IsValid
            ? MathF.Max(weapon.AutoReacquireCooldownRemaining, AutoReacquireCooldownSeconds)
            : weapon.AutoReacquireCooldownRemaining;
        if (weapon.AttackTargetIsManual)
        {
            cooldown = 0;
            clearLastKnownTarget = true;
        }

        if (!weapon.AttackTarget.IsValid
            && !weapon.AttackTargetIsManual
            && Mathf.IsEqualApprox(weapon.AutoReacquireCooldownRemaining, cooldown)
            && (!clearLastKnownTarget || (weapon.LastKnownTargetPosition is null && weapon.LastKnownTargetRemaining <= 0)))
        {
            return weapon;
        }

        var next = weapon with
        {
            AttackTarget = default,
            AttackTargetKind = CombatTargetKind.Unit,
            AttackTargetIsManual = false,
            AutoReacquireCooldownRemaining = cooldown,
            LastKnownTargetPosition = clearLastKnownTarget ? null : weapon.LastKnownTargetPosition,
            LastKnownTargetRemaining = clearLastKnownTarget ? 0 : weapon.LastKnownTargetRemaining,
        };
        attacker.Components.Set(next);
        return next;
    }

    private static void ApplyLastKnownTargetIntent(
        EntityWorld world,
        EntityInstance attacker,
        WeaponUserComponentState weapon,
        AutonomyModel autonomy)
    {
        if (weapon.LastKnownTargetPosition is not { } lastKnown || weapon.LastKnownTargetRemaining <= 0)
        {
            StopBlindCombatMove(attacker);
            return;
        }

        if (!ShouldChaseLastKnownTarget(world, attacker, weapon, autonomy)
            || !attacker.Components.TryGet<MovementComponentState>(out var movement)
            || !attacker.Components.TryGet<MovementProfileComponentState>(out var profile))
        {
            StopBlindCombatMove(attacker);
            return;
        }

        var distance = attacker.Transform.Position.DistanceTo(lastKnown);
        if (distance <= MathF.Max(profile.ArriveRadius, 2f))
        {
            attacker.Components.Set(movement with
            {
                Velocity = Vector2.Zero,
                MoveTarget = null,
                FormationSlot = null,
            });
            return;
        }

        attacker.Components.Set(movement with
        {
            MoveTarget = lastKnown,
            FormationSlot = null,
        });
    }

    private static bool ShouldChaseLastKnownTarget(
        EntityWorld world,
        EntityInstance attacker,
        WeaponUserComponentState weapon,
        AutonomyModel autonomy)
    {
        if (!autonomy.AllowsChase || !attacker.Components.Has<MovementProfileComponentState>())
        {
            return false;
        }

        var range = WeaponRange(world, attacker, weapon);
        return range > 0
            && range <= LastKnownShortRangeChaseThreshold
            && !UsesTrackingProjectileRule(world, weapon);
    }

    private static bool UsesTrackingProjectileRule(EntityWorld world, WeaponUserComponentState weapon)
    {
        foreach (var mount in weapon.Mounts)
        {
            if (world.TryGetWeaponDefinition(mount.WeaponId, out var definition)
                && world.TryGetAmmoDefinition(definition.AmmoId, out var ammo)
                && ammo.Behavior == ProjectileBehavior.Tracking)
            {
                return true;
            }
        }

        return false;
    }

    private static void StopBlindCombatMove(EntityInstance attacker)
    {
        if (!attacker.Components.TryGet<MovementComponentState>(out var movement)
            || movement.MoveTarget is null)
        {
            return;
        }

        attacker.Components.Set(movement with
        {
            Velocity = Vector2.Zero,
            MoveTarget = null,
            FormationSlot = null,
        });
    }
}
