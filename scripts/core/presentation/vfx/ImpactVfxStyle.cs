using Godot;

namespace ProceduralRts.Core;

public readonly record struct ImpactVfxStyle(
    float Expansion,
    float LineWidth,
    float SparkScale,
    int SparkCount,
    Color SecondaryColor,
    float ShakeAmplitude,
    float ShakeRadius,
    bool EmitsEmbers,
    bool EmitsEmpDissolve);
