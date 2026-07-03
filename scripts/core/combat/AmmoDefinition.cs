using Godot;

namespace ProceduralRts.Core;

public sealed record AmmoDefinition
{
    public string Id { get; init; }
    public string Label { get; init; }
    public ProjectileBehavior Behavior { get; init; }
    public HitRule HitRule { get; init; }
    public float Speed { get; init; }
    public float BaseDamage { get; init; }
    public float BeamDuration { get; init; }
    public float BeamWidth { get; init; }
    public float AccuracyRadiusMultiplier { get; init; }
    public float TrackingStrength { get; init; }
    public float SplashRadius { get; init; }
    public float SplashMinDamageRatio { get; init; }
    public Color Accent { get; init; }
    public DamageProfile DamageProfile { get; init; }
    public SpecialAttackHook Hooks { get; init; }
    public AmmoKind? LegacyKind { get; init; }

    public AmmoKind Kind => LegacyKind
        ?? throw new InvalidOperationException($"Ammo '{Id}' has no legacy AmmoKind enum alias.");

    public AmmoDefinition(
        AmmoKind Kind,
        string Label,
        ProjectileBehavior Behavior,
        HitRule HitRule,
        float Speed,
        float BaseDamage,
        float BeamDuration,
        float BeamWidth,
        float AccuracyRadiusMultiplier,
        float TrackingStrength,
        Color Accent,
        DamageProfile DamageProfile,
        SpecialAttackHook Hooks,
        float SplashRadius = 0,
        float SplashMinDamageRatio = 0)
        : this(
            WeaponCatalog.IdFor(Kind),
            Label,
            Behavior,
            HitRule,
            Speed,
            BaseDamage,
            BeamDuration,
            BeamWidth,
            AccuracyRadiusMultiplier,
            TrackingStrength,
            Accent,
            DamageProfile,
            Hooks,
            SplashRadius,
            SplashMinDamageRatio,
            Kind)
    {
    }

    public AmmoDefinition(
        string Id,
        string Label,
        ProjectileBehavior Behavior,
        HitRule HitRule,
        float Speed,
        float BaseDamage,
        float BeamDuration,
        float BeamWidth,
        float AccuracyRadiusMultiplier,
        float TrackingStrength,
        Color Accent,
        DamageProfile DamageProfile,
        SpecialAttackHook Hooks,
        float SplashRadius = 0,
        float SplashMinDamageRatio = 0,
        AmmoKind? LegacyKind = null)
    {
        this.Id = Id;
        this.Label = Label;
        this.Behavior = Behavior;
        this.HitRule = HitRule;
        this.Speed = Speed;
        this.BaseDamage = BaseDamage;
        this.BeamDuration = BeamDuration;
        this.BeamWidth = BeamWidth;
        this.AccuracyRadiusMultiplier = AccuracyRadiusMultiplier;
        this.TrackingStrength = TrackingStrength;
        this.SplashRadius = SplashRadius;
        this.SplashMinDamageRatio = SplashMinDamageRatio;
        this.Accent = Accent;
        this.DamageProfile = DamageProfile;
        this.Hooks = Hooks;
        this.LegacyKind = LegacyKind;
    }
}
