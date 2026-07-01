using Godot;

namespace ProceduralRts.Core;

public sealed record EntityPresentationDescriptor(
    string? BuildingSpecId,
    FactionId FactionId,
    string NameKey,
    string RoleKey,
    string ShortCode,
    IconGlyph MainGlyph,
    IconGlyph RoleGlyph,
    IconGlyph FactionGlyph,
    string PortraitMode,
    Color RoleAccent,
    Color FactionAccent,
    Color EntityAccent,
    Color OwnershipOverlay,
    Color MinimapPip
);
