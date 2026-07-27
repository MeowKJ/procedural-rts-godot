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
    bool FireWhileMoving,
    WeaponKind? WeaponKindAlias = null)
{
    public WeaponKind WeaponKind => WeaponKindAlias
        ?? WeaponCatalog.KindForWeaponId(WeaponId)
        ?? throw new InvalidOperationException($"Weapon mount '{MountId}' has no WeaponKind alias for '{WeaponId}'.");

    public static WeaponMountSpec BodyFixed(string mountId, WeaponKind weaponKind, Vector2 anchor, Vector2 muzzleOffset, float arcRadians, bool fireWhileMoving)
    {
        return BodyFixed(mountId, WeaponCatalog.IdFor(weaponKind), anchor, muzzleOffset, arcRadians, fireWhileMoving, weaponKind);
    }

    public static WeaponMountSpec BodyFixed(string mountId, string weaponId, Vector2 anchor, Vector2 muzzleOffset, float arcRadians, bool fireWhileMoving)
    {
        return BodyFixed(mountId, weaponId, anchor, muzzleOffset, arcRadians, fireWhileMoving, null);
    }

    private static WeaponMountSpec BodyFixed(string mountId, string weaponId, Vector2 anchor, Vector2 muzzleOffset, float arcRadians, bool fireWhileMoving, WeaponKind? weaponKindAlias)
    {
        return new WeaponMountSpec(mountId, weaponId, WeaponMountFacingMode.BodyFixed, anchor, muzzleOffset, arcRadians, 0, fireWhileMoving, weaponKindAlias);
    }

    public static WeaponMountSpec Independent(string mountId, WeaponKind weaponKind, Vector2 anchor, Vector2 muzzleOffset, float arcRadians, float turnRate, bool fireWhileMoving)
    {
        return Independent(mountId, WeaponCatalog.IdFor(weaponKind), anchor, muzzleOffset, arcRadians, turnRate, fireWhileMoving, weaponKind);
    }

    public static WeaponMountSpec Independent(string mountId, string weaponId, Vector2 anchor, Vector2 muzzleOffset, float arcRadians, float turnRate, bool fireWhileMoving)
    {
        return Independent(mountId, weaponId, anchor, muzzleOffset, arcRadians, turnRate, fireWhileMoving, null);
    }

    private static WeaponMountSpec Independent(string mountId, string weaponId, Vector2 anchor, Vector2 muzzleOffset, float arcRadians, float turnRate, bool fireWhileMoving, WeaponKind? weaponKindAlias)
    {
        return new WeaponMountSpec(mountId, weaponId, WeaponMountFacingMode.Independent, anchor, muzzleOffset, arcRadians, turnRate, fireWhileMoving, weaponKindAlias);
    }

    public static WeaponMountSpec Omni(string mountId, WeaponKind weaponKind, Vector2 anchor, bool fireWhileMoving)
    {
        return Omni(mountId, WeaponCatalog.IdFor(weaponKind), anchor, fireWhileMoving, weaponKind);
    }

    public static WeaponMountSpec Omni(string mountId, string weaponId, Vector2 anchor, bool fireWhileMoving)
    {
        return Omni(mountId, weaponId, anchor, fireWhileMoving, null);
    }

    private static WeaponMountSpec Omni(string mountId, string weaponId, Vector2 anchor, bool fireWhileMoving, WeaponKind? weaponKindAlias)
    {
        return new WeaponMountSpec(mountId, weaponId, WeaponMountFacingMode.Omni, anchor, Vector2.Zero, MathF.Tau, 0, fireWhileMoving, weaponKindAlias);
    }
}
