using Godot;

namespace ProceduralRts.Core;

/// <summary>
/// Transitional EntityWorld combat for mobile units whose current target is a
/// building/turret entity. Unit-vs-unit combat remains on the legacy path during
/// M1, so this system deliberately ignores non-structure targets.
/// </summary>
public sealed class BuildingTargetCombatSystem : ISimSystem
{
    private const float StandoffFraction = 0.85f;

    public void Step(SimContext context)
    {
        var world = context.World;
        var dt = context.FixedDelta;

        foreach (var attacker in world.OrderedEntities)
        {
            if (!world.TryGetSpec(attacker.SpecId, out var attackerSpec)
                || attackerSpec.Kind != EntityKind.Unit
                || !attacker.Components.TryGet<WeaponUserComponentState>(out var weapon)
                || weapon.AttackTargetKind != CombatTargetKind.Building)
            {
                continue;
            }

            if (!TryResolveTarget(world, attacker, weapon, out var target))
            {
                ClearTarget(attacker, weapon, dt);
                continue;
            }

            Engage(context, attacker, weapon, target, dt);
        }
    }

    private static bool TryResolveTarget(
        EntityWorld world,
        EntityInstance attacker,
        WeaponUserComponentState weapon,
        out EntityInstance target)
    {
        target = null!;
        if (!weapon.AttackTarget.IsValid
            || !world.TryGet(weapon.AttackTarget, out var candidate)
            || !world.TryGetSpec(candidate.SpecId, out var spec)
            || spec.Kind is not (EntityKind.Building or EntityKind.Turret)
            || !candidate.Components.TryGet<HealthComponentState>(out var health)
            || health.Hp <= 0
            || !world.Relations.CanAttack(attacker.OwnerId, candidate.OwnerId)
            || !WeaponEngagementQueries.CanAnyMountTarget(world, weapon, candidate))
        {
            return false;
        }

        target = candidate;
        return true;
    }

    private static void Engage(
        SimContext context,
        EntityInstance attacker,
        WeaponUserComponentState weapon,
        EntityInstance target,
        float dt)
    {
        var toTarget = target.Transform.Position - attacker.Transform.Position;
        var distance = toTarget.Length();
        var range = WeaponMath.EffectiveRange(context.World, attacker, weapon)
            + (target.Components.TryGet<CollisionComponentState>(out var targetCollision) ? targetCollision.Radius : 0);
        var inRange = distance <= range;

        if (attacker.Components.TryGet<MovementComponentState>(out var movement))
        {
            if (!inRange && distance > 0.001f)
            {
                var direction = toTarget / distance;
                var standoff = target.Transform.Position - direction * MathF.Max(range * StandoffFraction, 1);
                attacker.Components.Set(movement with { MoveTarget = standoff });
            }
            else if (inRange && movement.MoveTarget is not null)
            {
                attacker.Components.Set(movement with { Velocity = Vector2.Zero, MoveTarget = null });
            }
        }

        var desiredFacing = toTarget.Angle();
        var mounts = WeaponEngagementState.WritableMounts(attacker, weapon);
        WeaponSystem.Tick(
            context,
            attacker,
            target,
            mounts,
            desiredFacing,
            dt,
            new WeaponSystemOptions(
                InRange: inRange,
                CenterDistance: distance,
                TargetRadius: target.Components.TryGet<CollisionComponentState>(out var firingTargetCollision) ? firingTargetCollision.Radius : 0,
                RespectMinimumRange: false,
                UseStructureTargetDefaults: false,
                RequirePositivePriority: false,
                FireOnlyOneMount: false,
                AnchorMovementOnFire: true,
                DamageVariance: 0));

        attacker.Components.Set(weapon with
        {
            Mounts = WeaponEngagementState.ReadOnlyMounts(mounts),
            AttackTarget = target.Id,
            AttackTargetKind = CombatTargetKind.Building,
        });
    }

    private static void ClearTarget(EntityInstance attacker, WeaponUserComponentState weapon, float dt)
    {
        attacker.Components.Set(weapon with
        {
            Mounts = WeaponEngagementState.CoolMountsCopy(weapon.Mounts, dt),
            AttackTarget = EntityId.None,
            AttackTargetKind = CombatTargetKind.Unit,
            AttackTargetIsManual = false,
        });
    }

}
