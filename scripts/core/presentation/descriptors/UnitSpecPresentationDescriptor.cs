using Godot;

namespace ProceduralRts.Core;

public sealed record UnitSpecPresentationDescriptor(
    string SpecId,
    string NameKey,
    string RoleKey,
    string ShortCode,
    IconGlyph Icon,
    string PortraitMode,
    Color Accent,
    UnitArtRecipe Art,
    IconGlyph RoleGlyph = IconGlyph.None
);
