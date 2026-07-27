using Godot;

namespace ProceduralRts.Core;

public sealed record WeaponMountSpec(
    string MountId,
    string WeaponId,
    WeaponMountFacingMode FacingMode,
    Vector2 Anchor,
    Vector2 MuzzleOffset,
    float ArcRadians,
    float TurnRate,
    bool FireWhileMoving)
{
    public static WeaponMountSpec BodyFixed(string mountId, string weaponId, Vector2 anchor, Vector2 muzzleOffset, float arcRadians, bool fireWhileMoving)
    {
        return new WeaponMountSpec(mountId, weaponId, WeaponMountFacingMode.BodyFixed, anchor, muzzleOffset, arcRadians, 0, fireWhileMoving);
    }

    public static WeaponMountSpec Independent(string mountId, string weaponId, Vector2 anchor, Vector2 muzzleOffset, float arcRadians, float turnRate, bool fireWhileMoving)
    {
        return new WeaponMountSpec(mountId, weaponId, WeaponMountFacingMode.Independent, anchor, muzzleOffset, arcRadians, turnRate, fireWhileMoving);
    }

    public static WeaponMountSpec Omni(string mountId, string weaponId, Vector2 anchor, bool fireWhileMoving)
    {
        return new WeaponMountSpec(mountId, weaponId, WeaponMountFacingMode.Omni, anchor, Vector2.Zero, MathF.Tau, 0, fireWhileMoving);
    }
}
