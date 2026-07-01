using Godot;

namespace ProceduralRts.Core;

public readonly record struct DeathVfxStyle(
    float Lifetime,
    float BurstScale,
    int FragmentCount,
    int SmokeCount,
    float SmokeScale,
    float RingWidth,
    Color SecondaryColor,
    bool EmitsEmbers,
    bool EmitsEmpDissolve);
