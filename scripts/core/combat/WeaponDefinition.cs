namespace ProceduralRts.Core;

public sealed record WeaponDefinition
{
    public string Id { get; init; }
    public string Label { get; init; }
    public string AmmoId { get; init; }
    public WeaponMountKind MountKind { get; init; }
    public float Range { get; init; }
    public float Cooldown { get; init; }
    public float FireArcRadians { get; init; }
    public bool CanFireWhileMoving { get; init; }
    public WeaponTargetProfile TargetProfile { get; init; }
    public SpecialAttackHook Hooks { get; init; }
    public float MinRange { get; init; }
    public bool CanInterceptProjectiles { get; init; }
    public WeaponKind? LegacyKind { get; init; }
    public AmmoKind? LegacyAmmoKind { get; init; }

    public WeaponKind Kind => LegacyKind
        ?? throw new InvalidOperationException($"Weapon '{Id}' has no legacy WeaponKind enum alias.");

    public AmmoKind AmmoKind => LegacyAmmoKind
        ?? throw new InvalidOperationException($"Weapon '{Id}' has no legacy AmmoKind enum alias.");

    public WeaponDefinition(
        WeaponKind Kind,
        string Label,
        AmmoKind AmmoKind,
        WeaponMountKind MountKind,
        float Range,
        float Cooldown,
        float FireArcRadians,
        bool CanFireWhileMoving,
        WeaponTargetProfile TargetProfile,
        SpecialAttackHook Hooks,
        float MinRange = 0,
        bool CanInterceptProjectiles = false)
        : this(
            WeaponCatalog.IdFor(Kind),
            Label,
            WeaponCatalog.IdFor(AmmoKind),
            MountKind,
            Range,
            Cooldown,
            FireArcRadians,
            CanFireWhileMoving,
            TargetProfile,
            Hooks,
            MinRange,
            CanInterceptProjectiles,
            Kind,
            AmmoKind)
    {
    }

    public WeaponDefinition(
        string Id,
        string Label,
        string AmmoId,
        WeaponMountKind MountKind,
        float Range,
        float Cooldown,
        float FireArcRadians,
        bool CanFireWhileMoving,
        WeaponTargetProfile TargetProfile,
        SpecialAttackHook Hooks,
        float MinRange = 0,
        bool CanInterceptProjectiles = false,
        WeaponKind? LegacyKind = null,
        AmmoKind? LegacyAmmoKind = null)
    {
        this.Id = Id;
        this.Label = Label;
        this.AmmoId = AmmoId;
        this.MountKind = MountKind;
        this.Range = Range;
        this.Cooldown = Cooldown;
        this.FireArcRadians = FireArcRadians;
        this.CanFireWhileMoving = CanFireWhileMoving;
        this.TargetProfile = TargetProfile;
        this.Hooks = Hooks;
        this.MinRange = MinRange;
        this.CanInterceptProjectiles = CanInterceptProjectiles;
        this.LegacyKind = LegacyKind;
        this.LegacyAmmoKind = LegacyAmmoKind;
    }
}
