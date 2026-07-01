namespace ProceduralRts.Core;

public sealed record WeaponMountRuntimeState(
    string MountId,
    string WeaponId,
    float Facing,
    float CooldownRemaining,
    WeaponKind? LegacyWeaponKind = null)
{
    public WeaponMountRuntimeState(string MountId, WeaponKind WeaponKind, float Facing, float CooldownRemaining)
        : this(MountId, WeaponCatalog.IdFor(WeaponKind), Facing, CooldownRemaining, WeaponKind)
    {
    }

    public WeaponKind WeaponKind => LegacyWeaponKind
        ?? WeaponCatalog.LegacyKindForWeapon(WeaponId)
        ?? throw new InvalidOperationException($"Weapon mount '{MountId}' uses non-legacy weapon '{WeaponId}'.");
}
