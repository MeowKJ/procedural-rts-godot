using Godot;

namespace ProceduralRts.Core;

public sealed partial class CombatSystem
{
    private HostileSearchResult BestHostile(
        EntityWorld world,
        EntityInstance attacker,
        WeaponUserComponentState weapon,
        float range,
        AutonomyModel autonomy)
    {
        if (range <= 0)
        {
            return new HostileSearchResult(null, 0);
        }

        var rangeSq = range * range;
        var eligibleCount = 0;
        TargetCandidate? bestBase = null;
        TargetCandidate? bestWithThreat = null;
        var origin = attacker.Transform.Position;
        var cellRadius = _targetGrid.CellRadiusFor(range);

        foreach (var candidate in _targetGrid.Neighbors(origin, cellRadius))
        {
            if (candidate.Id.Value == attacker.Id.Value || IsDead(candidate))
            {
                continue;
            }

            if (!world.Relations.CanAttack(attacker.OwnerId, candidate.OwnerId))
            {
                continue;
            }

            if (!IsVisibleToOwner(world, attacker.OwnerId, candidate))
            {
                continue;
            }

            if (IsOutsideLeash(attacker, candidate, autonomy))
            {
                continue;
            }

            var distSq = origin.DistanceSquaredTo(candidate.Transform.Position);
            if (distSq > rangeSq)
            {
                continue;
            }

            var basePriority = TargetPriority(world, weapon, candidate);
            if (basePriority <= 0)
            {
                continue;
            }

            eligibleCount++;
            var baseCandidate = new TargetCandidate(candidate, basePriority, distSq);
            if (bestBase is null || IsPreferred(baseCandidate, bestBase.Value))
            {
                bestBase = baseCandidate;
            }

            var weightedPriority = AutoTargetPriority(world, attacker, weapon, candidate, autonomy, allowThreatWeight: true);
            var weightedCandidate = new TargetCandidate(candidate, weightedPriority, distSq);
            if (bestWithThreat is null || IsPreferred(weightedCandidate, bestWithThreat.Value))
            {
                bestWithThreat = weightedCandidate;
            }
        }

        return new HostileSearchResult(
            eligibleCount <= ThreatTargetMaxLocalCandidates ? bestWithThreat : bestBase,
            eligibleCount);
    }

    private static bool TryGetValidHostile(
        EntityWorld world,
        EntityInstance attacker,
        EntityId targetId,
        out EntityInstance target)
    {
        if (targetId.Value > 0
            && world.TryGet(targetId, out target!)
            && !IsDead(target)
            && world.Relations.CanAttack(attacker.OwnerId, target.OwnerId))
        {
            return true;
        }

        target = null!;
        return false;
    }

    private static bool IsVisibleToOwner(EntityWorld world, OwnerId owner, EntityInstance target)
    {
        return world.Visibility.IsVisible(owner, target.Id);
    }

    private static bool HasExplicitMoveOrder(EntityInstance attacker, WeaponUserComponentState weapon)
    {
        if (weapon.AttackTargetIsManual
            || !attacker.Components.TryGet<MovementComponentState>(out var movement)
            || movement.MoveTarget is not { } moveTarget
            || !attacker.Components.TryGet<CommandableComponentState>(out var commandable)
            || commandable.MoveMode == MoveCommandMode.Attack)
        {
            return false;
        }

        if (commandable.MoveMode == MoveCommandMode.Ignore)
        {
            return true;
        }

        if (commandable.PlayerIntentTarget is { } intent && intent.DistanceSquaredTo(moveTarget) <= 1f)
        {
            return true;
        }

        if (commandable.CommandVisualTarget is { } visual && visual.DistanceSquaredTo(moveTarget) <= 1f)
        {
            return true;
        }

        return movement.FormationSlot is { } slot && slot.DistanceSquaredTo(moveTarget) <= 1f;
    }
    private static float AcceptableStickinessRange(float acquireRange, float weaponRange)
    {
        var slack = MathF.Max(TargetStickinessMinSlack, weaponRange * (TargetStickinessRangeMultiplier - 1f));
        return acquireRange + slack;
    }

    private static bool IsClearlyBetter(TargetCandidate candidate, TargetCandidate current)
    {
        if (current.Priority <= 0)
        {
            return candidate.Priority > 0;
        }

        return candidate.Priority >= current.Priority * TargetSwitchPriorityMargin
            || (candidate.Priority >= current.Priority && candidate.DistanceSq <= current.DistanceSq * TargetSwitchDistanceFactor);
    }

