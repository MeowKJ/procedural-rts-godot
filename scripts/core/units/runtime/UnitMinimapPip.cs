using Godot;

namespace ProceduralRts.Core;

public readonly record struct UnitMinimapPip(
    Vector2 Position,
    PlayerSlotId PlayerSlotId,
    UnitFactionId Faction,
    PlayerRelation Relation,
    bool Selected,
    float AlertPulse,
    bool IsVisible);
