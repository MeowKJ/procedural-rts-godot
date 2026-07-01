using Godot;

namespace ProceduralRts.Core;

public readonly record struct BuildingEntitySeed(
    int Id,
    string Kind,
    PlayerSlotId PlayerSlotId,
    UnitFactionId Faction,
    Vector2 Position,
    float Facing,
    float Hp);
