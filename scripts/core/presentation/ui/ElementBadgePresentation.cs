using Godot;

namespace ProceduralRts.Core;

public readonly record struct ElementBadgePresentation(
    string DamageElementId,
    string Label,
    string ShortCode,
    Color Accent,
    Color Background,
    Color Text);
