using Godot;

namespace ProceduralRts.Core;

public sealed partial class CombatSystem
{
    private const float KiteSafetyPadding = 28f;
    private const float KiteRangeFraction = 0.86f;

    private static bool TryPlanKiteMove(
        EntityWorld world,
        EntityInstance attacker,
        WeaponUserComponentState weapon,
        EntityInstance target,
        float distance,
        float targetRadius,
        float weaponRange,
        out Vector2 kiteTarget)
    {
        kiteTarget = default;
        var minRange = WeaponMath.MaxMountMinRange(world, weapon);
        if (minRange <= 0
            || weaponRange <= minRange
            || !HasMobileMinRangeWeapon(world, weapon, target)
            || !attacker.Components.TryGet<MovementComponentState>(out var movement))
        {
            return false;
        }

        var edgeDistance = WeaponMath.EffectiveTargetDistance(distance, targetRadius);
        if (edgeDistance >= minRange)
        {
            return false;
        }

        var away = AwayFromTarget(attacker, target, distance);
        var desiredDistance = MathF.Min(weaponRange * KiteRangeFraction, targetRadius + minRange + KiteSafetyPadding);
        var desired = target.Transform.Position + (away * desiredDistance);
        kiteTarget = ClampToWorld(world, desired);
        return movement.MoveTarget is null || movement.MoveTarget.Value.DistanceSquaredTo(kiteTarget) > 4f;
    }

    private static bool HasMobileMinRangeWeapon(
        EntityWorld world,
        WeaponUserComponentState weapon,
        EntityInstance target)
    {
        foreach (var mount in weapon.Mounts)
        {
            if (world.TryGetWeaponDefinition(mount.WeaponId, out var definition)
                && definition.CanFireWhileMoving
                && definition.MinRange > 0
                && WeaponPriority(world, definition, target) > 0)
            {
                return true;
            }
        }

        return false;
    }

    private static Vector2 AwayFromTarget(EntityInstance attacker, EntityInstance target, float distance)
    {
        if (distance > 0.001f)
        {
            return (attacker.Transform.Position - target.Transform.Position) / distance;
        }

        var facing = attacker.Transform.Facing + MathF.PI;
        return new Vector2(MathF.Cos(facing), MathF.Sin(facing));
    }

    private static Vector2 ClampToWorld(EntityWorld world, Vector2 point)
    {
        return new Vector2(
            Math.Clamp(point.X, 0, world.WorldWidth),
            Math.Clamp(point.Y, 0, world.WorldHeight));
    }
}
