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

    public static ProjectileVfxStyle StyleFor(AmmoKind? ammoKind)
    {
        return ammoKind switch
        {
            AmmoKind.NeedleDart => new ProjectileVfxStyle(
                28f,
                MinimumTrailWidth,
                MinimumCoreWidth,
                MinimumHeadRadius,
                MinimumTrailAlpha,
                MinimumCoreAlpha,
                MinimumHeadAlpha,
                CullingPadding,
                new Color("#d8fff7", 0.22f)),
            AmmoKind.SeekerRocket => new ProjectileVfxStyle(
                34f,
                7.6f,
                2.6f,
                5.8f,
                0.52f,
                0.86f,
                MinimumHeadAlpha,
                CullingPadding,
                new Color("#ffefad", 0.38f)),
            _ => new ProjectileVfxStyle(
                22f,
                8.4f,
                2.8f,
                4.6f,
                0.50f,
                0.84f,
                MinimumHeadAlpha,
                CullingPadding,
                new Color("#fff0d2", 0.24f)),
        };
    }
}
