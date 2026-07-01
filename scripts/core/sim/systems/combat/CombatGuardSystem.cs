using Godot;

namespace ProceduralRts.Core;

public sealed partial class CombatSystem
{
    private TargetCandidate? BestGuardHostile(
        EntityWorld world,
        EntityInstance attacker,
        WeaponUserComponentState weapon,
        GuardOrderComponentState guard,
        Vector2 anchor,
        EntityInstance? guardedEntity)
    {
        if (guard.Radius <= 0)
        {
            return null;
        }

        var rangeSq = guard.Radius * guard.Radius;
        TargetCandidate? best = null;
        var cellRadius = _targetGrid.CellRadiusFor(guard.Radius);

        foreach (var candidate in _targetGrid.Neighbors(anchor, cellRadius))
        {
            if (!IsGuardHostileEligible(world, attacker, candidate, anchor, rangeSq))
            {
                continue;
            }

            var priority = GuardTargetPriority(world, attacker, weapon, candidate, anchor, rangeSq, guardedEntity);
            if (priority <= 0)
            {
                continue;
            }

            var guardCandidate = new TargetCandidate(
                candidate,
                priority,
                attacker.Transform.Position.DistanceSquaredTo(candidate.Transform.Position));
            if (best is null || IsPreferred(guardCandidate, best.Value))
            {
                best = guardCandidate;
            }
        }

        return best;
    }

    private bool TryResolveGuardTarget(
        EntityWorld world,
        EntityInstance attacker,
        ref WeaponUserComponentState weapon,
        out EntityInstance target)
    {
        target = null!;
        if (!attacker.Components.TryGet<GuardOrderComponentState>(out var guard)
            || guard.Radius <= 0)
        {
            return false;
        }

        if (HasExplicitMoveOrder(attacker, weapon))
        {
            weapon = ClearAutoTarget(attacker, weapon);
            return false;
        }

        var anchor = GuardAnchor(world, attacker, guard, out var guardedEntity);
        var rangeSq = guard.Radius * guard.Radius;
        var best = BestGuardHostile(world, attacker, weapon, guard, anchor, guardedEntity);
        var isCoolingDown = weapon.AutoReacquireCooldownRemaining > 0;
        var hadAutoTarget = !weapon.AttackTargetIsManual && weapon.AttackTarget.IsValid;
        EntityInstance? current = null;
        if (hadAutoTarget
            && (!TryGetValidHostile(world, attacker, weapon.AttackTarget, out current)
                || !IsGuardHostileEligible(world, attacker, current, anchor, rangeSq)
                || !IsVisibleToOwner(world, attacker.OwnerId, current)
                || TargetPriority(world, weapon, current) <= 0))
        {
            weapon = ClearAutoTarget(attacker, weapon, startReacquireCooldown: true);
            return false;
        }

        if (TryGetValidHostile(world, attacker, weapon.AttackTarget, out current)
            && IsGuardHostileEligible(world, attacker, current, anchor, rangeSq)
            && IsVisibleToOwner(world, attacker.OwnerId, current)
            && TargetPriority(world, weapon, current) > 0)
        {
            weapon = RememberVisibleTarget(attacker, weapon, current);
            var currentCandidate = new TargetCandidate(
                current,
                GuardTargetPriority(world, attacker, weapon, current, anchor, rangeSq, guardedEntity),
                attacker.Transform.Position.DistanceSquaredTo(current.Transform.Position));
            if (currentCandidate.Priority > 0
                && (isCoolingDown || best is null || best.Value.Entity.Id.Value == current.Id.Value || !IsClearlyBetter(best.Value, currentCandidate)))
            {
                target = current;
                return true;
            }
        }

        if (isCoolingDown)
        {
            return false;
        }

        if (best is { } chosen)
        {
            weapon = SetAutoTarget(world, attacker, weapon, chosen.Entity);
            target = chosen.Entity;
            return true;
        }

        weapon = ClearAutoTarget(attacker, weapon);
        return false;
    }
    private static Vector2 GuardAnchor(
        EntityWorld world,
        EntityInstance attacker,
        GuardOrderComponentState guard,
        out EntityInstance? guardedEntity)
    {
        guardedEntity = null;
        if (guard.TargetEntity.IsValid
            && world.TryGet(guard.TargetEntity, out var target)
            && world.Relations.Relation(attacker.OwnerId, target.OwnerId) is PlayerRelation.Self or PlayerRelation.Allied
            && (!target.Components.TryGet<HealthComponentState>(out var health) || health.Hp > 0))
        {
            guardedEntity = target;
            return target.Transform.Position;
        }

        return guard.GuardPoint;
    }

    private static bool IsGuardHostileEligible(
        EntityWorld world,
        EntityInstance attacker,
        EntityInstance target,
        Vector2 anchor,
        float rangeSq)
    {
        return target.Id.Value != attacker.Id.Value
            && !IsDead(target)
            && world.Relations.CanAttack(attacker.OwnerId, target.OwnerId)
            && IsVisibleToOwner(world, attacker.OwnerId, target)
            && target.Transform.Position.DistanceSquaredTo(anchor) <= rangeSq;
    }

    private static float GuardTargetPriority(
        EntityWorld world,
        EntityInstance attacker,
        WeaponUserComponentState weapon,
        EntityInstance target,
        Vector2 anchor,
        float rangeSq,
        EntityInstance? guardedEntity)
    {
        var priority = TargetPriority(world, weapon, target);
        if (priority <= 0)
        {
            return 0;
        }

        return IsThreateningGuardedEntity(world, attacker, target, anchor, rangeSq, guardedEntity)
            ? priority * ThreatTargetPriorityMultiplier
            : priority;
    }

    private static bool IsThreateningGuardedEntity(
        EntityWorld world,
        EntityInstance attacker,
        EntityInstance target,
        Vector2 anchor,
        float rangeSq,
        EntityInstance? guardedEntity)
    {
        if (!target.Components.TryGet<WeaponUserComponentState>(out var targetWeapon)
            || !targetWeapon.AttackTarget.IsValid
            || !world.TryGet(targetWeapon.AttackTarget, out var threatened)
            || (threatened.Components.TryGet<HealthComponentState>(out var health) && health.Hp <= 0))
        {
            return false;
        }

        if (guardedEntity is not null && threatened.Id.Value == guardedEntity.Id.Value)
        {
            return true;
        }

        return world.Relations.Relation(attacker.OwnerId, threatened.OwnerId) is PlayerRelation.Self or PlayerRelation.Allied
            && threatened.Transform.Position.DistanceSquaredTo(anchor) <= rangeSq;
    }
}
