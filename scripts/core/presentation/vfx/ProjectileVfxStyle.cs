using Godot;

namespace ProceduralRts.Core;

public readonly record struct ProjectileVfxStyle(
    float TailLength,
    float TrailWidth,
    float CoreWidth,
    float HeadRadius,
    float TrailAlpha,
    float CoreAlpha,
    float HeadAlpha,
    float CullingPadding,
    Color TailFlare,
    float MinimumVisibleSeconds);
