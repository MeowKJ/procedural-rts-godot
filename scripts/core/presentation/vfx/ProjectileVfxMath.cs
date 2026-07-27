using Godot;

namespace ProceduralRts.Core;

public static class ProjectileVfxMath
{
    public const float MinimumTrailWidth = 3.6f;
    public const float MinimumCoreWidth = 1.8f;
    public const float MinimumHeadRadius = 3.4f;
    public const float MinimumTrailAlpha = 0.48f;
    public const float MinimumCoreAlpha = 0.82f;
    public const float MinimumHeadAlpha = 0.96f;
    public const float CullingPadding = 50f;
    public const float MinimumVisibleSeconds = 0.10f;

    public static ProjectileVfxStyle StyleFor(AmmoDefinition ammo)
    {
        return StyleFor(ammo.Id, ammo.DamageElementId) with
        {
            MinimumVisibleSeconds = MinimumVisibleSecondsFor(ammo.Behavior),
        };
    }

    public static ProjectileVfxStyle StyleFor(string? ammoId)
    {
        return StyleFor(ammoId, ElementPresentationCatalog.DamageElementIdFor(ammoId));
    }

    public static ProjectileVfxStyle StyleFor(string? ammoId, string? damageElementId)
    {
        var style = ElementPresentationCatalog.TryFor(damageElementId, out var element)
            ? element.Projectile
            : StyleForKind(ammoId);
        return style with
        {
            MinimumVisibleSeconds = MinimumVisibleSecondsFor(ammoId),
        };
    }

    private static float MinimumVisibleSecondsFor(ProjectileBehavior behavior)
    {
        return behavior switch
        {
            ProjectileBehavior.Ballistic => 0.16f,
            ProjectileBehavior.Tracking => 0.12f,
            ProjectileBehavior.Direct => MinimumVisibleSeconds,
            _ => 0,
        };
    }

    private static float MinimumVisibleSecondsFor(string? ammoId)
    {
        return ammoId switch
        {
            AmmoIds.BallisticCannon => 0.16f,
            AmmoIds.SeekerRocket => 0.12f,
            AmmoIds.NeedleDart => MinimumVisibleSeconds,
            _ => MinimumVisibleSeconds,
        };
    }

    private static ProjectileVfxStyle StyleForKind(string? ammoId)
    {
        return ammoId switch
        {
            AmmoIds.NeedleDart => new ProjectileVfxStyle(
                28f,
                MinimumTrailWidth,
                MinimumCoreWidth,
                MinimumHeadRadius,
                MinimumTrailAlpha,
                MinimumCoreAlpha,
                MinimumHeadAlpha,
                CullingPadding,
                new Color("#d8fff7", 0.22f),
                MinimumVisibleSeconds),
            AmmoIds.SeekerRocket => new ProjectileVfxStyle(
                34f,
                7.6f,
                2.6f,
                5.8f,
                0.52f,
                0.86f,
                MinimumHeadAlpha,
                CullingPadding,
                new Color("#ffefad", 0.38f),
                0.12f),
            _ => new ProjectileVfxStyle(
                22f,
                8.4f,
                2.8f,
                4.6f,
                0.50f,
                0.84f,
                MinimumHeadAlpha,
                CullingPadding,
                new Color("#fff0d2", 0.24f),
                MinimumVisibleSeconds),
        };
    }
}
