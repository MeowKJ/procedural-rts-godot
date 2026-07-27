using Godot;

namespace ProceduralRts.Core;

public readonly record struct UnitInstanceDeathInfo(
    int Id,
    string DesignId,
    PlayerSlotId PlayerSlotId,
    UnitFactionId Faction,
    Vector2 Position,
    float Radius,
    UnitWeightClass WeightClass,
    MovementDomain MovementDomain,
    string? KillingAmmoId,
    float OverkillDamage);
