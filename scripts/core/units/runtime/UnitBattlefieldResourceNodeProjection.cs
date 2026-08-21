using Godot;

namespace ProceduralRts.Core;

public readonly record struct UnitBattlefieldResourceNodeProjection(
    EntityId EntityId,
    Vector2 Position,
    float Radius,
    int MaxAmount,
    int Amount,
    Color Accent);