    private static bool IsPreferred(TargetCandidate candidate, TargetCandidate current)
    {
        if (!Mathf.IsEqualApprox(candidate.Priority, current.Priority))
        {
            return candidate.Priority > current.Priority;
        }

        if (!Mathf.IsEqualApprox(candidate.DistanceSq, current.DistanceSq))
        {
            return candidate.DistanceSq < current.DistanceSq;
        }

        return candidate.Entity.Id.Value < current.Entity.Id.Value;
    }
    private static float TargetPriority(EntityWorld world, WeaponUserComponentState weapon, EntityInstance target)
    {
        var anyWeapon = false;
        var best = 0f;
        foreach (var mount in weapon.Mounts)
        {
            if (!world.TryGetWeaponDefinition(mount.WeaponId, out var weaponDef))
            {
                continue;
            }

            anyWeapon = true;
            best = MathF.Max(best, WeaponPriority(world, weaponDef, target));
        }

        return anyWeapon ? best : 1f;
    }

    private static float AutoTargetPriority(
        EntityWorld world,
        EntityInstance attacker,
        WeaponUserComponentState weapon,
        EntityInstance target,
        AutonomyModel autonomy,
        bool allowThreatWeight)
    {
        var priority = TargetPriority(world, weapon, target);
        if (priority <= 0)
        {
            return 0;
        }

        if (!allowThreatWeight)
        {
            return priority;
        }

        if (IsThreateningTarget(target, attacker.Id))
        {
            return priority * ThreatTargetPriorityMultiplier;
        }

        return IsSharedAllyThreat(world, attacker, weapon, target, autonomy)
            ? priority * SharedAllyThreatPriorityMultiplier
            : priority;
    }

    private static bool IsThreateningTarget(EntityInstance target, EntityId attackerId)
    {
        return attackerId.IsValid
            && target.Components.TryGet<WeaponUserComponentState>(out var targetWeapon)
            && !targetWeapon.AttackTargetIsManual
            && targetWeapon.AttackTarget.Value == attackerId.Value;
    }

    private static bool IsSharedAllyThreat(
        EntityWorld world,
        EntityInstance responder,
        WeaponUserComponentState responderWeapon,
        EntityInstance target,
        AutonomyModel autonomy)
    {
        if (!target.Components.TryGet<WeaponUserComponentState>(out var targetWeapon)
            || targetWeapon.AttackTargetIsManual
            || !targetWeapon.AttackTarget.IsValid
            || !world.TryGet(targetWeapon.AttackTarget, out var threatened)
            || threatened.Id.Value == responder.Id.Value
            || IsDead(threatened)
            || world.Relations.Relation(responder.OwnerId, threatened.OwnerId) is not (PlayerRelation.Self or PlayerRelation.Allied))
        {
            return false;
        }

        var stance = responder.Components.TryGet<StanceComponentState>(out var stanceState)
            ? stanceState.Stance
            : UnitStance.Aggressive;
        if (stance is UnitStance.Ignore or UnitStance.PassiveRetaliate)
        {
            return false;
        }

        if (responder.Transform.Position.DistanceSquaredTo(threatened.Transform.Position)
            > SharedAllyThreatRadius * SharedAllyThreatRadius)
        {
            return false;
        }

        if (stance == UnitStance.Hold)
        {
            var holdRange = WeaponMath.EffectiveRange(world, responder, responderWeapon) + HoldSharedThreatSlack;
            return responder.Transform.Position.DistanceSquaredTo(target.Transform.Position) <= holdRange * holdRange;
        }

        if (stance == UnitStance.ReturnGuard
            && autonomy.AnchorPosition is { } anchor
            && autonomy.LeashRange > 0
            && threatened.Transform.Position.DistanceSquaredTo(anchor) > autonomy.LeashRange * autonomy.LeashRange)
        {
            return false;
        }

        return true;
    }

    private static float WeaponPriority(EntityWorld world, WeaponDefinition weaponDef, EntityInstance target)
    {
        return WeaponMath.TargetPriority(world, weaponDef, target, useStructureDefaults: true);
    }

    private readonly record struct TargetCandidate(EntityInstance Entity, float Priority, float DistanceSq);

    private readonly record struct HostileSearchResult(TargetCandidate? Best, int EligibleCount);
}
