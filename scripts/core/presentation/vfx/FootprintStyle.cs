using Godot;

namespace ProceduralRts.Core;

public readonly record struct FootprintStyle(
    FootprintMarkKind MarkKind,
    float Spacing,
    float Lifetime,
    float Width,
    float Length,
    float LateralOffset,
    Color Color);
