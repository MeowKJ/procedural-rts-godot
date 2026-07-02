using Godot;

namespace ProceduralRts.Core;

public sealed partial class CombatSystem
{
    private EntityInstance? ResolveTarget(SimContext context, EntityInstance attacker, WeaponUserComponentState weapon)
    {
        var world = context.World;

        // 1. Manual focus, if still valid (alive + hostile).
        if (weapon.AttackTargetIsManual)
        {
            if (TryGetValidHostile(world, attacker, weapon.AttackTarget, out var manual)
                && TargetPriority(world, weapon, manual) > 0)
            {
                if (weapon.AutoReacquireCooldownRemaining > 0)
                {
                    weapon = weapon with { AutoReacquireCooldownRemaining = 0 };
                    attacker.Components.Set(weapon);
                }

                return manual;
            }

            weapon = ClearAttackTarget(attacker, weapon);
        }

        // 2. PassiveRetaliate only responds to a recorded attacker. It does not
        // scan for fresh targets, and explicit movement still takes priority.
        if (TryResolveRetaliationTarget(world, attacker, ref weapon, out var retaliationTarget))
        {
            return retaliationTarget;
        }

        // 3. Guard orders are bounded local protection. They do not fall through
        // to default aggressive scans, otherwise a guard could wander off intent.
        if (attacker.Components.Has<GuardOrderComponentState>())
        {
            if (TryResolveGuardTarget(world, attacker, ref weapon, out var guardTarget))
            {
                return guardTarget;
            }

            ClearAutoTarget(attacker, weapon);
            return null;
        }

        // 3. Stance-driven auto-acquire.
        var autonomy = EffectiveAutonomy(world, attacker, weapon);
        if (!autonomy.AllowsAutoAcquire)
        {
            ClearAutoTarget(attacker, weapon);
            return null;
        }

        if (HasExplicitMoveOrder(attacker, weapon))
        {
            ClearAutoTarget(attacker, weapon);
            return null;
        }

        var acquireRange = autonomy.AcquireRange;
        if (acquireRange <= 0)
        {
            ClearAutoTarget(attacker, weapon);
            ReturnToAnchor(attacker, autonomy);
            return null;
        }

        var search = BestHostile(world, attacker, weapon, acquireRange, autonomy);
        var best = search.Best;
        var allowThreatWeight = search.EligibleCount <= ThreatTargetMaxLocalCandidates;
        var isCoolingDown = weapon.AutoReacquireCooldownRemaining > 0;
        var hadAutoTarget = !weapon.AttackTargetIsManual && weapon.AttackTarget.IsValid;
        EntityInstance? current = null;
        if (hadAutoTarget)
        {
            if (!TryGetValidHostile(world, attacker, weapon.AttackTarget, out current)
                || TargetPriority(world, weapon, current) <= 0)
            {
                ClearAutoTarget(attacker, weapon, startReacquireCooldown: true, clearLastKnownTarget: true);
                return null;
            }

            if (!IsVisibleToOwner(world, attacker.OwnerId, current))
            {
                weapon = ClearAutoTarget(attacker, weapon, startReacquireCooldown: true);
                ApplyLastKnownTargetIntent(world, attacker, weapon, autonomy);
                return null;
            }
        }

        if (TryGetValidHostile(world, attacker, weapon.AttackTarget, out current)
            && IsVisibleToOwner(world, attacker.OwnerId, current)
            && TargetPriority(world, weapon, current) > 0)
        {
            weapon = RememberVisibleTarget(attacker, weapon, current);
            if (IsOutsideLeash(attacker, current, autonomy))
            {
                ClearAutoTarget(attacker, weapon, startReacquireCooldown: true, clearLastKnownTarget: true);
                ReturnToAnchor(attacker, autonomy);
                return null;
            }

            var acceptableRange = AcceptableStickinessRange(acquireRange, WeaponMath.EffectiveRange(world, attacker, weapon));
            var currentDistanceSq = attacker.Transform.Position.DistanceSquaredTo(current.Transform.Position);
            if (isCoolingDown || currentDistanceSq <= acceptableRange * acceptableRange)
            {
                var currentCandidate = new TargetCandidate(
                    current,
                    AutoTargetPriority(world, attacker, weapon, current, autonomy, allowThreatWeight),
                    currentDistanceSq);

                if (isCoolingDown || best is null || best.Value.Entity.Id.Value == current.Id.Value || !IsClearlyBetter(best.Value, currentCandidate))
                {
                    return current;
                }
            }
        }

        if (isCoolingDown)
        {
            return null;
        }

        if (best is { } chosen)
        {
            SetAutoTarget(world, attacker, weapon, chosen.Entity);
            return chosen.Entity;
        }

        ClearAutoTarget(attacker, weapon);
        ReturnToAnchor(attacker, autonomy);
        return null;
    }

    private static bool TryResolveRetaliationTarget(
        EntityWorld world,
        EntityInstance attacker,
        ref WeaponUserComponentState weapon,
        out EntityInstance target)
    {
        target = null!;
        if (!IsPassiveRetaliate(attacker)
            || !attacker.Components.TryGet<RetaliationComponentState>(out var retaliation)
            || !retaliation.Target.IsValid)
        {
            return false;
        }

        if (HasExplicitMoveOrder(attacker, weapon))
        {
            weapon = ClearAutoTarget(attacker, weapon);
            return false;
        }

        if (!TryGetValidHostile(world, attacker, retaliation.Target, out target))
        {
            attacker.Components.Remove<RetaliationComponentState>();
            weapon = ClearAutoTarget(attacker, weapon);
            return false;
        }

        if (!IsVisibleToOwner(world, attacker.OwnerId, target))
        {
            weapon = ClearAutoTarget(attacker, weapon);
            return false;
        }

        var autonomy = EffectiveAutonomy(world, attacker, weapon);
        var range = WeaponMath.EffectiveRange(world, attacker, weapon);
        if (range <= 0
            || attacker.Transform.Position.DistanceSquaredTo(target.Transform.Position) > range * range
            || IsOutsideRetaliationLeash(attacker, target, autonomy)
            || TargetPriority(world, weapon, target) <= 0)
        {
            weapon = ClearAutoTarget(attacker, weapon);
            return false;
        }

        weapon = SetAutoTarget(world, attacker, weapon, target);
        return true;
    }
}
