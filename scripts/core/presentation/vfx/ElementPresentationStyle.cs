using Godot;

namespace ProceduralRts.Core;

public enum ElementProjectileTrailStyle
{
    Needle,
    Burst,
    Heat,
    Surge,
    Veil,
    Decay,
    Pulse
}

public enum ElementBeamStyle
{
    None,
    Focused,
    Burning,
    Surging,
    Veiled,
    Decaying,
    Harmonic
}

public sealed record ElementPresentationStyle(
    string DamageElementId,
    string Label,
    string ShortCode,
    Color Accent,
    Color ProjectileTrail,
    Color ProjectileCore,
    Color ProjectileHead,
    ElementProjectileTrailStyle TrailStyle,
    ProjectileVfxStyle Projectile,
    ElementBeamStyle BeamStyle,
    Color BeamColor,
    float BeamWidthMultiplier,
    Color ImpactColor,
    Color DeathColor,
    ElementBadgePresentation Badge,
    bool EmitsEmbers = false,
    bool EmitsEmpDissolve = false);
