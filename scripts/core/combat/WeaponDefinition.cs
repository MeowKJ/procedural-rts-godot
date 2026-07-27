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
    public float Warmup { get; init; }
    public float Reload { get; init; }
    public WeaponKind? KindAlias { get; init; }
    public AmmoKind? AmmoKindAlias { get; init; }

    public WeaponKind Kind => KindAlias
        ?? throw new InvalidOperationException($"Weapon '{Id}' has no WeaponKind alias.");

    public AmmoKind AmmoKind => AmmoKindAlias
        ?? throw new InvalidOperationException($"Weapon '{Id}' has no AmmoKind alias.");

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
        bool CanInterceptProjectiles = false,
        float Warmup = 0,
        float Reload = 0)
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
            Warmup,
            Reload,
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
        float Warmup = 0,
        float Reload = 0,
        WeaponKind? KindAlias = null,
        AmmoKind? AmmoKindAlias = null)
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
        this.Warmup = Warmup;
        this.Reload = Reload;
        this.KindAlias = KindAlias;
        this.AmmoKindAlias = AmmoKindAlias;
    }
}
