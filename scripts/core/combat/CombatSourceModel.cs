using Godot;

namespace ProceduralRts.Core;

public readonly record struct CombatSourceModel(
    CombatSourceKind Kind,
    int Id,
    Owner Owner,
    FactionId FactionId,
    Vector2 Position,
    float BodyFacing,
    float WeaponFacing,
    float Radius,
    float TurnRate,
    WeaponKind WeaponKind,
    Color Accent
);
