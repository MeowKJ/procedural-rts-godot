using Godot;

namespace ProceduralRts.Core;

public readonly record struct UnitBattlefieldBuildingDeathInfo(
    int Id,
    string Kind,
    PlayerSlotId PlayerSlotId,
    UnitFactionId Faction,
    Vector2 Position,
    Vector2 Footprint);
