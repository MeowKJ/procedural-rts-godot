using Godot;

namespace ProceduralRts.Core;

public sealed partial class CombatSystem
{
    private static void EngageTarget(
        SimContext context,
        EntityInstance attacker,
        WeaponUserComponentState weapon,
        EntityInstance target,
        float dt)
    {
        var world = context.World;
        var weaponRange = WeaponMath.EffectiveRange(world, attacker, weapon);
        var targetRadius = target.Components.TryGet<CollisionComponentState>(out var targetCollision) ? targetCollision.Radius : 0f;
        var standoffRadius = AttackSlotMath.StandoffRadius(weaponRange, targetRadius);
        var origin = attacker.Transform.Position;
        var toTarget = target.Transform.Position - origin;
        var distance = toTarget.Length();
        var inRange = distance <= weaponRange;

        var stance = attacker.Components.TryGet<StanceComponentState>(out var s) ? s.Stance : UnitStance.Aggressive;
        var autonomy = EffectiveAutonomy(world, attacker, weapon);
        var mayChase = autonomy.AllowsChase || (weapon.AttackTargetIsManual && stance != UnitStance.Ignore);
        var preserveAttackSlot = TargetAllowsAttackSlots(world, target);

        // Movement: chase to a standoff ring if out of range and allowed; stop
        // when in range so ranged units do not pile into the target center.
        if (attacker.Components.TryGet<MovementComponentState>(out var movement))
        {
            var canMove = attacker.Components.TryGet<MovementProfileComponentState>(out var profile);
            var kiteTarget = default(Vector2);
            var shouldKite = canMove
                && mayChase
                && TryPlanKiteMove(world, attacker, weapon, target, distance, targetRadius, weaponRange, out kiteTarget);
            var shouldHoldAnchor = inRange && !shouldKite && (movement.FireAnchorRemaining > 0 || WeaponEngagementState.HasCoolingMount(weapon));
            if (shouldKite)
            {
                attacker.Components.Set(movement with { MoveTarget = kiteTarget, FormationSlot = null });
            }
            else if (shouldHoldAnchor)
            {
                if (movement.MoveTarget is not null || movement.Velocity != Vector2.Zero)
                {
                    attacker.Components.Set(movement with { Velocity = Vector2.Zero, MoveTarget = null });
                }
            }
            else if (canMove
                && mayChase
                && preserveAttackSlot
                && TryGetAttackFormationSlot(movement, target, weaponRange, targetRadius, out var attackSlot)
                && !HasArrivedAtSlot(origin, attackSlot, profile.ArriveRadius))
            {
                attacker.Components.Set(movement with { MoveTarget = attackSlot, FormationSlot = attackSlot });
            }
            else if (!inRange && mayChase && canMove && distance > 0.001f)
            {
                var direction = toTarget / distance;
                var standoff = target.Transform.Position - (direction * standoffRadius);
                attacker.Components.Set(movement with { MoveTarget = standoff, FormationSlot = null });
            }
            else if ((inRange || !canMove) && movement.MoveTarget is not null)
            {
                attacker.Components.Set(movement with { Velocity = Vector2.Zero, MoveTarget = null, FormationSlot = null });
            }
        }

        var desiredFacing = toTarget.Angle();
        var mounts = WeaponEngagementState.WritableMounts(attacker, weapon);
        WeaponEngagementMountLoop.Tick(
            context,
            attacker,
            target,
            mounts,
            desiredFacing,
            dt,
            new WeaponEngagementMountLoopOptions(
                InRange: inRange,
                CenterDistance: distance,
                TargetRadius: targetRadius,
                RespectMinimumRange: true,
                UseStructureTargetDefaults: true,
                RequirePositivePriority: true,
                FireOnlyOneMount: false,
                AnchorMovementOnFire: true,
                DamageVariance: DamageVariance));
    }

    private static bool TryGetAttackFormationSlot(
        MovementComponentState movement,
        EntityInstance target,
        float weaponRange,
        float targetRadius,
        out Vector2 slot)
    {
        slot = default;
        if (weaponRange <= 0 || movement.FormationSlot is not { } candidate)
        {
            return false;
        }

        var distanceToTarget = candidate.DistanceTo(target.Transform.Position);
        if (distanceToTarget < targetRadius + 1f
            || distanceToTarget > targetRadius + weaponRange + 2f)
        {
            return false;
        }

        slot = candidate;
        return true;
    }

    private static bool HasArrivedAtSlot(Vector2 position, Vector2 slot, float arriveRadius)
    {
        var radius = MathF.Max(arriveRadius, 2f);
        return position.DistanceSquaredTo(slot) <= radius * radius;
    }

    private static bool TargetAllowsAttackSlots(EntityWorld world, EntityInstance target)
    {
        return !world.TryGetSpec(target.SpecId, out var spec)
            || spec.Movement?.Domain != MovementDomain.Air;
    }

    private static void CoolMounts(EntityInstance entity, WeaponUserComponentState weapon, float dt)
    {
        WeaponEngagementState.CoolMountsInPlace(entity, weapon, dt);
    }
}
