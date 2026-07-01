using Godot;

namespace ProceduralRts.Core;

public sealed record FactionPresentationDescriptor(
    FactionId FactionId,
    string NameKey,
    string ShortCode,
    IconGlyph Glyph,
    Color Accent,
    Color HudColor,
    Color MinimapBaseColor
);
