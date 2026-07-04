namespace ProceduralRts.Core;

public sealed record WeaponMountRuntimeState(
    string MountId,
    string WeaponId,
    float Facing,
    float CooldownRemaining,
    WeaponMountPhase Phase = WeaponMountPhase.Acquire,
    float WarmupRemaining = 0,
    float ReloadRemaining = 0,
    WeaponKind? LegacyWeaponKind = null)
{
    public WeaponMountRuntimeState(string MountId, WeaponKind WeaponKind, float Facing, float CooldownRemaining)
        : this(MountId, WeaponCatalog.IdFor(WeaponKind), Facing, CooldownRemaining, WeaponMountPhase.Acquire, 0, 0, WeaponKind)
    {
    }

    public WeaponMountRuntimeState(string MountId, string WeaponId, float Facing, float CooldownRemaining, WeaponKind? LegacyWeaponKind)
        : this(MountId, WeaponId, Facing, CooldownRemaining, WeaponMountPhase.Acquire, 0, 0, LegacyWeaponKind)
    {
    }

    public WeaponKind WeaponKind => LegacyWeaponKind
        ?? WeaponCatalog.LegacyKindForWeapon(WeaponId)
        ?? throw new InvalidOperationException($"Weapon mount '{MountId}' uses non-legacy weapon '{WeaponId}'.");
}
