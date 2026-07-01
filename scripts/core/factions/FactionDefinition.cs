using Godot;

namespace ProceduralRts.Core;

public sealed record FactionDefinition(
    FactionId Id,
    string DisplayNameKey,
    string ShortCode,
    IconGlyph Glyph,
    Color Accent,
    Color HudColor);
