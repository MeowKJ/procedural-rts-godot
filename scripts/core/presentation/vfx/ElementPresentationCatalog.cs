using Godot;

namespace ProceduralRts.Core;

public static partial class ElementPresentationCatalog
{
    private static readonly SortedDictionary<string, ElementPresentationStyle> DefinitionsByElement = CreateElementStyles();

    public static IReadOnlyDictionary<string, ElementPresentationStyle> Definitions => DefinitionsByElement;

    public static ElementPresentationStyle For(string damageElementId)
    {
        return DefinitionsByElement.TryGetValue(damageElementId, out var style)
            ? style
            : throw new InvalidOperationException($"Unknown element presentation style '{damageElementId}'.");
    }

    public static bool TryFor(string? damageElementId, out ElementPresentationStyle style)
    {
        if (!string.IsNullOrWhiteSpace(damageElementId)
            && DefinitionsByElement.TryGetValue(damageElementId, out var found))
        {
            style = found;
            return true;
        }

        style = null!;
        return false;
    }

    public static ElementBadgePresentation BadgeFor(string damageElementId)
    {
        return For(damageElementId).Badge;
    }

    public static string? DamageElementIdFor(string? ammoId)
    {
        return ammoId is { } kind && WeaponCatalog.AmmoDefinitions.TryGetValue(kind, out var ammo)
            ? ammo.DamageElementId
            : null;
    }

    public static Color ProjectileAccentFor(string? damageElementId, Color fallback)
    {
        return TryFor(damageElementId, out var style) ? style.Accent : fallback;
    }

    public static Color BeamAccentFor(string? damageElementId, Color fallback)
    {
        return TryFor(damageElementId, out var style) ? style.BeamColor : fallback;
    }

    public static float BeamWidthMultiplierFor(string? damageElementId)
    {
        return TryFor(damageElementId, out var style) ? style.BeamWidthMultiplier : 1f;
    }

    private static ElementPresentationStyle Style(
        string damageElementId,
        string label,
        string shortCode,
        string accent,
        string projectileTrail,
        string projectileCore,
        string projectileHead,
        ElementProjectileTrailStyle trailStyle,
        ProjectileVfxStyle projectile,
        ElementBeamStyle beamStyle,
        string beam,
        float BeamWidthMultiplier,
        string Impact,
        string Death,
        bool EmitsEmbers = false,
        bool EmitsEmpDissolve = false)
    {
        _ = DamageElementCatalog.For(damageElementId);
        var accentColor = new Color(accent);
        return new ElementPresentationStyle(
            damageElementId,
            label,
            shortCode,
            accentColor,
            new Color(projectileTrail),
            new Color(projectileCore),
            new Color(projectileHead),
            trailStyle,
            projectile,
            beamStyle,
            new Color(beam),
            BeamWidthMultiplier,
            new Color(Impact),
            new Color(Death),
            new ElementBadgePresentation(
                damageElementId,
                label,
                shortCode,
                accentColor,
                accentColor.Darkened(0.58f),
                new Color("#ffffff")),
            EmitsEmbers,
            EmitsEmpDissolve);
    }

    private static ProjectileVfxStyle Projectile(
        float TailLength,
        float TrailWidth,
        float CoreWidth,
        float HeadRadius,
        string Flare,
        float FlareAlpha)
    {
        return new ProjectileVfxStyle(
            TailLength,
            MathF.Max(ProjectileVfxMath.MinimumTrailWidth, TrailWidth),
            MathF.Max(ProjectileVfxMath.MinimumCoreWidth, CoreWidth),
            MathF.Max(ProjectileVfxMath.MinimumHeadRadius, HeadRadius),
            ProjectileVfxMath.MinimumTrailAlpha,
            ProjectileVfxMath.MinimumCoreAlpha,
            ProjectileVfxMath.MinimumHeadAlpha,
            ProjectileVfxMath.CullingPadding,
            new Color(Flare, FlareAlpha),
            ProjectileVfxMath.MinimumVisibleSeconds);
    }
}
