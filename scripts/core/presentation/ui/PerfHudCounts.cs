namespace ProceduralRts.Core;

public readonly record struct PerfHudCounts(
    int LiveEntityCount,
    int LiveUnitCount,
    int VisibleUnitCount,
    int ProjectileCount,
    int EffectCount,
    int FogTextureUploads,
    double LastFogUpdateMs);
