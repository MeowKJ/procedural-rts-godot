using Godot;

namespace ProceduralRts.Core;

public static class FogOfWarVisualPolicy
{
    public const float DefaultCellSize = 24;
    public const float WorldRedrawIntervalSeconds = 0.12f;
    public const float UnexploredAlpha = 0.97f;
    public const float ExploredMemoryAlpha = 0.54f;
    public const float VisibleAlpha = 0;
    public const float ShaderVisibilityLow = 0.35f;
    public const float ShaderVisibilityHigh = 0.75f;

    public static readonly Color UnexploredOverlay = new("#000000", UnexploredAlpha);
    public static readonly Color ExploredMemoryOverlay = new("#02070d", ExploredMemoryAlpha);

    public static float CellSizeFor(FogQualityTier quality)
    {
        return quality switch
        {
            FogQualityTier.Low => 36,
            FogQualityTier.High => 16,
            _ => DefaultCellSize,
        };
    }

    public static float WorldRedrawIntervalFor(FogQualityTier quality)
    {
        return quality switch
        {
            FogQualityTier.Low => 0.20f,
            FogQualityTier.High => 0.08f,
            _ => WorldRedrawIntervalSeconds,
        };
    }

    public static float CameraScopedUploadWorldStepFor(FogQualityTier quality)
    {
        return CellSizeFor(quality) * 3f;
    }

    public static Vector2I MaskSize(Vector2 worldSize, FogQualityTier quality)
    {
        return MaskSize(worldSize, CellSizeFor(quality));
    }

    public static Vector2I MaskSize(Vector2 worldSize, float cellSize = DefaultCellSize)
    {
        return new Vector2I(
            Math.Max(1, Mathf.CeilToInt(worldSize.X / cellSize)),
            Math.Max(1, Mathf.CeilToInt(worldSize.Y / cellSize)));
    }

    public static Color MaskPixel(float visibleStrength, float exploredStrength)
    {
        visibleStrength = Mathf.Clamp(visibleStrength, 0, 1);
        exploredStrength = Mathf.Clamp(Math.Max(exploredStrength, visibleStrength), 0, 1);
        var memoryAlpha = Mathf.Lerp(UnexploredAlpha, ExploredMemoryAlpha, exploredStrength);
        var alpha = memoryAlpha * (1 - visibleStrength);
        return new Color(visibleStrength, exploredStrength, 0, alpha);
    }
}
