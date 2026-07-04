using Godot;

namespace ProceduralRts.Core;

public sealed record DamageElementDefinition(
    string Id,
    string Label,
    Color Accent,
    float DamageMultiplier = 1f);
