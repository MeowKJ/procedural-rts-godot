using Godot;

namespace ProceduralRts.Core;

public sealed record ProductionPresentationDescriptor(
    string TooltipKey,
    string ShortCode,
    IconGlyph Icon,
    Color Accent,
    IconGlyph RoleGlyph,
    ProductionCategory Category,
    string OutputDesignId
);
