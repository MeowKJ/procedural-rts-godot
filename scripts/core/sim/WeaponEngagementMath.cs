using Godot;

namespace ProceduralRts.Core;

static class WeaponEngagementMath
{
    public const float FacingTolerance = 0.12f;

    private static readonly float InstantMountTurnRate = MathF.Tau * 4;

    public static float TickCooldown(float cooldownRemaining, float dt)
    {
        return MathF.Max(0, cooldownRemaining - dt);
    }

    public static float RotateToward(float current, float desired, float maxStep)
    {
        var diff = Mathf.AngleDifference(current, desired);
        if (MathF.Abs(diff) <= maxStep)
        {
            return desired;
        }

        return current + (MathF.Sign(diff) * maxStep);
    }

    public static bool IsAimed(float facing, float desired)
    {
        return MathF.Abs(Mathf.AngleDifference(facing, desired)) <= FacingTolerance;
    }

    public static float MountTurnRate(EntityWorld world, EntityInstance attacker, WeaponMountRuntimeState mount)
    {
        if (world.TryGetSpec(attacker.SpecId, out var spec))
        {
            foreach (var mountSpec in spec.Weapons)
            {
                if (mountSpec.MountId == mount.MountId)
                {
                    return mountSpec.FacingMode == WeaponMountFacingMode.Independent && mountSpec.TurnRate > 0
                        ? mountSpec.TurnRate
                        : InstantMountTurnRate;
                }
            }
        }

        return InstantMountTurnRate;
    }
}
