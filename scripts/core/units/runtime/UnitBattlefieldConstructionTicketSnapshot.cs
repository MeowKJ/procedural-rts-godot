using Godot;

namespace ProceduralRts.Core;

public readonly record struct UnitBattlefieldConstructionTicketSnapshot(
    EntityId EntityId,
    string Kind,
    PlayerSlotId PlayerSlotId,
    Vector2 Position,
    float Progress,
    bool ReadyToPlace,
    int Cost);
