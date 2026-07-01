using Godot;

namespace ProceduralRts.Core;

public readonly record struct ImpactVfxStyle(
    float Expansion,
    float LineWidth,
    float SparkScale,
    int SparkCount,
    Color SecondaryColor,
    bool EmitsEmbers,
    bool EmitsEmpDissolve);
